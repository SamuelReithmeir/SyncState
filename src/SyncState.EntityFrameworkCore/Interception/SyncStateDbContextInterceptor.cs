using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SyncState.EntityFrameworkCore.Configuration.Models;
using SyncState.EntityFrameworkCore.Interfaces;
using SyncState.Models.Configuration;

namespace SyncState.EntityFrameworkCore.Interception;

public class SyncStateDbContextInterceptor : ISaveChangesInterceptor, IDbTransactionInterceptor
{
    private readonly Dictionary<Type, HashSet<EntityChangeEntry>> _entityChangeEntriesByDbContextType = [];
    private readonly Dictionary<Type, IEnumerable<IChangeHandler>> _changeHandlersByEntityType = [];
    private EfCoreSyncStateExtension _configuration;
    private readonly IEnumerable<IChangeHandler> _changeHandlers;
    private readonly IEnumerable<ICommandDispatcher> _commandDispatchers;

    public SyncStateDbContextInterceptor(IEnumerable<IChangeHandler> changeHandlers,
        SyncStateConfiguration configuration, IEnumerable<ICommandDispatcher> commandDispatchers)
    {
        _changeHandlers = changeHandlers;
        _commandDispatchers = commandDispatchers;
        if (configuration.GetExtension<EfCoreSyncStateExtension>() is not { } extension)
        {
            throw new InvalidOperationException("EfCoreSyncStateExtension must be registered");
        }

        _configuration = extension;
    }

    private void HandleSavingChanges(DbContextEventData eventData)
    {
        if (eventData.Context is null || !_configuration.ConfiguredDbContextTypes.Contains(eventData.Context.GetType()))
        {
            // If the context is null or not configured
            return;
        }

        var contextType = eventData.Context.GetType();
        var changeTracker = eventData.Context.ChangeTracker;

        var entityChangeEntries = changeTracker.Entries()
            .Select(entityEntry => new EntityChangeEntry { Entry = entityEntry, State = entityEntry.State })
            .ToHashSet();

        _entityChangeEntriesByDbContextType[contextType] = entityChangeEntries;
    }

    private async Task HandleSavedChangesAsync(SaveChangesCompletedEventData eventData,
        CancellationToken cancellationToken)
    {
        if (eventData.Context is null || !_configuration.ConfiguredDbContextTypes.Contains(eventData.Context.GetType()))
        {
            // If the context is null or not configured
            return;
        }

        var contextType = eventData.Context.GetType();
        
        if (!_entityChangeEntriesByDbContextType.TryGetValue(contextType, out var entityChangeEntries))
        {
            return;
        }

        foreach (var entityChangeEntry in entityChangeEntries)
        {
            foreach (var handler in GetChangeHandlersForType(entityChangeEntry.Entry.Entity.GetType()))
            {
                await handler.HandleChangeAsync(entityChangeEntry, entityChangeEntry.State, eventData.Context.ChangeTracker, cancellationToken);
            }
        }

        if (!_configuration.WaitForTransactionCommit || eventData.Context.Database.CurrentTransaction is null)
        {
            foreach (var dispatcher in _commandDispatchers)
            {
                await dispatcher.DispatchAsync(cancellationToken);
            }
        }

        // Clean up after processing
        _entityChangeEntriesByDbContextType.Remove(contextType);
    }

    private async Task HandleTransactionCommitAsync(CancellationToken cancellationToken)
    {
        if (_configuration.WaitForTransactionCommit)
        {
            foreach (var dispatcher in _commandDispatchers)
            {
                await dispatcher.DispatchAsync(cancellationToken);
            }
        }
    }

    private Task HandleSavedChangesFailedAsync()
    {
        _entityChangeEntriesByDbContextType.Clear();
        return Task.CompletedTask;
    }

    private IEnumerable<IChangeHandler> GetChangeHandlersForType(Type entityType)
    {
        if (_changeHandlersByEntityType.TryGetValue(entityType, out var handlers))
        {
            return handlers;
        }

        handlers = _changeHandlers
            .Where(handler => handler.HandlesEntityType(entityType))
            .ToList();

        _changeHandlersByEntityType[entityType] = handlers;
        return handlers;
    }

    #region InterceptorImlementations

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        HandleSavingChanges(eventData);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new())
    {
        HandleSavingChanges(eventData);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        HandleSavedChangesAsync(eventData, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = new())
    {
        await HandleSavedChangesAsync(eventData, cancellationToken);
        return result;
    }

    public void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        HandleSavedChangesFailedAsync().GetAwaiter().GetResult();
    }

    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
    {
        HandleTransactionCommitAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = new())
    {
        return HandleTransactionCommitAsync(cancellationToken);
    }

    #endregion
}