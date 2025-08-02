using GitHubWorkflowManager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace GitHubWorkflowManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IWebhookService webhookService,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpPost("github")]
    public async Task<IActionResult> HandleGitHubWebhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(payload))
            {
                return BadRequest("Empty payload");
            }

            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();

            _logger.LogInformation("Received GitHub webhook: Event={EventType}, HasSignature={HasSignature}", 
                eventType, !string.IsNullOrEmpty(signature));

            // Only process workflow-related events
            if (eventType == "workflow_run" || eventType == "workflow_job")
            {
                await _webhookService.ProcessWebhookAsync(payload, signature);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling GitHub webhook");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("test")]
    public IActionResult TestWebhook()
    {
        return Ok(new { status = "Webhook endpoint is working", timestamp = DateTime.UtcNow });
    }
}
