namespace SyncState.Interfaces;

public interface ISyncCommandService
{
    /// <summary>
    /// Sends a command to state managers which are configured to handle it.
    /// </summary>
    /// <param name="command">The custom command object to be handled.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <typeparam name="TCommand">The type of command to handle.</typeparam>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task HandleAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : class;

    /// <summary>
    /// Disables direct digestion of commands and instead buffers them until <see cref="ExecuteBufferedCommandsAsync"/> is called.
    /// </summary>
    void StartCommandBuffer();
    
    /// <summary>
    /// Discards all buffered commands and enables direct digestion of commands again.
    /// </summary>
    void ClearCommandBuffer();
    
    /// <summary>
    /// Commits all buffered commands and enables direct digestion of commands again.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteBufferedCommandsAsync(CancellationToken cancellationToken = default);
}