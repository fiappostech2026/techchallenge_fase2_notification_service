using FCG.Notifications.Worker.Middleware;

namespace FCG.Notifications.Worker.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
