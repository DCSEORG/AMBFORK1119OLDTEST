using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

/// <summary>
/// Provides dummy data when database connection fails - for graceful degradation
/// </summary>
public class DummyDataService
{
    public static List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                CategoryId = 1,
                StatusId = 2,
                AmountMinor = 12000,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 1, 15),
                Description = "Train tickets to London (DEMO DATA)",
                UserName = "Alice Example",
                CategoryName = "Travel",
                StatusName = "Submitted",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                CategoryId = 2,
                StatusId = 2,
                AmountMinor = 6900,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 1, 10),
                Description = "Team lunch meeting (DEMO DATA)",
                UserName = "Alice Example",
                CategoryName = "Meals",
                StatusName = "Submitted",
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 1,
                CategoryId = 3,
                StatusId = 3,
                AmountMinor = 9950,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 12, 4),
                Description = "Office supplies - printer paper (DEMO DATA)",
                UserName = "Alice Example",
                CategoryName = "Supplies",
                StatusName = "Approved",
                CreatedAt = DateTime.UtcNow.AddDays(-50)
            },
            new Expense
            {
                ExpenseId = 4,
                UserId = 1,
                CategoryId = 1,
                StatusId = 3,
                AmountMinor = 1920,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 12, 18),
                Description = "Uber to client site (DEMO DATA)",
                UserName = "Alice Example",
                CategoryName = "Travel",
                StatusName = "Approved",
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            }
        };
    }

    public static List<Expense> GetDummyPendingExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                CategoryId = 1,
                StatusId = 2,
                AmountMinor = 12000,
                Currency = "GBP",
                ExpenseDate = new DateTime(2024, 1, 20),
                Description = "Conference travel (DEMO DATA - Pending)",
                UserName = "Alice Example",
                CategoryName = "Travel",
                StatusName = "Submitted",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                CategoryId = 3,
                StatusId = 2,
                AmountMinor = 9950,
                Currency = "GBP",
                ExpenseDate = new DateTime(2023, 12, 14),
                Description = "Office equipment (DEMO DATA - Pending)",
                UserName = "Alice Example",
                CategoryName = "Supplies",
                StatusName = "Submitted",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };
    }

    public static List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new ExpenseCategory { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    public static List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }

    public static List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee" },
            new User { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager" }
        };
    }
}
