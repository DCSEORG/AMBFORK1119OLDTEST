using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public List<ExpenseStatus> Statuses { get; set; } = new();
    public string? Filter { get; set; }
    public string? StatusFilter { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }

    public async Task OnGetAsync(string? filter = null, string? status = null)
    {
        Filter = filter;
        StatusFilter = status;

        try
        {
            Expenses = await _expenseService.GetExpensesAsync(filter, status);
            Statuses = await _expenseService.GetStatusesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expenses");
            ErrorMessage = "Unable to connect to database - showing demo data";
            ErrorDetails = GetErrorDetails(ex);
            Expenses = DummyDataService.GetDummyExpenses();
            Statuses = DummyDataService.GetDummyStatuses();
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(int expenseId)
    {
        try
        {
            await _expenseService.UpdateExpenseStatusAsync(expenseId, "Submitted", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting expense {ExpenseId}", expenseId);
        }

        return RedirectToPage();
    }

    private static string GetErrorDetails(Exception ex)
    {
        var details = $"Error Type: {ex.GetType().Name}\nMessage: {ex.Message}";

        if (ex.Message.Contains("Managed Identity") || ex.Message.Contains("authentication") ||
            ex.Message.Contains("Login failed") || ex.Message.Contains("token"))
        {
            details += "\n\nMANAGED IDENTITY FIX:\n";
            details += "1. Ensure the managed identity is assigned to the App Service\n";
            details += "2. Run the run-sql-dbrole.py script to grant database permissions\n";
            details += "3. Verify AZURE_CLIENT_ID app setting matches the managed identity client ID\n";
            details += "4. If running locally, use 'az login' and change connection string to 'Authentication=Active Directory Default'";
        }

        return details;
    }
}
