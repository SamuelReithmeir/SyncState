using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SyncState.Interfaces.Interceptors;
using SyncState.Interfaces.Managers;
using SyncState.Models.InterceptorContexts;
using SyncState.Services;

namespace SyncState.ErrorHandling.Interceptors;

/// <summary>
/// A property interceptor that provides fallback values when property initialization or command handling fails.
/// </summary>
/// <typeparam name="TProperty">The type of property.</typeparam>
public class FallbackPropertyInterceptor<TProperty> : IPropertyInterceptor<TProperty>
{
    private readonly ILogger<FallbackPropertyInterceptor<TProperty>>? _logger;

    /// <summary>
    /// Creates a new fallback interceptor.
    /// </summary>
    /// <param name="logger">Optional logger for logging fallback events.</param>
    public FallbackPropertyInterceptor(ILogger<FallbackPropertyInterceptor<TProperty>>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Intercepts property initialization and provides a fallback value on failure.
    /// </summary>
    public async Task InitializeAsync(
        PropertyInitializationContext<TProperty> context,
        Func<PropertyInitializationContext<TProperty>, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        var fallbackExtension = context.Configuration.GetExtension<FallbackExtension<TProperty>>();
        if (fallbackExtension == null)
        {
            // No fallback configuration, just pass through
            await next(context, cancellationToken);
            return;
        }

        try
        {
            await next(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Don't fallback if cancellation was requested
            throw;
        }
        catch (Exception ex)
        {
            if (fallbackExtension.ShouldFallback != null && !fallbackExtension.ShouldFallback(ex))
            {
                _logger?.LogWarning(
                    ex,
                    "Property {PropertyType} initialization failed with non-fallback exception",
                    typeof(TProperty).Name);
                throw;
            }

            _logger?.LogWarning(
                ex,
                "Property {PropertyType} initialization failed, using fallback value",
                typeof(TProperty).Name);

            var serviceProvider = SyncStateService.CurrentExecutionServiceProvider.Value
                ?? throw new InvalidOperationException("No service provider available for fallback");

            var fallbackValue = fallbackExtension.FallbackProvider(serviceProvider);
            
            if (context.PropertyManager is IPropertyManager<TProperty> propertyManager)
            {
                propertyManager.SetValue(fallbackValue);
            }
        }
    }

    /// <summary>
    /// Intercepts command handling and provides a fallback value on failure.
    /// </summary>
    public async Task HandleCommandAsync<TCommand>(
        PropertyCommandContext<TProperty, TCommand> context,
        Func<PropertyCommandContext<TProperty, TCommand>, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        var fallbackExtension = context.Configuration.GetExtension<FallbackExtension<TProperty>>();
        if (fallbackExtension == null)
        {
            // No fallback configuration, just pass through
            await next(context, cancellationToken);
            return;
        }

        try
        {
            await next(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Don't fallback if cancellation was requested
            throw;
        }
        catch (Exception ex)
        {
            if (fallbackExtension.ShouldFallback != null && !fallbackExtension.ShouldFallback(ex))
            {
                _logger?.LogWarning(
                    ex,
                    "Property {PropertyType} command {CommandType} failed with non-fallback exception",
                    typeof(TProperty).Name,
                    typeof(TCommand).Name);
                throw;
            }

            _logger?.LogWarning(
                ex,
                "Property {PropertyType} command {CommandType} failed, using fallback value",
                typeof(TProperty).Name,
                typeof(TCommand).Name);

            var serviceProvider = SyncStateService.CurrentExecutionServiceProvider.Value
                ?? throw new InvalidOperationException("No service provider available for fallback");

            var fallbackValue = fallbackExtension.FallbackProvider(serviceProvider);
            
            if (context.PropertyManager is IPropertyManager<TProperty> propertyManager)
            {
                propertyManager.SetValue(fallbackValue);
            }
        }
    }
}

