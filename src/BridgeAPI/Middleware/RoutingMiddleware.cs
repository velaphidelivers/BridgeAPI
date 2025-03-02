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
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var path = context.Request?.Path.Value?.Substring(1);
        string appName = string.Empty;
        string route = string.Empty;
        var apiKey = _configuration.GetValue<string>("ApiKey");
        var correlationId = context?.Request?.Headers["Correlation-Id"].ToString();
        var client = new HttpClient();

        //health calls
        if (path != null && path.Equals("health", StringComparison.OrdinalIgnoreCase))
        {
            var healthStatus = new HealthStatus("Healthy", DateTime.UtcNow);
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(healthStatus);
            return;
        }

        //authentication calls
        if (context != null && path != null && path.Equals("Anonymous/Authenticate", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(new { });
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
                throw new HttpException((int)ErrorCodes.TokenMalformedError, "The application token is null or malformed.");
            }
            var url = _configuration.GetValue<string>($"{appName}");

            client.BaseAddress = new Uri(url ?? throw new HttpException((int)ErrorCodes.MissingConfigData, "BaseAddress configuration is missing."));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", applicationToken.Token);
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("Correlation-Id", correlationId ?? Guid.NewGuid().ToString());

            var httpRequestMessage = new HttpRequestMessage
            {
                Content = null,
                Method = new HttpMethod(context.Request.Method)
            };
            if (new[] { "POST", "PATCH", "PUT", "DELETE", "HEAD" }.Contains(context.Request.Method))
            {
                if (context.Request.HasFormContentType)
                {
                    // Handle multipart/form-data content
                    var formDataContent = new MultipartFormDataContent();

                    foreach (var formFile in context.Request.Form.Files)
                    {
                        var fileContent = new StreamContent(formFile.OpenReadStream())
                        {
                            Headers =
                            {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = formFile.Name,
                                    FileName = formFile.FileName
                                }
                            }
                        };
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);
                        formDataContent.Add(fileContent);
                    }

                    foreach (var formField in context.Request.Form)
                    {
                        if (!context.Request.Form.Files.Any(f => f.Name == formField.Key))
                        {
                            var value = formField.Value.ToString();
                            formDataContent.Add(new StringContent(value), formField.Key);
                        }
                    }

                    httpRequestMessage.Content = formDataContent;
                }
                else
                {
                    var jsonBody = string.Empty;
                    using (var reader = new StreamReader(context.Request.Body))
                    {
                        jsonBody = await reader.ReadToEndAsync();
                    }
                    httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, context.Request.ContentType ?? "application/json");
                }
            }
            var param = context.Request.Query;
            var queryString = string.Join('&', param.Select(item => $"{item.Key}={System.Web.HttpUtility.UrlEncode(item.Value)}"));
            var UrlEncode = string.Empty;
            foreach (var piece in route.Split('/'))
            {
                if (UrlEncode.Length == 0)
                    UrlEncode += $"{HttpUtility.UrlEncode(piece)}";
                else
                    UrlEncode += $"/{HttpUtility.UrlEncode(piece)}";

            }
            httpRequestMessage.RequestUri = new Uri($"{url}{UrlEncode}?{queryString}");

            //populate headers
            foreach (var header in context.Request.Headers)
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value.FirstOrDefault());
            }


            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(new
            {
                Debug = new
                {
                    httpRequestMessage.RequestUri,
                    httpRequestMessage.Headers
                }
            });
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
