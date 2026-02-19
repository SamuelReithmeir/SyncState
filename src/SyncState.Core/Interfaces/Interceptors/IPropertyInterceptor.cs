using SyncState.Models.InterceptorContexts;

namespace SyncState.Interfaces.Interceptors;

/// <summary>
/// interceptor on property manager level
/// </summary>
public interface IPropertyInterceptor<TProperty>:ISyncStateInterceptor
{
    /// <summary>
    /// intercept command before it is executed
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="next"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    Task HandleCommandAsync<TCommand>(PropertyCommandContext<TProperty,TCommand> context, Func<PropertyCommandContext<TProperty,TCommand>,CancellationToken, Task> next,
        CancellationToken cancellationToken) => next(context,cancellationToken);

    /// <summary>
    /// intercept initialization
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task InitializeAsync(PropertyInitializationContext<TProperty> context,Func<PropertyInitializationContext<TProperty>,CancellationToken, Task> next, CancellationToken cancellationToken) =>
        next(context,cancellationToken);
    
}