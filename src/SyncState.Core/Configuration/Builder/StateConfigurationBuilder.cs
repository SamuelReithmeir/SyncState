using System.Linq.Expressions;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.Models.Configuration;
using SyncState.Services.Managers;
using SyncState.Utils;

namespace SyncState.Configuration.Builder;

internal abstract class StateConfigurationBuilder
{
    public abstract StateConfiguration Build();
}

internal class StateConfigurationBuilder<TState> : StateConfigurationBuilder, IInternalStateConfigurationBuilder<TState>
    where TState : class
{
    private readonly List<PropertyConfigurationBuilder> _propertyBuilders = [];
    private Type? _stateManagerType;
    private readonly Dictionary<Type, object> _extensions = new();
    private readonly SyncStateBuilder _syncStateBuilder;
    private readonly List<Action<StateConfiguration<TState>>> _configurationPostProcessors = [];

    public StateConfigurationBuilder(SyncStateBuilder syncStateBuilder)
    {
        _syncStateBuilder = syncStateBuilder;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> Property<TProperty>(
        Expression<Func<TState, TProperty>> propertyExpression)
    {
        var propertyInfo = propertyExpression.GetPropertyInfo();
        var propertyBuilder = new PropertyConfigurationBuilder<TState, TProperty>(this, propertyInfo);
        _propertyBuilders.Add(propertyBuilder);
        return propertyBuilder;
    }

    public IPartialCollectionPropertyBuilder<TState, TEntry> Collection<TEntry>(
        Expression<Func<TState, IEnumerable<TEntry>>> collectionExpression)
    {
        return new PartialCollectionPropertyBuilder<TState, TEntry>(this, collectionExpression);
    }

    public ICollectionPropertyBuilder<TState, TEntry, TKey> Collection<TEntry, TKey>(
        Expression<Func<TState, IEnumerable<TEntry>>> collectionExpression, Expression<Func<TEntry, TKey>> keySelector)
        where TKey : struct
    {
        var propertyBuilder =
            new CollectionPropertyBuilder<TState, TEntry, TKey>(this, collectionExpression, keySelector);
        _propertyBuilders.Add(propertyBuilder);
        return propertyBuilder;
    }

    public IStateConfigurationBuilder<TState> WithStateManager<TStateManager>() where TStateManager : class
    {
        _stateManagerType = typeof(TStateManager);
        return this;
    }

    public IInternalStateConfigurationBuilder<TState> AddPropertyBuilder(PropertyConfigurationBuilder propertyBuilder)
    {
        _propertyBuilders.Add(propertyBuilder);
        return this;
    }

    public StateConfigurationBuilder<TState> AddExtension<T>(T extension) where T : class
    {
        _extensions[typeof(T)] = extension;
        return this;
    }

    public IInternalSyncStateBuilder GetSyncStateBuilder()
    {
        return _syncStateBuilder;
    }

    public IInternalStateConfigurationBuilder<TState> AddConfigurationPostProcessor(
        Action<StateConfiguration<TState>> processor)
    {
        _configurationPostProcessors.Add(processor);
        return this;
    }

    public override StateConfiguration Build()
    {
        if (_stateManagerType == null)
        {
            _stateManagerType = typeof(StateManager<TState>);
        }

        var propertyConfigurations = _propertyBuilders
            .Select(builder => builder.Build())
            .ToList();

        //throw if a property was not configured
        var stateProperties = typeof(TState).GetProperties();
        foreach (var property in stateProperties)
        {
            if (propertyConfigurations.All(pc => pc.PropertyInfo.Name != property.Name))
            {
                throw new InvalidOperationException(
                    $"Property {property.Name} was not configured for state type {typeof(TState).FullName}");
            }
        }

        var configuration = new StateConfiguration<TState>
        {
            Properties = propertyConfigurations,
            StateManagerType = _stateManagerType,
            Extensions = _extensions
        };
        foreach (var processor in _configurationPostProcessors)
        {
            processor(configuration);
        }

        return configuration;
    }

    IInternalStateConfigurationBuilder<TState> IInternalStateConfigurationBuilder<TState>.AddExtension<TExtension>(
        TExtension extension)
    {
        return AddExtension(extension);
    }
}