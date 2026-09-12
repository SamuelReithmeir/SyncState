using System.Reflection;

namespace SyncState.Configuration.Interfaces;

/// <summary>
/// Builder for configuring the SyncState system.
/// </summary>
public interface ISyncStateBuilder
{
    /// <summary>
    /// Adds a state type to the SyncState system.
    /// </summary>
    /// <param name="configure">Configuration action for the state type.</param>
    /// <typeparam name="TState">The type of state to add.</typeparam>
    /// <returns>The SyncState builder for method chaining.</returns>
    /// <exception cref="InvalidOperationException">The state type was already added.</exception>
    ISyncStateBuilder AddState<TState>(Action<IStateConfigurationBuilder<TState>> configure) where TState : class;

    /// <summary>
    /// Adds the state types configured by <typeparamref name="TConfiguration"/>. A configuration class implementing
    /// <see cref="IStateConfiguration{TState}"/> for several state types adds each of them.
    /// </summary>
    /// <typeparam name="TConfiguration">The state configuration class.</typeparam>
    /// <returns>The SyncState builder for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TConfiguration"/> implements no <see cref="IStateConfiguration{TState}"/>, or a state type was already added.
    /// </exception>
    ISyncStateBuilder AddStateConfiguration<TConfiguration>() where TConfiguration : class, IStateConfiguration, new();

    /// <summary>
    /// Registers a property configuration for automatic assignment. A public property of a state that is not configured
    /// by the state's configuration receives the registered configuration matching its type, in this order of precedence:
    /// an <see cref="IPropertyConfiguration{TProperty}"/> whose <c>TProperty</c> equals the property type,
    /// an <see cref="ICollectionPropertyConfiguration{TEntry,TKey}"/> whose <c>IEnumerable&lt;TEntry&gt;</c> the property type implements,
    /// an <see cref="ICollectionPropertyConfiguration{TEntry,TKey}"/> whose <c>IEnumerable&lt;TEntry&gt;</c> the property type is assignable to.
    /// Registering the same configuration class twice has no effect. Several registered configurations matching one
    /// property with the same precedence make the build of the SyncState configuration throw.
    /// </summary>
    /// <typeparam name="TConfiguration">The property configuration class.</typeparam>
    /// <returns>The SyncState builder for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// <typeparamref name="TConfiguration"/> implements neither <see cref="IPropertyConfiguration{TProperty}"/>
    /// nor <see cref="ICollectionPropertyConfiguration{TEntry,TKey}"/>.
    /// </exception>
    ISyncStateBuilder AddPropertyConfiguration<TConfiguration>() where TConfiguration : class, IPropertyConfiguration, new();

    /// <summary>
    /// Adds every non-abstract, non-generic class of <paramref name="assembly"/> that implements
    /// <see cref="IStateConfiguration{TState}"/> (see <see cref="AddStateConfiguration{TConfiguration}"/>),
    /// <see cref="IPropertyConfiguration{TProperty}"/> or <see cref="ICollectionPropertyConfiguration{TEntry,TKey}"/>
    /// (see <see cref="AddPropertyConfiguration{TConfiguration}"/>). Configuration classes are instantiated through
    /// their public parameterless constructor.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="predicate">Optional filter to restrict which configuration classes are added.</param>
    /// <returns>The SyncState builder for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// A matching configuration class has no public parameterless constructor, or a state type was already added.
    /// </exception>
    ISyncStateBuilder AddConfigurationsFromAssembly(Assembly assembly, Func<Type, bool>? predicate = null);

    /// <summary>
    /// Adds every configuration class of the assembly that contains <typeparamref name="TMarker"/>.
    /// See <see cref="AddConfigurationsFromAssembly"/>.
    /// </summary>
    /// <param name="predicate">Optional filter to restrict which configuration classes are added.</param>
    /// <typeparam name="TMarker">Any type of the assembly to scan.</typeparam>
    /// <returns>The SyncState builder for method chaining.</returns>
    ISyncStateBuilder AddConfigurationsFromAssemblyContaining<TMarker>(Func<Type, bool>? predicate = null)
    {
        return AddConfigurationsFromAssembly(typeof(TMarker).Assembly, predicate);
    }
}
