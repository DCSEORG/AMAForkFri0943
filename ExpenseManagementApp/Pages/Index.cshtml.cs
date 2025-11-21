using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagementApp.Models;
using ExpenseManagementApp.Services;

namespace ExpenseManagementApp.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DatabaseService dbService, ILogger<IndexModel> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Expenses = await _dbService.GetExpensesAsync();
        ErrorMessage = _dbService.GetLastError();
    }
}
