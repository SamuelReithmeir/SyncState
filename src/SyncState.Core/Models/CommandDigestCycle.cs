using SyncState.InternalInterfaces;

namespace SyncState.Models;

/// <summary>
/// represents an instance in which multiple commands may be executed upon the global SyncState service
/// </summary>
public class CommandDigestCycle:IDisposable
{
    public required IInternalSyncStateService SyncStateService { get; init; }
    public required IServiceProvider CallerServiceProvider { get; init; }

    public void Dispose()
    {
        SyncStateService.DisposeCommandDigestCycle(this);
    }
}