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

        // Return JSON error response for unsupported URLs
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        var errorResponse = new { Error = "URL not supported" };
        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}

record HealthStatus(string Status, DateTime CheckedAt);
