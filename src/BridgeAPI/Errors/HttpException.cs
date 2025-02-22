namespace Errors
{
    public class HttpException : Exception
    {
        /// <summary>
        /// Gets the HTTP exception details associated with this exception.
        /// </summary>
        public HttpExceptionDetails HttpExceptionDetails { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code as per the error codes representation.</param>
        /// <param name="description">A description of the error.</param>
        public HttpException(int? errorCode, string description)
            : base(description)
        {
            HttpExceptionDetails = new HttpExceptionDetails(errorCode, description);
        }
    }
}
