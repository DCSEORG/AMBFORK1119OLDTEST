using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(string? filter = null, string? status = null);
    Task<List<Expense>> GetPendingExpensesAsync(string? filter = null);
    Task<Expense?> GetExpenseByIdAsync(int id);
    Task<Expense> CreateExpenseAsync(CreateExpenseRequest request);
    Task<bool> UpdateExpenseStatusAsync(int expenseId, string status, int reviewerId);
    Task<List<ExpenseCategory>> GetCategoriesAsync();
    Task<List<ExpenseStatus>> GetStatusesAsync();
    Task<List<User>> GetUsersAsync();
}

public class ExpenseService : IExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public async Task<List<Expense>> GetExpensesAsync(string? filter = null, string? status = null)
    {
        var expenses = new List<Expense>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("EXEC GetExpenses @Filter, @Status", connection);
        command.Parameters.AddWithValue("@Filter", (object?)filter ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            expenses.Add(MapExpense(reader));
        }

        return expenses;
    }

    public async Task<List<Expense>> GetPendingExpensesAsync(string? filter = null)
    {
        var expenses = new List<Expense>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("EXEC GetPendingExpenses @Filter", connection);
        command.Parameters.AddWithValue("@Filter", (object?)filter ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            expenses.Add(MapExpense(reader));
        }

        return expenses;
    }

    public async Task<Expense?> GetExpenseByIdAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("EXEC GetExpenseById @ExpenseId", connection);
        command.Parameters.AddWithValue("@ExpenseId", id);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapExpense(reader);
        }

        return null;
    }

    public async Task<Expense> CreateExpenseAsync(CreateExpenseRequest request)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var amountMinor = (int)(request.Amount * 100);

        using var command = new SqlCommand("EXEC CreateExpense @UserId, @CategoryId, @AmountMinor, @ExpenseDate, @Description", connection);
        command.Parameters.AddWithValue("@UserId", request.UserId);
        command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
        command.Parameters.AddWithValue("@AmountMinor", amountMinor);
        command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
        command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return MapExpense(reader);
        }

        throw new InvalidOperationException("Failed to create expense");
    }

    public async Task<bool> UpdateExpenseStatusAsync(int expenseId, string status, int reviewerId)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("EXEC UpdateExpenseStatus @ExpenseId, @Status, @ReviewerId", connection);
        command.Parameters.AddWithValue("@ExpenseId", expenseId);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@ReviewerId", reviewerId);

        var result = await command.ExecuteNonQueryAsync();
        return result > 0;
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        var categories = new List<ExpenseCategory>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("EXEC GetCategories", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            categories.Add(new ExpenseCategory
            {
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            });
        }

        return categories;
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        var statuses = new List<ExpenseStatus>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("EXEC GetStatuses", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            statuses.Add(new ExpenseStatus
            {
                StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
            });
        }

        return statuses;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new SqlCommand("EXEC GetUsers", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                RoleName = reader.IsDBNull(reader.GetOrdinal("RoleName")) ? null : reader.GetString(reader.GetOrdinal("RoleName"))
            });
        }

        return users;
    }

    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? null : reader.GetString(reader.GetOrdinal("UserName")),
            CategoryName = reader.IsDBNull(reader.GetOrdinal("CategoryName")) ? null : reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusName = reader.IsDBNull(reader.GetOrdinal("StatusName")) ? null : reader.GetString(reader.GetOrdinal("StatusName")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }
}
