using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.Interfaces.Interceptors;
using SyncState.Models.Configuration;
using SyncState.Services.Managers;
using SyncState.Utils;

namespace SyncState.Configuration.Builder;

internal abstract class StateConfigurationBuilder
{
    public abstract Type StateType { get; }

    public abstract StateConfiguration Build();
}

internal class StateConfigurationBuilder<TState> : StateConfigurationBuilder, IInternalStateConfigurationBuilder<TState>
    where TState : class
{
    private readonly List<PropertyConfigurationBuilder> _propertyBuilders = [];
    private Type? _stateManagerType;
    private readonly Dictionary<Type, object> _extensions = new();
    private readonly SyncStateBuilder _syncStateBuilder;
    private readonly List<Type> InterceptorTypes = [];
    private readonly List<Action<StateConfiguration<TState>>> _configurationPostProcessors = [];

    public StateConfigurationBuilder(SyncStateBuilder syncStateBuilder)
    {
        _syncStateBuilder = syncStateBuilder;
    }

    public override Type StateType => typeof(TState);

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

    public IStateConfigurationBuilder<TState> WithInterceptor<TInterceptor>() where TInterceptor : class, IStateInterceptor<TState>
    {
        InterceptorTypes.Add(typeof(TInterceptor));
        _syncStateBuilder.AddServiceCollectionProcessor(services =>
        {
            services.AddTransient<IStateInterceptor<TState>, TInterceptor>();
        });
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

        //throw if a property was configured more than once
        var duplicateProperty = propertyConfigurations
            .GroupBy(pc => pc.PropertyInfo.Name)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProperty != null)
        {
            throw new InvalidOperationException(
                $"Property {duplicateProperty.Key} was configured more than once for state type {typeof(TState).FullName}");
        }

        //assign registered property configurations to unconfigured properties, throw if none matches
        var stateProperties = typeof(TState).GetProperties();
        foreach (var property in stateProperties)
        {
            if (propertyConfigurations.Any(pc => pc.PropertyInfo.Name == property.Name))
            {
                continue;
            }

            var registration = ResolvePropertyConfigurationRegistration(property);
            if (registration == null)
            {
                throw new InvalidOperationException(
                    $"Property {property.Name} was not configured for state type {typeof(TState).FullName}");
            }

            propertyConfigurations.Add(registration.CreateBuilder(this, property).Build());
        }

        var configuration = new StateConfiguration<TState>
        {
            Properties = propertyConfigurations,
            StateManagerType = _stateManagerType,
            InterceptorTypes = InterceptorTypes,
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

    /// <summary>
    /// Returns the registered property configuration with the highest match precedence for <paramref name="property"/>,
    /// or null if none matches.
    /// </summary>
    private PropertyConfigurationRegistration? ResolvePropertyConfigurationRegistration(PropertyInfo property)
    {
        var bestMatches = _syncStateBuilder.PropertyConfigurationRegistrations
            .Select(registration => (Registration: registration, Precedence: registration.GetMatchPrecedence(property)))
            .Where(match => match.Precedence > PropertyConfigurationRegistration.NoMatch)
            .GroupBy(match => match.Precedence)
            .OrderByDescending(group => group.Key)
            .FirstOrDefault()
            ?.Select(match => match.Registration)
            .ToList();

        if (bestMatches == null)
        {
            return null;
        }

        if (bestMatches.Count > 1)
        {
            var configurationTypes = string.Join(", ", bestMatches.Select(r => r.ConfigurationType.FullName));
            throw new InvalidOperationException(
                $"Property {property.Name} of state type {typeof(TState).FullName} matches several registered " +
                $"property configurations: {configurationTypes}. Configure the property explicitly.");
        }

        return bestMatches[0];
    }
}
