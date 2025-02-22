public class RoutingMiddleware
{
    private readonly RequestDelegate _next;

    public RoutingMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
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

        // Return 403 for unsupported URLs
        /*       context.Response.StatusCode = StatusCodes.Status403Forbidden;
              await context.Response.WriteAsync("URL not supported"); */
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            Error = "Url not supported"
        });
    }
}

record HealthStatus(string Status, DateTime CheckedAt);
