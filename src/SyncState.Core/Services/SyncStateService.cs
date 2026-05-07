using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using SyncState.Factories;
using SyncState.InternalInterfaces;
using SyncState.Models;
using SyncState.Models.Configuration;
using SyncState.Models.Diagnostics;

namespace SyncState.Services;

public class SyncStateService : IInternalSyncStateService
{
    private readonly SyncStateConfiguration _configuration;
    private readonly IServiceProvider _rootServiceProvider;
    private readonly IInternalSyncEventHub _eventHub;
    private readonly ConcurrentDictionary<Type, IInternalStateManager> _stateManagersByType = [];
    private readonly ConcurrentDictionary<Guid, IInternalStateManager> _stateManagersById = [];

    public static readonly AsyncLocal<IServiceProvider?> CurrentExecutionServiceProvider = new();
    private readonly WeakReference<CommandDigestCycle?> _currentCommandDigestCycle = new(null);
    private readonly SemaphoreSlim _commandDigestCycleLock = new(1);

    public SyncStateService(SyncStateConfiguration configuration, IServiceProvider rootServiceProvider,
        IInternalSyncEventHub eventHub)
    {
        _configuration = configuration;
        _rootServiceProvider = rootServiceProvider;
        _eventHub = eventHub;
    }

    #region StateGetters

    public async Task<TState> GetStateAsync<TState>(CancellationToken cancellationToken = default) where TState : class
    {
        if (!_stateManagersByType.TryGetValue(typeof(TState), out var untypedManager) ||
            untypedManager is not IInternalStateManager<TState> manager)
        {
            throw new InvalidOperationException(
                $"No state manager registered for state type {typeof(TState).FullName}");
        }

        return await manager.GetStateStream(cancellationToken).ReadAsync(cancellationToken);
    }

    public IAsyncEnumerable<TState> GetStateEnumerable<TState>(CancellationToken cancellationToken = default)
        where TState : class
    {
        if (!_stateManagersByType.TryGetValue(typeof(TState), out var untypedManager) ||
            untypedManager is not IInternalStateManager<TState> manager)
        {
            throw new InvalidOperationException(
                $"No state manager registered for state type {typeof(TState).FullName}");
        }

        return manager.GetStateStream(cancellationToken).ReadAllAsync(cancellationToken);
    }

    public ChannelReader<TState> GetStateChannelReader<TState>(CancellationToken cancellationToken = default)
        where TState : class
    {
        if (!_stateManagersByType.TryGetValue(typeof(TState), out var untypedManager) ||
            untypedManager is not IInternalStateManager<TState> manager)
        {
            throw new InvalidOperationException(
                $"No state manager registered for state type {typeof(TState).FullName}");
        }

        return manager.GetStateStream(cancellationToken);
    }

    public void RegisterStateCallback<TState>(Action<TState> onStateUpdated, bool invokeForCurrentState = true)
        where TState : class
    {
        if (!_stateManagersByType.TryGetValue(typeof(TState), out var untypedManager) ||
            untypedManager is not IInternalStateManager<TState> manager)
        {
            throw new InvalidOperationException(
                $"No state manager registered for state type {typeof(TState).FullName}");
        }

        var reader = manager.GetStateStream();
        _ = Task.Run(async () =>
        {
            await foreach (var state in reader.ReadAllAsync())
            {
                onStateUpdated(state);
            }
        });
    }

