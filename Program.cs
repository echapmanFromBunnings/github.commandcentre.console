using GitHubWorkflowManager.Components;
using GitHubWorkflowManager.Services.Interfaces;
using GitHubWorkflowManager.Services.Implementations;
using GitHubWorkflowManager.Hubs;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add SignalR
builder.Services.AddSignalR();

// Add controllers for webhook endpoints
builder.Services.AddControllers();

// Add data protection for secure token storage
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")))
    .SetApplicationName("GitHubWorkflowManager");

// Add HTTP client
builder.Services.AddHttpClient();

// Add HTTP client for internal API calls with base address
builder.Services.AddScoped<HttpClient>(serviceProvider =>
{
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    var httpContext = httpContextAccessor.HttpContext;
    var request = httpContext?.Request;
    
    if (request != null)
    {
        var baseUrl = $"{request.Scheme}://{request.Host}";
        return new HttpClient { BaseAddress = new Uri(baseUrl) };
    }
    
    // Fallback for development
    return new HttpClient { BaseAddress = new Uri("http://localhost:5058") };
});

// Add IHttpContextAccessor for HttpClient configuration
builder.Services.AddHttpContextAccessor();

// Add memory caching
builder.Services.AddMemoryCache();

// Register application services
builder.Services.AddScoped<IGitHubConfigurationService, GitHubConfigurationService>();
builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddScoped<IWorkflowParsingService, WorkflowParsingService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map controllers for webhook endpoints
app.MapControllers();

// Map SignalR hub
app.MapHub<WorkflowHub>("/workflowhub");

// Map Razor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
