namespace SyncState.ReloadInterval;

public record TimerExtension
{
    public HashSet<TimeSpan> Intervals { get; set; } = [];
}