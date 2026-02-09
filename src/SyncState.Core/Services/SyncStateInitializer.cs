using Microsoft.Extensions.Hosting;
using SyncState.InternalInterfaces;

namespace SyncState.Services;

public class SyncStateInitializer:BackgroundService
{
    private readonly IInternalSyncStateService _internalSyncStateService;

    public SyncStateInitializer(IInternalSyncStateService internalSyncStateService)
    {
        _internalSyncStateService = internalSyncStateService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _internalSyncStateService.InitializeAsync(stoppingToken);
    }
}