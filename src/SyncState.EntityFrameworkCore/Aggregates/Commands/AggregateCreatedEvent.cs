namespace SyncState.EntityFrameworkCore.Aggregates.Commands;

public record AggregateCreatedCommand<TAggregate>
{
    public required TAggregate Aggregate { get; init; }
}