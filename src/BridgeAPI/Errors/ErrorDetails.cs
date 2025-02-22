using Newtonsoft.Json;

namespace Errors
{
    /// <summary>
    /// Represents details of an error.
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// Gets or sets the error code. For example, 1001.
        /// </summary>
        public int? ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets additional details about the error.
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Returns the JSON representation of the error details.
        /// </summary>
        /// <returns>A JSON string representing the error details.</returns>
        public override string ToString()
        {
            try
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }
            catch (Exception ex)
            {
                // Handle serialization exceptions (log, rethrow, etc.)
                return $"Error serializing to JSON: {ex.Message}";
            }
        }
    }
}
