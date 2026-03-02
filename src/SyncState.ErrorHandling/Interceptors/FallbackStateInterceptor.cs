using Microsoft.Extensions.Logging;
using SyncState.Interfaces.Interceptors;
using SyncState.Models.InterceptorContexts;
using SyncState.Services;

namespace SyncState.ErrorHandling.Interceptors;

/// <summary>
/// A state interceptor that provides a fallback state when state initialization or command handling fails.
/// </summary>
/// <typeparam name="TState">The type of state.</typeparam>
public class FallbackStateInterceptor<TState> : IStateInterceptor<TState> where TState : class
{
    private readonly ILogger<FallbackStateInterceptor<TState>>? _logger;

    /// <summary>
    /// Creates a new fallback interceptor.
    /// </summary>
    /// <param name="logger">Optional logger for logging fallback events.</param>
    public FallbackStateInterceptor(ILogger<FallbackStateInterceptor<TState>>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Intercepts state initialization and provides a fallback state on failure.
    /// </summary>
    public async Task InitializeAsync(
        StateInitializationContext<TState> context,
        Func<StateInitializationContext<TState>, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        var fallbackExtension = context.Configuration.GetExtension<FallbackExtension<TState>>();
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
                    "State {StateType} initialization failed with non-fallback exception",
                    typeof(TState).Name);
                throw;
            }

            _logger?.LogWarning(
                ex,
                "State {StateType} initialization failed, using fallback value",
                typeof(TState).Name);

            var serviceProvider = SyncStateService.CurrentExecutionServiceProvider.Value
                ?? throw new InvalidOperationException("No service provider available for fallback");

            var fallbackValue = fallbackExtension.FallbackProvider(serviceProvider,ex);
            context.StateManager.SetValue(fallbackValue);
        }
    }

    /// <summary>
    /// Intercepts command handling and provides a fallback state on failure.
    /// </summary>
    public async Task HandleCommandAsync<TCommand>(
        StateCommandContext<TState, TCommand> context,
        Func<StateCommandContext<TState, TCommand>, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        var fallbackExtension = context.Configuration.GetExtension<FallbackExtension<TState>>();
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
                    "State {StateType} command {CommandType} failed with non-fallback exception",
                    typeof(TState).Name,
                    typeof(TCommand).Name);
                throw;
            }

            _logger?.LogWarning(
                ex,
                "State {StateType} command {CommandType} failed, using fallback value",
                typeof(TState).Name,
                typeof(TCommand).Name);

            var serviceProvider = SyncStateService.CurrentExecutionServiceProvider.Value
                ?? throw new InvalidOperationException("No service provider available for fallback");

            var fallbackValue = fallbackExtension.FallbackProvider(serviceProvider,ex);
            context.StateManager.SetValue(fallbackValue);
        }
    }
}

