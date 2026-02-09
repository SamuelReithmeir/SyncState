using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Interfaces;
using SyncState.Models.Configuration;

namespace SyncState.Configuration.InternalInterfaces;

internal interface IInternalSyncStateBuilder:ISyncStateBuilder
{
    IInternalSyncStateBuilder AddExtension<TExtension>(TExtension extension) where TExtension : class;
    IInternalSyncStateBuilder AddOrUpdateExtension<TExtension>(TExtension extension, Func<TExtension, TExtension> updateFactory)
        where TExtension : class;
    TExtension? GetExtension<TExtension>() where TExtension : class;
    
    IInternalSyncStateBuilder AddConfigurationPostProcessor(Action<SyncStateConfiguration> processor);
    IInternalSyncStateBuilder AddServiceCollectionProcessor(Action<IServiceCollection> processor);
    
    /// <summary>
    /// add an action that will be executed during the initialization of the SyncState managers
    /// </summary>
    /// <param name="initAction">async func that receives the root service provider</param>
    /// <returns></returns>
    IInternalSyncStateBuilder AddInitAction(Func<IServiceProvider, CancellationToken, Task> initAction);
}