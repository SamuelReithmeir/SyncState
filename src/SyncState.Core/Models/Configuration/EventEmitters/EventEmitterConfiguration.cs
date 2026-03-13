using SyncState.InternalInterfaces;

namespace SyncState.Models.Configuration.EventEmitters;

public class EventEmitterConfiguration<TProperty>
{
    /// <summary>
    /// Defines the event to be emitted when a change in the property value is detected.
    /// Parameters: (newValue, oldValue, syncEventHub).
    /// </summary>
    public required Action<TProperty, TProperty, IInternalSyncEventHub> EmitEvent { get; init; }
}

