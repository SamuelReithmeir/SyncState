using SyncState.Interfaces.Managers;

namespace SyncState.InternalInterfaces;

public interface IInternalPropertyManager : IPropertyManager
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    void ApplyToStateObject(object stateObject);
    bool HandlesCommandType<TCommand>() where TCommand : notnull;

    /// <summary>
    /// Handles a command of type TCommand.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TCommand"></typeparam>
    Task HandleCommandAsync<TCommand>(TCommand command,CancellationToken cancellationToken) where TCommand : notnull;
    
    /// <summary>
    /// Discard all pending property changes.
    /// </summary>
    void DiscardChanges();

    /// <summary>
    /// commit undigested changes and return true if a value change occurred
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task<bool> CommitChangesAsync(CancellationToken cancellationToken);
}

public interface IInternalPropertyManager<TProperty> : IInternalPropertyManager, IPropertyManager<TProperty>;