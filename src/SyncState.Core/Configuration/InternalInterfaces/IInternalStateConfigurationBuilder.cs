using SyncState.Configuration.Builder;
using SyncState.Configuration.Interfaces;
using SyncState.Models.Configuration;

namespace SyncState.Configuration.InternalInterfaces;

internal interface IInternalStateConfigurationBuilder<TState>:IStateConfigurationBuilder<TState> where TState : class
{
    IInternalStateConfigurationBuilder<TState> AddPropertyBuilder(PropertyConfigurationBuilder propertyBuilder);
    IInternalStateConfigurationBuilder<TState> AddExtension<TExtension>(TExtension extension) where TExtension : class;
    
    IInternalSyncStateBuilder GetSyncStateBuilder();
    IInternalStateConfigurationBuilder<TState> AddConfigurationPostProcessor(Action<StateConfiguration<TState>> processor);
}