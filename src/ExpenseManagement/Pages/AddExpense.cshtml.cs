using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<AddExpenseModel> _logger;

    public AddExpenseModel(IExpenseService expenseService, ILogger<AddExpenseModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public List<ExpenseCategory> Categories { get; set; } = new();
    
    [BindProperty]
    public decimal Amount { get; set; }
    
    [BindProperty]
    public DateTime? ExpenseDate { get; set; }
    
    [BindProperty]
    public int CategoryId { get; set; }
    
    [BindProperty]
    public string? Description { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        ExpenseDate = DateTime.Today;
        await LoadCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCategoriesAsync();

        if (Amount <= 0)
        {
            ErrorMessage = "Amount must be greater than 0";
            return Page();
        }

        if (!ExpenseDate.HasValue)
        {
            ErrorMessage = "Date is required";
            return Page();
        }

        try
        {
            var request = new CreateExpenseRequest
            {
                Amount = Amount,
                ExpenseDate = ExpenseDate.Value,
                CategoryId = CategoryId,
                Description = Description,
                UserId = 1 // Default user for demo
            };

            await _expenseService.CreateExpenseAsync(request);
            SuccessMessage = "Expense created successfully!";
            
            // Clear form
            Amount = 0;
            ExpenseDate = DateTime.Today;
            Description = null;
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorMessage = $"Unable to create expense: {ex.Message}";
            return Page();
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            Categories = await _expenseService.GetCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading categories");
            Categories = DummyDataService.GetDummyCategories();
        }
    }
}
