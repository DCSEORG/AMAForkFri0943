using Microsoft.Data.SqlClient;
using ExpenseManagementApp.Models;
using Azure.Identity;
using Azure.Core;

namespace ExpenseManagementApp.Services;

public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;
    private string? _lastError;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string? GetLastError() => _lastError;

    private SqlConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        var connection = new SqlConnection(connectionString);
        
        // Configure Managed Identity token if using Azure AD authentication
        if (connectionString.Contains("Authentication=Active Directory Managed Identity", StringComparison.OrdinalIgnoreCase))
        {
            var credential = new DefaultAzureCredential();
            var token = credential.GetToken(new TokenRequestContext(new[] { "https://database.windows.net/.default" }));
            connection.AccessToken = token.Token;
        }
        
        return connection;
    }

    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT e.ExpenseId, e.UserId, e.CategoryId, e.StatusId, e.AmountMinor, 
                       e.Currency, e.ExpenseDate, e.Description, e.ReceiptFile, 
                       e.SubmittedAt, e.ReviewedBy, e.ReviewedAt, e.CreatedAt,
                       u.UserName, c.CategoryName, s.StatusName, r.UserName as ReviewerName
                FROM dbo.Expenses e
                JOIN dbo.Users u ON e.UserId = u.UserId
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
                WHERE (@UserId IS NULL OR e.UserId = @UserId)
                  AND (@StatusId IS NULL OR e.StatusId = @StatusId)
                ORDER BY e.CreatedAt DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);

            var expenses = new List<Expense>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }

            _lastError = null;
            return expenses;
        }
        catch (Exception ex)
        {
            _lastError = $"Error retrieving expenses: {ex.Message} (DatabaseService.cs, Line {ex.StackTrace?.Split('\n')[0]})";
            _logger.LogError(ex, "Error retrieving expenses");
            return GetDummyExpenses();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT e.ExpenseId, e.UserId, e.CategoryId, e.StatusId, e.AmountMinor, 
                       e.Currency, e.ExpenseDate, e.Description, e.ReceiptFile, 
                       e.SubmittedAt, e.ReviewedBy, e.ReviewedAt, e.CreatedAt,
                       u.UserName, c.CategoryName, s.StatusName, r.UserName as ReviewerName
                FROM dbo.Expenses e
                JOIN dbo.Users u ON e.UserId = u.UserId
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                LEFT JOIN dbo.Users r ON e.ReviewedBy = r.UserId
                WHERE e.ExpenseId = @ExpenseId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                _lastError = null;
                return MapExpense(reader);
            }

            return null;
        }
        catch (Exception ex)
        {
            _lastError = $"Error retrieving expense: {ex.Message} (DatabaseService.cs)";
            _logger.LogError(ex, "Error retrieving expense {ExpenseId}", expenseId);
            return null;
        }
    }

    public async Task<int> CreateExpenseAsync(Expense expense)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, 
                                          ExpenseDate, Description, ReceiptFile, SubmittedAt, CreatedAt)
                OUTPUT INSERTED.ExpenseId
                VALUES (@UserId, @CategoryId, @StatusId, @AmountMinor, @Currency, 
                        @ExpenseDate, @Description, @ReceiptFile, @SubmittedAt, SYSUTCDATETIME())";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", expense.UserId);
            command.Parameters.AddWithValue("@CategoryId", expense.CategoryId);
            command.Parameters.AddWithValue("@StatusId", expense.StatusId);
            command.Parameters.AddWithValue("@AmountMinor", expense.AmountMinor);
            command.Parameters.AddWithValue("@Currency", expense.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)expense.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)expense.ReceiptFile ?? DBNull.Value);
            command.Parameters.AddWithValue("@SubmittedAt", (object?)expense.SubmittedAt ?? DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            var expenseId = result != null ? (int)result : -1;
            _lastError = null;
            return expenseId;
        }
        catch (Exception ex)
        {
            _lastError = $"Error creating expense: {ex.Message} (DatabaseService.cs)";
            _logger.LogError(ex, "Error creating expense");
            return -1;
        }
    }

    public async Task<bool> UpdateExpenseAsync(Expense expense)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                UPDATE dbo.Expenses 
                SET CategoryId = @CategoryId,
                    StatusId = @StatusId,
                    AmountMinor = @AmountMinor,
                    Currency = @Currency,
                    ExpenseDate = @ExpenseDate,
                    Description = @Description,
                    ReceiptFile = @ReceiptFile,
                    SubmittedAt = @SubmittedAt,
                    ReviewedBy = @ReviewedBy,
                    ReviewedAt = @ReviewedAt
                WHERE ExpenseId = @ExpenseId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", expense.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", expense.CategoryId);
            command.Parameters.AddWithValue("@StatusId", expense.StatusId);
            command.Parameters.AddWithValue("@AmountMinor", expense.AmountMinor);
            command.Parameters.AddWithValue("@Currency", expense.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)expense.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)expense.ReceiptFile ?? DBNull.Value);
            command.Parameters.AddWithValue("@SubmittedAt", (object?)expense.SubmittedAt ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReviewedBy", (object?)expense.ReviewedBy ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReviewedAt", (object?)expense.ReviewedAt ?? DBNull.Value);

            await command.ExecuteNonQueryAsync();
            _lastError = null;
            return true;
        }
        catch (Exception ex)
        {
            _lastError = $"Error updating expense: {ex.Message} (DatabaseService.cs)";
            _logger.LogError(ex, "Error updating expense {ExpenseId}", expense.ExpenseId);
            return false;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = "DELETE FROM dbo.Expenses WHERE ExpenseId = @ExpenseId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);

            await command.ExecuteNonQueryAsync();
            _lastError = null;
            return true;
        }
        catch (Exception ex)
        {
            _lastError = $"Error deleting expense: {ex.Message} (DatabaseService.cs)";
            _logger.LogError(ex, "Error deleting expense {ExpenseId}", expenseId);
            return false;
        }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = "SELECT CategoryId, CategoryName, IsActive FROM dbo.ExpenseCategories WHERE IsActive = 1";

            using var command = new SqlCommand(query, connection);
            var categories = new List<ExpenseCategory>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }

            _lastError = null;
            return categories;
        }
        catch (Exception ex)
        {
            _lastError = $"Error retrieving categories: {ex.Message} (DatabaseService.cs)";
            _logger.LogError(ex, "Error retrieving categories");
            return GetDummyCategories();
        }
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = "SELECT StatusId, StatusName FROM dbo.ExpenseStatus";

            using var command = new SqlCommand(query, connection);
            var statuses = new List<ExpenseStatus>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1)
                });
            }

            _lastError = null;
            return statuses;
        }
        catch (Exception ex)
        {
            _lastError = $"Error retrieving statuses: {ex.Message} (DatabaseService.cs)";
            _logger.LogError(ex, "Error retrieving statuses");
            return GetDummyStatuses();
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var query = @"
                SELECT u.UserId, u.UserName, u.Email, u.RoleId, u.ManagerId, u.IsActive, u.CreatedAt, r.RoleName
                FROM dbo.Users u
                JOIN dbo.Roles r ON u.RoleId = r.RoleId
                WHERE u.IsActive = 1";

            using var command = new SqlCommand(query, connection);
            var users = new List<User>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(0),
                    UserName = reader.GetString(1),
                    Email = reader.GetString(2),
                    RoleId = reader.GetInt32(3),
                    ManagerId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    IsActive = reader.GetBoolean(5),
                    CreatedAt = reader.GetDateTime(6),
                    RoleName = reader.GetString(7)
                });
            }

            _lastError = null;
            return users;
        }
        catch (Exception ex)
        {
            _lastError = $"Error retrieving users: {ex.Message} (DatabaseService.cs)";
            _logger.LogError(ex, "Error retrieving users");
            return GetDummyUsers();
        }
    }

    private Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            CategoryId = reader.GetInt32(2),
            StatusId = reader.GetInt32(3),
            AmountMinor = reader.GetInt32(4),
            Currency = reader.GetString(5),
            ExpenseDate = reader.GetDateTime(6),
            Description = reader.IsDBNull(7) ? null : reader.GetString(7),
            ReceiptFile = reader.IsDBNull(8) ? null : reader.GetString(8),
            SubmittedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
            ReviewedBy = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            ReviewedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
            CreatedAt = reader.GetDateTime(12),
            UserName = reader.GetString(13),
            CategoryName = reader.GetString(14),
            StatusName = reader.GetString(15),
            ReviewerName = reader.IsDBNull(16) ? null : reader.GetString(16)
        };
    }

    // Dummy data methods for fallback when database is unavailable
    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                CategoryId = 1,
                StatusId = 2,
                AmountMinor = 2540,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-5),
                Description = "Taxi from airport (DUMMY DATA)",
                UserName = "Alice Example",
                CategoryName = "Travel",
                StatusName = "Submitted",
                CreatedAt = DateTime.Now.AddDays(-5)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                CategoryId = 2,
                StatusId = 3,
                AmountMinor = 1425,
                Currency = "GBP",
                ExpenseDate = DateTime.Today.AddDays(-10),
                Description = "Client lunch (DUMMY DATA)",
                UserName = "Alice Example",
                CategoryName = "Meals",
                StatusName = "Approved",
                ReviewerName = "Bob Manager",
                CreatedAt = DateTime.Now.AddDays(-10)
            }
        };
    }

    private List<ExpenseCategory> GetDummyCategories()
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

    private List<ExpenseStatus> GetDummyStatuses()
    {
        return new List<ExpenseStatus>
        {
            new ExpenseStatus { StatusId = 1, StatusName = "Draft" },
            new ExpenseStatus { StatusId = 2, StatusName = "Submitted" },
            new ExpenseStatus { StatusId = 3, StatusName = "Approved" },
            new ExpenseStatus { StatusId = 4, StatusName = "Rejected" }
        };
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User { UserId = 1, UserName = "Alice Example", Email = "alice@example.co.uk", RoleId = 1, RoleName = "Employee", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-6) },
            new User { UserId = 2, UserName = "Bob Manager", Email = "bob.manager@example.co.uk", RoleId = 2, RoleName = "Manager", IsActive = true, CreatedAt = DateTime.Now.AddMonths(-12) }
        };
    }
}
