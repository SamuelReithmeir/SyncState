using SyncState.InternalInterfaces;

namespace SyncState.Models.Configuration.EventEmitters;

public class EventEmitterConfiguration<TProperty>
{
    /// <summary>
    /// Defines the event to be emitted when a change in the property value is detected. The event will be emitted with the old value, the new value, and an instance of IInternalSyncEventHub for further event handling.
    /// </summary>
    public required Action<TProperty, TProperty, IInternalSyncEventHub> EmitEvent { get; init; }
}

