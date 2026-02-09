namespace SyncState.EntityFrameworkCore.Aggregates;

/// <summary>
/// state of an aggregate
/// </summary>
public enum AggregateState
{
    Added,
    Deleted,
    Updated,
    AggregateParticipantChanged
}