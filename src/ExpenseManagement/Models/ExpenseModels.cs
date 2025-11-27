namespace ExpenseManagement.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int StatusId { get; set; }
    public int AmountMinor { get; set; } // Amount in pence
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public string? ReceiptFile { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation/Display properties
    public string? UserName { get; set; }
    public string? CategoryName { get; set; }
    public string? StatusName { get; set; }
    public string? ReviewerName { get; set; }

    // Computed property for amount in pounds
    public decimal AmountPounds => AmountMinor / 100.0m;

    // Display formatted amount
    public string FormattedAmount => $"£{AmountPounds:F2}";
}

public class ExpenseCategory
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class ExpenseStatus
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

public class User
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string? RoleName { get; set; }
}

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// DTOs for API requests
public class CreateExpenseRequest
{
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public int UserId { get; set; } = 1; // Default to first user for demo
}

public class UpdateExpenseStatusRequest
{
    public int ExpenseId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ReviewerId { get; set; }
}

// Response wrapper for API error handling
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? ErrorDetails { get; set; }
}
