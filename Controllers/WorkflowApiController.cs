using GitHubWorkflowManager.Models;
using GitHubWorkflowManager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GitHubWorkflowManager.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowApiController : ControllerBase
{
    private readonly IGitHubService _gitHubService;
    private readonly ILogger<WorkflowApiController> _logger;

    public WorkflowApiController(IGitHubService gitHubService, ILogger<WorkflowApiController> logger)
    {
        _gitHubService = gitHubService;
        _logger = logger;
    }

    [HttpGet("workflows")]
    public async Task<ActionResult<List<WorkflowDefinition>>> GetWorkflows()
    {
        try
        {
            var workflows = await _gitHubService.GetWorkflowsAsync();
            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows");
            return StatusCode(500, new { error = "Failed to load workflows" });
        }
    }

    [HttpGet("workflow-runs/latest")]
    public async Task<ActionResult<Dictionary<string, WorkflowRunInfo?>>> GetLatestWorkflowRuns([FromQuery] string[] workflowNames)
    {
        try
        {
            if (workflowNames == null || workflowNames.Length == 0)
            {
                return BadRequest(new { error = "Workflow names are required" });
            }

            var latestRuns = await _gitHubService.GetLatestWorkflowRunsAsync(workflowNames);
            return Ok(latestRuns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest workflow runs");
            return StatusCode(500, new { error = "Failed to load workflow runs" });
        }
    }

    [HttpGet("workflow/{workflowName}/runs")]
    public async Task<ActionResult<List<WorkflowRunInfo>>> GetWorkflowRuns(string workflowName, [FromQuery] int count = 10)
    {
        try
        {
            var runs = await _gitHubService.GetWorkflowRunsAsync(workflowName, count);
            return Ok(runs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow runs for {WorkflowName}", workflowName);
            return StatusCode(500, new { error = "Failed to load workflow runs" });
        }
    }

    [HttpPost("workflow/trigger")]
    public async Task<ActionResult<WorkflowExecutionResult>> TriggerWorkflow([FromBody] WorkflowExecutionRequest request)
    {
        try
        {
            var result = await _gitHubService.TriggerWorkflowAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering workflow {WorkflowId}", request.WorkflowId);
            return StatusCode(500, new { error = "Failed to trigger workflow" });
        }
    }

    [HttpDelete("cache/workflows")]
    public ActionResult ClearWorkflowsCache()
    {
        try
        {
            _gitHubService.ClearWorkflowsCache();
            return Ok(new { message = "Cache cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing workflows cache");
            return StatusCode(500, new { error = "Failed to clear cache" });
        }
    }
}
