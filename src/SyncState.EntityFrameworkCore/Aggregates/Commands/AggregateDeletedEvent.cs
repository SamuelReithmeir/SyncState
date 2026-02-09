namespace SyncState.EntityFrameworkCore.Aggregates.Commands;

public record AggregateDeletedCommand<TAggregate,TKey>
{
    public required TKey Key { get; init; }
}