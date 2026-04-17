using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using SyncState.Interfaces;
using SyncState.Sample.DTOs;
using SyncState.Sample.Events;

namespace SyncState.Sample.Hubs;

public interface IApplicationStateClient
{
    Task ReceiveApplicationState(ApplicationStateDto state);
}

public class ApplicationStateHub : Hub<IApplicationStateClient>
{
    private readonly IActiveUserStore _activeUserStore;
    private readonly ISyncStateService _syncStateService;

    public ApplicationStateHub(IActiveUserStore activeUserStore, ISyncStateService syncStateService)
    {
        _activeUserStore = activeUserStore;
        _syncStateService = syncStateService;
    }

    public override async Task OnConnectedAsync()
    {
        await _activeUserStore.AddUserAsync(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _activeUserStore.RemoveUserAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async IAsyncEnumerable<List<ApplicationStateEvent>> GetApplicationState(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (initialState, eventsEnumerable) = await _syncStateService
            .GetCurrentStateAndSubsequentBatchedEventsAsync<ApplicationStateDto, ApplicationStateEvent>(
                cancellationToken);
        await Clients.Caller.ReceiveApplicationState(initialState);
        await foreach (var batch in eventsEnumerable.WithCancellation(cancellationToken))
        {
            yield return batch.Events.ToList();
        }
    }
}