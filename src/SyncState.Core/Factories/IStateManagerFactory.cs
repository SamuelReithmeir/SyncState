using Microsoft.Extensions.DependencyInjection;
using SyncState.InternalInterfaces;
using SyncState.Models.Configuration;

namespace SyncState.Factories;

/// <summary>
/// Factory responsible for creating the state manager instances
/// </summary>
public interface IStateManagerFactory
{
    IInternalStateManager CreateStateManager(StateConfiguration stateConfiguration);
}

/// <summary>
/// default implementation of the state manager factory which instantiates a manager using <see cref="ActivatorUtilities"/> and the service provider of the factory
/// </summary>
public class DefaultStateManagerFactory : IStateManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultStateManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IInternalStateManager CreateStateManager(StateConfiguration stateConfiguration)
    {
        var managerType = stateConfiguration.StateManagerType;
        var manager = (IInternalStateManager)ActivatorUtilities.CreateInstance(
            _serviceProvider,
            managerType,
            stateConfiguration);
        return manager;
    }
}