namespace SyncState.ErrorHandling;

/// <summary>
/// Extension class for value fallback on error
/// </summary>
public class FallbackExtension<TValue>
{
    /// <summary>
    /// The fallback value provider function.
    /// Takes the service provider and returns the fallback value.
    /// </summary>
    public required Func<IServiceProvider,Exception, TValue> FallbackProvider { get; init; }

    /// <summary>
    /// Optional predicate to determine if an exception should trigger the fallback.
    /// If null, all exceptions will trigger the fallback.
    /// </summary>
    public Func<Exception, bool>? ShouldFallback { get; set; }
}

