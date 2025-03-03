using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Web;
using Errors;
using Helpers.Interfaces;
using Models;
using Newtonsoft.Json.Linq;
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

        var (client, httpRequestMessage) = BuildHttpRequest(context, applicationToken.Token, appName, route);

        client.DefaultRequestHeaders.Host = null;
        HttpResponseMessage? response = await client.SendAsync(httpRequestMessage);

        if (response != null && response.IsSuccessStatusCode)
        {
            string responseBody = string.Empty;
            context.Response.ContentType = response.Content?.Headers?.ContentType?.MediaType;

            if (response.Content != null)
            {
                context.Response.ContentLength = (await response.Content.ReadAsByteArrayAsync()).Length;
                responseBody = await response.Content.ReadAsStringAsync();
            }
            else
            {
                context.Response.ContentLength = 0;

            }
            context.Response.StatusCode = (int)response.StatusCode;

            context.Response.Headers.AccessControlAllowOrigin = "*";
            context.Response.Headers.AccessControlAllowHeaders = "*";
            context.Response.Headers.AccessControlAllowMethods = "*";

            await context.Response.WriteAsync(responseBody);
        }
        else
        {
            // Handle case where response is null
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Failed to receive a response from the upstream service.");
        }
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

    private (HttpClient, HttpRequestMessage) BuildHttpRequest(HttpContext context, string token, string appName, string route)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(_configuration.GetValue<string>($"{appName}") ?? throw new HttpException((int)ErrorCodes.MissingConfigData, "BaseAddress configuration is missing."))
        };

        var httpRequestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), string.Empty);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("x-api-key", _configuration.GetValue<string>("ApiKey"));
        client.DefaultRequestHeaders.Add("Correlation-Id", context.Request.Headers["Correlation-Id"].ToString() ?? Guid.NewGuid().ToString());

        HttpContent content = null;

        // Check if the method requires content
        if (new[] { "POST", "PATCH", "PUT", "DELETE", "HEAD" }.Contains(context.Request.Method))
        {
            var contentType = context.Request.ContentType;

            // Handle JSON content
            if (!string.IsNullOrEmpty(contentType) && contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                content = BuildJsonContent(context);
            }
            // Handle Form content (multipart/form-data)
            else if (!string.IsNullOrEmpty(contentType) && contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                content = BuildMultipartContent(context);
            }
            // Handle form-urlencoded content (application/x-www-form-urlencoded)
            else if (!string.IsNullOrEmpty(contentType) && contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                content = BuildFormUrlEncodedContent(context);
            }
        }

        if (content != null)
        {
            httpRequestMessage.Content = content;

            // Set Content-Type and Content-Length only when content is populated
            httpRequestMessage.Headers.TryAddWithoutValidation("Content-Type", content.Headers.ContentType.ToString());
            httpRequestMessage.Headers.TryAddWithoutValidation("Content-Length", content.Headers.ContentLength?.ToString());
        }

        var queryString = string.Join('&', context.Request.Query.Select(item => $"{item.Key}={HttpUtility.UrlEncode(item.Value)}"));
        httpRequestMessage.RequestUri = new Uri($"{client.BaseAddress}{EncodeRoute(route)}?{queryString}");

        foreach (var header in context.Request.Headers)
        {
            if (!header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                client.DefaultRequestHeaders.Add(header.Key, header.Value.FirstOrDefault());
        }

        return (client, httpRequestMessage);
    }

    private static StringContent BuildJsonContent(HttpContext context)
    {
        using var reader = new StreamReader(context.Request.Body);
        var jsonBody = reader.ReadToEndAsync().Result;
        return new StringContent(jsonBody, Encoding.UTF8, "application/json");
    }

    private static MultipartFormDataContent BuildMultipartContent(HttpContext context)
    {
        var formDataContent = new MultipartFormDataContent();
        foreach (var formFile in context.Request.Form.Files)
        {
            var fileContent = new StreamContent(formFile.OpenReadStream());
            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = formFile.Name, FileName = formFile.FileName };
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);
            formDataContent.Add(fileContent);
        }
        foreach (var formField in context.Request.Form.Where(f => !context.Request.Form.Files.Any(file => file.Name == f.Key)))
        {
            formDataContent.Add(new StringContent(formField.Value.ToString()), formField.Key);
        }
        return formDataContent;
    }

    private static FormUrlEncodedContent BuildFormUrlEncodedContent(HttpContext context)
    {
        var formFields = context.Request.Form.Select(f => new KeyValuePair<string, string>(f.Key, f.Value.ToString()));
        return new FormUrlEncodedContent(formFields);
    }

    private static string EncodeRoute(string route)
    {
        return string.Join("/", route.Split('/').Select(HttpUtility.UrlEncode));
    }
}

record HealthStatus(string Status, DateTime CheckedAt);
