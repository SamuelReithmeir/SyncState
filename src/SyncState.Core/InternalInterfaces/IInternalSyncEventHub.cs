using System.Threading.Channels;
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
    /// get a channel reader for event batches of type TEvent
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    ChannelReader<EventBatch<TEvent>> GetEventStream<TEvent>(CancellationToken cancellationToken = default) where TEvent : notnull;
}