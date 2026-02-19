using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SyncState.Enums;
using SyncState.Interfaces.Interceptors;
using SyncState.InternalInterfaces;
using SyncState.Models;
using SyncState.Models.Configuration;
using SyncState.Models.InterceptorContexts;
using SyncState.Utils;

namespace SyncState.Services.Managers;

public abstract class BasePropertyManager<TProperty> : IInternalPropertyManager<TProperty>
{
    protected readonly PropertyConfiguration<TProperty> Configuration;
    protected readonly IServiceProvider RootServiceProvider;
    protected readonly IInternalSyncEventHub SyncEventHub;
    private Func<IServiceProvider, CancellationToken, Task<TProperty>> Gatherer => Configuration.Gatherer;
    private PropertyInfo PropertyInfo => Configuration.PropertyInfo;
    private PropertyGatheringServiceScopeBehavior ScopeBehavior => Configuration.ScopeBehavior;

    private readonly ConcurrentDictionary<Type, List<CommandHandlerConfiguration>>
        _commandHandlerConfigurationsByType = new();

    private List<CommandHandlerConfiguration> CommandHandlerConfigurations =>
        Configuration.CommandHandlerConfigurations;

    public BasePropertyManager(PropertyConfiguration<TProperty> configuration, IServiceProvider rootServiceProvider,
        IInternalSyncEventHub syncEventHub)
    {
        Configuration = configuration;
        RootServiceProvider = rootServiceProvider;
        SyncEventHub = syncEventHub;
    }

    public abstract void SetValue(TProperty newValue);
    public abstract TProperty GetCurrentValue(bool allowUnpublished = false);

    public async Task ReloadValueAsync(CancellationToken cancellationToken = default)
    {
        TProperty value;
        switch (ScopeBehavior)
        {
            case PropertyGatheringServiceScopeBehavior.ResolveFromRoot:
                value = await Gatherer(RootServiceProvider, cancellationToken);
                break;
            case PropertyGatheringServiceScopeBehavior.CreateOwnScope:
                using (var scope = RootServiceProvider.CreateScope())
                {
                    value = await Gatherer(scope.ServiceProvider, cancellationToken);
                }

                break;
            case PropertyGatheringServiceScopeBehavior.ShareScope:
                if (SyncStateService.CurrentExecutionServiceProvider.Value is not { } serviceProvider)
                {
                    throw new InvalidOperationException(
                        $"Property with scope behavior {nameof(PropertyGatheringServiceScopeBehavior.ShareScope)} " +
                        $"was attempted to be gathered outside of a command digest cycle.");
                }

                value = await Gatherer(serviceProvider, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        SetValue(value);
    }

    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (Configuration.InterceptorTypes.Count == 0)
        {
            await InitializeAsyncImpl(cancellationToken);
            return;
        }

        if (SyncStateService.CurrentExecutionServiceProvider.Value is not { } serviceProvider)
        {
            throw new InvalidOperationException("No service provider available to initialize interceptors");
        }

        var interceptors = Configuration.InterceptorTypes
            .Select(type => serviceProvider.GetRequiredService(type))
            .OfType<IPropertyInterceptor<TProperty>>()
            .ToList();

        var pipeline =
            InterceptorUtils.CreateInterceptorPipeline<IPropertyInterceptor<TProperty>, PropertyInitializationContext<TProperty>>(
                interceptors,
                (_, ct) => InitializeAsyncImpl(ct),
                interceptor => interceptor.InitializeAsync
            );

        await pipeline(new PropertyInitializationContext<TProperty>(this), cancellationToken);
    }
    public virtual async Task InitializeAsyncImpl(CancellationToken cancellationToken = default)
    {
        await ReloadValueAsync(cancellationToken);
        await CommitChangesAsync(cancellationToken);
    }

    public virtual void ApplyToStateObject(object stateObject)
    {
        PropertyInfo.SetValue(stateObject, GetCurrentValue());
    }

    public bool HandlesCommandType<TCommand>() where TCommand : notnull
    {
        return GetCommandHandlerConfigurationsForCommandType(typeof(TCommand)).Count != 0;
    }

    public abstract void DiscardChanges();

    public abstract Task<bool> CommitChangesAsync(CancellationToken cancellationToken);

    public async Task HandleCommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : notnull
    {
        if (Configuration.InterceptorTypes.Count == 0)
        {
            await HandleCommandAsyncImpl(command, cancellationToken);
            return;
        }

        if (SyncStateService.CurrentExecutionServiceProvider.Value is not { } serviceProvider)
        {
            throw new InvalidOperationException("No service provider available to initialize interceptors");
        }

        var interceptors = Configuration.InterceptorTypes
            .Select(type => serviceProvider.GetRequiredService(type))
            .OfType<IPropertyInterceptor<TProperty>>()
            .ToList();

        var pipeline =
            InterceptorUtils
                .CreateInterceptorPipeline<IPropertyInterceptor<TProperty>,
                    PropertyCommandContext<TProperty, TCommand>>(
                    interceptors,
                    (context, ct) => HandleCommandAsyncImpl(context.Command, ct),
                    interceptor => interceptor.HandleCommandAsync
                );

        await pipeline(new PropertyCommandContext<TProperty, TCommand>(command, this), cancellationToken);
    }

    public async Task HandleCommandAsyncImpl<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : notnull
    {
        foreach (var configuration in GetCommandHandlerConfigurationsForCommandType(typeof(TCommand))
                     .Cast<CommandHandlerConfiguration<TCommand, TProperty>>()
                     .Where(config => config.CommandFilter?.Invoke(command) ?? true))
        {
            await configuration.OnCommandAsync(command, this, cancellationToken);
        }
    }

    private List<CommandHandlerConfiguration> GetCommandHandlerConfigurationsForCommandType(Type commandType)
    {
        if (_commandHandlerConfigurationsByType.TryGetValue(commandType, out var configurations))
        {
            return configurations;
        }

        configurations = CommandHandlerConfigurations
            .Where(config => config.CommandTypeFilter(commandType))
            .ToList();

        _commandHandlerConfigurationsByType[commandType] = configurations;
        return configurations;
    }
}

public class DefaultPropertyManager<TProperty> : BasePropertyManager<TProperty>
{
    private TProperty _value = default!;
    private Option<TProperty> _pendingValue = Option<TProperty>.None;

    public DefaultPropertyManager(PropertyConfiguration<TProperty> configuration, IServiceProvider rootServiceProvider,
        IInternalSyncEventHub syncEventHub)
        : base(configuration, rootServiceProvider, syncEventHub)
    {
    }

    public override void SetValue(TProperty newValue)
    {
        if (Configuration.EqualityComparer.Equals(newValue, GetCurrentValue(allowUnpublished: true)))
        {
            return;
        }

        _pendingValue = Option<TProperty>.Some(newValue);
    }

    public override TProperty GetCurrentValue(bool allowUnpublished = false)
    {
        if (allowUnpublished && _pendingValue.TryGetValue(out var value))
        {
            return value;
        }

        return _value;
    }

    public override void DiscardChanges()
    {
        _pendingValue = Option<TProperty>.None;
    }

    public override Task<bool> CommitChangesAsync(CancellationToken cancellationToken)
    {
        if (_pendingValue.TryGetValue(out var newValue))
        {
            foreach (var eventEmitterConfiguration in Configuration.EventEmitters)
            {
                eventEmitterConfiguration.EmitEvent(_value, newValue, SyncEventHub);
            }

            _value = newValue;
            _pendingValue = Option<TProperty>.None;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}