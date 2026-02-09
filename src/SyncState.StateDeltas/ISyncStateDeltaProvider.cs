namespace SyncState.StateDeltas;

/// <summary>
/// Provides state synchronization using delta encoding for efficient state updates.
/// </summary>
public interface ISyncStateDeltaProvider
{
    /// <summary>
    /// Gets the current state and an async enumerable of subsequent state deltas.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TState">The type of state to retrieve.</typeparam>
    /// <returns>A tuple containing the initial state and an async enumerable of subsequent state deltas.</returns>
    Task<(TState, IAsyncEnumerable<StateDelta>)> GetStateWithDeltas<TState>(CancellationToken cancellationToken = default) where TState : class;
}