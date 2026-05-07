using SyncState.Interfaces;
using SyncState.Models.Diagnostics;
using SyncState.Services;

namespace SyncState.Diagnostics.Extensions;

public static class SyncCommandServiceExtensions
{
    
    /// <summary>
    /// Executes all buffered commands and returns the state changes that happened during the execution.
    /// </summary>
    /// <param name="syncCommandService"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Task<CommandDigestCycleCommitResult> DiagnosticExecuteBufferedCommandsImplAsync(
        this ISyncCommandService syncCommandService,
        CancellationToken cancellationToken = default)
    {
        if (syncCommandService is not SyncCommandService impl)
        {
            throw new InvalidOperationException(
                "Diagnostic extension method can only be used on original SyncCommandService instance");
        }

        return impl.ExecuteBufferedCommandsImplAsync(cancellationToken);
    }
}