
using Microsoft.Extensions.Logging;

namespace GmachManagement.API.Middlewares
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        internal const string HeaderName = "X-Correlation-ID";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ILogger<CorrelationIdMiddleware> logger)
        {
            var correlationId = Guid.NewGuid().ToString();

            context.Response.Headers[HeaderName] = correlationId;

            using (logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["CorrelationId"] = correlationId
                }))
            {

                logger.LogInformation(
                    "Incoming request: {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                await _next(context);

            }
        }
    }
}
