using Serilog.Context;

namespace backend_api.Api.Middleware;

/// <summary>
/// Reads the X-Correlation-ID request header (or generates a new GUID if absent),
/// echoes it back in the response, and pushes it into Serilog's LogContext so every
/// log line emitted during the request includes a CorrelationId property.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        // Make the correlation ID available to downstream code via HttpContext.Items
        context.Items["CorrelationId"] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
