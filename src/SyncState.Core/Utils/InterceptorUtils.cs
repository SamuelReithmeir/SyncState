namespace SyncState.Utils;

public static class InterceptorUtils
{
    public static Func<TContext, CancellationToken, Task> CreateInterceptorPipeline<TInterceptor, TContext>(
        IEnumerable<TInterceptor> interceptors,
        Func<TContext, CancellationToken, Task> target,
        Func<TInterceptor, Func<TContext, Func<TContext, CancellationToken, Task>, CancellationToken, Task>>
            interceptorCallSelector)
    {
        return interceptors.Aggregate(target, (next, interceptor) =>
        {
            var interceptorCall = interceptorCallSelector(interceptor);
            return (context, cancellationToken) => interceptorCall(context, next, cancellationToken);
        });
    }
    public static Func<TContext, CancellationToken, Task<TResult>> CreateInterceptorPipeline<TInterceptor, TContext,TResult>(
        IEnumerable<TInterceptor> interceptors,
        Func<TContext, CancellationToken, Task<TResult>> target,
        Func<TInterceptor, Func<TContext, Func<TContext, CancellationToken, Task<TResult>>, CancellationToken, Task<TResult>>>
            interceptorCallSelector)
    {
        return interceptors.Aggregate(target, (next, interceptor) =>
        {
            var interceptorCall = interceptorCallSelector(interceptor);
            return (context, cancellationToken) => interceptorCall(context, next, cancellationToken);
        });
    }
}