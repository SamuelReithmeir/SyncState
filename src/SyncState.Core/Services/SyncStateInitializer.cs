using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using SyncState.Diagnostics;
using SyncState.InternalInterfaces;

namespace SyncState.Services;

public class SyncStateInitializer:BackgroundService
{
    private readonly IInternalSyncStateService _internalSyncStateService;

    public SyncStateInitializer(IInternalSyncStateService internalSyncStateService)
    {
        _internalSyncStateService = internalSyncStateService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = SyncStateActivitySource.Instance.StartActivity(
            "SyncState.Initialize", ActivityKind.Internal);

        try
        {
            await _internalSyncStateService.InitializeAsync(stoppingToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            activity?.AddTag("exception.message", ex.Message);
            throw;
        }
    }
}