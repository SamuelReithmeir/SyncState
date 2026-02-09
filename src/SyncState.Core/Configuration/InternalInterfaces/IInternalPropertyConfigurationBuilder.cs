using SyncState.Configuration.Interfaces;
using SyncState.Models.Configuration;

namespace SyncState.Configuration.InternalInterfaces;

internal interface IInternalPropertyConfigurationBuilder<TState, TProperty>:IPropertyConfigurationBuilder<TState,TProperty> where TState : class
{
    IInternalPropertyConfigurationBuilder<TState, TProperty> AddExtension<TExtension>(TExtension extension)
        where TExtension : class;
    IInternalStateConfigurationBuilder<TState> GetStateBuilder();
    IInternalPropertyConfigurationBuilder<TState, TProperty> AddBuildAction(Action buildAction);
    IInternalPropertyConfigurationBuilder<TState, TProperty> AddConfigurationPostProcessor(Action<PropertyConfiguration<TProperty>> processor);
}