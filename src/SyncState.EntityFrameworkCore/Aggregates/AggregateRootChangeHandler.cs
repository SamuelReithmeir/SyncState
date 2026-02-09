using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SyncState.EntityFrameworkCore.Configuration.Models;
using SyncState.EntityFrameworkCore.Interception;
using SyncState.EntityFrameworkCore.Interfaces;
using SyncState.Models.Configuration;

namespace SyncState.EntityFrameworkCore.Aggregates;

public class AggregateRootChangeHandler<TAggregate, TAggregateRoot, TKey> : IChangeHandler<TAggregateRoot>
    where TAggregateRoot : class where TKey : struct
{
    private readonly IAggregateStateStore _aggregateStateStore;
    private readonly EfCoreSyncStateExtension _configuration;

    public AggregateRootChangeHandler(IAggregateStateStore aggregateStateStore, SyncStateConfiguration configuration)
    {
        _aggregateStateStore = aggregateStateStore;
        if (configuration.GetExtension<EfCoreSyncStateExtension>() is not { } extension)
        {
            throw new InvalidOperationException("EfCoreSyncStateExtension must be registered");
        }

        _configuration = extension;
    }

    public Task HandleChangeAsync(EntityChangeEntry entityChangeEntry, EntityState stateUponSaving,
        ChangeTracker changeTracker, CancellationToken cancellationToken)
    {
        if (entityChangeEntry.Entry.Entity is not TAggregateRoot rootEntity)
        {
            return Task.CompletedTask;
        }

        var keySelector = (Func<TAggregateRoot, TKey>)_configuration.EntityKeySelectors[typeof(TAggregateRoot)];
        switch (stateUponSaving)
        {
            case EntityState.Added:
                _aggregateStateStore.SetAggregateState<TAggregate, TAggregateRoot, TKey>(rootEntity,
                    keySelector(rootEntity), AggregateState.Added);
                break;
            case EntityState.Modified:
                _aggregateStateStore.SetAggregateState<TAggregate, TAggregateRoot, TKey>(rootEntity,
                    keySelector(rootEntity), AggregateState.Updated);
                break;
            case EntityState.Deleted:
                _aggregateStateStore.SetAggregateState<TAggregate, TAggregateRoot, TKey>(rootEntity,
                    keySelector(rootEntity), AggregateState.Deleted);
                break;
        }
        
        return Task.CompletedTask;
    }
}