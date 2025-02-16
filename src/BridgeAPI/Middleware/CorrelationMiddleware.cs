using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the Correlation-Id header is present
        if (!context.Request.Headers.ContainsKey(CorrelationIdHeader))
        {
            // Log the missing header
            _logger.LogWarning("Request missing required header: {HeaderName}", CorrelationIdHeader);

            // Return a 400 Bad Request with a custom error message
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                Error = $"Missing required header: {CorrelationIdHeader}"
            });
            return;
        }

        // Continue with the next middleware or endpoint
        await _next(context);
    }
}
