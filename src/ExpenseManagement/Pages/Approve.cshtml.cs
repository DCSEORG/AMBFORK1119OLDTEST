using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ApproveModel> _logger;

    public ApproveModel(IExpenseService expenseService, ILogger<ApproveModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public string? Filter { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync(string? filter = null)
    {
        Filter = filter;
        await LoadPendingExpensesAsync();
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId)
    {
        try
        {
            // ReviewerId 2 is Bob Manager from seed data
            await _expenseService.UpdateExpenseStatusAsync(expenseId, "Approved", 2);
            SuccessMessage = "Expense approved successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense {ExpenseId}", expenseId);
            ErrorMessage = $"Unable to approve expense: {ex.Message}";
        }

        await LoadPendingExpensesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId)
    {
        try
        {
            await _expenseService.UpdateExpenseStatusAsync(expenseId, "Rejected", 2);
            SuccessMessage = "Expense rejected.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense {ExpenseId}", expenseId);
            ErrorMessage = $"Unable to reject expense: {ex.Message}";
        }

        await LoadPendingExpensesAsync();
        return Page();
    }

    private async Task LoadPendingExpensesAsync()
    {
        try
        {
            PendingExpenses = await _expenseService.GetPendingExpensesAsync(Filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending expenses");
            ErrorMessage = "Unable to connect to database - showing demo data";
            ErrorDetails = GetErrorDetails(ex);
            PendingExpenses = DummyDataService.GetDummyPendingExpenses();
        }
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
            details += "3. Verify AZURE_CLIENT_ID app setting matches the managed identity client ID";
        }

        return details;
    }
}
