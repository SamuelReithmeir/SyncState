using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.Interfaces;

namespace SyncState.OptionsMonitor.Configuration;

/// <summary>
/// Extension methods for configuring properties to gather values from IOptionsMonitor.
/// </summary>
public static class PropertyBuilderExtensions
{
    /// <summary>
    /// Configures the property to gather its value from IOptionsMonitor and automatically update when options change.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <typeparam name="TState">The type of state containing the property.</typeparam>
    /// <typeparam name="TProperty">The type of the property and the options type.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the builder doesn't implement the required internal interface.</exception>
    public static IPropertyConfigurationBuilder<TState, TProperty> GatherFromOptionsMonitor<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder) where TState : class
    {
        if (builder is not IInternalPropertyConfigurationBuilder<TState, TProperty> internalBuilder)
        {
            throw new InvalidOperationException("Builder must implement IInternalPropertyConfigurationBuilder");
        }

        builder.GatherFrom<IOptionsMonitor<TProperty>>(optionsMonitor => optionsMonitor.CurrentValue);
        builder.On<OptionsPropertyChangeCommand<TProperty>>((c, pm) => pm.SetValue(c.NewValue));
        internalBuilder.GetStateBuilder().GetSyncStateBuilder().AddInitAction((sp, _) =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<TProperty>>();
            optionsMonitor.OnChange(newValue =>
            {
                using var scope = sp.CreateScope();
                var syncCommandService = scope.ServiceProvider.GetRequiredService<ISyncCommandService>();
                //sync execution of async function to ensure scope is not disposed before execution completes
                syncCommandService
                    .HandleAsync(new OptionsPropertyChangeCommand<TProperty>(newValue), CancellationToken.None)
                    .GetAwaiter().GetResult();
            });
            return Task.CompletedTask;
        });
        return builder;
    }
    
    /// <summary>
    /// Configures the property to gather its value from IOptionsMonitor with a mapping function and automatically update when options change.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="mappingFunc">Function to map from the options type to the property type.</param>
    /// <typeparam name="TState">The type of state containing the property.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <typeparam name="TOption">The type of the options to monitor.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the builder doesn't implement the required internal interface.</exception>
    public static IPropertyConfigurationBuilder<TState, TProperty> GatherFromOptionsMonitor<TState, TProperty, TOption>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder, Func<TOption, TProperty> mappingFunc) where TState : class
    {
        if (builder is not IInternalPropertyConfigurationBuilder<TState, TProperty> internalBuilder)
        {
            throw new InvalidOperationException("Builder must implement IInternalPropertyConfigurationBuilder");
        }

        builder.GatherFrom<IOptionsMonitor<TOption>>(optionsMonitor => mappingFunc(optionsMonitor.CurrentValue));
        builder.On<OptionsPropertyChangeCommand<TProperty>>((c, pm) => pm.SetValue(c.NewValue));
        internalBuilder.GetStateBuilder().GetSyncStateBuilder().AddInitAction((sp, _) =>
        {
            var optionsMonitor = sp.GetRequiredService<IOptionsMonitor<TOption>>();
            optionsMonitor.OnChange(newValue =>
            {
                using var scope = sp.CreateScope();
                var syncCommandService = scope.ServiceProvider.GetRequiredService<ISyncCommandService>();
                //sync execution of async function to ensure scope is not disposed before execution completes
                syncCommandService
                    .HandleAsync(new OptionsPropertyChangeCommand<TProperty>(mappingFunc(newValue)), CancellationToken.None)
                    .GetAwaiter().GetResult();
            });
            return Task.CompletedTask;
        });
        return builder;
    }
}