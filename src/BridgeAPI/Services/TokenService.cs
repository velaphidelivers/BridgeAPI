using Models;
using Newtonsoft.Json;
using Services.Interfaces;
using Errors;  // Ensure this namespace is included for HttpException
using System.Text;

namespace Services
{
    /// <summary>
    /// Implementation of the <see cref="ITokenService"/> that retrieves tokens.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenService"/> class.
        /// </summary>
        /// <param name="configuration">The configuration instance for retrieving settings.</param>
        /// <param name="httpClient">The HTTP client instance.</param>
        public TokenService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            var apiKey = _configuration.GetValue<string>("ApiKey");
            var baseAddress = _configuration.GetValue<string>("BaseAddress");
            var application = _configuration.GetValue<string>("RouterName");

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new HttpException((int)ErrorCodes.MissingConfigData, "ApiKey is not configured.");
            }

            if (string.IsNullOrEmpty(baseAddress))
            {
                throw new HttpException((int)ErrorCodes.MissingConfigData, "BaseAddress is not configured.");
            }

            _httpClient.DefaultRequestHeaders.Add("ApiKey", apiKey);
            _httpClient.DefaultRequestHeaders.Add("Application", application);
            _httpClient.BaseAddress = new Uri(baseAddress);
        }

        /// <summary>
        /// Retrieves a token based on the provided correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID for tracking the request.</param>
        /// <returns>A task representing the asynchronous operation, with a <see cref="TokenResponse"/> result.</returns>
        public async Task<SystemSecurityToken> GetToken(string correlationId)
        {
            var routerName = _configuration.GetValue<string>("RouterName");

            if (string.IsNullOrEmpty(routerName))
            {
                routerName = "default"; // Provide a fallback or handle accordingly
            }

            var requestUri = "api/Application/get/token";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(new { ApplicationName = routerName }),
                    Encoding.UTF8,
                    "application/json"
                )
            };

            // Optionally, add correlation ID to headers if needed
            if (!string.IsNullOrEmpty(correlationId))
            {
                httpRequest.Headers.Add("Correlation-ID", correlationId);
            }
            var httpResponse = await _httpClient.SendAsync(httpRequest);

            if (httpResponse.IsSuccessStatusCode)
            {
                var responseBody = await httpResponse.Content.ReadAsStringAsync();
                var token = JsonConvert.DeserializeObject<SystemSecurityToken>(responseBody);

                if (token == null)
                {
                    throw new HttpException((int)ErrorCodes.DataProcessingError, "Failed to deserialize token response.");
                }
                return token;
            }

            // Throw HttpException for unsuccessful responses
            var errorMessage = $"Failed to retrieve token. Status Code: {httpResponse.StatusCode}";
            throw new HttpException((int)ErrorCodes.GenericError, errorMessage);
        }
    }
}
