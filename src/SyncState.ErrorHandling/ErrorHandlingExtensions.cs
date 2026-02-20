using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.ErrorHandling.Interceptors;
using SyncState.Interfaces.Interceptors;

namespace SyncState.ErrorHandling;

/// <summary>
/// Extension methods for configuring error handling and retry behavior on SyncState builders.
/// </summary>
public static class ErrorHandlingExtensions
{
    #region Retry Extensions

    /// <summary>
    /// Configures the property to retry initialization on failure with default options.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithRetry<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder)
        where TState : class
    {
        return builder.WithRetry(new RetryExtension());
    }

    /// <summary>
    /// Configures the property to retry initialization on failure with the specified options.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="options">The retry configuration options.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithRetry<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder,
        RetryExtension options)
        where TState : class
    {
        if (builder is not IInternalPropertyConfigurationBuilder<TState, TProperty> internalBuilder)
        {
            throw new InvalidOperationException("Builder must implement IInternalPropertyConfigurationBuilder.");
        }

        // Add the retry extension to the configuration
        internalBuilder.AddExtension(options);

        // Register the interceptor with DI
        internalBuilder.GetStateBuilder().GetSyncStateBuilder().AddServiceCollectionProcessor(services =>
        {
            services.AddTransient<IPropertyInterceptor<TProperty>, RetryPropertyInterceptor<TProperty>>();
        });

        // Add the interceptor using the standard method
        return builder.WithInterceptor<RetryPropertyInterceptor<TProperty>>();
    }

    /// <summary>
    /// Configures the property to retry initialization on failure using a configuration action.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="configure">Action to configure the retry options.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithRetry<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder,
        Action<RetryExtension> configure)
        where TState : class
    {
        var options = new RetryExtension();
        configure(options);
        return builder.WithRetry(options);
    }

    #endregion

    #region Fallback Extensions

    /// <summary>
    /// Configures the property to use a fallback value when initialization or command handling fails.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="fallbackValue">The fallback value to use on failure.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithFallback<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder,
        TProperty fallbackValue)
        where TState : class
    {
        return builder.WithFallback(_ => fallbackValue);
    }

    /// <summary>
    /// Configures the property to use a fallback value from a factory function when initialization or command handling fails.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="fallbackFactory">Factory function to create the fallback value.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithFallback<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder,
        Func<TProperty> fallbackFactory)
        where TState : class
    {
        return builder.WithFallback(_ => fallbackFactory());
    }

    /// <summary>
    /// Configures the property to use a fallback value from the service provider when initialization or command handling fails.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="fallbackProvider">Function to get the fallback value from the service provider.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithFallback<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder,
        Func<IServiceProvider, TProperty> fallbackProvider)
        where TState : class
    {
        return builder.WithFallback(new FallbackExtension<TProperty> { FallbackProvider = fallbackProvider });
    }

    /// <summary>
    /// Configures the property to use a fallback value from a service when initialization or command handling fails.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="fallbackProvider">Function to get the fallback value from the service.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <typeparam name="TService">The type of service to resolve.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithFallbackFrom<TState, TProperty, TService>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder,
        Func<TService, TProperty> fallbackProvider)
        where TState : class
        where TService : notnull
    {
        return builder.WithFallback(sp => fallbackProvider(sp.GetRequiredService<TService>()));
    }

    /// <summary>
    /// Configures the property to use a fallback value with full configuration options.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="options">The fallback configuration options.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithFallback<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder,
        FallbackExtension<TProperty> options)
        where TState : class
    {
        if (builder is not IInternalPropertyConfigurationBuilder<TState, TProperty> internalBuilder)
        {
            throw new InvalidOperationException("Builder must implement IInternalPropertyConfigurationBuilder.");
        }

        // Add the fallback extension to the configuration
        internalBuilder.AddExtension(options);

        // Register the interceptor with DI
        internalBuilder.GetStateBuilder().GetSyncStateBuilder().AddServiceCollectionProcessor(services =>
        {
            services.AddTransient<IPropertyInterceptor<TProperty>, FallbackPropertyInterceptor<TProperty>>();
        });

        // Add the interceptor using the standard method
        return builder.WithInterceptor<FallbackPropertyInterceptor<TProperty>>();
    }

    /// <summary>
    /// Configures the property to use a fallback value with a configuration action.
    /// </summary>
    /// <param name="builder">The property configuration builder.</param>
    /// <param name="fallbackValue">The fallback value to use on failure.</param>
    /// <param name="configure">Action to configure additional fallback options.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TProperty">The type of property.</typeparam>
    /// <returns>The property configuration builder for method chaining.</returns>
    public static IPropertyConfigurationBuilder<TState, TProperty> WithFallback<TState, TProperty>(
        this IPropertyConfigurationBuilder<TState, TProperty> builder,
        TProperty fallbackValue,
        Action<FallbackExtension<TProperty>> configure)
        where TState : class
    {
        var options = new FallbackExtension<TProperty> { FallbackProvider = _ => fallbackValue };
        configure(options);
        return builder.WithFallback(options);
    }

    #endregion

    #region State Fallback Extensions

    /// <summary>
    /// Configures the state to use a fallback value when initialization or command handling fails.
    /// </summary>
    /// <param name="builder">The state configuration builder.</param>
    /// <param name="fallbackValue">The fallback state to use on failure.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <returns>The state configuration builder for method chaining.</returns>
    public static IStateConfigurationBuilder<TState> WithFallback<TState>(
        this IStateConfigurationBuilder<TState> builder,
        TState fallbackValue)
        where TState : class
    {
        return builder.WithFallback(_ => fallbackValue);
    }

    /// <summary>
    /// Configures the state to use a fallback value from a factory function when initialization or command handling fails.
    /// </summary>
    /// <param name="builder">The state configuration builder.</param>
    /// <param name="fallbackFactory">Factory function to create the fallback state.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <returns>The state configuration builder for method chaining.</returns>
    public static IStateConfigurationBuilder<TState> WithFallback<TState>(
        this IStateConfigurationBuilder<TState> builder,
        Func<TState> fallbackFactory)
        where TState : class
    {
        return builder.WithFallback(_ => fallbackFactory());
    }

    /// <summary>
    /// Configures the state to use a fallback value from the service provider when initialization or command handling fails.
    /// </summary>
    /// <param name="builder">The state configuration builder.</param>
    /// <param name="fallbackProvider">Function to get the fallback state from the service provider.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <returns>The state configuration builder for method chaining.</returns>
    public static IStateConfigurationBuilder<TState> WithFallback<TState>(
        this IStateConfigurationBuilder<TState> builder,
        Func<IServiceProvider, TState> fallbackProvider)
        where TState : class
    {
        return builder.WithFallback(new FallbackExtension<TState> { FallbackProvider = fallbackProvider });
    }

    /// <summary>
    /// Configures the state to use a fallback value from a service when initialization or command handling fails.
    /// </summary>
    /// <param name="builder">The state configuration builder.</param>
    /// <param name="fallbackProvider">Function to get the fallback state from the service.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <typeparam name="TService">The type of service to resolve.</typeparam>
    /// <returns>The state configuration builder for method chaining.</returns>
    public static IStateConfigurationBuilder<TState> WithFallbackFrom<TState, TService>(
        this IStateConfigurationBuilder<TState> builder,
        Func<TService, TState> fallbackProvider)
        where TState : class
        where TService : notnull
    {
        return builder.WithFallback(sp => fallbackProvider(sp.GetRequiredService<TService>()));
    }

    /// <summary>
    /// Configures the state to use a fallback value with full configuration options.
    /// </summary>
    /// <param name="builder">The state configuration builder.</param>
    /// <param name="options">The fallback configuration options.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <returns>The state configuration builder for method chaining.</returns>
    public static IStateConfigurationBuilder<TState> WithFallback<TState>(
        this IStateConfigurationBuilder<TState> builder,
        FallbackExtension<TState> options)
        where TState : class
    {
        if (builder is not IInternalStateConfigurationBuilder<TState> internalBuilder)
        {
            throw new InvalidOperationException("Builder must implement IInternalStateConfigurationBuilder.");
        }

        // Add the fallback extension to the configuration
        internalBuilder.AddExtension(options);

        // Register the interceptor with DI
        internalBuilder.GetSyncStateBuilder().AddServiceCollectionProcessor(services =>
        {
            services.AddTransient<IStateInterceptor<TState>, FallbackStateInterceptor<TState>>();
        });

        // Add the interceptor using the standard method
        return builder.WithInterceptor<FallbackStateInterceptor<TState>>();
    }

    /// <summary>
    /// Configures the state to use a fallback value with a configuration action.
    /// </summary>
    /// <param name="builder">The state configuration builder.</param>
    /// <param name="fallbackValue">The fallback state to use on failure.</param>
    /// <param name="configure">Action to configure additional fallback options.</param>
    /// <typeparam name="TState">The type of state.</typeparam>
    /// <returns>The state configuration builder for method chaining.</returns>
    public static IStateConfigurationBuilder<TState> WithFallback<TState>(
        this IStateConfigurationBuilder<TState> builder,
        TState fallbackValue,
        Action<FallbackExtension<TState>> configure)
        where TState : class
    {
        var options = new FallbackExtension<TState> { FallbackProvider = _ => fallbackValue };
        configure(options);
        return builder.WithFallback(options);
    }

    #endregion
}



