namespace SyncState.Models.Configuration;

public abstract class StateConfiguration : BaseConfiguration
{
    public List<PropertyConfiguration> Properties { get; }
    public Type StateType { get; }
    public Type StateManagerType { get; }

    public StateConfiguration(List<PropertyConfiguration> properties,
        Type stateType,
        Type stateManagerType,
        Dictionary<Type, object> extensions) : base(extensions)
    {
        Properties = properties;
        StateType = stateType;
        StateManagerType = stateManagerType;
    }
}

public class StateConfiguration<TState> : StateConfiguration
{
    public StateConfiguration(List<PropertyConfiguration> properties, 
        Type stateManagerType,
        Dictionary<Type, object> extensions
        ) : base(properties, typeof(TState), stateManagerType,extensions)
    {
    }
}