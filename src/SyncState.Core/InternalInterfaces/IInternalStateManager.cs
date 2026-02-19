using System.Threading.Channels;
using SyncState.Interfaces.Managers;

namespace SyncState.InternalInterfaces;

public interface IInternalStateManager
{
    
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle a command digestion cycle for a specific command type.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TCommand"></typeparam>
    Task HandleCommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : notnull;
    
    /// <summary>
    /// Incorporate all pending property changes into property managers and return true if any changes occurred.
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task<bool> CommitChangesAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Discard all pending property changes.
    /// </summary>
    void DiscardChanges();
}

public interface IInternalStateManager<TState> : IInternalStateManager,IStateManager<TState> where TState : class
{
    ChannelReader<TState> GetStateStream(CancellationToken cancellationToken = default);
}