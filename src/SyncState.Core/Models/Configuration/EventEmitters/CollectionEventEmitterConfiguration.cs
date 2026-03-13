using SyncState.InternalInterfaces;

namespace SyncState.Models.Configuration.EventEmitters;

public abstract class CollectionEventEmitterConfiguration<TEntry, TKey>;

public class CollectionOnAddEventEmitterConfiguration<TEntry,TKey> : CollectionEventEmitterConfiguration<TEntry, TKey>
    where TKey : struct
{
    public required Action<TEntry, IInternalSyncEventHub> EmitEvent { get; init; }
}

public class CollectionOnUpdateEventEmitterConfiguration<TEntry, TKey> : CollectionEventEmitterConfiguration<TEntry, TKey>
    where TKey : struct
{
    /// <summary>
    /// Defines the event to be emitted when an entry is updated.
    /// Parameters: (newEntry, oldEntry, syncEventHub).
    /// </summary>
    public required Action<TEntry, TEntry, IInternalSyncEventHub> EmitEvent { get; init; }
}

public class
    CollectionOnRemoveEventEmitterConfiguration<TEntry, TKey> : CollectionEventEmitterConfiguration<TEntry, TKey>
    where TKey : struct
{
    public required Action<TEntry, TKey, IInternalSyncEventHub> EmitEvent { get; init; }
}