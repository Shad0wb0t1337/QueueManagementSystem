using Microsoft.AspNetCore.SignalR;
using QueueSystem.API.Constants;

namespace QueueSystem.API.Hubs;

public class QueueHub : Hub
{
    public async Task JoinDisplay()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Display);
    }

    public async Task JoinService(int serviceTypeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Service(serviceTypeId));
    }

    public async Task LeaveService(int serviceTypeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Service(serviceTypeId));
    }

    public async Task JoinCounter(int counterId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SignalRGroups.Counter(counterId));
    }

    public async Task LeaveCounter(int counterId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SignalRGroups.Counter(counterId));
    }
}
