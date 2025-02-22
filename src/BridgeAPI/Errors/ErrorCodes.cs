namespace Errors
{
    /// <summary>
    /// Represents different error codes used across the application.
    /// </summary>
    public enum ErrorCodes
    {
        /// <summary>
        /// A generic unknown error has occurred.
        /// </summary>
        GenericError = 1000,

        /// <summary>
        /// Error occurred due to database connectivity issues.
        /// </summary>
        DatabaseConnectionError = 1001,

        /// <summary>
        /// Error occurred while executing a SQL command.
        /// </summary>
        DatabaseExecutionError = 1002,

        /// <summary>
        /// Error occurred while querying the database.
        /// </summary>
        DatabaseQueryError = 1003,

        /// <summary>
        /// Error occurred while handling data processing.
        /// </summary>
        DataProcessingError = 1004,

        /// <summary>
        /// Error occurred due to invalid input parameters.
        /// </summary>
        InvalidInputError = 1005,

        /// <summary>
        /// Error occurred due to missing required data.
        /// </summary>
        MissingDataError = 1006,

        /// <summary>
        /// The token has expired.
        /// </summary>
        TokenExpiredError = 1007,

        /// <summary>
        /// The token signature is invalid.
        /// </summary>
        TokenInvalidSignatureError = 1008,

        /// <summary>
        /// The token is malformed or invalid.
        /// </summary>
        TokenMalformedError = 1009,

        /// <summary>
        /// The token is malformed or invalid.
        /// </summary>
        AccountUnverified = 1010,

           /// <summary>
           /// The config entry is missing from the application config.
           /// </summary>
        MissingConfigData = 1011
    }
}
