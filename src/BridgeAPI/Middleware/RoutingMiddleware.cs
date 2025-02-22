using Errors;
using Helpers.Interfaces;
using Services.Interfaces;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAllowedUrls _allowedUrls;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;

    public RoutingMiddleware(RequestDelegate next, IAllowedUrls allowedUrls, IConfiguration configuration, ITokenService tokenService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _allowedUrls = allowedUrls;
        _configuration = configuration;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var path = context.Request?.Path.Value?.Substring(1);
        string appName = string.Empty;
        string route = string.Empty;
        var apiKey = _configuration.GetValue<string>("ApiKey");
        var correlationId = context?.Request?.Headers["Correlation-Id"].ToString();

        if (path != null && path.Equals("health", StringComparison.OrdinalIgnoreCase))
        {
            var healthStatus = new HealthStatus("Healthy", DateTime.UtcNow);
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(healthStatus);
            return;
        }
        if (path != null && (path.StartsWith("Secure", StringComparison.OrdinalIgnoreCase) || path.StartsWith("Anonymous", StringComparison.OrdinalIgnoreCase)))
        {
            if (path.Equals("Anonymous/Authenticate", StringComparison.OrdinalIgnoreCase))
            {
                appName = "UserAuthApiBaseUrl";
                route = "Users/Login";
            }
            else if (!_allowedUrls.IsAllowed(path ?? string.Empty))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    Error = "Url not supported"
                });
            }

            var applicationToken = await _tokenService.GetToken(correlationId);

            if (applicationToken?.Token == null)
            {
                throw new HttpException((int)ErrorCodes.TokenMalformedError, "The tolen application is null or malformed.");
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(new { Token = applicationToken });
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
