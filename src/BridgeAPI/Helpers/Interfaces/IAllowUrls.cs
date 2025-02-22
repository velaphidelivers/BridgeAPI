namespace Helpers.Interfaces
{
    /// <summary>
    /// Defines a contract for checking if a given URL or resource is allowed based on specific rules.
    /// </summary>
    public interface IAllowedUrls
    {
        /// <summary>
        /// Determines whether the specified resource is allowed.
        /// </summary>
        /// <param name="resource">The URL or resource to check.</param>
        /// <returns><c>true</c> if the resource is allowed; otherwise, <c>false</c>.</returns>
        bool IsAllowed(string resource);
    }
}
