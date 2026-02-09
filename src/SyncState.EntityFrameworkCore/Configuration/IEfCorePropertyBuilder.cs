using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SyncState.EntityFrameworkCore.Configuration;

/// <summary>
/// Intermediate builder for EF Core collection properties that requires specifying the root entity type.
/// </summary>
/// <typeparam name="TState">The type of state containing the collection.</typeparam>
/// <typeparam name="TEntry">The type of entries in the collection (DTO).</typeparam>
/// <typeparam name="TKey">The type of the key used to identify entries.</typeparam>
public interface IPartialEfCoreCollectionBuilder<TState, TEntry, TKey> where TState : class where TKey : struct
{
    /// <summary>
    /// Specifies the root entity type and its key selector for the EF Core collection.
    /// </summary>
    /// <param name="keySelector">Expression selecting the key property on the entity.</param>
    /// <typeparam name="TEntity">The type of the root entity in the database.</typeparam>
    /// <returns>An EF Core collection builder for further configuration.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> FromEntity<TEntity>(
        Expression<Func<TEntity, TKey>> keySelector)
        where TEntity : class;
}

/// <summary>
/// Builder for configuring EF Core-backed collection properties.
/// </summary>
/// <typeparam name="TState">The type of state containing the collection.</typeparam>
/// <typeparam name="TEntry">The type of entries in the collection (DTO).</typeparam>
/// <typeparam name="TEntity">The type of the root entity in the database.</typeparam>
/// <typeparam name="TKey">The type of the key used to identify entries.</typeparam>
public interface IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey>
    where TState : class where TEntity : class where TKey : struct
{
    /// <summary>
    /// Configures a filter to apply to the root entities.
    /// </summary>
    /// <param name="filter">Expression defining the filter predicate.</param>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithFilter(Expression<Func<TEntity, bool>> filter);
    
    /// <summary>
    /// Configures a synchronous mapping function from entity to DTO.
    /// </summary>
    /// <param name="mapFunc">Function to map from entity to DTO.</param>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithMapping(Func<TEntity, TEntry> mapFunc);
    
    /// <summary>
    /// Configures an asynchronous mapping function from entity to DTO.
    /// </summary>
    /// <param name="mapFunc">Async function to map from entity to DTO.</param>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAsyncMapping(Func<TEntity, Task<TEntry>> mapFunc);

    /// <summary>
    /// Configures an asynchronous mapping function with cancellation support from entity to DTO.
    /// </summary>
    /// <param name="mapFunc">Async function to map from entity to DTO.</param>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAsyncMapping(
        Func<TEntity, CancellationToken, Task<TEntry>> mapFunc);

    /// <summary>
    /// Configures a synchronous mapping function from entity to DTO with dependency injection support.
    /// </summary>
    /// <param name="mapFunc">Function to map from entity to DTO using a service.</param>
    /// <typeparam name="TService">The type of service to resolve from the DI container.</typeparam>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithMapping<TService>(
        Func<TEntity, TService, TEntry> mapFunc) where TService : notnull;

    /// <summary>
    /// Configures an asynchronous mapping function from entity to DTO with dependency injection support.
    /// </summary>
    /// <param name="mapFunc">Async function to map from entity to DTO using a service.</param>
    /// <typeparam name="TService">The type of service to resolve from the DI container.</typeparam>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAsyncMapping<TService>(
        Func<TEntity, TService, Task<TEntry>> mapFunc) where TService : notnull;

    /// <summary>
    /// Configures an asynchronous mapping function with cancellation support from entity to DTO with dependency injection support.
    /// </summary>
    /// <param name="mapFunc">Async function to map from entity to DTO using a service.</param>
    /// <typeparam name="TService">The type of service to resolve from the DI container.</typeparam>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAsyncMapping<TService>(
        Func<TEntity, TService, CancellationToken, Task<TEntry>> mapFunc) where TService : notnull;

    /// <summary>
    /// Adds a secondary entity that participates in the DTO mapping with a direct foreign key reference to the root entity.
    /// </summary>
    /// <param name="rootKeySelector">Function that selects the root key from the additional entity. Should directly access a property as it will be called on EF Core OriginalValues.</param>
    /// <typeparam name="TAdditionalEntity">The type of the additional entity.</typeparam>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    /// <remarks>
    /// The rootKeySelector should directly access a property on the additional entity as it will be called on EF Core OriginalValues.
    /// </remarks>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithDirectAdditionalEntity<TAdditionalEntity>(
        Func<TAdditionalEntity, TKey?> rootKeySelector) where TAdditionalEntity : class;

    /// <summary>
    /// Adds a secondary entity that participates in the DTO mapping with a reference to the root entity.
    /// </summary>
    /// <param name="rootKeySelector">Function that selects the root key from the additional entity.</param>
    /// <typeparam name="TAdditionalEntity">The type of the additional entity.</typeparam>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAdditionalEntity<TAdditionalEntity>(
        Func<TAdditionalEntity, TKey?> rootKeySelector) where TAdditionalEntity : class;

    /// <summary>
    /// Adds a secondary entity that participates in the DTO mapping with references to multiple root entities.
    /// </summary>
    /// <param name="rootKeysSelector">Function that selects the root keys from the additional entity.</param>
    /// <param name="originalRootKeysSelector">Optional function to retrieve the original root keys before changes. If null, defaults to using the rootKeysSelector.</param>
    /// <typeparam name="TAdditionalEntity">The type of the additional entity.</typeparam>
    /// <returns>The EF Core collection builder for method chaining.</returns>
    IEfCoreCollectionBuilder<TState, TEntry, TEntity, TKey> WithAdditionalEntity<TAdditionalEntity>(
        Func<TAdditionalEntity, IEnumerable<TKey>> rootKeysSelector,
        Func<EntityEntry<TAdditionalEntity>, ChangeTracker, IEnumerable<TKey>>? originalRootKeysSelector = null)
        where TAdditionalEntity : class;
}