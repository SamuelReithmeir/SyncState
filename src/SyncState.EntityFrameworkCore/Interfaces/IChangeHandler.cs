using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SyncState.EntityFrameworkCore.Interception;

namespace SyncState.EntityFrameworkCore.Interfaces;

public interface IChangeHandler
{
    /// <summary>
    /// general method to determine if this handler can process the given entity type
    /// used for performance optimizations
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    bool HandlesEntityType(Type entityType);
    
    /// <summary>
    /// handle a single entity change after a successful SaveChanges call
    /// </summary>
    /// <param name="entityChangeEntry"></param>
    /// <param name="stateUponSaving"></param>
    /// <param name="changeTracker"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task HandleChangeAsync(EntityChangeEntry entityChangeEntry, EntityState stateUponSaving,ChangeTracker changeTracker, CancellationToken cancellationToken);
}

public interface IChangeHandler<TEntity>: IChangeHandler where TEntity : class
{
    bool IChangeHandler.HandlesEntityType(Type entityType)=>typeof(TEntity).IsAssignableFrom(entityType);
}