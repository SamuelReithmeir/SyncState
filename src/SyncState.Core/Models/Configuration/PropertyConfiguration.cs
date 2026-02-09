using System.Reflection;
using SyncState.Enums;
using SyncState.Models.Configuration.EventEmitters;

namespace SyncState.Models.Configuration;

public abstract class PropertyConfiguration : BaseConfiguration
{
    public List<CommandHandlerConfiguration> CommandHandlerConfigurations { get; }
    public PropertyInfo PropertyInfo { get; }
    public Type PropertyManagerType { get; }

    public PropertyConfiguration(PropertyInfo propertyInfo,
        Type propertyManagerType,
        List<CommandHandlerConfiguration> commandHandlerConfigurations,
        Dictionary<Type, object> extensions) : base(extensions)
    {
        PropertyInfo = propertyInfo;
        PropertyManagerType = propertyManagerType;
        CommandHandlerConfigurations = commandHandlerConfigurations;
    }
}

public class PropertyConfiguration<TProperty> : PropertyConfiguration
{
    public Func<IServiceProvider, CancellationToken, Task<TProperty>> Gatherer { get; }
    
    public PropertyGatheringServiceScopeBehavior ScopeBehavior { get; }
    public List<EventEmitterConfiguration<TProperty>> EventEmitters { get; }
    public IEqualityComparer<TProperty> EqualityComparer { get; init; }
    public PropertyConfiguration(PropertyInfo propertyInfo,
        Func<IServiceProvider, CancellationToken, Task<TProperty>> gatherer,
        PropertyGatheringServiceScopeBehavior scopeBehavior,
        Type propertyManagerType,
        IEqualityComparer<TProperty> equalityComparer,
        List<CommandHandlerConfiguration> commandHandlerConfigurations,
        List<EventEmitterConfiguration<TProperty>> eventEmitters,
        Dictionary<Type, object> extensions) : base(propertyInfo, propertyManagerType, commandHandlerConfigurations,
        extensions)
    {
        Gatherer = gatherer;
        ScopeBehavior = scopeBehavior;
        EventEmitters = eventEmitters;
        EqualityComparer = equalityComparer;
    }
}