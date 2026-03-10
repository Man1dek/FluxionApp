using Microsoft.AspNetCore.SignalR;

namespace Fluxion.Api.Hubs;

/// <summary>
/// SignalR hub for pushing real-time curriculum updates to connected clients.
/// Clients join a group based on their learnerId to receive personalised events.
/// </summary>
public class CurriculumHub : Hub
{
    /// <summary>
    /// Called by the client to join their learner-specific group.
    /// </summary>
    public async Task JoinLearnerGroup(string learnerId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, learnerId);
        await Clients.Caller.SendAsync("JoinedGroup", learnerId);
    }

    /// <summary>
    /// Called by the client to leave their learner group.
    /// </summary>
    public async Task LeaveLearnerGroup(string learnerId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, learnerId);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}
