namespace SyncState.Utils;

public static class InterceptorUtils
{
    /// <summary>
    /// Builds a middleware pipeline from a list of interceptors and a terminal target.
    /// Interceptors are folded using Aggregate, so the last interceptor in the list becomes
    /// the outermost wrapper — it executes first and its next delegate calls the
    /// second-to-last, and so on down to the terminal target.
    /// Registration order: [A, B, C] → execution order: C(B(A(target)))
    /// </summary>
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