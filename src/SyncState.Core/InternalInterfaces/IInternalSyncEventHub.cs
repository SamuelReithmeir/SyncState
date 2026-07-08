using SyncState.Models;

namespace SyncState.InternalInterfaces;

public interface IInternalSyncEventHub
{
    /// <summary>
    /// enqueue an event to be broadcast later
    /// </summary>
    /// <param name="syncEvent"></param>
    /// <typeparam name="TEvent"></typeparam>
    public void QueueEvent<TEvent>(TEvent syncEvent) where TEvent : notnull;

    /// <summary>
    /// Broadcast all queued events
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task BroadcastAsync(CancellationToken cancellationToken);

    /// <summary>
    /// discard all pending events
    /// </summary>
    void DiscardChanges();
    
    /// <summary>
    /// get an async enumerable of event batches of type TEvent.
    /// The subscription channel is automatically cleaned up when iteration ends.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    IAsyncEnumerable<EventBatch<TEvent>> GetEventStream<TEvent>(CancellationToken cancellationToken = default) where TEvent : notnull;
}