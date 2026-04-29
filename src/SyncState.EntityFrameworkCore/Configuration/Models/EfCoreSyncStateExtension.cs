using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SyncState.EntityFrameworkCore.Configuration.Models;

public record EfCoreSyncStateExtension
{
    public bool WaitForTransactionCommit { get; init; } = false;
    public Dictionary<Type, Delegate> EntityKeySelectors { get; init; } = [];
    public Dictionary<Type, Delegate> AggregateRootEntityFilters { get; init; } = [];

    public Dictionary<(Type aggregateType, Type participantType), Delegate> AggregateParticipantRootsSelectors
    {
        get;
        init;
    } = [];

    public Dictionary<(Type aggregateType, Type participantType), Delegate> AggregateParticipantUpdateFilters
    {
        get;
        init;
    } = [];

    public Dictionary<Type, Delegate> MappingFunctions { get; init; } = [];
    public HashSet<Type> ConfiguredDbContextTypes { get; init; } = [];

    #region Accessors

    public Func<TEntity, TKey> GetEntityKeySelector<TEntity, TKey>()
        where TEntity : class where TKey : struct
    {
        var type = typeof(TEntity);
        if (EntityKeySelectors.TryGetValue(type, out var del))
        {
            return (Func<TEntity, TKey>)del;
        }

        throw new InvalidOperationException($"No key selector registered for entity type {type.FullName}");
    }

    public void SetEntityKeySelector<TEntity, TKey>(Func<TEntity, TKey> keySelector)
        where TEntity : class where TKey : struct
    {
        var type = typeof(TEntity);
        EntityKeySelectors[type] = keySelector;
    }

    public Func<TParticipant, EntityEntry<TParticipant>, ChangeTracker, IEnumerable<TKey>> GetAggregateRootsSelector<
        TAggregate, TAggregateRoot, TKey,
        TParticipant>() where TParticipant : class
    {
        if (AggregateParticipantRootsSelectors.TryGetValue((typeof(TAggregate), typeof(TParticipant)), out var del))
        {
            return (Func<TParticipant, EntityEntry<TParticipant>, ChangeTracker, IEnumerable<TKey>>)del;
        }

        throw new InvalidOperationException(
            $"No aggregate participant roots selector registered for aggregate type {typeof(TAggregate).FullName} and participant type {typeof(TParticipant).FullName}");
    }

    public void SetAggregateRootsSelector<TAggregate, TAggregateRoot, TKey, TParticipant>(
        Func<TParticipant, EntityEntry<TParticipant>, ChangeTracker, IEnumerable<TKey>> rootsSelector)
        where TParticipant : class
    {
        AggregateParticipantRootsSelectors[(typeof(TAggregate), typeof(TParticipant))] = rootsSelector;
    }

    public Func<EntityEntry<TParticipant>, bool>? GetAggregateParticipantUpdateFilter<
        TAggregate, TAggregateRoot, TParticipant>() where TParticipant : class
    {
        if (AggregateParticipantUpdateFilters.TryGetValue((typeof(TAggregate), typeof(TParticipant)), out var del))
        {
            return (Func<EntityEntry<TParticipant>, bool>)del;
        }

        return null;
    }

    public void SetAggregateParticipantUpdateFilter<TAggregate, TAggregateRoot, TParticipant>(
        Func<EntityEntry<TParticipant>, bool> updateFilter)
        where TParticipant : class
    {
        AggregateParticipantUpdateFilters[(typeof(TAggregate), typeof(TParticipant))] = updateFilter;
    }

    public Func<TAggregateRoot, IServiceProvider, CancellationToken, Task<TAggregate>> GetMappingFunction<TAggregate,
        TAggregateRoot>()
    {
        var type = typeof(TAggregate);
        if (MappingFunctions.TryGetValue(type, out var del))
        {
            return (Func<TAggregateRoot, IServiceProvider, CancellationToken, Task<TAggregate>>)del;
        }

        throw new InvalidOperationException($"No mapping function registered for aggregate type {type.FullName}");
    }

    public void SetMappingFunction<TAggregate, TAggregateRoot>(
        Func<TAggregateRoot, IServiceProvider, CancellationToken, Task<TAggregate>> mappingFunction)
    {
        var type = typeof(TAggregate);
        MappingFunctions[type] = mappingFunction;
    }

    public Func<TAggregateRoot, bool> GetFilterFunction<TAggregate, TAggregateRoot>()
    {
        var type = typeof(TAggregate);
        if (AggregateRootEntityFilters.TryGetValue(type, out var del))
        {
            return (Func<TAggregateRoot, bool>)del;
        }

        throw new InvalidOperationException($"No mapping function registered for aggregate type {type.FullName}");
    }

    public void SetFilterFunction<TAggregate, TAggregateRoot>(Func<TAggregateRoot, bool> filterFunction)
    {
        var type = typeof(TAggregate);
        AggregateRootEntityFilters[type] = filterFunction;
    }

    #endregion
}