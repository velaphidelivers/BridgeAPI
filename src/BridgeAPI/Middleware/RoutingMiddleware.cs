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
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var apiKey = _config.GetValue<string>("ApiKey");
        var correlationId = context.Request.Headers["Correlation-Id"].ToString();

        var requestPath = context.Request.Path.Value;
        if (string.IsNullOrEmpty(requestPath))
        {
            // Handle cases where the request path is null or empty
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid request path.");
            return;
        }

        if (requestPath.StartsWith("/secure", StringComparison.OrdinalIgnoreCase) ||
            requestPath.StartsWith("/authenticate", StringComparison.OrdinalIgnoreCase))
        {
            var client = new HttpClient();
            string resource;
            string api;

            if (requestPath.StartsWith("/authenticate", StringComparison.OrdinalIgnoreCase))
            {
                api = "UserAuthApiBaseUrl";
                resource = "Users/Login";
            }
            else
            {
                api = requestPath.Split('/')[2];
                resource = requestPath.Remove(0, (9 + api.Length));

                if (!_allowUrls.IsAllowed(resource))
                {
                    Console.WriteLine("Resource: " + resource);
                    Console.WriteLine("Api Name: " + api);
                    Console.WriteLine("Path: " + requestPath);
                    Console.WriteLine("Expression(!_allowUrls.IsAllowed(resource)): " + !_allowUrls.IsAllowed(resource));
                    throw new HttpException((int)ErrorCodes.InvalidInputError, "The resource passed in is forbidden.");
                }

                client.DefaultRequestHeaders.Add("X-UserToken", context.Request.Headers["X-UserToken"].ToString());
            }

            client.DefaultRequestHeaders.Add("X-Refresh-Token", context.Request.Headers["X-Refresh-Token"].ToString());

            var applicationToken = await _systemSecurity.GetToken(correlationId);

            if (applicationToken?.Token == null)
            {
                throw new HttpException((int)ErrorCodes.TokenMalformedError, "The tolen application is null or malformed.");
            }

            var url = _config.GetValue<string>($"{api}");

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
            foreach (var piece in resource.Split('/'))
            {
                if (UrlEncode.Length == 0)
                    UrlEncode += $"{HttpUtility.UrlEncode(piece)}";
                else
                    UrlEncode += $"/{HttpUtility.UrlEncode(piece)}";

            }
            httpRequestMessage.RequestUri = new Uri($"{url}{UrlEncode}?{queryString}");
            Console.WriteLine("PAth: " + httpRequestMessage.RequestUri.AbsolutePath);
            HttpResponseMessage? response = await client.SendAsync(httpRequestMessage);

            if (response != null)
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

                context.Response.Headers.Add("Correlation-Id", correlationId);

                Console.WriteLine("Response:");
                Console.WriteLine("StatusCode: " + (int)response.StatusCode);
                Console.WriteLine("Headers:");
                foreach (var header in response.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }
                Console.WriteLine("Body: " + responseBody);

                await context.Response.WriteAsync(responseBody);
                return;
            }
            else
            {
                // Handle case where response is null
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Failed to receive a response from the upstream service.");
                return;
            }
        }
        else
        {
            // Pass control to the next middleware in the pipeline
            await _next(context);
        }
    }
}
