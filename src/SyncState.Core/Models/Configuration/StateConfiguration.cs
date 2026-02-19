namespace SyncState.Models.Configuration;

public abstract record StateConfiguration : BaseConfiguration
{
    public required List<PropertyConfiguration> Properties { get; init; }
    public abstract Type StateType { get; }
    public required Type StateManagerType { get; init; }
}

public record StateConfiguration<TState> : StateConfiguration
{
    public override Type StateType => typeof(TState);
}