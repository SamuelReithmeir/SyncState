using Microsoft.Extensions.DependencyInjection;
using SyncState.InternalInterfaces;
using SyncState.Models.Configuration;

namespace SyncState.Factories;

/// <summary>
/// Factory responsible for creating the property manager instances
/// </summary>
public interface IPropertyManagerFactory
{
    IInternalPropertyManager CreatePropertyManager(PropertyConfiguration propertyConfiguration);
}

/// <summary>
/// default implementation of the property manager factory which instantiates a manager using <see cref="ActivatorUtilities"/> and the service provider of the factory
/// </summary>
public class DefaultPropertyManagerFactory : IPropertyManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultPropertyManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IInternalPropertyManager CreatePropertyManager(PropertyConfiguration propertyConfiguration)
    {
        var managerType = propertyConfiguration.PropertyManagerType;
        var manager = (IInternalPropertyManager)ActivatorUtilities.CreateInstance(
            _serviceProvider,
            managerType,
            propertyConfiguration);
        return manager;
    }
}