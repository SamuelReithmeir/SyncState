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
    ISyncStateBuilder AddState<TState>(Action<IStateConfigurationBuilder<TState>> configure) where TState : class;
}