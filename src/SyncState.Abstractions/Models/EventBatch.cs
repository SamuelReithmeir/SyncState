namespace SyncState.Models;

/// <summary>
/// Base class for event batches.
/// </summary>
public abstract record EventBatch;

/// <summary>
/// A batch of events emitted by a change digestion cycle.
/// </summary>
/// <typeparam name="TEvent">The type of events in the batch.</typeparam>
public record EventBatch<TEvent>
{
    /// <summary>
    /// Gets the list of events in this batch.
    /// </summary>
    public required IReadOnlyList<TEvent> Events { get; init; }
}