using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.EntityFrameworkCore.Aggregates;
using SyncState.EntityFrameworkCore.Aggregates.Commands;
using SyncState.EntityFrameworkCore.Configuration.Models;
using SyncState.EntityFrameworkCore.Interfaces;
using SyncState.Utils;

namespace SyncState.EntityFrameworkCore.Configuration;

public class EfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> :
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey>
    where TState : class where TEntity : class where TKey : struct
{
    private readonly EfCoreSyncStateExtension _extension;
    private readonly IInternalSyncStateBuilder _internalSyncStateBuilder;

    public EfCoreCollectionBuilder(ICollectionPropertyBuilder<TState, TEntry, TKey> collectionBuilder,
        Expression<Func<TEntity, TKey>> keySelector)
    {
        if (collectionBuilder is not IInternalPropertyConfigurationBuilder<TState, IEnumerable<TEntry>> builder)
        {
            throw new InvalidOperationException(
                "Collection builder must be of type IInternalPropertyConfigurationBuilder");
        }

        _internalSyncStateBuilder = builder.GetStateBuilder().GetSyncStateBuilder();
        if (_internalSyncStateBuilder.GetExtension<EfCoreSyncStateExtension>() is not
            { } extension)
        {
            extension = new EfCoreSyncStateExtension();
        }

        _extension = extension;
        _internalSyncStateBuilder.AddExtension(_extension);

        _extension.SetEntityKeySelector(keySelector.Compile());
        _extension.SetFilterFunction<TEntry, TEntity>(_ => true);

        //add commandHandlers
        collectionBuilder.On<AggregateCreatedCommand<TEntry>>((c, cm) => cm.SetEntry(c.Aggregate));
        collectionBuilder.On<AggregateUpdatedCommand<TEntry>>((c, cm) => cm.SetEntry(c.Aggregate));
        collectionBuilder.On<AggregateDeletedCommand<TEntry, TKey>>((c, cm) => cm.RemoveEntry(c.Key));

        //register the change handler for the root entity and the command dispatcher
        _internalSyncStateBuilder.AddServiceCollectionProcessor(services =>
        {
            services.TryAddScopedImplementation<IChangeHandler, AggregateRootChangeHandler<TEntry, TEntity, TKey>>();
            services
                .TryAddScopedImplementation<ICommandDispatcher, AggregateCommandDispatcher<TEntry, TEntity, TKey>>();
            //register IAggregateStateStore if not already registered
            services.TryAddScoped<IAggregateStateStore, AggregateStateStore>();
        });
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithFilter(Expression<Func<TEntity, bool>> filter)
    {
        _extension.SetFilterFunction<TEntry, TEntity>(filter.Compile());
        return this;
    }

    #region Mappings

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithMapping(Func<TEntity, TEntry> mapFunc)
    {
        _extension.SetMappingFunction<TEntry, TEntity>((entity, _, _) => Task.FromResult(mapFunc(entity)));
        return this;
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAsyncMapping(Func<TEntity, Task<TEntry>> mapFunc)
    {
        _extension.SetMappingFunction<TEntry, TEntity>((entity, _, _) => mapFunc(entity));
        return this;
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAsyncMapping(
        Func<TEntity, CancellationToken, Task<TEntry>> mapFunc)
    {
        _extension.SetMappingFunction<TEntry, TEntity>((entity, _, token) => mapFunc(entity, token));
        return this;
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithMapping<TService>(
        Func<TEntity, TService, TEntry> mapFunc) where TService : notnull
    {
        _extension.SetMappingFunction<TEntry, TEntity>((entity, sp, _) =>
            Task.FromResult(mapFunc(entity, sp.GetRequiredService<TService>())));
        return this;
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAsyncMapping<TService>(
        Func<TEntity, TService, Task<TEntry>> mapFunc) where TService : notnull
    {
        _extension.SetMappingFunction<TEntry, TEntity>((entity, sp, _) =>
            mapFunc(entity, sp.GetRequiredService<TService>()));
        return this;
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAsyncMapping<TService>(
        Func<TEntity, TService, CancellationToken, Task<TEntry>> mapFunc) where TService : notnull
    {
        _extension.SetMappingFunction<TEntry, TEntity>((entity, sp, token) =>
            mapFunc(entity, sp.GetRequiredService<TService>(), token));
        return this;
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithDirectAdditionalEntity<TAdditionalEntity>(
        Func<TAdditionalEntity, TKey?> rootKeySelector, Func<EntityEntry<TAdditionalEntity>, bool>? filter = null)
        where TAdditionalEntity : class
    {
        return WithAdditionalEntity(rootKeySelector, OriginalRootKeysSelector, filter);

        IEnumerable<TKey> OriginalRootKeysSelector(EntityEntry<TAdditionalEntity> entry, ChangeTracker changeTracker)
        {
            var originalValue = (TAdditionalEntity)entry.OriginalValues.ToObject();
            if (rootKeySelector(originalValue) is { } key)
            {
                yield return key;
            }
        }
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAdditionalEntity<TAdditionalEntity>(
        Func<TAdditionalEntity, TKey?> rootKeySelector, Func<EntityEntry<TAdditionalEntity>, bool>? filter = null)
        where TAdditionalEntity : class
    {
        return WithAdditionalEntity((Func<TAdditionalEntity, IEnumerable<TKey>>)RootsSelector, null, filter);

        IEnumerable<TKey> RootsSelector(TAdditionalEntity x) =>
            rootKeySelector(x) is { } key ? [key] : Array.Empty<TKey>();
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAdditionalEntity<TAdditionalEntity>(
        Func<TAdditionalEntity, TKey?> rootKeySelector, Func<EntityEntry<TAdditionalEntity>, ChangeTracker, IEnumerable<
            TKey>> originalRootKeysSelector, Func<EntityEntry<TAdditionalEntity>, bool>? filter = null)
        where TAdditionalEntity : class
    {
        return WithAdditionalEntity((Func<TAdditionalEntity, IEnumerable<TKey>>)RootsSelector,
            originalRootKeysSelector, filter);

        IEnumerable<TKey> RootsSelector(TAdditionalEntity x)
        {
            if (rootKeySelector(x) is { } key)
            {
                yield return key;
            }
        }
    }

    public IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAdditionalEntity<TAdditionalEntity>(
        Func<TAdditionalEntity, IEnumerable<TKey>> rootKeysSelector,
        Func<EntityEntry<TAdditionalEntity>, ChangeTracker, IEnumerable<TKey>>? originalRootKeySelector = null,
        Func<EntityEntry<TAdditionalEntity>, bool>? filter = null)
        where TAdditionalEntity : class
    {
        _extension.SetAggregateRootsSelector<TEntry, TEntity, TKey, TAdditionalEntity>(RootKeysSelector);
        if (filter is not null)
        {
            _extension.SetAggregateParticipantUpdateFilter<TEntry, TEntity, TAdditionalEntity>(filter);
        }

        //register the change handler for the additional entity
        _internalSyncStateBuilder.AddServiceCollectionProcessor(services =>
        {
            services
                .TryAddScopedImplementation<IChangeHandler,
                    AggregateParticipantChangeHandler<TEntry, TEntity, TAdditionalEntity, TKey>>();
        });
        return this;

        IEnumerable<TKey> RootKeysSelector(TAdditionalEntity x, EntityEntry<TAdditionalEntity> entry,
            ChangeTracker changeTracker)
        {
            foreach (var key in rootKeysSelector(x))
            {
                yield return key;
            }

            if (originalRootKeySelector is not null)
            {
                foreach (var key in originalRootKeySelector(entry, changeTracker))
                {
                    yield return key;
                }
            }
        }
    }

    #endregion
}