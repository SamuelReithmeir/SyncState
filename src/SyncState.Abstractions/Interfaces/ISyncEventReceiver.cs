namespace SyncState.Interfaces;

/// <summary>
/// Interface for receiving events from states.
/// </summary>
/// <typeparam name="TEvent">The type of event to receive.</typeparam>
public interface ISyncEventReceiver<in TEvent>
{
    /// <summary>
    /// Receives an event from a state.
    /// </summary>
    /// <param name="event">The event to receive.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ReceiveEventAsync(TEvent @event);
}

