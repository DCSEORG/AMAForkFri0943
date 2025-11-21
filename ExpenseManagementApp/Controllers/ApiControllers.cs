using Microsoft.AspNetCore.Mvc;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(DatabaseService dbService, ILogger<ExpensesController> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses, optionally filtered by user and/or status
    /// </summary>
    /// <param name="userId">Optional user ID to filter by</param>
    /// <param name="statusId">Optional status ID to filter by</param>
    /// <returns>List of expenses</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses([FromQuery] int? userId = null, [FromQuery] int? statusId = null)
    {
        var expenses = await _dbService.GetExpensesAsync(userId, statusId);
        return Ok(expenses);
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <returns>The expense if found</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var expense = await _dbService.GetExpenseByIdAsync(id);
        
        if (expense == null)
        {
            return NotFound(new { message = $"Expense with ID {id} not found" });
        }

        return Ok(expense);
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    /// <param name="expense">Expense details</param>
    /// <returns>The created expense</returns>
    [HttpPost]
    public async Task<ActionResult<Expense>> CreateExpense([FromBody] Expense expense)
    {
        var expenseId = await _dbService.CreateExpenseAsync(expense);
        
        if (expenseId == -1)
        {
            return BadRequest(new { message = "Failed to create expense", error = _dbService.GetLastError() });
        }

        expense.ExpenseId = expenseId;
        return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, expense);
    }

    /// <summary>
    /// Update an existing expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="expense">Updated expense details</param>
    /// <returns>Success indicator</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(int id, [FromBody] Expense expense)
    {
        if (id != expense.ExpenseId)
        {
            return BadRequest(new { message = "Expense ID mismatch" });
        }

        var success = await _dbService.UpdateExpenseAsync(expense);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to update expense", error = _dbService.GetLastError() });
        }

        return NoContent();
    }

    /// <summary>
    /// Delete an expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <returns>Success indicator</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(int id)
    {
        var success = await _dbService.DeleteExpenseAsync(id);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to delete expense", error = _dbService.GetLastError() });
        }

        return NoContent();
    }

    /// <summary>
    /// Submit an expense for approval
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <returns>Success indicator</returns>
    [HttpPost("{id}/submit")]
    public async Task<IActionResult> SubmitExpense(int id)
    {
        var expense = await _dbService.GetExpenseByIdAsync(id);
        
        if (expense == null)
        {
            return NotFound(new { message = $"Expense with ID {id} not found" });
        }

        expense.StatusId = 2; // Submitted
        expense.SubmittedAt = DateTime.UtcNow;

        var success = await _dbService.UpdateExpenseAsync(expense);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to submit expense", error = _dbService.GetLastError() });
        }

        return Ok(new { message = "Expense submitted successfully" });
    }

    /// <summary>
    /// Approve an expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="reviewerId">ID of the reviewer approving the expense</param>
    /// <returns>Success indicator</returns>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveExpense(int id, [FromQuery] int reviewerId)
    {
        var expense = await _dbService.GetExpenseByIdAsync(id);
        
        if (expense == null)
        {
            return NotFound(new { message = $"Expense with ID {id} not found" });
        }

        expense.StatusId = 3; // Approved
        expense.ReviewedBy = reviewerId;
        expense.ReviewedAt = DateTime.UtcNow;

        var success = await _dbService.UpdateExpenseAsync(expense);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to approve expense", error = _dbService.GetLastError() });
        }

        return Ok(new { message = "Expense approved successfully" });
    }

    /// <summary>
    /// Reject an expense
    /// </summary>
    /// <param name="id">Expense ID</param>
    /// <param name="reviewerId">ID of the reviewer rejecting the expense</param>
    /// <returns>Success indicator</returns>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectExpense(int id, [FromQuery] int reviewerId)
    {
        var expense = await _dbService.GetExpenseByIdAsync(id);
        
        if (expense == null)
        {
            return NotFound(new { message = $"Expense with ID {id} not found" });
        }

        expense.StatusId = 4; // Rejected
        expense.ReviewedBy = reviewerId;
        expense.ReviewedAt = DateTime.UtcNow;

        var success = await _dbService.UpdateExpenseAsync(expense);
        
        if (!success)
        {
            return BadRequest(new { message = "Failed to reject expense", error = _dbService.GetLastError() });
        }

        return Ok(new { message = "Expense rejected successfully" });
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public CategoriesController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all expense categories
    /// </summary>
    /// <returns>List of categories</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseCategory>>> GetCategories()
    {
        var categories = await _dbService.GetCategoriesAsync();
        return Ok(categories);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusesController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public StatusesController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    /// <returns>List of statuses</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseStatus>>> GetStatuses()
    {
        var statuses = await _dbService.GetStatusesAsync();
        return Ok(statuses);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public UsersController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    /// <returns>List of users</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        var users = await _dbService.GetUsersAsync();
        return Ok(users);
    }
}
