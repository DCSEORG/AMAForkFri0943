using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagementApp.Pages;

public class ChatModel : PageModel
{
    private readonly IConfiguration _configuration;

    public ChatModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
        var openAIEndpoint = _configuration["OpenAI:Endpoint"];
        
        if (string.IsNullOrEmpty(openAIEndpoint))
        {
            ErrorMessage = "Azure OpenAI endpoint not configured. Run deploy-chatui.sh to enable GenAI features.";
        }
    }
}
