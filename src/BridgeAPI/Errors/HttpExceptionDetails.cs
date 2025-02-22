namespace Errors
{
    /// <summary>
    /// Represents the details of an HTTP exception.
    /// </summary>
    /// <param name="ErrorCode">The error code associated with the exception.</param>
    /// <param name="Description">A description of the error.</param>
    public sealed record HttpExceptionDetails(int? ErrorCode, string Description);
}
