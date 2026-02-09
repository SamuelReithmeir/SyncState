namespace SyncState.EntityFrameworkCore.Interfaces;

/// <summary>
/// Service responsible for dispatching commands to SyncState based on data gathered by a change handler.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches the queued commands asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DispatchAsync(CancellationToken cancellationToken = default);
}