
    using GmachManagement.Core.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;


namespace GmachManagement.API.Middlewares
    {
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
                catch (ConcurrencyConflictException ex)
                {
                    using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = context.Response.Headers["X-Correlation-ID"].ToString() });
                    _logger.LogWarning(ex, "Concurrency conflict");

                    context.Response.StatusCode = 409;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = ex.Message
                    });
                }

            catch (DbUpdateConcurrencyException ex)
            {
                using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = context.Response.Headers["X-Correlation-ID"].ToString() });
                _logger.LogWarning(ex, "Optimistic concurrency conflict");
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new { message = "The resource was modified by another request. Please retry." });
            }

            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sqlEx &&
                      (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = context.Response.Headers["X-Correlation-ID"].ToString() });
                _logger.LogWarning(ex, "Unique constraint violation");

                context.Response.StatusCode = StatusCodes.Status409Conflict;

                await context.Response.WriteAsJsonAsync(new
                {
                    message = "A record with this value already exists."
                });
            }

            catch (Exception ex)
                {
                    using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = context.Response.Headers["X-Correlation-ID"].ToString() });
                    _logger.LogError(ex, "Unhandled exception");

                    context.Response.StatusCode = 500;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "אירעה שגיאה פנימית בשרת"
                    });
                }
            }
        }
    }
