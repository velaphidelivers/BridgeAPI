public class RoutingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            var healthStatus = new HealthStatus("Healthy", DateTime.UtcNow);
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(healthStatus);
            return;
        }

        // Return JSON error response for unsupported URLs (consistent format)
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
       await context.Response.WriteAsync("URL not supported.");
    }
}

record HealthStatus(string Status, DateTime CheckedAt);
