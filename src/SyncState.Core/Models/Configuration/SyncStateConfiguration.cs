
using Microsoft.Extensions.DependencyInjection;

namespace SyncState.Models.Configuration;

public class SyncStateConfiguration:BaseConfiguration
{
    public List<StateConfiguration> StateConfigurations { get; }
    public List<Action<IServiceCollection>> ServiceCollectionProcessors { get; }
    public List<Func<IServiceProvider,CancellationToken,Task>> InitActions { get; }
    
    public SyncStateConfiguration(List<StateConfiguration> stateConfigurations,
        List<Action<IServiceCollection>> serviceCollectionProcessors,
        List<Func<IServiceProvider, CancellationToken, Task>> initActions,
        Dictionary<Type, object> extensions):base(extensions)
    {
        StateConfigurations = stateConfigurations;
        ServiceCollectionProcessors = serviceCollectionProcessors;
        InitActions = initActions;
    }
}