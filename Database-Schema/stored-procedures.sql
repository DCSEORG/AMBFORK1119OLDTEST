/*
  stored-procedures.sql
  Stored procedures for Expense Management System
  All app database operations use these procedures - no direct T-SQL in application code
*/

SET NOCOUNT ON;
GO

-- Get all expenses with optional filtering
CREATE OR ALTER PROCEDURE GetExpenses
    @Filter NVARCHAR(200) = NULL,
    @Status NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        e.CategoryId,
        e.StatusId,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt,
        u.UserName,
        c.CategoryName,
        s.StatusName
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE (@Filter IS NULL OR e.Description LIKE '%' + @Filter + '%' OR c.CategoryName LIKE '%' + @Filter + '%')
      AND (@Status IS NULL OR s.StatusName = @Status)
    ORDER BY e.ExpenseDate DESC, e.CreatedAt DESC;
END
GO

-- Get pending expenses (Submitted status) for approval
CREATE OR ALTER PROCEDURE GetPendingExpenses
    @Filter NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        e.CategoryId,
        e.StatusId,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt,
        u.UserName,
        c.CategoryName,
        s.StatusName
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE s.StatusName = 'Submitted'
      AND (@Filter IS NULL OR e.Description LIKE '%' + @Filter + '%' OR c.CategoryName LIKE '%' + @Filter + '%')
    ORDER BY e.SubmittedAt ASC;
END
GO

-- Get expense by ID
CREATE OR ALTER PROCEDURE GetExpenseById
    @ExpenseId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        e.CategoryId,
        e.StatusId,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt,
        u.UserName,
        c.CategoryName,
        s.StatusName
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE e.ExpenseId = @ExpenseId;
END
GO

-- Create new expense (defaults to Draft status)
CREATE OR ALTER PROCEDURE CreateExpense
    @UserId INT,
    @CategoryId INT,
    @AmountMinor INT,
    @ExpenseDate DATE,
    @Description NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = 'Draft';
    
    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, CreatedAt)
    VALUES (@UserId, @CategoryId, @StatusId, @AmountMinor, 'GBP', @ExpenseDate, @Description, SYSUTCDATETIME());
    
    -- Return the created expense
    DECLARE @NewExpenseId INT = SCOPE_IDENTITY();
    
    SELECT 
        e.ExpenseId,
        e.UserId,
        e.CategoryId,
        e.StatusId,
        e.AmountMinor,
        e.Currency,
        e.ExpenseDate,
        e.Description,
        e.ReceiptFile,
        e.SubmittedAt,
        e.ReviewedBy,
        e.ReviewedAt,
        e.CreatedAt,
        u.UserName,
        c.CategoryName,
        s.StatusName
    FROM dbo.Expenses e
    INNER JOIN dbo.Users u ON e.UserId = u.UserId
    INNER JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
    INNER JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
    WHERE e.ExpenseId = @NewExpenseId;
END
GO

-- Update expense status (Submit, Approve, Reject)
CREATE OR ALTER PROCEDURE UpdateExpenseStatus
    @ExpenseId INT,
    @Status NVARCHAR(50),
    @ReviewerId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StatusId INT;
    SELECT @StatusId = StatusId FROM dbo.ExpenseStatus WHERE StatusName = @Status;
    
    IF @StatusId IS NULL
    BEGIN
        RAISERROR('Invalid status name', 16, 1);
        RETURN;
    END
    
    UPDATE dbo.Expenses
    SET StatusId = @StatusId,
        SubmittedAt = CASE WHEN @Status = 'Submitted' THEN SYSUTCDATETIME() ELSE SubmittedAt END,
        ReviewedBy = CASE WHEN @Status IN ('Approved', 'Rejected') THEN @ReviewerId ELSE ReviewedBy END,
        ReviewedAt = CASE WHEN @Status IN ('Approved', 'Rejected') THEN SYSUTCDATETIME() ELSE ReviewedAt END
    WHERE ExpenseId = @ExpenseId;
END
GO

-- Get all categories
CREATE OR ALTER PROCEDURE GetCategories
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT CategoryId, CategoryName, IsActive
    FROM dbo.ExpenseCategories
    WHERE IsActive = 1
    ORDER BY CategoryName;
END
GO

-- Get all statuses
CREATE OR ALTER PROCEDURE GetStatuses
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT StatusId, StatusName
    FROM dbo.ExpenseStatus
    ORDER BY StatusId;
END
GO

-- Get all users
CREATE OR ALTER PROCEDURE GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        u.UserId,
        u.UserName,
        u.Email,
        u.RoleId,
        u.ManagerId,
        u.IsActive,
        u.CreatedAt,
        r.RoleName
    FROM dbo.Users u
    INNER JOIN dbo.Roles r ON u.RoleId = r.RoleId
    WHERE u.IsActive = 1
    ORDER BY u.UserName;
END
GO

PRINT 'All stored procedures created successfully';
GO
