namespace GitHubWorkflowManager.Models;

public class WorkflowExecutionRequest
{
    public string WorkflowId { get; set; } = string.Empty;
    public string Ref { get; set; } = "main"; // Branch or tag
    public Dictionary<string, object> Inputs { get; set; } = new();
}

public class WorkflowExecutionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public long? RunId { get; set; }
    public string? RunUrl { get; set; }
    public string? Error { get; set; }
}

public class WebhookPayload
{
    public string Action { get; set; } = string.Empty;
    public WorkflowRunInfo? WorkflowRun { get; set; }
    public Repository? Repository { get; set; }
}

public class Repository
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Owner? Owner { get; set; }
}

public class Owner
{
    public string Login { get; set; } = string.Empty;
}
