using System.Text.Json.Serialization;

namespace GitHubWorkflowManager.Models;

public class WorkflowDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public WorkflowTriggers On { get; set; } = new();
    public Dictionary<string, WorkflowInput> Inputs { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime LastModified { get; set; }
    public WorkflowRunInfo? LastRun { get; set; }
    public WorkflowMetadata? Metadata { get; set; }
}

public class WorkflowMetadata
{
    public string Purpose { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    public List<string> Inputs { get; set; } = new();
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

public class WorkflowTriggers
{
    public bool Push { get; set; }
    public bool PullRequest { get; set; }
    public bool WorkflowDispatch { get; set; }
    public bool Schedule { get; set; }
    public List<string> Other { get; set; } = new();
}

public class WorkflowInput
{
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, boolean, choice, environment
    public object? Default { get; set; }
    public bool Required { get; set; }
    public List<string> Options { get; set; } = new(); // For choice type
}

public class WorkflowRunInfo
{
    public long Id { get; set; }
    public string Status { get; set; } = string.Empty; // queued, in_progress, completed
    public string Conclusion { get; set; } = string.Empty; // success, failure, cancelled, etc.
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public int RunNumber { get; set; }
    public string HeadSha { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public List<WorkflowJob> Jobs { get; set; } = new();
}

public class WorkflowJob
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Conclusion { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<WorkflowStep> Steps { get; set; } = new();
}

public class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Conclusion { get; set; } = string.Empty;
    public int Number { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
