using System.ComponentModel.DataAnnotations;

namespace GitHubWorkflowManager.Models;

public class GitHubConfiguration
{
    [Required(ErrorMessage = "GitHub Personal Access Token is required")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repository owner is required")]
    public string RepositoryOwner { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repository name is required")]
    public string RepositoryName { get; set; } = string.Empty;

    public string WorkflowsPath { get; set; } = ".github/workflows";

    public string WebhookSecret { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrEmpty(AccessToken) && 
                               !string.IsNullOrEmpty(RepositoryOwner) && 
                               !string.IsNullOrEmpty(RepositoryName);
}

public class GitHubConfigurationViewModel : GitHubConfiguration
{
    [Display(Name = "GitHub Personal Access Token")]
    public new string AccessToken { get; set; } = string.Empty;

    [Display(Name = "Repository Owner/Organization")]
    public new string RepositoryOwner { get; set; } = string.Empty;

    [Display(Name = "Repository Name")]
    public new string RepositoryName { get; set; } = string.Empty;

    [Display(Name = "Workflows Directory Path")]
    public new string WorkflowsPath { get; set; } = ".github/workflows";

    [Display(Name = "Webhook Secret (optional)")]
    public new string WebhookSecret { get; set; } = string.Empty;
}
