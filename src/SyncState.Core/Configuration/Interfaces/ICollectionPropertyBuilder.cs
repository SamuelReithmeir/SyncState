using System.Linq.Expressions;
using SyncState.Enums;
using SyncState.Interfaces.Managers; 

namespace SyncState.Configuration.Interfaces;

/// <summary>
/// Intermediate builder for collection properties that requires a key selector to be specified.
/// </summary>
public interface IPartialCollectionPropertyBuilder<TState, TEntry>
    where TState : class
{
    /// <summary>
    /// Specifies the key selector for the collection entries.
    /// </summary>
    /// <param name="keyExpression">Expression selecting the key property for collection entries.</param>
    ICollectionPropertyBuilder<TState, TEntry, TKey> WithKey<TKey>(Expression<Func<TEntry, TKey>> keyExpression)
        where TKey : struct;
}

public interface ICollectionPropertyBuilder<TState, TEntry, TKey>
    : IPropertyConfigurationBuilder<TState, IEnumerable<TEntry>> where TState : class where TKey : struct
{
    // GatherFrom overloads that return ICollectionPropertyBuilder
    new ICollectionPropertyBuilder<TState, TEntry, TKey> GatherFrom<TService>(
        Func<TService, IEnumerable<TEntry>> gatherer) where TService : notnull
    {
        ((IPropertyConfigurationBuilder<TState, IEnumerable<TEntry>>)this).GatherFrom(gatherer);
        return this;
    }

    new ICollectionPropertyBuilder<TState, TEntry, TKey> GatherFromAsync<TService>(
        Func<TService, Task<IEnumerable<TEntry>>> gatherer) where TService : notnull
    {
        ((IPropertyConfigurationBuilder<TState, IEnumerable<TEntry>>)this).GatherFromAsync(gatherer);
        return this;
    }

    new ICollectionPropertyBuilder<TState, TEntry, TKey> GatherFromAsync<TService>(
        Func<TService, CancellationToken, Task<IEnumerable<TEntry>>> gatherer) where TService : notnull
    {
        ((IPropertyConfigurationBuilder<TState, IEnumerable<TEntry>>)this).GatherFromAsync(gatherer);
        return this;
    }

    // WithPropertyManager overload
    new ICollectionPropertyBuilder<TState, TEntry, TKey> WithPropertyManager<TPropertyManager>()
        where TPropertyManager : IPropertyManager<IEnumerable<TEntry>>
    {
        ((IPropertyConfigurationBuilder<TState, IEnumerable<TEntry>>)this).WithPropertyManager<TPropertyManager>();
        return this;
    }

    // Emit overloads
    new ICollectionPropertyBuilder<TState, TEntry, TKey> Emit<TEvent>(
        Func<IEnumerable<TEntry>, IEnumerable<TEntry>, TEvent?> eventFactory)
        where TEvent : notnull
    {
        ((IPropertyConfigurationBuilder<TState, IEnumerable<TEntry>>)this).Emit(eventFactory);
        return this;
    }

    new ICollectionPropertyBuilder<TState, TEntry, TKey> Emit<TEvent>(
        Func<IEnumerable<TEntry>, TEvent?> eventFactory)
        where TEvent : notnull
    {
        ((IPropertyConfigurationBuilder<TState, IEnumerable<TEntry>>)this).Emit(eventFactory);
        return this;
    }

    // ScopeBehavior overload
    new ICollectionPropertyBuilder<TState, TEntry, TKey> ScopeBehavior(PropertyGatheringServiceScopeBehavior scopeBehavior)
    {
        ((IPropertyConfigurationBuilder<TState, IEnumerable<TEntry>>)this).ScopeBehavior(scopeBehavior);
        return this;
    }

    ICollectionPropertyBuilder<TState, TEntry, TKey> On<TCommand>(
        Action<TCommand, ICollectionPropertyManager<TEntry, TKey>> handler)
        where TCommand : notnull;

    ICollectionPropertyBuilder<TState, TEntry, TKey> On<TCommand>(Func<TCommand, bool> commandFilter,
        Action<TCommand, ICollectionPropertyManager<TEntry, TKey>> handler)
        where TCommand : notnull;
    
    /// <summary>
    /// Configure an event to be emitted when an entry is added to the collection.
    /// </summary>
    /// <param name="eventFactory">Takes the new entry and returns an event to emit, or null to emit nothing.</param>
    /// <typeparam name="TEvent">The type of event to emit.</typeparam>
    ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnAdd<TEvent>(Func<TEntry, TEvent?> eventFactory) where TEvent : notnull;
    
    /// <summary>
    /// Configure an event to be emitted when an entry is updated in the collection.
    /// </summary>
    /// <param name="eventFactory">Takes the new and old entry values and returns an event to emit, or null to emit nothing.</param>
    /// <typeparam name="TEvent">The type of event to emit.</typeparam>
    ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnUpdate<TEvent>(Func<TEntry, TEntry, TEvent?> eventFactory) where TEvent : notnull;


    /// <summary>
    /// Configure an event to be emitted when an entry is updated in the collection.
    /// </summary>
    /// <param name="eventFactory">Takes the new entry value and returns an event to emit, or null to emit nothing.</param>
    /// <typeparam name="TEvent">The type of event to emit.</typeparam>
    ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnUpdate<TEvent>(Func<TEntry, TEvent?> eventFactory) where TEvent : notnull;
    
    /// <summary>
    /// Configure an event to be emitted when an entry is removed from the collection.
    /// </summary>
    /// <param name="eventFactory">Takes the removed entry and returns an event to emit, or null to emit nothing.</param>
    /// <typeparam name="TEvent">The type of event to emit.</typeparam>
    ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnRemove<TEvent>(Func<TEntry, TEvent?> eventFactory) where TEvent : notnull;
    
    /// <summary>
    /// Configure an event to be emitted when an entry is removed from the collection.
    /// </summary>
    /// <param name="eventFactory">Takes the removed entry and its key and returns an event to emit, or null to emit nothing.</param>
    /// <typeparam name="TEvent">The type of event to emit.</typeparam>
    ICollectionPropertyBuilder<TState, TEntry, TKey> EmitOnRemove<TEvent>(Func<TEntry, TKey, TEvent?> eventFactory) where TEvent : notnull;
    
    /// <summary>
    /// Configures the equality comparer to use for determining if entries are added, updated, or removed.
    /// </summary>
    /// <param name="equalityComparer">The equality comparer to use for comparing entries in the collection.</param>
    /// <returns>>The collection property builder for method chaining.</returns>
    ICollectionPropertyBuilder<TState, TEntry, TKey> WithEntryEqualityComparer(IEqualityComparer<TEntry> equalityComparer);
}