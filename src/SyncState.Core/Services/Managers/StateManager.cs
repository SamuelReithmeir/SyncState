using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using SyncState.Factories;
using SyncState.Interfaces.Interceptors;
using SyncState.InternalInterfaces;
using SyncState.Models.Configuration;
using SyncState.Models.InterceptorContexts;
using SyncState.Utils;

namespace SyncState.Services.Managers;

public class StateManager<TState> : IInternalStateManager<TState> where TState : class
{
    private readonly StateConfiguration<TState> _stateConfiguration;
    private readonly IServiceProvider _rootServiceProvider;

    private readonly Dictionary<Guid, IInternalPropertyManager> _propertyManagers = [];

    private readonly ConcurrentDictionary<Type, List<Guid>> _commandTypeRegisteredPropertyManagers = [];

    private readonly ConcurrentBag<Channel<TState>> _stateChannels = [];
    private TState _currentState = null!;

    public StateManager(StateConfiguration<TState> stateConfiguration, IServiceProvider rootServiceProvider)
    {
        _stateConfiguration = stateConfiguration;
        _rootServiceProvider = rootServiceProvider;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_stateConfiguration.InterceptorTypes.Count == 0)
        {
            await InitializeAsyncImpl(cancellationToken);
            return;
        }

        if (SyncStateService.CurrentExecutionServiceProvider.Value is not { } serviceProvider)
        {
            throw new InvalidOperationException("No service provider available to initialize interceptors");
        }
        var interceptors = _stateConfiguration.InterceptorTypes
            .Select(type => serviceProvider.GetRequiredService(type))
            .OfType<IStateInterceptor<TState>>()
            .ToList();

        var pipeline =
            InterceptorUtils.CreateInterceptorPipeline<IStateInterceptor<TState>, StateInitializationContext<TState>>(
                interceptors,
                (_, ct) => InitializeAsyncImpl(ct),
                interceptor => interceptor.InitializeAsync
            );
        
        await pipeline(new StateInitializationContext<TState>(this, _stateConfiguration), cancellationToken);
    }

    public async Task InitializeAsyncImpl(CancellationToken cancellationToken = default)
    {
        //create property managers
        foreach (var propertyConfiguration in _stateConfiguration.Properties)
        {
            var propertyManager = _rootServiceProvider.GetRequiredService<IPropertyManagerFactory>()
                .CreatePropertyManager(propertyConfiguration);
            _propertyManagers.Add(propertyConfiguration.Id, propertyManager);
        }

        //load initial state
        var initialState = await LoadInitialStateAsync(cancellationToken);
        HandleStateChange(initialState);
    }

    public async Task<bool> CommitChangesAsync(CancellationToken cancellationToken = default)
    {
        var anyChanges = false;
        foreach (var propertyManager in _propertyManagers.Values)
        {
            anyChanges |= await propertyManager.CommitChangesAsync(cancellationToken);
        }

        if (!anyChanges)
        {
            return false;
        }

        var newState = Activator.CreateInstance<TState>();
        foreach (var propertyManager in _propertyManagers.Values)
        {
            propertyManager.ApplyToStateObject(newState);
        }

        HandleStateChange(newState);
        return anyChanges;
    }

    public ChannelReader<TState> GetStateStream(CancellationToken cancellationToken = default)
    {
        //create channel
        var channel = Channel.CreateBounded<TState>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropOldest
            }
        );
        _stateChannels.Add(channel);
        //write initial state to channel
        if (!channel.Writer.TryWrite(_currentState))
        {
            throw new InvalidOperationException("Failed to write initial state to channel");
        }

        return channel.Reader;
    }

    public async Task HandleCommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : notnull
    {
        
        if (_stateConfiguration.InterceptorTypes.Count == 0)
        {
            await HandleCommandAsyncImpl(command, cancellationToken);
            return;
        }

        if (SyncStateService.CurrentExecutionServiceProvider.Value is not { } serviceProvider)
        {
            throw new InvalidOperationException("No service provider available to initialize interceptors");
        }
        var interceptors = _stateConfiguration.InterceptorTypes
            .Select(type => serviceProvider.GetRequiredService(type))
            .OfType<IStateInterceptor<TState>>()
            .ToList();

        var pipeline =
            InterceptorUtils.CreateInterceptorPipeline<IStateInterceptor<TState>, StateCommandContext<TState,TCommand>>(
                interceptors,
                (context, ct) => HandleCommandAsyncImpl(context.Command, ct),
                interceptor => interceptor.HandleCommandAsync
            );
        
        await pipeline(new StateCommandContext<TState, TCommand>(command, this, _stateConfiguration), cancellationToken);
    }
    public async Task HandleCommandAsyncImpl<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : notnull
    {
        foreach (var (id, propertyManager) in GetPropertyManagerIdsForCommandType<TCommand>()
                     .Select(id => (id, _propertyManagers[id])))
        {
            await propertyManager.HandleCommandAsync(command, cancellationToken);
        }
    }

    public void DiscardChanges()
    {
        foreach (var propertyManager in _propertyManagers.Values)
        {
            propertyManager.DiscardChanges();
        }
    }

    private void HandleStateChange(TState newState)
    {
        _currentState = newState;
        foreach (var channel in _stateChannels)
        {
            channel.Writer.TryWrite(newState);
        }
    }

    private async Task<TState> LoadInitialStateAsync(CancellationToken cancellationToken)
    {
        var instance = Activator.CreateInstance<TState>();
        //initialize all property managers and apply their values to the state object
        foreach (var propertyManager in _propertyManagers.Values)
        {
            await propertyManager.InitializeAsync(cancellationToken);
            propertyManager.ApplyToStateObject(instance);
        }

        return instance;
    }

    private IEnumerable<Guid> GetPropertyManagerIdsForCommandType<TCommand>()
        where TCommand : notnull
    {
        if (!_commandTypeRegisteredPropertyManagers.TryGetValue(typeof(TCommand), out var propertyManagers))
        {
            //find all property managers that are registered for this command type
            propertyManagers = _propertyManagers
                .Where(kvp => kvp.Value.HandlesCommandType<TCommand>())
                .Select(kvp => kvp.Key)
                .ToList();
            _commandTypeRegisteredPropertyManagers.TryAdd(typeof(TCommand), propertyManagers);
        }

        return propertyManagers;
    }

    public void SetValue(TState newValue)
    {
        HandleStateChange(newValue);
    }

    public TState GetCurrentValue()
    {
        return _currentState;
    }
}