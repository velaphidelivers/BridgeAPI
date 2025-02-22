using Models;

namespace Services.Interfaces
{
    /// <summary>
    /// Defines a service for retrieving tokens.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Retrieves a token based on the provided correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID for tracking the request.</param>
        /// <returns>A task representing the asynchronous operation, with a <see cref="TokenResponse"/> result.</returns>
        Task<SystemSecurityToken> GetToken(string correlationId);
    }
}
