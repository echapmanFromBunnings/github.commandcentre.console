using GitHubWorkflowManager.Models;
using GitHubWorkflowManager.Services.Interfaces;
using GitHubWorkflowManager.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GitHubWorkflowManager.Services.Implementations;

public class WebhookService : IWebhookService
{
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly ILogger<WebhookService> _logger;
    private readonly IGitHubConfigurationService _configurationService;

    public WebhookService(
        IHubContext<WorkflowHub> hubContext,
        ILogger<WebhookService> logger,
        IGitHubConfigurationService configurationService)
    {
        _hubContext = hubContext;
        _logger = logger;
        _configurationService = configurationService;
    }

    public async Task ProcessWebhookAsync(string payload, string? signature = null)
    {
        try
        {
            // Validate signature if provided
            if (!string.IsNullOrEmpty(signature))
            {
                var config = await _configurationService.GetConfigurationAsync();
                if (!string.IsNullOrEmpty(config?.WebhookSecret))
                {
                    if (!ValidateSignature(payload, signature, config.WebhookSecret))
                    {
                        _logger.LogWarning("Invalid webhook signature");
                        return;
                    }
                }
            }

            var webhookPayload = JsonSerializer.Deserialize<WebhookPayload>(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (webhookPayload?.WorkflowRun != null)
            {
                await NotifyWorkflowUpdateAsync(webhookPayload.WorkflowRun);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook payload");
        }
    }

    public bool ValidateSignature(string payload, string signature, string secret)
    {
        try
        {
            if (!signature.StartsWith("sha256="))
                return false;

            var expectedSignature = signature.Substring(7); // Remove "sha256=" prefix
            
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();
            
            return string.Equals(expectedSignature, computedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }

    public async Task NotifyWorkflowUpdateAsync(WorkflowRunInfo workflowRun)
    {
        try
        {
            // Notify global group
            await _hubContext.Clients.Group("global_updates")
                .SendAsync("WorkflowRunUpdated", workflowRun);

            // Notify specific workflow group if we can determine the workflow name
            // Note: This might need adjustment based on how workflow names are handled
            if (!string.IsNullOrEmpty(workflowRun.Event))
            {
                await _hubContext.Clients.Group($"workflow_{workflowRun.Event}")
                    .SendAsync("WorkflowRunUpdated", workflowRun);
            }

            _logger.LogInformation("Notified clients about workflow run update: {RunId}", workflowRun.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying workflow update");
        }
    }
}

public class NotificationService : INotificationService
{
    private readonly IHubContext<WorkflowHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<WorkflowHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyWorkflowStatusChangeAsync(string workflowName, string status, string? message = null)
    {
        try
        {
            var notification = new
            {
                Type = "WorkflowStatusChange",
                WorkflowName = workflowName,
                Status = status,
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("Notification", notification);
            _logger.LogInformation("Sent workflow status notification: {WorkflowName} - {Status}", workflowName, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending workflow status notification");
        }
    }

    public async Task NotifyErrorAsync(string message, Exception? exception = null)
    {
        try
        {
            var notification = new
            {
                Type = "Error",
                Message = message,
                Details = exception?.Message,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("Notification", notification);
            _logger.LogError(exception, "Error notification sent: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending error notification");
        }
    }

    public async Task NotifyInfoAsync(string message)
    {
        try
        {
            var notification = new
            {
                Type = "Info",
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.All.SendAsync("Notification", notification);
            _logger.LogInformation("Info notification sent: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending info notification");
        }
    }
}
