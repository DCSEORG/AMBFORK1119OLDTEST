using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using OpenAI.Chat;
using ExpenseManagement.Models;
using System.Text.Json;

namespace ExpenseManagement.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage, List<ChatMessageContent>? history = null);
    bool IsConfigured { get; }
}

public class ChatMessageContent
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly IExpenseService _expenseService;
    private readonly ChatClient? _chatClient;
    private readonly bool _isConfigured;

    public bool IsConfigured => _isConfigured;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, IExpenseService expenseService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;

        var endpoint = configuration["OpenAI:Endpoint"];
        var deploymentName = configuration["OpenAI:DeploymentName"];

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
        {
            _logger.LogWarning("Azure OpenAI not configured. Chat will return placeholder responses.");
            _isConfigured = false;
            return;
        }

        try
        {
            var managedIdentityClientId = configuration["ManagedIdentityClientId"];
            TokenCredential credential;

            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
            _chatClient = azureClient.GetChatClient(deploymentName);
            _isConfigured = true;
            _logger.LogInformation("Azure OpenAI configured successfully with endpoint: {Endpoint}", endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure OpenAI client");
            _isConfigured = false;
        }
    }

    public async Task<string> GetChatResponseAsync(string userMessage, List<ChatMessageContent>? history = null)
    {
        if (!_isConfigured || _chatClient == null)
        {
            return GetPlaceholderResponse(userMessage);
        }

        try
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt())
            };

            // Add history if provided
            if (history != null)
            {
                foreach (var msg in history)
                {
                    if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                        messages.Add(new UserChatMessage(msg.Content));
                    else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                        messages.Add(new AssistantChatMessage(msg.Content));
                }
            }

            messages.Add(new UserChatMessage(userMessage));

            var options = new ChatCompletionOptions
            {
                Tools = { GetFunctionTools() }
            };

            // Function calling loop
            var maxIterations = 5;
            var iteration = 0;

            while (iteration < maxIterations)
            {
                iteration++;
                var completion = await _chatClient.CompleteChatAsync(messages, options);
                var response = completion.Value;

                if (response.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Process tool calls
                    var assistantMessage = new AssistantChatMessage(response);
                    messages.Add(assistantMessage);

                    foreach (var toolCall in response.ToolCalls)
                    {
                        var functionResult = await ExecuteFunctionAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                        messages.Add(new ToolChatMessage(toolCall.Id, functionResult));
                    }
                }
                else
                {
                    // Return final response
                    return response.Content[0].Text;
                }
            }

            return "I apologize, but I was unable to complete your request. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response");
            return $"I encountered an error: {ex.Message}. Please try again later.";
        }
    }

    private static ChatTool GetFunctionTools()
    {
        return ChatTool.CreateFunctionTool(
            "get_expenses",
            "Retrieves expenses from the database with optional filtering",
            BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                    "filter": {
                        "type": "string",
                        "description": "Optional text filter for expense description or category"
                    },
                    "status": {
                        "type": "string",
                        "description": "Optional status filter: Draft, Submitted, Approved, or Rejected"
                    }
                }
            }
            """)
        );
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            _logger.LogInformation("Executing function: {FunctionName} with args: {Arguments}", functionName, arguments);

            switch (functionName)
            {
                case "get_expenses":
                    var args = JsonSerializer.Deserialize<GetExpensesArgs>(arguments);
                    var expenses = await _expenseService.GetExpensesAsync(args?.filter, args?.status);
                    return JsonSerializer.Serialize(expenses.Select(e => new
                    {
                        e.ExpenseId,
                        e.Description,
                        e.FormattedAmount,
                        e.CategoryName,
                        e.StatusName,
                        Date = e.ExpenseDate.ToString("dd/MM/yyyy"),
                        e.UserName
                    }));

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private record GetExpensesArgs(string? filter, string? status);

    private static string GetSystemPrompt()
    {
        return """
            You are a helpful assistant for the Expense Management System. You can help users:
            - View their expenses
            - Understand expense statuses (Draft, Submitted, Approved, Rejected)
            - Filter expenses by category or status
            - Explain how the expense approval process works

            When listing expenses, format them nicely with:
            - Description
            - Amount in GBP (£)
            - Category
            - Status
            - Date

            Use the get_expenses function to retrieve real data from the database.
            Always be helpful and provide clear, formatted responses.
            When showing lists, use bullet points or numbered lists for clarity.
            """;
    }

    private static string GetPlaceholderResponse(string userMessage)
    {
        var lowerMessage = userMessage.ToLower();

        if (lowerMessage.Contains("expense") || lowerMessage.Contains("list") || lowerMessage.Contains("show"))
        {
            return """
                **Demo Mode - GenAI Services Not Deployed**
                
                To enable AI-powered chat functionality, please run the `deploy-with-chat.sh` script which will:
                1. Deploy Azure OpenAI resources
                2. Deploy Azure AI Search
                3. Configure the application with the necessary endpoints
                
                Once deployed, you'll be able to:
                - Ask questions about your expenses in natural language
                - Get summaries and insights
                - Filter and search expenses using conversational queries
                
                For now, please use the Expenses and Approve pages to manage expenses directly.
                """;
        }

        return """
            **Welcome to the Expense Management Chat!**
            
            I'm currently running in demo mode because GenAI services haven't been deployed yet.
            
            To enable full AI capabilities:
            1. Run `deploy-with-chat.sh` to deploy Azure OpenAI and AI Search
            2. The chat will automatically connect to the AI services
            
            In the meantime, you can:
            - Navigate to the **Expenses** page to view all expenses
            - Use the **Add Expense** page to create new expenses
            - Go to **Approve** to review pending expenses
            """;
    }
}
