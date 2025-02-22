public class RoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoutingMiddleware> _logger;
    public RoutingMiddleware(RequestDelegate next, ILogger<RoutingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    public async Task InvokeAsync(HttpContext context)
    {
      //  await context.Response.WriteAsync(string.Empty);
        // Continue with the next middleware or endpoint
        await _next(context);
    }
}