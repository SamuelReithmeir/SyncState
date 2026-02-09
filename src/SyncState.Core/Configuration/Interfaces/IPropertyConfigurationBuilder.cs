using SyncState.Enums;
using SyncState.Interfaces.Managers;

namespace SyncState.Configuration.Interfaces;

public interface IPropertyConfigurationBuilder<TState, TProperty> where TState : class
{
    /// <summary>
    /// Configures a synchronous gatherer function to retrieve the property value from a service.
    /// </summary>
    /// <param name="gatherer">Function that retrieves the property value from the service.</param>
    /// <typeparam name="TService">The type of service to resolve from the DI container.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> GatherFrom<TService>(
        Func<TService, TProperty> gatherer) where TService : notnull;

    /// <summary>
    /// Configures an asynchronous gatherer function to retrieve the property value from a service.
    /// </summary>
    /// <param name="gatherer">Async function that retrieves the property value from the service.</param>
    /// <typeparam name="TService">The type of service to resolve from the DI container.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> GatherFromAsync<TService>(
        Func<TService, Task<TProperty>> gatherer) where TService : notnull;

    /// <summary>
    /// Configures an asynchronous gatherer function with cancellation support to retrieve the property value from a service.
    /// </summary>
    /// <param name="gatherer">Async function that retrieves the property value from the service.</param>
    /// <typeparam name="TService">The type of service to resolve from the DI container.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> GatherFromAsync<TService>(
        Func<TService, CancellationToken, Task<TProperty>> gatherer) where TService : notnull;

    /// <summary>
    /// Configures a custom property manager to handle this property.
    /// </summary>
    /// <typeparam name="TPropertyManager">The type of property manager to use.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> WithPropertyManager<TPropertyManager>()
        where TPropertyManager : IPropertyManager<TProperty>;

    /// <summary>
    /// Registers a synchronous command handler for this property.
    /// </summary>
    /// <param name="handler">The handler to invoke when the command is received.</param>
    /// <typeparam name="TCommand">The type of command to handle.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Action<TCommand, IPropertyManager<TProperty>> handler) where TCommand : notnull;
    
    /// <summary>
    /// Registers an asynchronous command handler for this property.
    /// </summary>
    /// <param name="handler">The async handler to invoke when the command is received.</param>
    /// <typeparam name="TCommand">The type of command to handle.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Func<TCommand, IPropertyManager<TProperty>, Task> handler) where TCommand : notnull;
    
    /// <summary>
    /// Registers an asynchronous command handler with cancellation support for this property.
    /// </summary>
    /// <param name="handler">The async handler to invoke when the command is received.</param>
    /// <typeparam name="TCommand">The type of command to handle.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Func<TCommand, IPropertyManager<TProperty>, CancellationToken, Task> handler) where TCommand : notnull;

    /// <summary>
    /// Registers a filtered synchronous command handler for this property.
    /// </summary>
    /// <param name="commandFilter">Predicate to determine if the command should be handled.</param>
    /// <param name="handler">The handler to invoke when the command passes the filter.</param>
    /// <typeparam name="TCommand">The type of command to handle.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Func<TCommand, bool> commandFilter, Action<TCommand, IPropertyManager<TProperty>> handler) where TCommand : notnull;
    
    /// <summary>
    /// Registers a filtered asynchronous command handler for this property.
    /// </summary>
    /// <param name="commandFilter">Predicate to determine if the command should be handled.</param>
    /// <param name="handler">The async handler to invoke when the command passes the filter.</param>
    /// <typeparam name="TCommand">The type of command to handle.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Func<TCommand, bool> commandFilter, Func<TCommand, IPropertyManager<TProperty>, Task> handler) where TCommand : notnull;
    
    /// <summary>
    /// Registers a filtered asynchronous command handler with cancellation support for this property.
    /// </summary>
    /// <param name="commandFilter">Predicate to determine if the command should be handled.</param>
    /// <param name="handler">The async handler to invoke when the command passes the filter.</param>
    /// <typeparam name="TCommand">The type of command to handle.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Func<TCommand, bool> commandFilter, Func<TCommand, IPropertyManager<TProperty>, CancellationToken, Task> handler) where TCommand : notnull;

    /// <summary>
    /// Configure an event to be emitted when the property value changes.
    /// </summary>
    /// <param name="eventFactory">Takes the new and old property values and returns an event to emit, or null to emit nothing.</param>
    /// <typeparam name="TEvent">The type of event to emit.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> Emit<TEvent>(
        Func<TProperty, TProperty, TEvent?> eventFactory)
        where TEvent : notnull;

    /// <summary>
    /// Configure an event to be emitted when the property value changes.
    /// </summary>
    /// <param name="eventFactory">Takes the new property value and returns an event to emit, or null to emit nothing.</param>
    /// <typeparam name="TEvent">The type of event to emit.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> Emit<TEvent>(
        Func<TProperty, TEvent?> eventFactory)
        where TEvent : notnull
    {
        return Emit<TEvent>((newValue, _) => eventFactory(newValue));
    }

    /// <summary>
    /// Configures the service scope behavior for property gathering operations.
    /// </summary>
    /// <param name="scopeBehavior">The scope behavior to use when resolving services.</param>
    /// <returns>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> ScopeBehavior(PropertyGatheringServiceScopeBehavior scopeBehavior);
    
    
    /// <summary>
    /// Configures a custom equality comparer to use when comparing old and new property values for change detection and event emission.
    /// </summary>
    /// <param name="equalityComparer">The equality comparer to use for comparing property values.</param>
    /// <returns>>The property configuration builder for method chaining.</returns>
    IPropertyConfigurationBuilder<TState, TProperty> WithEqualityComparer(IEqualityComparer<TProperty> equalityComparer);
}