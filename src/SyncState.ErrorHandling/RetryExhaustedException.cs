namespace SyncState.ErrorHandling;

/// <summary>
/// Exception thrown when all retry attempts have been exhausted.
/// </summary>
public class RetryExhaustedException : Exception
{
    /// <summary>
    /// Creates a new instance of the <see cref="RetryExhaustedException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused the final retry failure.</param>
    public RetryExhaustedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

