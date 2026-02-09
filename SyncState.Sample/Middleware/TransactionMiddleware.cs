using SyncState.Sample.Infrastructure;

namespace SyncState.Sample.Middleware;

public class TransactionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TransactionMiddleware> _logger;

    public TransactionMiddleware(RequestDelegate next, ILogger<TransactionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, SampleDbContext dbContext)
    {
        var method = context.Request.Method.ToUpper();
        var isTransactional = method == "PUT" || method == "POST" || method == "DELETE";

        if (!isTransactional)
        {
            await _next(context);
            return;
        }

        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            _logger.LogInformation("Transaction started for {Method} {Path}", method, context.Request.Path);
            
            await _next(context);

            // Only commit if the response is successful
            if (context.Response.StatusCode < 400)
            {
                await transaction.CommitAsync();
                _logger.LogInformation("Transaction committed for {Method} {Path}", method, context.Request.Path);
            }
            else
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("Transaction rolled back for {Method} {Path} with status code {StatusCode}", 
                    method, context.Request.Path, context.Response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Transaction rolled back due to exception for {Method} {Path}", method, context.Request.Path);
            throw;
        }
    }
}

