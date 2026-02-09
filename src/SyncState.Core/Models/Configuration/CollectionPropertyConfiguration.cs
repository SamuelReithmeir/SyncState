using System.Linq.Expressions;
using System.Reflection;
using SyncState.Enums;
using SyncState.Models.Configuration.EventEmitters;

namespace SyncState.Models.Configuration;

public class CollectionPropertyConfiguration<TEntry, TKey> : PropertyConfiguration<IEnumerable<TEntry>> where TKey : struct
{
    public Expression<Func<TEntry, TKey>> KeySelector { get; }
    public List<CollectionOnAddEventEmitterConfiguration<TEntry,TKey>> OnAddEventEmitterConfigurations { get; }
    public List<CollectionOnRemoveEventEmitterConfiguration<TEntry,TKey>> OnRemoveEventEmitterConfigurations { get; }
    public List<CollectionOnUpdateEventEmitterConfiguration<TEntry,TKey>> OnUpdateEventEmitterConfigurations { get; }
    public IEqualityComparer<TEntry> EntryEqualityComparer { get; }
    

    public CollectionPropertyConfiguration(
        PropertyInfo propertyInfo,
        Func<IServiceProvider, CancellationToken, Task<IEnumerable<TEntry>>> gatherer,
        PropertyGatheringServiceScopeBehavior scopeBehavior,
        Type propertyManagerType,
        List<CommandHandlerConfiguration> commandHandlerConfigurations,
        IEqualityComparer<IEnumerable<TEntry>> equalityComparer,
        IEqualityComparer<TEntry> entryEqualityComparer,
        List<EventEmitterConfiguration<IEnumerable<TEntry>>> eventEmitterConfigurations,
        List<CollectionOnAddEventEmitterConfiguration<TEntry,TKey>> onAddEventEmitterConfigurations,
        List<CollectionOnRemoveEventEmitterConfiguration<TEntry,TKey>> onRemoveEventEmitterConfigurations,
        List<CollectionOnUpdateEventEmitterConfiguration<TEntry,TKey>> onUpdateEventEmitterConfigurations,
        Expression<Func<TEntry, TKey>> keySelector,
        Dictionary<Type, object> extensions) : base(propertyInfo, gatherer, scopeBehavior, propertyManagerType,equalityComparer,
        commandHandlerConfigurations, eventEmitterConfigurations, extensions)
    {
        KeySelector = keySelector;
        OnAddEventEmitterConfigurations = onAddEventEmitterConfigurations;
        OnRemoveEventEmitterConfigurations = onRemoveEventEmitterConfigurations;
        OnUpdateEventEmitterConfigurations = onUpdateEventEmitterConfigurations;
        EntryEqualityComparer = entryEqualityComparer;
    }
}