using SyncState.Interfaces;
using SyncState.InternalInterfaces;
using SyncState.Models;

namespace SyncState.Services;

public class SyncCommandService : ISyncCommandService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInternalSyncStateService _syncStateService;

    private bool _bufferingEnabled;
    private List<IBufferedCommand> _bufferedCommands = [];


    public SyncCommandService(IServiceProvider serviceProvider, IInternalSyncStateService syncStateService)
    {
        _serviceProvider = serviceProvider;
        _syncStateService = syncStateService;
    }

    public async Task HandleAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
    {
        if (_bufferingEnabled)
        {
            _bufferedCommands.Add(new BufferedCommand<TCommand>(command));
        }
        else
        {
            //instantly execute and digest command in its own digest cycle
            using var commandDigestCycle = await _syncStateService
                .AcquireCommandDigestCycleAsync(_serviceProvider, cancellationToken);
            await _syncStateService.HandleAsync(command, commandDigestCycle, cancellationToken);
            await _syncStateService.CommitCommandDigestCycleAsync(commandDigestCycle, cancellationToken);
        }
    }

    public void StartCommandBuffer()
    {
        _bufferingEnabled = true;
        _bufferedCommands = [];
    }

    public void ClearCommandBuffer()
    {
        _bufferedCommands.Clear();
        _bufferingEnabled = false;
    }

    public async Task ExecuteBufferedCommandsAsync(CancellationToken cancellationToken = default)
    {
        using var commandDigestCycle = await _syncStateService
            .AcquireCommandDigestCycleAsync(_serviceProvider, cancellationToken);
        foreach (var bufferedNotification in _bufferedCommands)
        {
            await bufferedNotification.DispatchAsync(_syncStateService, commandDigestCycle, cancellationToken);
        }
        
        await _syncStateService.CommitCommandDigestCycleAsync(commandDigestCycle, cancellationToken);

        _bufferedCommands.Clear();
        _bufferingEnabled = false;
    }
}

internal interface IBufferedCommand
{
    Task DispatchAsync(IInternalSyncStateService syncStateService, CommandDigestCycle commandDigestCycle,
        CancellationToken cancellationToken);
}

internal sealed class BufferedCommand<TCommand>(TCommand command) : IBufferedCommand where TCommand : class
{
    public Task DispatchAsync(IInternalSyncStateService syncStateService, CommandDigestCycle commandDigestCycle,
        CancellationToken cancellationToken)
    {
        return syncStateService.HandleAsync(command, commandDigestCycle, cancellationToken);
    }
}
