using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SyncState.EntityFrameworkCore.Configuration.Models;
using SyncState.EntityFrameworkCore.Interception;
using SyncState.EntityFrameworkCore.Interfaces;
using SyncState.Models.Configuration;

namespace SyncState.EntityFrameworkCore.Aggregates;

public class
    AggregateNonRootChangeHandler<TAggregate, TAggregateRoot, TParticipant, TKey> : IChangeHandler<TParticipant>
    where TParticipant : class where TKey : struct where TAggregateRoot : class
{
    private readonly IAggregateStateStore _aggregateStateStore;
    private readonly EfCoreSyncStateExtension _configuration;

    public AggregateNonRootChangeHandler(IAggregateStateStore aggregateStateStore, SyncStateConfiguration configuration)
    {
        _aggregateStateStore = aggregateStateStore;
        if (configuration.GetExtension<EfCoreSyncStateExtension>() is not { } extension)
        {
            throw new InvalidOperationException("EfCoreSyncStateExtension must be registered");
        }

        _configuration = extension;
    }

    public Task HandleChangeAsync(EntityChangeEntry entityChangeEntry, EntityState stateUponSaving,
        ChangeTracker changeTracker,
        CancellationToken cancellationToken)
    {
        if (entityChangeEntry.Entry.Entity is not TParticipant participant ||
            stateUponSaving is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            return Task.CompletedTask;
        }

        var typedEntry = entityChangeEntry.Entry.Context.Entry((TParticipant)entityChangeEntry.Entry.Entity);

        var rootsSelector = _configuration.GetAggregateRootsSelector<TAggregate, TAggregateRoot, TKey, TParticipant>();
        var aggregateRootKeys = rootsSelector(participant, typedEntry, changeTracker);
        foreach (var rootKey in aggregateRootKeys.Distinct())
        {
            if (changeTracker.Context.Find<TAggregateRoot>(rootKey) is not { } rootEntity)
            {
                continue;
            }

            _aggregateStateStore.SetAggregateState<TAggregate, TAggregateRoot, TKey>(rootEntity, rootKey,
                AggregateState.AggregateParticipantChanged);
        }

        return Task.CompletedTask;
    }
}