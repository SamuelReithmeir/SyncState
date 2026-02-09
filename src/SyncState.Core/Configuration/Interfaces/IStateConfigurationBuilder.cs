using System.Linq.Expressions;

namespace SyncState.Configuration.Interfaces;

/// <summary>
/// Builder for configuring a state type in the SyncState system.
/// </summary>
/// <typeparam name="TState">The type of state being configured.</typeparam>
public interface IStateConfigurationBuilder<TState> where TState : class
{
    /// <summary>
    /// Configures a simple property on the state object.
    /// </summary>
    /// <param name="propertyExpression">Expression selecting the property to configure.</param>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <returns>A property configuration builder for further configuration.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> Property<TProperty>(
        Expression<Func<TState, TProperty>> propertyExpression);

    /// <summary>
    /// Configures a collection property on the state object (requires specifying a key selector).
    /// </summary>
    /// <param name="collectionExpression">Expression selecting the collection property to configure.</param>
    /// <typeparam name="TEntry">The type of entries in the collection.</typeparam>
    /// <returns>A partial collection property builder that requires a key selector.</returns>
    IPartialCollectionPropertyBuilder<TState, TEntry> Collection<TEntry>(
        Expression<Func<TState, IEnumerable<TEntry>>> collectionExpression);

    /// <summary>
    /// Configures a collection property on the state object with a key selector.
    /// </summary>
    /// <param name="collectionExpression">Expression selecting the collection property to configure.</param>
    /// <param name="keySelector">Expression selecting the key property for collection entries.</param>
    /// <typeparam name="TEntry">The type of entries in the collection.</typeparam>
    /// <typeparam name="TKey">The type of the key used to identify entries.</typeparam>
    /// <returns>A collection property builder for further configuration.</returns>
    ICollectionPropertyBuilder<TState, TEntry, TKey> Collection<TEntry, TKey>(
        Expression<Func<TState, IEnumerable<TEntry>>> collectionExpression, Expression<Func<TEntry, TKey>> keySelector) where TKey : struct;

    /// <summary>
    /// Configures a custom state manager to handle this state type.
    /// </summary>
    /// <typeparam name="TStateManager">The type of state manager to use.</typeparam>
    /// <returns>The state configuration builder for method chaining.</returns>
    IStateConfigurationBuilder<TState> WithStateManager<TStateManager>()
        where TStateManager : class;
}