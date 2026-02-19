using SyncState.Models.InterceptorContexts;

namespace SyncState.Interfaces.Interceptors;

/// <summary>
/// interceptor on state manager level
/// </summary>
public interface IStateInterceptor<TState>:ISyncStateInterceptor where TState : class
{
    /// <summary>
    /// intercept command before it is executed
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="next"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    Task HandleCommandAsync<TCommand>(StateCommandContext<TState,TCommand> context, Func<StateCommandContext<TState,TCommand>,CancellationToken, Task> next,
        CancellationToken cancellationToken) => next(context,cancellationToken);

    /// <summary>
    /// intercept initialization
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task InitializeAsync(StateInitializationContext<TState> context,Func<StateInitializationContext<TState>,CancellationToken, Task> next, CancellationToken cancellationToken) =>
        next(context,cancellationToken);
    
}