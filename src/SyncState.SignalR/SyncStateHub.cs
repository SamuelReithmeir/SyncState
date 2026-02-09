using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using SyncState.Interfaces;
using SyncState.StateDeltas;

namespace SyncState.SignalR;

public abstract class SyncStateHub : Hub<ISyncStateClient>
{
    private readonly ISyncStateService _syncStateService;

    public SyncStateHub(ISyncStateService syncStateService)
    {
        _syncStateService = syncStateService;
    }

    protected Task<TState> GetCurrentState<TState>(CancellationToken cancellationToken) where TState : class
    {
        return _syncStateService.GetStateAsync<TState>(cancellationToken);
    }

    protected IAsyncEnumerable<TState> GetStateStream<TState>(CancellationToken cancellationToken)
        where TState : class
    {
        return _syncStateService.GetStateEnumerable<TState>(cancellationToken);
    }

    protected async IAsyncEnumerable<StateDelta> GetDeltaEncodedStateStream<TState>(
        [EnumeratorCancellation] CancellationToken cancellationToken)
        where TState : class
    {
        var (initial, enumerable) = await _syncStateService.GetStateWithDeltas<TState>(cancellationToken);

        await Clients.Caller.ReceiveDeltaEncodingInitialStateAsync(initial, cancellationToken);

        await foreach (var stateDelta in enumerable.WithCancellation(cancellationToken))
        {
            yield return stateDelta;
        }
    }
}