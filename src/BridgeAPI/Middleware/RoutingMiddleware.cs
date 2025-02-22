using Helpers.Interfaces;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAllowedUrls _allowedUrls;

    public RoutingMiddleware(RequestDelegate next, IAllowedUrls allowedUrls)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _allowedUrls = allowedUrls;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            var healthStatus = new HealthStatus("Healthy", DateTime.UtcNow);
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(healthStatus);
            return;
        }

        if (_allowedUrls.IsAllowed(context.Request.Path.Value.Substring(1)))
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(new { });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            Error = "Url not supported"
        });
    }
}

record HealthStatus(string Status, DateTime CheckedAt);
