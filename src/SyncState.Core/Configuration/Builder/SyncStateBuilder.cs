using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.Models.Configuration;

namespace SyncState.Configuration.Builder;

internal class SyncStateBuilder : IInternalSyncStateBuilder
{
    private readonly List<StateConfigurationBuilder> _stateConfigurationBuilders = [];
    private readonly Dictionary<Type, object> _extensions = new();
    private readonly List<Action<IServiceCollection>> _serviceCollectionProcessors = [];
    private readonly List<Action<SyncStateConfiguration>> _configurationPostProcessors = [];
    private readonly List<Func<IServiceProvider, CancellationToken, Task>> _initActions = [];

    public ISyncStateBuilder AddState<TState>(Action<IStateConfigurationBuilder<TState>> configure)
        where TState : class
    {
        var stateBuilder = new StateConfigurationBuilder<TState>(this);
        configure(stateBuilder);
        _stateConfigurationBuilders.Add(stateBuilder);
        return this;
    }

    public SyncStateBuilder AddExtension<T>(T extension) where T : class
    {
        _extensions[typeof(T)] = extension;
        return this;
    }

    public IInternalSyncStateBuilder AddOrUpdateExtension<TExtension>(TExtension extension, Func<TExtension, TExtension> updateFactory) where TExtension : class
    {
        if (_extensions.TryGetValue(typeof(TExtension), out var existingExtensionObj) &&
            existingExtensionObj is TExtension existingExtension)
        {
            var updatedExtension = updateFactory(existingExtension);
            _extensions[typeof(TExtension)] = updatedExtension;
        }
        else
        {
            _extensions[typeof(TExtension)] = extension;
        }

        return this;
    }

    public TExtension? GetExtension<TExtension>() where TExtension : class
    {
        if (_extensions.TryGetValue(typeof(TExtension), out var extensionObj) &&
            extensionObj is TExtension extension)
        {
            return extension;
        }

        return null;
    }

    public IInternalSyncStateBuilder AddConfigurationPostProcessor(Action<SyncStateConfiguration> processor)
    {
        _configurationPostProcessors.Add(processor);
        return this;
    }

    public IInternalSyncStateBuilder AddServiceCollectionProcessor(Action<IServiceCollection> processor)
    {
        _serviceCollectionProcessors.Add(processor);
        return this;
    }

    public IInternalSyncStateBuilder AddInitAction(Func<IServiceProvider, CancellationToken, Task> initAction)
    {
        _initActions.Add(initAction);
        return this;
    }

    public SyncStateConfiguration Build()
    {
        var stateConfigurations = _stateConfigurationBuilders
            .Select(builder => builder.Build())
            .ToList();

        var configuration = new SyncStateConfiguration(stateConfigurations, _serviceCollectionProcessors,_initActions, _extensions);
        foreach (var processor in _configurationPostProcessors)
        {
            processor(configuration);
        }

        return configuration;
    }

    IInternalSyncStateBuilder IInternalSyncStateBuilder.AddExtension<TExtension>(TExtension extension)
    {
        return AddExtension(extension);
    }
}