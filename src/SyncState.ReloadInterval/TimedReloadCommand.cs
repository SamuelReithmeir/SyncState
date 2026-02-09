namespace SyncState.ReloadInterval;

/// <summary>
/// Command to reload given the interval
/// </summary>
public record TimedReloadCommand
{
    public required TimeSpan Interval { get; init; }
}