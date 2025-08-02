using Microsoft.AspNetCore.SignalR;

namespace GitHubWorkflowManager.Hubs;

public class WorkflowHub : Hub
{
    private readonly ILogger<WorkflowHub> _logger;

    public WorkflowHub(ILogger<WorkflowHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGlobalGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "global_updates");
        _logger.LogInformation("Client {ConnectionId} joined global updates group", Context.ConnectionId);
    }

    public async Task LeaveGlobalGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "global_updates");
        _logger.LogInformation("Client {ConnectionId} left global updates group", Context.ConnectionId);
    }

    public async Task JoinWorkflowGroup(string workflowName)
    {
        var groupName = $"workflow_{workflowName}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined workflow group: {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveWorkflowGroup(string workflowName)
    {
        var groupName = $"workflow_{workflowName}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left workflow group: {GroupName}", Context.ConnectionId, groupName);
    }
}
