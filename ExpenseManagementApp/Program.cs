using ExpenseManagementApp.Services;
using Azure.AI.OpenAI;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Expense Management API",
        Version = "v1",
        Description = "API for managing employee expenses with approval workflow"
    });
});

// Add Database Service
builder.Services.AddScoped<DatabaseService>();

// Add OpenAI Client (if configured)
var openAIEndpoint = builder.Configuration["OpenAI:Endpoint"];
if (!string.IsNullOrEmpty(openAIEndpoint))
{
    builder.Services.AddSingleton<AzureOpenAIClient>(sp =>
    {
        var endpoint = new Uri(openAIEndpoint);
        var credential = new DefaultAzureCredential();
        return new AzureOpenAIClient(endpoint, credential);
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Management API v1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
