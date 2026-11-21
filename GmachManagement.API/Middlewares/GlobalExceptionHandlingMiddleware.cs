using GmachManagement.Core.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GmachManagement.API.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ConcurrencyConflictException ex) when (!context.Response.HasStarted)
        {
            await WriteConflictAsync(context, ex, "Concurrency conflict", ex.Message);
        }
        catch (DbUpdateConcurrencyException ex) when (!context.Response.HasStarted)
        {
            await WriteConflictAsync(context, ex, "Optimistic concurrency conflict",
                "The resource was modified by another request. Please retry.");
        }
        catch (DbUpdateException ex) when (!context.Response.HasStarted &&
            ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await WriteConflictAsync(context, ex, "Unique constraint violation",
                "A record with this value already exists.");
        }
        catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested)
        {
            using var scope = BeginCorrelationScope(context);
            _logger.LogError(ex, "Unhandled exception");
            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { message = "אירעה שגיאה פנימית בשרת" });
        }
        finally
        {
            using var scope = BeginCorrelationScope(context);
            _logger.LogInformation("Request completed with status {StatusCode}", context.Response.StatusCode);
        }
    }

    private async Task WriteConflictAsync(HttpContext context, Exception exception, string reason, string message)
    {
        using var scope = BeginCorrelationScope(context);
        _logger.LogWarning(exception, "{ConflictReason}", reason);
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new { message });
    }

    private IDisposable? BeginCorrelationScope(HttpContext context)
        => _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString()
        });
}
