using GitHubWorkflowManager.Models;
using GitHubWorkflowManager.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using System.Text;

namespace GitHubWorkflowManager.Services.Implementations;

public class GitHubService : IGitHubService
{
    private readonly IGitHubConfigurationService _configurationService;
    private readonly IWorkflowParsingService _workflowParsingService;
    private readonly ILogger<GitHubService> _logger;
    private readonly IMemoryCache _cache;
    private GitHubClient? _gitHubClient;
    private GitHubConfiguration? _configuration;

    public GitHubService(
        IGitHubConfigurationService configurationService,
        IWorkflowParsingService workflowParsingService,
        ILogger<GitHubService> logger,
        IMemoryCache cache)
    {
        _configurationService = configurationService;
        _workflowParsingService = workflowParsingService;
        _logger = logger;
        _cache = cache;
    }

    private async Task<GitHubClient> GetGitHubClientAsync()
    {
        if (_gitHubClient == null || _configuration == null)
        {
            _configuration = await _configurationService.GetConfigurationAsync();
            if (_configuration == null || !_configuration.IsConfigured)
            {
                throw new InvalidOperationException("GitHub is not configured");
            }

            _gitHubClient = new GitHubClient(new ProductHeaderValue("GitHubWorkflowManager"))
            {
                Credentials = new Credentials(_configuration.AccessToken)
            };
        }

        return _gitHubClient;
    }