    public async IAsyncEnumerable<TEvent> GetEventStreamAsync<TEvent>(
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where TEvent : notnull
    {
        var reader = _eventHub.GetEventStream<TEvent>(cancellationToken);

        await foreach (var batch in reader.ReadAllAsync(cancellationToken))
        {
            foreach (var @event in batch.Events)
            {
                yield return @event;
            }
        }
    }

    public async IAsyncEnumerable<EventBatch<TEvent>> GetBatchedEventStreamAsync<TEvent>(
        [EnumeratorCancellation] CancellationToken cancellationToken = default) where TEvent : notnull
    {
        var reader = _eventHub.GetEventStream<TEvent>(cancellationToken);

        await foreach (var batch in reader.ReadAllAsync(cancellationToken))
        {
            yield return batch;
        }
    }

    public async Task<(TState, IAsyncEnumerable<TEvent>)> GetCurrentStateAndSubsequentEventsAsync<TState, TEvent>(
        CancellationToken cancellationToken = default) where TEvent : notnull where TState : class
    {
        //we need to lock here to ensure that no commands are being processed that could change the state between getting the state and registering the event stream
        await _commandDigestCycleLock.WaitAsync(cancellationToken);
        try
        {
            var stateManager = (IInternalStateManager<TState>)_stateManagersByType[typeof(TState)];
            var state = await stateManager.GetStateStream(cancellationToken).ReadAsync(cancellationToken);
            return (state, GetEventStreamAsync<TEvent>(cancellationToken));
        }
        finally
        {
            _commandDigestCycleLock.Release();
        }
    }

    public async Task<(TState, IAsyncEnumerable<EventBatch<TEvent>>)>
        GetCurrentStateAndSubsequentBatchedEventsAsync<TState, TEvent>(CancellationToken cancellationToken = default)
        where TEvent : notnull where TState : class
    {
        //we need to lock here to ensure that no commands are being processed that could change the state between getting the state and registering the event stream
        await _commandDigestCycleLock.WaitAsync(cancellationToken);
        try
        {
            var stateManager = (IInternalStateManager<TState>)_stateManagersByType[typeof(TState)];
            var state = await stateManager.GetStateStream(cancellationToken).ReadAsync(cancellationToken);
            return (state, GetBatchedEventStreamAsync<TEvent>(cancellationToken));
        }
        finally
        {
            _commandDigestCycleLock.Release();
        }
    }

    #endregion

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Block command digestion scope creation during initialization
        await _commandDigestCycleLock.WaitAsync(cancellationToken);
        try
        {
            await using var scope = _rootServiceProvider.CreateAsyncScope();
            CurrentExecutionServiceProvider.Value = scope.ServiceProvider;

            //execute configured init actions
            foreach (var initAction in _configuration.InitActions)
            {
                await initAction(_rootServiceProvider, cancellationToken);
            }

            //init state managers
            foreach (var stateConfiguration in _configuration.StateConfigurations)
            {
                var stateManager = _rootServiceProvider.GetRequiredService<IStateManagerFactory>()
                    .CreateStateManager(stateConfiguration);
                _stateManagersByType.TryAdd(stateConfiguration.StateType, stateManager);
                _stateManagersById.TryAdd(stateConfiguration.Id, stateManager);
                await stateManager.InitializeAsync(cancellationToken);
            }
        }
        finally
        {
            _commandDigestCycleLock.Release();
        }
    }

    public async Task HandleAsync<TCommand>(TCommand command, CommandDigestCycle commandDigestCycle,
        CancellationToken cancellationToken = default) where TCommand : class
    {
        if (!_currentCommandDigestCycle.TryGetTarget(out var existingCycle) ||
            existingCycle != commandDigestCycle)
        {
            throw new InvalidOperationException("The provided command digest cycle is not the current active cycle.");
        }

        CurrentExecutionServiceProvider.Value = existingCycle.CallerServiceProvider;

        foreach (var stateManager in _stateManagersById.Values)
        {
            await stateManager.HandleCommandAsync(command, cancellationToken);
        }
    }

    public async Task<CommandDigestCycle> AcquireCommandDigestCycleAsync(IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await _commandDigestCycleLock.WaitAsync(cancellationToken);
        var commandDigestCycle = new CommandDigestCycle
        {
            SyncStateService = this,
            CallerServiceProvider = serviceProvider,
        };
        _currentCommandDigestCycle.SetTarget(commandDigestCycle);
        CurrentExecutionServiceProvider.Value = serviceProvider;
        return commandDigestCycle;
    }

    public async Task<CommandDigestCycleCommitResult> CommitCommandDigestCycleAsync(
        CommandDigestCycle commandDigestCycle,
        CancellationToken cancellationToken = default)
    {
        if (!_currentCommandDigestCycle.TryGetTarget(out var existingCycle) ||
            existingCycle != commandDigestCycle)
        {
            throw new InvalidOperationException("The provided command digest cycle is not the current active cycle.");
        }

        List<StateChangeData> stateChanges = [];

        foreach (var stateManager in _stateManagersById.Values)
        {
            if (await stateManager.CommitChangesAsync(cancellationToken) is { } stateChangeData)
            {
                stateChanges.Add(stateChangeData);
            }
        }

        await _eventHub.BroadcastAsync(cancellationToken);

        return new CommandDigestCycleCommitResult(stateChanges);
    }

    public void DisposeCommandDigestCycle(CommandDigestCycle commandDigestCycle)
    {
        if (!_currentCommandDigestCycle.TryGetTarget(out var existingCycle) ||
            existingCycle != commandDigestCycle)
        {
            throw new InvalidOperationException("The provided command digest cycle is not the current active cycle.");
        }

        foreach (var stateManager in _stateManagersById.Values)
        {
            stateManager.DiscardChanges();
        }

        _currentCommandDigestCycle.SetTarget(null);
        CurrentExecutionServiceProvider.Value = null;
        _commandDigestCycleLock.Release();
    }

    public TService GetService<TService>() where TService : class
    {
        return _rootServiceProvider.GetRequiredService<TService>();
    }

    public void PublishEvent<TEvent>(TEvent @event) where TEvent : notnull
    {
        _eventHub.QueueEvent(@event);
    }
}