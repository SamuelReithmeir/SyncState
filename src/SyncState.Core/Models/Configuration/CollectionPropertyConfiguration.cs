using System.Linq.Expressions;
using SyncState.Models.Configuration.EventEmitters;

namespace SyncState.Models.Configuration;

public record CollectionPropertyConfiguration<TEntry, TKey> : PropertyConfiguration<IEnumerable<TEntry>> where TKey : struct
{
    public required Expression<Func<TEntry, TKey>> KeySelector { get; init; }
    public required List<CollectionOnAddEventEmitterConfiguration<TEntry, TKey>> OnAddEventEmitterConfigurations { get; init; }
    public required List<CollectionOnRemoveEventEmitterConfiguration<TEntry, TKey>> OnRemoveEventEmitterConfigurations { get; init; }
    public required List<CollectionOnUpdateEventEmitterConfiguration<TEntry, TKey>> OnUpdateEventEmitterConfigurations { get; init; }
    public required IEqualityComparer<TEntry> EntryEqualityComparer { get; init; }
}