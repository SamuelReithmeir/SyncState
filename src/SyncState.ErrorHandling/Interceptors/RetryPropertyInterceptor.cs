using Microsoft.Extensions.Logging;
using SyncState.Interfaces.Interceptors;
using SyncState.Models.InterceptorContexts;

namespace SyncState.ErrorHandling.Interceptors;

/// <summary>
/// A property interceptor that implements retry logic for property initialization (loading).
/// </summary>
/// <typeparam name="TProperty">The type of property being loaded.</typeparam>
public class RetryPropertyInterceptor<TProperty> : IPropertyInterceptor<TProperty>
{
    private readonly ILogger<RetryPropertyInterceptor<TProperty>>? _logger;

    /// <summary>
    /// Creates a new retry interceptor.
    /// </summary>
    /// <param name="logger">Optional logger for logging retry attempts.</param>
    public RetryPropertyInterceptor(ILogger<RetryPropertyInterceptor<TProperty>>? logger = null)
    {
        _logger = logger;
    }


    /// <summary>
    /// Intercepts property initialization and applies retry logic.
    /// </summary>
    public async Task InitializeAsync(
        PropertyInitializationContext<TProperty> context,
        Func<PropertyInitializationContext<TProperty>, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        var options = context.Configuration.GetExtension<RetryExtension>();
        if (options == null)
        {
            // No retry configuration, just pass through
            await next(context, cancellationToken);
            return;
        }

        await ExecuteWithRetryAsync(
            () => next(context, cancellationToken),
            options,
            typeof(TProperty).Name,
            "initialization",
            cancellationToken);
    }

    /// <summary>
    /// Intercepts command handling and applies retry logic.
    /// </summary>
    public async Task HandleCommandAsync<TCommand>(
        PropertyCommandContext<TProperty, TCommand> context,
        Func<PropertyCommandContext<TProperty, TCommand>, CancellationToken, Task> next,
        CancellationToken cancellationToken)
    {
        var options = context.Configuration.GetExtension<RetryExtension>();
        if (options == null)
        {
            // No retry configuration, just pass through
            await next(context, cancellationToken);
            return;
        }

        await ExecuteWithRetryAsync(
            () => next(context, cancellationToken),
            options,
            typeof(TProperty).Name,
            $"command {typeof(TCommand).Name}",
            cancellationToken);
    }

    private async Task ExecuteWithRetryAsync(
        Func<Task> action,
        RetryExtension options,
        string propertyName,
        string operationName,
        CancellationToken cancellationToken)
    {
        var attemptNumber = 0;
        Exception? lastException = null;

        while (attemptNumber <= options.MaxRetries)
        {
            attemptNumber++;
            try
            {
                await action();

                if (attemptNumber > 1)
                {
                    _logger?.LogInformation(
                        "Property {PropertyType} {Operation} succeeded on attempt {Attempt}",
                        propertyName,
                        operationName,
                        attemptNumber);
                }

                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Don't retry if cancellation was requested
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (options.ShouldRetry != null && !options.ShouldRetry(ex))
                {
                    _logger?.LogWarning(
                        ex,
                        "Property {PropertyType} {Operation} failed with non-retryable exception on attempt {Attempt}",
                        propertyName,
                        operationName,
                        attemptNumber);
                    throw;
                }

                if (attemptNumber > options.MaxRetries)
                {
                    _logger?.LogError(
                        ex,
                        "Property {PropertyType} {Operation} failed after {MaxAttempts} attempts",
                        propertyName,
                        operationName,
                        attemptNumber);
                    break;
                }

                var delay = options.CalculateDelay(attemptNumber);

                _logger?.LogWarning(
                    ex,
                    "Property {PropertyType} {Operation} failed on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms",
                    propertyName,
                    operationName,
                    attemptNumber,
                    options.MaxRetries + 1,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new RetryExhaustedException(
            $"Property {propertyName} {operationName} failed after {options.MaxRetries + 1} attempts",
            lastException!);
    }
}
