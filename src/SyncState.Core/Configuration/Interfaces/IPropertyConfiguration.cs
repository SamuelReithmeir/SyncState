using System.Linq.Expressions;

namespace SyncState.Configuration.Interfaces;

/// <summary>
/// Marker for property configuration classes. Configuration classes implement
/// <see cref="IPropertyConfiguration{TProperty}"/> or <see cref="ICollectionPropertyConfiguration{TEntry,TKey}"/>.
/// </summary>
public interface IPropertyConfiguration
{
}

/// <summary>
/// Configuration of a property of type <typeparamref name="TProperty"/>, independent of the state it belongs to.
/// It is applied to a property with <c>ApplyConfiguration</c> on the property builder. When registered with
/// <see cref="ISyncStateBuilder.AddPropertyConfiguration{TConfiguration}"/>, it is assigned to every public state
/// property of type <typeparamref name="TProperty"/> that has no explicit configuration.
/// </summary>
/// <typeparam name="TProperty">The type of the property being configured.</typeparam>
public interface IPropertyConfiguration<TProperty> : IPropertyConfiguration
{
    /// <summary>
    /// Configures the property.
    /// </summary>
    /// <param name="builder">The property configuration builder of the property this configuration is applied to.</param>
    /// <typeparam name="TState">The type of state the property belongs to.</typeparam>
    void Configure<TState>(IPropertyConfigurationBuilder<TState, TProperty> builder) where TState : class;
}

/// <summary>
/// Configuration of a collection property with entries of type <typeparamref name="TEntry"/> keyed by
/// <typeparamref name="TKey"/>, independent of the state it belongs to.
/// It is applied to a collection property with <c>ApplyConfiguration</c> on the collection builder. When registered with
/// <see cref="ISyncStateBuilder.AddPropertyConfiguration{TConfiguration}"/>, it is assigned to every public state
/// property assignable to <see cref="IEnumerable{T}"/> of <typeparamref name="TEntry"/> that has no explicit configuration.
/// </summary>
/// <typeparam name="TEntry">The type of entries in the collection.</typeparam>
/// <typeparam name="TKey">The type of the key used to identify entries.</typeparam>
public interface ICollectionPropertyConfiguration<TEntry, TKey> : IPropertyConfiguration where TKey : struct
{
    /// <summary>
    /// Selects the key that identifies an entry of the collection. It is used when the configuration is assigned
    /// automatically to an unconfigured property; an explicitly configured collection keeps the key selector of <c>WithKey</c>.
    /// </summary>
    Expression<Func<TEntry, TKey>> KeySelector { get; }

    /// <summary>
    /// Configures the collection property.
    /// </summary>
    /// <param name="builder">The collection property builder of the property this configuration is applied to.</param>
    /// <typeparam name="TState">The type of state the collection property belongs to.</typeparam>
    void Configure<TState>(ICollectionPropertyBuilder<TState, TEntry, TKey> builder) where TState : class;
}
