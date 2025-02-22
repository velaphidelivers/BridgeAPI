public class RoutingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        await context.Response.WriteAsync(string.Empty);
    }
}