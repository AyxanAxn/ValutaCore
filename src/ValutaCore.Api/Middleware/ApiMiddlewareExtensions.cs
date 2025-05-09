namespace ValutaCore.Api.Middleware;

public static class ApiMiddlewareExtensions
{
    public static IApplicationBuilder UseApiMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalErrorMiddleware>();
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseMiddleware<PerformanceLoggerMiddleware>();

        return app;
    }
}