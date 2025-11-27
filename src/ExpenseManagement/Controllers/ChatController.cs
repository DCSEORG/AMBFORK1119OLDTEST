using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Send a chat message and get a response
    /// </summary>
    /// <param name="request">Chat request with message and optional history</param>
    /// <returns>Chat response</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Chat request received: {Message}", request.Message);

            var history = request.History?.Select(h => new ChatMessageContent
            {
                Role = h.Role,
                Content = h.Content
            }).ToList();

            var response = await _chatService.GetChatResponseAsync(request.Message, history);

            return Ok(new ChatResponse
            {
                Success = true,
                Message = response,
                IsConfigured = _chatService.IsConfigured
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return Ok(new ChatResponse
            {
                Success = false,
                Message = $"An error occurred: {ex.Message}",
                IsConfigured = _chatService.IsConfigured
            });
        }
    }

    /// <summary>
    /// Check if chat service is configured
    /// </summary>
    /// <returns>Configuration status</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ChatStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<ChatStatusResponse> GetStatus()
    {
        return Ok(new ChatStatusResponse
        {
            IsConfigured = _chatService.IsConfigured,
            Message = _chatService.IsConfigured
                ? "Azure OpenAI is configured and ready"
                : "Azure OpenAI is not configured. Run deploy-with-chat.sh to enable AI features."
        });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryItem>? History { get; set; }
}

public class ChatHistoryItem
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ChatResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
}

public class ChatStatusResponse
{
    public bool IsConfigured { get; set; }
    public string Message { get; set; } = string.Empty;
}
