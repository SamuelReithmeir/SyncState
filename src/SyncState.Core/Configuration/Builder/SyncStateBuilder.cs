using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.Models.Configuration;

namespace SyncState.Configuration.Builder;

internal class SyncStateBuilder : IInternalSyncStateBuilder
{
    private static readonly MethodInfo AddStateFromConfigurationMethod = typeof(SyncStateBuilder)
        .GetMethod(nameof(AddStateFromConfiguration), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly List<StateConfigurationBuilder> _stateConfigurationBuilders = [];
    private readonly List<PropertyConfigurationRegistration> _propertyConfigurationRegistrations = [];
    private readonly Dictionary<Type, object> _extensions = new();
    private readonly List<Action<IServiceCollection>> _serviceCollectionProcessors = [];
    private readonly List<Action<SyncStateConfiguration>> _configurationPostProcessors = [];
    private readonly List<Func<IServiceProvider, CancellationToken, Task>> _initActions = [];

    /// <summary>
    /// Property configurations available for automatic assignment to unconfigured state properties.
    /// </summary>
    public IReadOnlyList<PropertyConfigurationRegistration> PropertyConfigurationRegistrations =>
        _propertyConfigurationRegistrations;

    public ISyncStateBuilder AddState<TState>(Action<IStateConfigurationBuilder<TState>> configure)
        where TState : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        EnsureStateNotRegistered<TState>();

        var stateBuilder = new StateConfigurationBuilder<TState>(this);
        configure(stateBuilder);
        _stateConfigurationBuilders.Add(stateBuilder);
        return this;
    }

    public ISyncStateBuilder AddStateConfiguration<TConfiguration>()
        where TConfiguration : class, IStateConfiguration, new()
    {
        AddStateConfigurationType(typeof(TConfiguration));
        return this;
    }

    public ISyncStateBuilder AddPropertyConfiguration<TConfiguration>()
        where TConfiguration : class, IPropertyConfiguration, new()
    {
        AddPropertyConfigurationType(typeof(TConfiguration));
        return this;
    }

    public ISyncStateBuilder AddConfigurationsFromAssembly(Assembly assembly, Func<Type, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var configurationTypes = GetLoadableTypes(assembly)
            .Where(type => type is { IsClass: true, IsAbstract: false, ContainsGenericParameters: false })
            .Where(type => typeof(IStateConfiguration).IsAssignableFrom(type) ||
                           typeof(IPropertyConfiguration).IsAssignableFrom(type))
            .Where(type => predicate == null || predicate(type))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();

        foreach (var configurationType in configurationTypes)
        {
            if (typeof(IStateConfiguration).IsAssignableFrom(configurationType))
            {
                AddStateConfigurationType(configurationType);
            }

            if (typeof(IPropertyConfiguration).IsAssignableFrom(configurationType))
            {
                AddPropertyConfigurationType(configurationType);
            }
        }

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

        var configuration = new SyncStateConfiguration
        {
            StateConfigurations = stateConfigurations,
            ServiceCollectionProcessors = _serviceCollectionProcessors,
            InitActions = _initActions,
            Extensions = _extensions
        };
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

    private void AddStateConfigurationType(Type configurationType)
    {
        EnsureParameterlessConstructor(configurationType);

        var stateTypes = GetGenericInterfaceArguments(configurationType, typeof(IStateConfiguration<>))
            .Select(arguments => arguments[0])
            .ToList();
        if (stateTypes.Count == 0)
        {
            throw new InvalidOperationException(
                $"Configuration type '{configurationType.FullName}' does not implement IStateConfiguration<TState>.");
        }

        var configuration = Activator.CreateInstance(configurationType)!;
        foreach (var stateType in stateTypes)
        {
            try
            {
                AddStateFromConfigurationMethod.MakeGenericMethod(stateType).Invoke(this, [configuration]);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(e.InnerException).Throw();
            }
        }
    }

    private void AddPropertyConfigurationType(Type configurationType)
    {
        if (_propertyConfigurationRegistrations.Any(registration => registration.ConfigurationType == configurationType))
        {
            return;
        }

        EnsureParameterlessConstructor(configurationType);

        var registrations = new List<PropertyConfigurationRegistration>();
        foreach (var arguments in GetGenericInterfaceArguments(configurationType, typeof(IPropertyConfiguration<>)))
        {
            var registrationType = typeof(PropertyConfigurationRegistration<,>)
                .MakeGenericType(arguments[0], configurationType);
            registrations.Add((PropertyConfigurationRegistration)Activator.CreateInstance(registrationType)!);
        }

        foreach (var arguments in GetGenericInterfaceArguments(configurationType, typeof(ICollectionPropertyConfiguration<,>)))
        {
            var registrationType = typeof(CollectionPropertyConfigurationRegistration<,,>)
                .MakeGenericType(arguments[0], arguments[1], configurationType);
            registrations.Add((PropertyConfigurationRegistration)Activator.CreateInstance(registrationType)!);
        }

        if (registrations.Count == 0)
        {
            throw new InvalidOperationException(
                $"Configuration type '{configurationType.FullName}' does not implement IPropertyConfiguration<TProperty> " +
                "or ICollectionPropertyConfiguration<TEntry, TKey>.");
        }

        _propertyConfigurationRegistrations.AddRange(registrations);
    }

    /// <summary>
    /// Adds a state from a configuration object. Invoked via reflection by <see cref="AddStateConfigurationType"/>.
    /// </summary>
    private void AddStateFromConfiguration<TState>(object configuration) where TState : class
    {
        AddState<TState>(((IStateConfiguration<TState>)configuration).Configure);
    }

    private void EnsureStateNotRegistered<TState>() where TState : class
    {
        if (_stateConfigurationBuilders.Any(builder => builder.StateType == typeof(TState)))
        {
            throw new InvalidOperationException(
                $"State type '{typeof(TState).FullName}' is already registered. Each state type can only be added once.");
        }
    }

    private static void EnsureParameterlessConstructor(Type configurationType)
    {
        if (configurationType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new InvalidOperationException(
                $"Configuration type '{configurationType.FullName}' must have a public parameterless constructor.");
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.OfType<Type>();
        }
    }

    /// <summary>
    /// Returns the type arguments of every implementation of <paramref name="genericInterfaceDefinition"/> on <paramref name="type"/>.
    /// </summary>
    private static IEnumerable<Type[]> GetGenericInterfaceArguments(Type type, Type genericInterfaceDefinition)
    {
        return type.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceDefinition)
            .Select(i => i.GetGenericArguments());
    }
}
