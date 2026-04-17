using SyncState.Interfaces;
using SyncState.InternalInterfaces;

namespace SyncState.StateDeltas;

/// <summary>
/// Extension methods for accessing state delta functionality on ISyncStateService.
/// </summary>
public static class SyncStateDeltaServiceExtensions
{
    /// <summary>
    /// Gets an initial state and an async enumerable of delta objects for efficient state synchronization.
    /// </summary>
    /// <param name="syncStateService">The SyncState service.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TState">The type of state to retrieve.</typeparam>
    /// <returns>A tuple containing the initial state and an async enumerable of subsequent state deltas.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ISyncStateService implementation doesn't support delta encoding.</exception>
    public static Task<(TState, IAsyncEnumerable<StateDelta>)> GetStateWithDeltas<TState>(
        this ISyncStateService syncStateService, CancellationToken cancellationToken = default) where TState : class
    {
        if (syncStateService as IInternalSyncStateService is not { } internalSyncStateService)
        {
            throw new InvalidOperationException(
                "The implementation of ISyncStateService must implement IInternalSyncStateService");
        }

        var deltaProvider = internalSyncStateService.GetService<ISyncStateDeltaProvider>();
        return deltaProvider.GetStateWithDeltas<TState>(cancellationToken);
    }
}