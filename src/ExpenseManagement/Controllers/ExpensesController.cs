using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(IExpenseService expenseService, ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses with optional filtering
    /// </summary>
    /// <param name="filter">Text filter for description/category</param>
    /// <param name="status">Filter by status name</param>
    /// <returns>List of expenses</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<Expense>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<Expense>>>> GetExpenses([FromQuery] string? filter = null, [FromQuery] string? status = null)
    {
        try
        {
            var expenses = await _expenseService.GetExpensesAsync(filter, status);
            return Ok(new ApiResponse<List<Expense>> { Success = true, Data = expenses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            return Ok(new ApiResponse<List<Expense>>
            {
                Success = false,
                Data = DummyDataService.GetDummyExpenses(),
                Error = "Database connection failed - showing demo data",
                ErrorDetails = GetErrorDetails(ex)
            });
        }
    }

    /// <summary>
    /// Get pending expenses for approval
    /// </summary>
    /// <param name="filter">Text filter</param>
    /// <returns>List of pending expenses</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(ApiResponse<List<Expense>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<Expense>>>> GetPendingExpenses([FromQuery] string? filter = null)
    {
        try
        {
            var expenses = await _expenseService.GetPendingExpensesAsync(filter);
            return Ok(new ApiResponse<List<Expense>> { Success = true, Data = expenses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses");
            return Ok(new ApiResponse<List<Expense>>
            {
                Success = false,
                Data = DummyDataService.GetDummyPendingExpenses(),
                Error = "Database connection failed - showing demo data",
                ErrorDetails = GetErrorDetails(ex)
            });
        }
    }

    /// <summary>
    /// Get expense by ID
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <returns>Expense details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Expense>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<Expense>>> GetExpenseById(int id)
    {
        try
        {
            var expense = await _expenseService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound(new ApiResponse<Expense> { Success = false, Error = "Expense not found" });
            }
            return Ok(new ApiResponse<Expense> { Success = true, Data = expense });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {Id}", id);
            return Ok(new ApiResponse<Expense>
            {
                Success = false,
                Error = "Database connection failed",
                ErrorDetails = GetErrorDetails(ex)
            });
        }
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    /// <param name="request">Expense details</param>
    /// <returns>Created expense</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Expense>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Expense>>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new ApiResponse<Expense> { Success = false, Error = "Amount must be greater than 0" });
            }

            var expense = await _expenseService.CreateExpenseAsync(request);
            return CreatedAtAction(nameof(GetExpenseById), new { id = expense.ExpenseId },
                new ApiResponse<Expense> { Success = true, Data = expense });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return Ok(new ApiResponse<Expense>
            {
                Success = false,
                Error = "Database connection failed - expense not created",
                ErrorDetails = GetErrorDetails(ex)
            });
        }
    }

    /// <summary>
    /// Update expense status (submit, approve, reject)
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Success indicator</returns>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateExpenseStatus(int id, [FromBody] UpdateExpenseStatusRequest request)
    {
        try
        {
            var validStatuses = new[] { "Draft", "Submitted", "Approved", "Rejected" };
            if (!validStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Error = $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}"
                });
            }

            var result = await _expenseService.UpdateExpenseStatusAsync(id, request.Status, request.ReviewerId);
            return Ok(new ApiResponse<bool> { Success = result, Data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense status for {Id}", id);
            return Ok(new ApiResponse<bool>
            {
                Success = false,
                Error = "Database connection failed - status not updated",
                ErrorDetails = GetErrorDetails(ex)
            });
        }
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    /// <returns>List of categories</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<List<ExpenseCategory>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ExpenseCategory>>>> GetCategories()
    {
        try
        {
            var categories = await _expenseService.GetCategoriesAsync();
            return Ok(new ApiResponse<List<ExpenseCategory>> { Success = true, Data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return Ok(new ApiResponse<List<ExpenseCategory>>
            {
                Success = false,
                Data = DummyDataService.GetDummyCategories(),
                Error = "Database connection failed - showing demo data",
                ErrorDetails = GetErrorDetails(ex)
            });
        }
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    /// <returns>List of statuses</returns>
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(ApiResponse<List<ExpenseStatus>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ExpenseStatus>>>> GetStatuses()
    {
        try
        {
            var statuses = await _expenseService.GetStatusesAsync();
            return Ok(new ApiResponse<List<ExpenseStatus>> { Success = true, Data = statuses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statuses");
            return Ok(new ApiResponse<List<ExpenseStatus>>
            {
                Success = false,
                Data = DummyDataService.GetDummyStatuses(),
                Error = "Database connection failed - showing demo data",
                ErrorDetails = GetErrorDetails(ex)
            });
        }
    }

    /// <summary>
    /// Get all users
    /// </summary>
    /// <returns>List of users</returns>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResponse<List<User>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<User>>>> GetUsers()
    {
        try
        {
            var users = await _expenseService.GetUsersAsync();
            return Ok(new ApiResponse<List<User>> { Success = true, Data = users });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return Ok(new ApiResponse<List<User>>
            {
                Success = false,
                Data = DummyDataService.GetDummyUsers(),
                Error = "Database connection failed - showing demo data",
                ErrorDetails = GetErrorDetails(ex)
            });
        }
    }

    private static string GetErrorDetails(Exception ex)
    {
        var details = $"Error Type: {ex.GetType().Name}\n";
        details += $"Message: {ex.Message}\n";

        if (ex.Message.Contains("Managed Identity") || ex.Message.Contains("authentication") ||
            ex.Message.Contains("Login failed") || ex.Message.Contains("token"))
        {
            details += "\nMANAGED IDENTITY FIX:\n";
            details += "1. Ensure the managed identity is assigned to the App Service\n";
            details += "2. Run the run-sql-dbrole.py script to grant database permissions\n";
            details += "3. Verify AZURE_CLIENT_ID app setting matches the managed identity client ID\n";
            details += "4. If running locally, use 'az login' and change connection string to use 'Authentication=Active Directory Default'\n";
        }

        if (ex.StackTrace != null)
        {
            var stackLines = ex.StackTrace.Split('\n');
            var relevantLine = stackLines.FirstOrDefault(l => l.Contains("ExpenseManagement"));
            if (!string.IsNullOrEmpty(relevantLine))
            {
                details += $"\nLocation: {relevantLine.Trim()}";
            }
        }

        return details;
    }
}
