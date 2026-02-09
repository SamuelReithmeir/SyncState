namespace SyncState.EntityFrameworkCore.Aggregates.Commands;

public class AggregateUpdatedCommand<TAggregate>
{
    public required TAggregate Aggregate { get; init; }
}