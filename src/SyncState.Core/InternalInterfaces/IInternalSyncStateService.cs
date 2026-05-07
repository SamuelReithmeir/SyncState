using SyncState.Interfaces;
using SyncState.Models;
using SyncState.Models.Diagnostics;

namespace SyncState.InternalInterfaces;

public interface IInternalSyncStateService : ISyncStateService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// sends a command to state managers which are configured to handle it
    /// </summary>
    /// <param name="command"></param>
    /// <param name="commandDigestCycle">the command digest cycle to execute the command on</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TCommand"></typeparam>
    Task HandleAsync<TCommand>(TCommand command,CommandDigestCycle commandDigestCycle, CancellationToken cancellationToken = default) where TCommand : class;

    /// <summary>
    /// acquire a new command digest cycle to execute commands on
    /// </summary>
    /// <param name="serviceProvider"> the service provider of the caller, expected to be scoped if properties have scoped dependencies for gathering</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<CommandDigestCycle> AcquireCommandDigestCycleAsync(IServiceProvider serviceProvider,CancellationToken cancellationToken = default);
    
    /// <summary>
    /// commit the changes of the command digest cycle
    /// </summary>
    /// <param name="commandDigestCycle"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<CommandDigestCycleCommitResult> CommitCommandDigestCycleAsync(CommandDigestCycle commandDigestCycle,CancellationToken cancellationToken = default);
    
    /// <summary>
    /// discard all pending property changes of the command digest cycle
    /// </summary>
    void DisposeCommandDigestCycle(CommandDigestCycle commandDigestCycle);

    /// <summary>
    /// resolves a singleton service from the service provider
    /// <remarks>used for internal extension</remarks>
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <returns></returns>
    TService GetService<TService>() where TService : class;

    /// <summary>
    /// add an event to the event stream
    /// </summary>
    /// <param name="event"></param>
    /// <typeparam name="TEvent"></typeparam>
    void PublishEvent<TEvent>(TEvent @event) where TEvent : notnull;
}