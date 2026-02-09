using SyncState.EntityFrameworkCore.Aggregates.Commands;
using SyncState.EntityFrameworkCore.Configuration.Models;
using SyncState.EntityFrameworkCore.Interfaces;
using SyncState.Interfaces;
using SyncState.Models.Configuration;

namespace SyncState.EntityFrameworkCore.Aggregates;

public class AggregateCommandDispatcher<TAggregate, TAggregateRoot, TKey> : ICommandDispatcher where TKey : struct
{
    private readonly IAggregateStateStore _aggregateStateStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISyncCommandService _syncCommandService;
    private readonly Func<TAggregateRoot,IServiceProvider,CancellationToken,Task<TAggregate>> _mappingFunc;
    private readonly Func<TAggregateRoot,bool> _filterFunc;

    public AggregateCommandDispatcher(IAggregateStateStore aggregateStateStore, IServiceProvider serviceProvider,
        ISyncCommandService syncCommandService, SyncStateConfiguration configuration)
    {
        _aggregateStateStore = aggregateStateStore;
        _serviceProvider = serviceProvider;
        _syncCommandService = syncCommandService;
        if (configuration.GetExtension<EfCoreSyncStateExtension>() is not { } extension)
        {
            throw new InvalidOperationException("EfCoreSyncStateExtension must be registered");
        }
        _mappingFunc = extension.GetMappingFunction<TAggregate, TAggregateRoot>();
        _filterFunc = extension.GetFilterFunction<TAggregate, TAggregateRoot>();
    }

    public async Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (aggregateRoot, key, state) in _aggregateStateStore
                     .GetAggregateRoots<TAggregate, TAggregateRoot, TKey>())
        {
            if (state == AggregateState.Deleted|| !_filterFunc(aggregateRoot))
            {
                await _syncCommandService.HandleAsync(new AggregateDeletedCommand<TAggregate, TKey>
                {
                    Key = key
                }, cancellationToken);
                continue;
            }
            var aggregate = await _mappingFunc(aggregateRoot, _serviceProvider, cancellationToken);
            if (state == AggregateState.Added)
            {
                await _syncCommandService.HandleAsync(new AggregateCreatedCommand<TAggregate>
                {
                    Aggregate = aggregate
                }, cancellationToken);
                continue;
            }
            await _syncCommandService.HandleAsync(new AggregateUpdatedCommand<TAggregate>
            {
                Aggregate = aggregate
            }, cancellationToken);
        }
        _aggregateStateStore.ClearAggregateStates<TAggregate>();
    }
}