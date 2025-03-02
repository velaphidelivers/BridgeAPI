using System.Net.Http.Headers;
using System.Text;
using System.Web;
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
        if (context == null) throw new ArgumentNullException(nameof(context));

        var path = context.Request?.Path.Value?.Substring(1);
        if (string.IsNullOrEmpty(path)) ReturnForbidden(context);

        if (path.Equals("health", StringComparison.OrdinalIgnoreCase))
        {
            await RespondWithHealthStatus(context);
            return;
        }

        if (!path.StartsWith("Secure", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("Anonymous", StringComparison.OrdinalIgnoreCase))
        {
            ReturnForbidden(context);
            return;
        }

        string appName = path.Equals("Anonymous/Authenticate", StringComparison.OrdinalIgnoreCase) ? "UserAuthApiBaseUrl" : string.Empty;
        string route = path.Equals("Anonymous/Authenticate", StringComparison.OrdinalIgnoreCase) ? "Users/Login" : string.Empty;

        if (!_allowedUrls.IsAllowed(path))
        {
            ReturnForbidden(context);
            return;
        }

        var applicationToken = await _tokenService.GetToken(context.Request.Headers["Correlation-Id"].ToString());
        if (applicationToken?.Token == null)
            throw new HttpException((int)ErrorCodes.TokenMalformedError, "The application token is null or malformed.");

        var httpRequestMessage = BuildHttpRequest(context, applicationToken.Token, appName, route);

        using var client = new HttpClient();
        var response = await client.SendAsync(httpRequestMessage);
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        await context.Response.WriteAsync(responseBody);
        
        /*  context.Response.StatusCode = StatusCodes.Status200OK;
         await context.Response.WriteAsJsonAsync(new { Debug = new { httpRequestMessage.RequestUri, httpRequestMessage.Headers } }); */
        return;
    }

    private static async Task RespondWithHealthStatus(HttpContext context)
    {
        var healthStatus = new HealthStatus("Healthy", DateTime.UtcNow);
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(healthStatus);
    }

    private static void ReturnForbidden(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.WriteAsJsonAsync(new { Error = "Url not supported" });
    }

    private HttpRequestMessage BuildHttpRequest(HttpContext context, string token, string appName, string route)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(_configuration.GetValue<string>($"{appName}") ?? throw new HttpException((int)ErrorCodes.MissingConfigData, "BaseAddress configuration is missing."))
        };

        var httpRequestMessage = new HttpRequestMessage { Method = new HttpMethod(context.Request.Method) };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("x-api-key", _configuration.GetValue<string>("ApiKey"));
        client.DefaultRequestHeaders.Add("Correlation-Id", context.Request.Headers["Correlation-Id"].ToString() ?? Guid.NewGuid().ToString());

        if (new[] { "POST", "PATCH", "PUT", "DELETE", "HEAD" }.Contains(context.Request.Method))
        {
            httpRequestMessage.Content = context.Request.HasFormContentType ? BuildMultipartContent(context) : BuildJsonContent(context);
        }

        var queryString = string.Join('&', context.Request.Query.Select(item => $"{item.Key}={HttpUtility.UrlEncode(item.Value)}"));
        httpRequestMessage.RequestUri = new Uri($"{client.BaseAddress}{EncodeRoute(route)}?{queryString}");

        foreach (var header in context.Request.Headers)
        {
            httpRequestMessage.Headers.Add(header.Key, header.Value.FirstOrDefault());
        }

        return httpRequestMessage;
    }

    private static MultipartFormDataContent BuildMultipartContent(HttpContext context)
    {
        var formDataContent = new MultipartFormDataContent();

        foreach (var formFile in context.Request.Form.Files)
        {
            var fileContent = new StreamContent(formFile.OpenReadStream())
            {
                Headers = { ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = formFile.Name, FileName = formFile.FileName } }
            };
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);
            formDataContent.Add(fileContent);
        }

        foreach (var formField in context.Request.Form.Where(f => !context.Request.Form.Files.Any(file => file.Name == f.Key)))
        {
            formDataContent.Add(new StringContent(formField.Value.ToString()), formField.Key);
        }

        return formDataContent;
    }

    private static StringContent BuildJsonContent(HttpContext context)
    {
        using var reader = new StreamReader(context.Request.Body);
        var jsonBody = reader.ReadToEndAsync().Result;
        return new StringContent(jsonBody, Encoding.UTF8, context.Request.ContentType ?? "application/json");
    }

    private static string EncodeRoute(string route)
    {
        return string.Join("/", route.Split('/').Select(HttpUtility.UrlEncode));
    }
}

record HealthStatus(string Status, DateTime CheckedAt);
