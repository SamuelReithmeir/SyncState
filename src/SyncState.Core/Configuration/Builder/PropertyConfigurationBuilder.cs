using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SyncState.Configuration.Builder.Common;
using SyncState.Configuration.Interfaces;
using SyncState.Configuration.InternalInterfaces;
using SyncState.Enums;
using SyncState.Interfaces.Interceptors;
using SyncState.Interfaces.Managers;
using SyncState.Models.Configuration;
using SyncState.Models.Configuration.EventEmitters;
using SyncState.Services.Managers;

namespace SyncState.Configuration.Builder;

internal abstract class PropertyConfigurationBuilder
{
    public abstract PropertyConfiguration Build();
}

internal class PropertyConfigurationBuilder<TState, TProperty> : PropertyConfigurationBuilder,
    IInternalPropertyConfigurationBuilder<TState, TProperty>
    where TState : class
{
    protected readonly StateConfigurationBuilder<TState> ParentBuilder;
    protected readonly PropertyInfo PropertyInfo;
    protected Func<IServiceProvider, CancellationToken, Task<TProperty>>? Gatherer;
    protected Type? PropertyManagerType;
    protected PropertyGatheringServiceScopeBehavior? _ScopeBehavior;
    protected readonly List<CommandHandlerConfiguration> CommandHandlers = [];
    protected readonly List<EventEmitterConfiguration<TProperty>> EventEmitters = [];
    protected readonly Dictionary<Type, object> Extensions = new();
    protected readonly List<Action<PropertyConfiguration<TProperty>>> ConfigurationPostProcessors = [];
    protected readonly List<Action> BuildActions = [];
    protected readonly List<Type> InterceptorTypes = [];
    protected IEqualityComparer<TProperty>? EqualityComparer;


    public PropertyConfigurationBuilder(StateConfigurationBuilder<TState> parentBuilder, PropertyInfo propertyInfo)
    {
        ParentBuilder = parentBuilder;
        PropertyInfo = propertyInfo;
        
        this.AddDefaultCommandHandlers();
    }

    public IPropertyConfigurationBuilder<TState, TProperty> GatherFrom<TService>(Func<TService, TProperty> gatherer)
        where TService : notnull
    {
        Gatherer = (serviceProvider, _) =>
        {
            var service = serviceProvider.GetRequiredService<TService>();
            var result = gatherer(service);
            return Task.FromResult(result);
        };
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> GatherFromAsync<TService>(
        Func<TService, Task<TProperty>> gatherer) where TService : notnull
    {
        Gatherer = async (serviceProvider, _) =>
        {
            var service = serviceProvider.GetRequiredService<TService>();
            var result = await gatherer(service);
            return result;
        };
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> GatherFromAsync<TService>(
        Func<TService, CancellationToken, Task<TProperty>> gatherer) where TService : notnull
    {
        Gatherer = async (serviceProvider, cancellationToken) =>
        {
            var service = serviceProvider.GetRequiredService<TService>();
            var result = await gatherer(service, cancellationToken);
            return result;
        };
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> WithPropertyManager<TPropertyManager>()
        where TPropertyManager : IPropertyManager<TProperty>
    {
        PropertyManagerType = typeof(TPropertyManager);
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(
        Action<TCommand, IPropertyManager<TProperty>> handler) where TCommand : notnull
    {
        CommandHandlers.Add(new CommandHandlerConfiguration<TCommand, TProperty>(null, Handler));
        return this;

        Task Handler(TCommand command, IPropertyManager<TProperty> propertyManager, CancellationToken cancellationToken)
        {
            handler(command, propertyManager);
            return Task.CompletedTask;
        }
    }

    public IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(
        Func<TCommand, IPropertyManager<TProperty>, Task> handler) where TCommand : notnull
    {
        CommandHandlers.Add(new CommandHandlerConfiguration<TCommand, TProperty>(null, Handler));
        return this;

        Task Handler(TCommand command, IPropertyManager<TProperty> propertyManager, CancellationToken cancellationToken)
        {
            return handler(command, propertyManager);
        }
    }

    public IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(
        Func<TCommand, IPropertyManager<TProperty>, CancellationToken, Task> handler) where TCommand : notnull
    {
        CommandHandlers.Add(new CommandHandlerConfiguration<TCommand, TProperty>(null, Handler));
        return this;

        Task Handler(TCommand command, IPropertyManager<TProperty> propertyManager, CancellationToken cancellationToken)
        {
            return handler(command, propertyManager, cancellationToken);
        }
    }

    public IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Func<TCommand, bool> commandFilter,
        Action<TCommand, IPropertyManager<TProperty>> handler) where TCommand : notnull
    {
        CommandHandlers.Add(new CommandHandlerConfiguration<TCommand, TProperty>(commandFilter, Handler));
        return this;

        Task Handler(TCommand command, IPropertyManager<TProperty> propertyManager, CancellationToken cancellationToken)
        {
            handler(command, propertyManager);
            return Task.CompletedTask;
        }
    }

    public IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Func<TCommand, bool> commandFilter,
        Func<TCommand, IPropertyManager<TProperty>, Task> handler) where TCommand : notnull
    {
        CommandHandlers.Add(new CommandHandlerConfiguration<TCommand, TProperty>(commandFilter, Handler));
        return this;

        Task Handler(TCommand command, IPropertyManager<TProperty> propertyManager, CancellationToken cancellationToken)
        {
            return handler(command, propertyManager);
        }
    }

    public IPropertyConfigurationBuilder<TState, TProperty> On<TCommand>(Func<TCommand, bool> commandFilter,
        Func<TCommand, IPropertyManager<TProperty>, CancellationToken, Task> handler) where TCommand : notnull
    {
        CommandHandlers.Add(new CommandHandlerConfiguration<TCommand, TProperty>(commandFilter, handler));
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> Emit<TEvent>(
        Func<TProperty, TProperty, TEvent?> eventFactory) where TEvent : notnull
    {
        EventEmitters.Add(new EventEmitterConfiguration<TProperty>
        {
            EmitEvent = (newProperty, oldProperty, eventService) =>
            {
                if( eventFactory(newProperty, oldProperty) is { } evt)
                {
                    eventService.QueueEvent(evt);
                }
            }
        });
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> ScopeBehavior(
        PropertyGatheringServiceScopeBehavior scopeBehavior)
    {
        _ScopeBehavior = scopeBehavior;
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> WithEqualityComparer(IEqualityComparer<TProperty> equalityComparer)
    {
        EqualityComparer = equalityComparer;
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> WithInterceptor<TInterceptor>() where TInterceptor : class, IPropertyInterceptor<TProperty>
    {
        InterceptorTypes.Add(typeof(TInterceptor));
        ParentBuilder.GetSyncStateBuilder().AddServiceCollectionProcessor(services =>
        {
            services.AddTransient<IPropertyInterceptor<TProperty>, TInterceptor>();
        });
        return this;
    }

    public IPropertyConfigurationBuilder<TState, TProperty> AddExtension<TExtension>(TExtension extension)
        where TExtension : class
    {
        Extensions[typeof(TExtension)] = extension;
        return this;
    }

    public IInternalStateConfigurationBuilder<TState> GetStateBuilder()
    {
        return ParentBuilder;
    }

    public IInternalPropertyConfigurationBuilder<TState, TProperty> AddBuildAction(Action buildAction)
    {
        BuildActions.Add(buildAction);
        return this;
    }

    public IInternalPropertyConfigurationBuilder<TState, TProperty> AddConfigurationPostProcessor(
        Action<PropertyConfiguration<TProperty>> processor)
    {
        ConfigurationPostProcessors.Add(processor);
        return this;
    }

    public override PropertyConfiguration Build()
    {
        if (Gatherer == null)
        {
            throw new InvalidOperationException(
                $"Gatherer function must be specified for property '{PropertyInfo.Name}'.");
        }

        if (PropertyManagerType == null)
        {
            PropertyManagerType = typeof(DefaultPropertyManager<TProperty>);
        }
        
        var equalityComparer = EqualityComparer ?? EqualityComparer<TProperty>.Default;

        var scopeBehavior = _ScopeBehavior ?? PropertyGatheringServiceScopeBehavior.ShareScope;

        var configuration = new PropertyConfiguration<TProperty>
        {
            PropertyInfo = PropertyInfo,
            Gatherer = Gatherer,
            ScopeBehavior = scopeBehavior,
            PropertyManagerType = PropertyManagerType,
            EqualityComparer = equalityComparer,
            CommandHandlerConfigurations = CommandHandlers,
            EventEmitters = EventEmitters,
            InterceptorTypes = InterceptorTypes,
            Extensions = Extensions
        };

        foreach (var buildAction in BuildActions)
        {
            buildAction();
        }

        foreach (var processor in ConfigurationPostProcessors)
        {
            processor(configuration);
        }

        return configuration;
    }

    IInternalPropertyConfigurationBuilder<TState, TProperty> IInternalPropertyConfigurationBuilder<TState, TProperty>.
        AddExtension<TExtension>(TExtension extension)
    {
        Extensions[typeof(TExtension)] = extension;
        return this;
    }
}