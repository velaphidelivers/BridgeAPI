using System.Net;
using Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace Handlers
{
    /// <summary>
    /// Provides extension methods for configuring global exception handling middleware.
    /// </summary>
    public static class ExceptionMiddleware
    {
        /// <summary>
        /// Configures global exception handling middleware to capture and handle exceptions globally.
        /// </summary>
        /// <param name="app">The application builder.</param>
        public static void ConfigureExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.ContentType = "application/json";

                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var statusCode = (int)HttpStatusCode.InternalServerError;
                    var errorCode = (int)ErrorCodes.GenericError;
                    string errorMessage = ErrorMessages.Get(ErrorCodes.GenericError);
                    string errorDetails = exceptionHandlerPathFeature?.Error?.Message ?? string.Empty;

                    // Handle specific HttpException types
                    if (exceptionHandlerPathFeature?.Error is HttpException httpException)
                    {
                        errorCode = httpException.HttpExceptionDetails.ErrorCode ?? errorCode;
                        errorMessage = httpException.HttpExceptionDetails.Description;
                        errorDetails = httpException.Message;

                        statusCode = errorCode switch
                        {
                            (int)ErrorCodes.TokenExpiredError => (int)HttpStatusCode.Unauthorized,
                            (int)ErrorCodes.TokenInvalidSignatureError => (int)HttpStatusCode.Unauthorized,
                            (int)ErrorCodes.TokenMalformedError => (int)HttpStatusCode.Unauthorized,
                            (int)ErrorCodes.AccountUnverified => (int)HttpStatusCode.BadRequest,
                            _ => (int)HttpStatusCode.InternalServerError
                        };
                    }

                    // Set the response status code
                    context.Response.StatusCode = statusCode;

                    // Create the error response
                    var errorResponse = new ErrorDetails
                    {
                        ErrorCode = errorCode,
                        Message = errorMessage,
                        Details = errorDetails
                    };

                    // Write the error response to the HTTP response body
                    try
                    {
                        var responseBody = errorResponse.ToString();
                        await context.Response.WriteAsync(responseBody);
                        return;
                    }
                    catch
                    {
                        await context.Response.WriteAsync(new ErrorDetails
                        {
                            ErrorCode = (int)ErrorCodes.GenericError,
                            Message = "An unexpected error occurred.",
                            Details = "Failed to serialize error response."
                        }.ToString());
                        return;
                    }
                });
            });
        }
    }
}
