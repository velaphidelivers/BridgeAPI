using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Errors;
using Helpers.Interfaces;
using Services.Interfaces;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;


    public RoutingMiddleware(RequestDelegate next, IConfiguration config, IAllowUrls allowUrls, ITokenService systemSecurity)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var healthStatus = new HealthStatus("Healthy", DateTime.Now);
        await context.Response.WriteAsJsonAsync(healthStatus);
        return;
    }
}

record HealthStatus(string Status, DateTime CheckedAt);
