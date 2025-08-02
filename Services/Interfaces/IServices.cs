using GitHubWorkflowManager.Models;

namespace GitHubWorkflowManager.Services.Interfaces;

public interface IGitHubConfigurationService
{
    Task<GitHubConfiguration?> GetConfigurationAsync();
    Task SaveConfigurationAsync(GitHubConfiguration configuration);
    Task<bool> ValidateConfigurationAsync(GitHubConfiguration configuration);
    Task ClearConfigurationAsync();
    Task<bool> IsConfiguredAsync();
}

public interface IGitHubService
{
    Task<bool> ValidateAccessTokenAsync(string token);
    Task<List<WorkflowDefinition>> GetWorkflowsAsync();
    Task<WorkflowDefinition?> GetWorkflowAsync(string workflowId);
    Task<List<WorkflowRunInfo>> GetWorkflowRunsAsync(string workflowId, int count = 10);
    Task<WorkflowRunInfo?> GetWorkflowRunAsync(long runId);
    Task<WorkflowExecutionResult> TriggerWorkflowAsync(WorkflowExecutionRequest request);
    Task<string> GetWorkflowLogsAsync(long runId);
    Task<bool> SetupWebhookAsync(string webhookUrl, string? secret = null);
    Task<bool> RemoveWebhookAsync(string webhookUrl);
    void ClearWorkflowsCache();
    Task<WorkflowRunInfo?> GetLatestWorkflowRunAsync(string workflowName);
    Task<Dictionary<string, WorkflowRunInfo?>> GetLatestWorkflowRunsAsync(IEnumerable<string> workflowNames);
}

public interface IWorkflowParsingService
{
    WorkflowDefinition ParseWorkflowFile(string fileName, string content);
    bool IsValidWorkflowFile(string content);
    Dictionary<string, WorkflowInput> ExtractInputs(string content);
    WorkflowTriggers ExtractTriggers(string content);
}

public interface IWebhookService
{
    Task ProcessWebhookAsync(string payload, string? signature = null);
    bool ValidateSignature(string payload, string signature, string secret);
    Task NotifyWorkflowUpdateAsync(WorkflowRunInfo workflowRun);
}

public interface INotificationService
{
    Task NotifyWorkflowStatusChangeAsync(string workflowName, string status, string? message = null);
    Task NotifyErrorAsync(string message, Exception? exception = null);
    Task NotifyInfoAsync(string message);
}