    public async Task<bool> ValidateAccessTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Access token is null or empty");
                return false;
            }

            var client = new GitHubClient(new ProductHeaderValue("GitHubWorkflowManager"))
            {
                Credentials = new Credentials(token)
            };

            var user = await client.User.Current();
            _logger.LogInformation("Successfully validated access token for user: {UserLogin}", user.Login);
            return user != null;
        }
        catch (AuthorizationException ex)
        {
            _logger.LogWarning(ex, "Authorization failed - invalid token or insufficient permissions");
            return false;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "GitHub API error during token validation: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating access token");
            return false;
        }
    }

    public async Task<List<WorkflowDefinition>> GetWorkflowsAsync()
    {
        var cacheKey = $"workflows_{_configuration?.RepositoryOwner}_{_configuration?.RepositoryName}";
        
        if (_cache.TryGetValue(cacheKey, out List<WorkflowDefinition>? cachedWorkflows))
        {
            _logger.LogInformation("Returning cached workflows ({Count} items)", cachedWorkflows!.Count);
            return cachedWorkflows;
        }

        try
        {
            _logger.LogInformation("Starting to get workflows...");
            var client = await GetGitHubClientAsync();
            var workflows = new List<WorkflowDefinition>();

            _logger.LogInformation("Getting workflow files from repository: {Owner}/{Repo}, Path: {Path}", 
                _configuration!.RepositoryOwner, _configuration.RepositoryName, _configuration.WorkflowsPath);

            // Get workflow files from the repository
            var contents = await client.Repository.Content.GetAllContents(
                _configuration!.RepositoryOwner,
                _configuration.RepositoryName,
                _configuration.WorkflowsPath);

            _logger.LogInformation("Found {ContentCount} items in workflows directory", contents.Count);

            var workflowFiles = contents.Where(c => c.Type == ContentType.File && 
                                                   (c.Name.EndsWith(".yml") || c.Name.EndsWith(".yaml"))).ToList();
            
            _logger.LogInformation("Found {WorkflowFileCount} workflow files", workflowFiles.Count);

            foreach (var content in workflowFiles)
            {
                try
                {
                    _logger.LogInformation("Processing workflow file: {FileName}", content.Name);
                    
                    // Use a timeout for getting file content
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    
                    var fileContent = await client.Repository.Content.GetRawContent(
                        _configuration.RepositoryOwner,
                        _configuration.RepositoryName,
                        content.Path);

                    var workflowContent = Encoding.UTF8.GetString(fileContent);
                    var workflow = _workflowParsingService.ParseWorkflowFile(content.Name, workflowContent);
                    workflow.FilePath = content.Path;
                    
                    _logger.LogInformation("Successfully parsed workflow: {WorkflowName}", workflow.Name);
                    
                    // Skip getting last run info for faster loading - can be loaded on demand
                    workflows.Add(workflow);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not parse workflow file {FileName}", content.Name);
                }
            }

            _logger.LogInformation("Successfully processed {WorkflowCount} workflows", workflows.Count);
            
            // Cache for 5 minutes
            _cache.Set(cacheKey, workflows, TimeSpan.FromMinutes(5));
            
            return workflows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows");
            throw;
        }
    }

    public async Task<WorkflowDefinition?> GetWorkflowAsync(string workflowId)
    {
        try
        {
            _logger.LogInformation("Getting workflow: {WorkflowId}", workflowId);
            
            var workflows = await GetWorkflowsAsync();
            _logger.LogInformation("Retrieved {WorkflowCount} workflows", workflows.Count);
            
            var workflow = workflows.FirstOrDefault(w => w.Name == workflowId || w.FileName == workflowId);
            _logger.LogInformation("Found workflow: {Found}", workflow != null);
            
            if (workflow == null)
            {
                _logger.LogWarning("Workflow not found. Available workflows: {WorkflowNames}", 
                    string.Join(", ", workflows.Select(w => w.Name)));
            }
            
            return workflow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {WorkflowId}", workflowId);
            throw;
        }
    }

    public async Task<List<WorkflowRunInfo>> GetWorkflowRunsAsync(string workflowId, int count = 10)
    {
        try
        {
            var client = await GetGitHubClientAsync();
            
            // Get all workflows to find the one with matching name
            var allWorkflows = await client.Actions.Workflows.List(_configuration!.RepositoryOwner, _configuration.RepositoryName);
            var workflow = allWorkflows.Workflows.FirstOrDefault(w => w.Name == workflowId);
            
            if (workflow == null)
            {
                _logger.LogWarning("Workflow {WorkflowId} not found", workflowId);
                return new List<WorkflowRunInfo>();
            }

            // Get workflow runs for this specific workflow using repository runs endpoint
            var apiOptions = new Octokit.ApiOptions
            {
                PageSize = count,
                PageCount = 1
            };
            var request = new Octokit.WorkflowRunsRequest();
            var allRuns = await client.Actions.Workflows.Runs.ListByWorkflow(_configuration.RepositoryOwner, _configuration.RepositoryName, workflow.Id, request, apiOptions);
            
            return allRuns.WorkflowRuns.Select(run => new WorkflowRunInfo
            {
                Id = run.Id,
                RunNumber = (int)run.RunNumber,
                Status = run.Status.StringValue,
                Conclusion = run.Conclusion?.StringValue ?? string.Empty,
                CreatedAt = run.CreatedAt.DateTime,
                UpdatedAt = run.UpdatedAt.DateTime,
                HtmlUrl = run.HtmlUrl,
                HeadSha = run.HeadSha,
                Event = run.Event
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow runs for {WorkflowId}", workflowId);
            return new List<WorkflowRunInfo>();
        }
    }

    public async Task<WorkflowRunInfo?> GetWorkflowRunAsync(long runId)
    {
        try
        {
            var client = await GetGitHubClientAsync();
            
            var run = await client.Actions.Workflows.Runs.Get(_configuration!.RepositoryOwner, _configuration.RepositoryName, runId);
            
            return new WorkflowRunInfo
            {
                Id = run.Id,
                RunNumber = (int)run.RunNumber,
                Status = run.Status.StringValue,
                Conclusion = run.Conclusion?.StringValue ?? string.Empty,
                CreatedAt = run.CreatedAt.DateTime,
                UpdatedAt = run.UpdatedAt.DateTime,
                HtmlUrl = run.HtmlUrl,
                HeadSha = run.HeadSha,
                Event = run.Event
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow run {RunId}", runId);
            return null;
        }
    }

    public async Task<WorkflowExecutionResult> TriggerWorkflowAsync(WorkflowExecutionRequest request)
    {
        try
        {
            var client = await GetGitHubClientAsync();
            
            // Get all workflows to find the one with matching name
            var allWorkflows = await client.Actions.Workflows.List(_configuration!.RepositoryOwner, _configuration.RepositoryName);
            var workflow = allWorkflows.Workflows.FirstOrDefault(w => w.Name == request.WorkflowId);
            
            if (workflow == null)
            {
                return new WorkflowExecutionResult
                {
                    Success = false,
                    Error = $"Workflow '{request.WorkflowId}' not found"
                };
            }

            var createWorkflowDispatch = new CreateWorkflowDispatch(request.Ref)
            {
                Inputs = request.Inputs.ToDictionary<KeyValuePair<string, object>, string, object>(
                    kvp => kvp.Key, 
                    kvp => kvp.Value?.ToString() ?? string.Empty)
            };

            await client.Actions.Workflows.CreateDispatch(
                _configuration.RepositoryOwner,
                _configuration.RepositoryName,
                workflow.Id,
                createWorkflowDispatch);

            // Get the most recent run for this workflow to get the run ID
            await Task.Delay(2000); // Give GitHub a moment to create the run
            var runs = await GetWorkflowRunsAsync(request.WorkflowId, 1);
            var latestRun = runs.FirstOrDefault();

            return new WorkflowExecutionResult
            {
                Success = true,
                Message = "Workflow triggered successfully",
                RunId = latestRun?.Id,
                RunUrl = latestRun?.HtmlUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering workflow {WorkflowId}", request.WorkflowId);
            return new WorkflowExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<string> GetWorkflowLogsAsync(long runId)
    {
        try
        {
            var client = await GetGitHubClientAsync();
            var logs = await client.Actions.Workflows.Runs.GetLogs(_configuration!.RepositoryOwner, _configuration.RepositoryName, runId);
            
            // The logs come as a stream/archive, you might need to process this differently
            // For now, return a message indicating logs are available
            return $"Logs available at GitHub for run {runId}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow logs for run {RunId}", runId);
            return $"Error retrieving logs: {ex.Message}";
        }
    }

    public async Task<bool> SetupWebhookAsync(string webhookUrl, string? secret = null)
    {
        try
        {
            var client = await GetGitHubClientAsync();
            
            var config = new Dictionary<string, string>
            {
                { "url", webhookUrl },
                { "content_type", "json" }
            };

            if (!string.IsNullOrEmpty(secret))
            {
                config["secret"] = secret;
            }

            var newHook = new NewRepositoryHook("web", config)
            {
                Events = new[] { "workflow_run", "workflow_job" },
                Active = true
            };

            await client.Repository.Hooks.Create(_configuration!.RepositoryOwner, _configuration.RepositoryName, newHook);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up webhook");
            return false;
        }
    }

    public async Task<bool> RemoveWebhookAsync(string webhookUrl)
    {
        try
        {
            var client = await GetGitHubClientAsync();
            var hooks = await client.Repository.Hooks.GetAll(_configuration!.RepositoryOwner, _configuration.RepositoryName);
            
            var hookToRemove = hooks.FirstOrDefault(h => h.Config.ContainsKey("url") && h.Config["url"] == webhookUrl);
            if (hookToRemove != null)
            {
                await client.Repository.Hooks.Delete(_configuration.RepositoryOwner, _configuration.RepositoryName, hookToRemove.Id);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing webhook");
            return false;
        }
    }
    
    public void ClearWorkflowsCache()
    {
        var cacheKey = $"workflows_{_configuration?.RepositoryOwner}_{_configuration?.RepositoryName}";
        _cache.Remove(cacheKey);
        _logger.LogInformation("Workflows cache cleared");
    }

    public async Task<WorkflowRunInfo?> GetLatestWorkflowRunAsync(string workflowName)
    {
        try
        {
            var runs = await GetWorkflowRunsAsync(workflowName, 1);
            return runs.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get latest run for workflow {WorkflowName}", workflowName);
            return null;
        }
    }

    public async Task<Dictionary<string, WorkflowRunInfo?>> GetLatestWorkflowRunsAsync(IEnumerable<string> workflowNames)
    {
        var result = new Dictionary<string, WorkflowRunInfo?>();
        
        // Process in parallel for better performance
        var tasks = workflowNames.Select(async workflowName =>
        {
            var latestRun = await GetLatestWorkflowRunAsync(workflowName);
            return new { WorkflowName = workflowName, LatestRun = latestRun };
        });

        var results = await Task.WhenAll(tasks);
        
        foreach (var item in results)
        {
            result[item.WorkflowName] = item.LatestRun;
        }

        return result;
    }
}
