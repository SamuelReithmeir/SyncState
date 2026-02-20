namespace SyncState.ErrorHandling;

/// <summary>
/// Extension class for retry configuration stored in the configuration extensions dictionary.
/// </summary>
public class RetryExtension
{
    /// <summary>
    /// Maximum number of retry attempts. Default is 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries. Default is 100ms.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum delay between retries. Default is 30 seconds.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Backoff multiplier for exponential backoff. Default is 2.0.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Whether to use jitter to add randomness to delays. Default is true.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Jitter factor (0.0 to 1.0) - percentage of the delay to randomize. Default is 0.25.
    /// </summary>
    public double JitterFactor { get; set; } = 0.25;

    /// <summary>
    /// Optional predicate to determine if an exception should trigger a retry.
    /// If null, all exceptions will trigger retries.
    /// </summary>
    public Func<Exception, bool>? ShouldRetry { get; set; }

    /// <summary>
    /// Calculates the delay for a specific retry attempt using exponential backoff with optional jitter.
    /// </summary>
    /// <param name="attemptNumber">The current attempt number (1-based).</param>
    /// <returns>The calculated delay.</returns>
    public TimeSpan CalculateDelay(int attemptNumber)
    {
        var exponentialDelay = TimeSpan.FromMilliseconds(
            InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attemptNumber - 1));

        var delay = exponentialDelay > MaxDelay ? MaxDelay : exponentialDelay;

        if (UseJitter)
        {
            var jitterRange = delay.TotalMilliseconds * JitterFactor;
            var jitter = (Random.Shared.NextDouble() * 2 - 1) * jitterRange;
            delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
        }

        return delay;
    }
}


