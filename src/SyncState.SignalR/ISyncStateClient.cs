namespace SyncState.SignalR;

/// <summary>
/// Interface for SignalR clients receiving state updates from the SyncState hub.
/// </summary>
public interface ISyncStateClient
{
    /// <summary>
    /// Receives the initial state when using delta encoding for state synchronization.
    /// </summary>
    /// <param name="state">The initial state object.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ReceiveDeltaEncodingInitialStateAsync(object state, CancellationToken cancellationToken);
}