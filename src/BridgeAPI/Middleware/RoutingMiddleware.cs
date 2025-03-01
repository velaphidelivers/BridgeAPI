using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Errors;
using Helpers.Interfaces;
using Services.Interfaces;

public class RoutingMiddleware
{
    private readonly IAllowedUrls _allowedUrls;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly HttpClient _httpClient;

    public RoutingMiddleware(IAllowedUrls allowedUrls, IConfiguration configuration, ITokenService tokenService, HttpClient httpClient)
    {
        _allowedUrls = allowedUrls ?? throw new ArgumentNullException(nameof(allowedUrls));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var path = context.Request?.Path.Value?.Trim('/');
        if (string.IsNullOrEmpty(path))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Error = "Invalid request path." });
            return;
        }

        if (path.Equals("health", StringComparison.OrdinalIgnoreCase))
        {
            await WriteHealthResponse(context);
            return;
        }

        string appName;
        string route;
        string userToken = context.Request.Headers["X-UserToken"];
        bool isSecureRequest = path.StartsWith("Secure", StringComparison.OrdinalIgnoreCase);
        bool isAnonymousRequest = path.StartsWith("Anonymous", StringComparison.OrdinalIgnoreCase);

        if (isAnonymousRequest && path.Equals("Anonymous/Authenticate", StringComparison.OrdinalIgnoreCase))
        {
            appName = "UserAuthApiBaseUrl";
            route = "Users/Login";
        }
        else if (isSecureRequest)
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 3)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { Error = "Invalid secure API request format." });
                return;
            }

            appName = segments[1];
            route = string.Join('/', segments.Skip(2));

            if (!_allowedUrls.IsAllowed(route))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { Error = "URL not supported." });
                return;
            }

            if (string.IsNullOrWhiteSpace(userToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { Error = "Missing authentication token." });
                return;
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { Error = "URL not supported." });
            return;
        }

        var baseUrl = _configuration.GetValue<string>(appName);
        if (string.IsNullOrEmpty(baseUrl))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { Error = "API base URL missing from configuration." });
            return;
        }

        var applicationToken = await _tokenService.GetToken(context.TraceIdentifier);
        if (applicationToken?.Token == null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { Error = "Failed to obtain a valid application token." });
            return;
        }

        var requestMessage = CreateHttpRequestMessage(context, baseUrl, route);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", applicationToken.Token);

        if (isSecureRequest)
        {
            requestMessage.Headers.Add("X-UserToken", userToken);
        }

        using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
        await CopyResponseToHttpContext(response, context);
    }

    private static async Task WriteHealthResponse(HttpContext context)
    {
        var healthStatus = new { Status = "Healthy", CheckedAt = DateTime.UtcNow };
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(healthStatus);
    }

    private static HttpRequestMessage CreateHttpRequestMessage(HttpContext context, string baseUrl, string route)
    {
        var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), $"{baseUrl}/{HttpUtility.UrlEncode(route)}{context.Request.QueryString}");

        foreach (var header in context.Request.Headers)
        {
            if (!request.Headers.Contains(header.Key))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
            }
        }

        if (context.Request.ContentLength > 0)
        {
            request.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers)
            {
                if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
                }
            }
        }

        return request;
    }

    private static async Task CopyResponseToHttpContext(HttpResponseMessage response, HttpContext context)
    {
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await responseStream.CopyToAsync(context.Response.Body);
    }
}
