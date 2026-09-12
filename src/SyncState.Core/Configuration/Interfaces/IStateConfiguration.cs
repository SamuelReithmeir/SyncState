namespace SyncState.Configuration.Interfaces;

/// <summary>
/// Marker for state configuration classes. Configuration classes implement <see cref="IStateConfiguration{TState}"/>.
/// </summary>
public interface IStateConfiguration
{
}

/// <summary>
/// Configuration of a state type.
/// Registered with <see cref="ISyncStateBuilder.AddStateConfiguration{TConfiguration}"/>
/// or discovered by <see cref="ISyncStateBuilder.AddConfigurationsFromAssembly"/>.
/// </summary>
/// <typeparam name="TState">The type of state being configured.</typeparam>
public interface IStateConfiguration<TState> : IStateConfiguration where TState : class
{
    /// <summary>
    /// Configures the state type. Public properties of <typeparamref name="TState"/> left unconfigured receive
    /// a registered property configuration matching their type.
    /// </summary>
    /// <param name="builder">The state configuration builder.</param>
    void Configure(IStateConfigurationBuilder<TState> builder);
}
