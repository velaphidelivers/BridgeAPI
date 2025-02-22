using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Errors;
using Helpers.Interfaces;
using Services.Interfaces;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly IAllowUrls _allowUrls;
    private readonly ITokenService _systemSecurity;

    public RoutingMiddleware(RequestDelegate next, IConfiguration config, IAllowUrls allowUrls, ITokenService systemSecurity)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _allowUrls = allowUrls ?? throw new ArgumentNullException(nameof(allowUrls));
        _systemSecurity = systemSecurity ?? throw new ArgumentNullException(nameof(systemSecurity));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Pass control to the next middleware in the pipeline
        await _next(context);
    }
}
