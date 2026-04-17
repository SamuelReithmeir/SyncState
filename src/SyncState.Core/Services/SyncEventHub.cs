using System.Collections.Concurrent;
using System.Threading.Channels;
using SyncState.InternalInterfaces;
using SyncState.Models;

namespace SyncState.Services;

public class SyncEventHub : IInternalSyncEventHub
{
    private readonly ConcurrentDictionary<Type, IEventHub> _eventHubs = [];
    
    public void QueueEvent<TEvent>(TEvent syncEvent) where TEvent : notnull
    {
        // Queue the event in all event hubs that are compatible with TEvent
        foreach (var eventHub in _eventHubs.Where(kvp => kvp.Key.IsAssignableFrom(typeof(TEvent)))
                     .Select(kvp => kvp.Value)
                     .Cast<IEventInHub<TEvent>>().ToList())
        {
            eventHub.QueueEvent(syncEvent);
        }
    }

    public async Task BroadcastAsync(CancellationToken cancellationToken)
    {
        foreach (var eventHub in _eventHubs.Values)
        {
            await eventHub.BroadcastAsync(cancellationToken);
        }
    }

    public void DiscardChanges()
    {
        foreach (var eventHub in _eventHubs.Values)
        {
            eventHub.DiscardChanges();
        }
    }

    public ChannelReader<EventBatch<TEvent>> GetEventStream<TEvent>(CancellationToken cancellationToken = default) where TEvent : notnull
    {
        var eventHub = (IEventOutHub<TEvent>)_eventHubs.GetOrAdd(typeof(TEvent), _ => new EventHub<TEvent>());
        return eventHub.GetEventStream(cancellationToken);
    }
}


interface IEventHub
{
    Task BroadcastAsync(CancellationToken cancellationToken);
    void DiscardChanges();
}

interface IEventOutHub<TEvent> : IEventHub where TEvent : notnull
{
    ChannelReader<EventBatch<TEvent>> GetEventStream(CancellationToken cancellationToken = default);
}
interface IEventInHub<in TEvent> : IEventHub where TEvent : notnull
{
    void QueueEvent(TEvent syncEvent);
}

class EventHub<TEvent>: IEventInHub<TEvent>, IEventOutHub<TEvent> where TEvent : notnull
{
    private readonly HashSet<Channel<EventBatch<TEvent>>> _subscribers = [];
    private List<TEvent> _pendingEvents = [];

    public async Task BroadcastAsync(CancellationToken cancellationToken)
    {
        if(_pendingEvents.Count == 0)
        {
            return;
        }

        var eventBatch = new EventBatch<TEvent>
        {
            Events = _pendingEvents
        };
        foreach (var subscriber in _subscribers)
        {
            await subscriber.Writer.WriteAsync(eventBatch, cancellationToken);
        }

        _pendingEvents = [];
    }

    public void DiscardChanges()
    {
        _pendingEvents = [];
    }

    public void QueueEvent(TEvent syncEvent)
    {
        _pendingEvents.Add(syncEvent);
    }

    public ChannelReader<EventBatch<TEvent>> GetEventStream(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<EventBatch<TEvent>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        _subscribers.Add(channel);
        return channel.Reader;
    }
}