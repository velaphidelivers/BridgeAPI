
namespace Errors
{
    /// <summary>
    /// Provides error messages corresponding to different error codes.
    /// </summary>
    public static partial class ErrorMessages
    {
        /// <summary>
        /// Retrieves the error message associated with a specific error code.
        /// </summary>
        /// <param name="code">The error code for which to retrieve the message.</param>
        /// <returns>A string containing the error message.</returns>
        public static string Get(ErrorCodes code)
        {
            return code switch
            {
                // Existing error messages...
                ErrorCodes.GenericError => "An unexpected error has occurred.",
                ErrorCodes.DatabaseConnectionError => "Unable to connect to the database. Please try again later.",
                ErrorCodes.DatabaseExecutionError => "An error occurred while executing the database command.",
                ErrorCodes.DatabaseQueryError => "An error occurred while querying the database.",
                ErrorCodes.DataProcessingError => "An error occurred while processing data.",
                ErrorCodes.InvalidInputError => "The input provided is invalid.",
                ErrorCodes.MissingDataError => "Required data is missing.",
                ErrorCodes.AccountUnverified => "Your account is currently unverified. Please verify your account to proceed.",
                ErrorCodes.MissingConfigData => "The config entry is missing from the application config.",
                // Custom error messages for token-related issues
                ErrorCodes.TokenExpiredError => "The token has expired. Please obtain a new token.",
                ErrorCodes.TokenInvalidSignatureError => "The token signature is invalid. The token may have been tampered with.",
                ErrorCodes.TokenMalformedError => "The token is malformed or invalid. Please check the token format.",
                _ => "An unknown error has occurred."
            };
        }
    }
}
