using System.Net;
using System.Text.Json;

namespace FCG.Notifications.Worker.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            var tracingId = Guid.NewGuid();
            _logger.LogError(ex, "Unhandled exception. TracingId: {TracingId}", tracingId);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(new { tracingId });
            await context.Response.WriteAsync(body);
        }
    }
}
