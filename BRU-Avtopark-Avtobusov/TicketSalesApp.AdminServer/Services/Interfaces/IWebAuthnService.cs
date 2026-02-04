using Fido2NetLib;
using Fido2NetLib.Objects;
using System.Threading.Tasks;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    /// <summary>
    /// Service for WebAuthn (FIDO2) authentication operations
    /// </summary>
    public interface IWebAuthnService
    {
        /// <summary>
        /// Begin WebAuthn credential registration for a user
        /// </summary>
        /// <param name="userId">The user's GUID ID</param>
        /// <param name="username">The username for display</param>
        /// <param name="displayName">The display name for the user</param>
        /// <returns>Credential creation options to send to the client</returns>
        Task<CredentialCreateOptions> BeginRegistrationAsync(Guid userId, string username, string displayName);

        /// <summary>
        /// Complete WebAuthn credential registration
        /// </summary>
        /// <param name="userId">The user's GUID ID</param>
        /// <param name="response">The authenticator response JSON from the client</param>
        /// <param name="friendlyName">Optional friendly name for the credential</param>
        /// <returns>Success status and any error message</returns>
        Task<(bool success, string message)> CompleteRegistrationAsync(Guid userId, string response, string? friendlyName = null);

        /// <summary>
        /// Begin WebAuthn authentication for a user
        /// </summary>
        /// <param name="username">The username attempting to authenticate</param>
        /// <returns>Assertion options to send to the client</returns>
        Task<AssertionOptions> BeginLoginAsync(string username);

        /// <summary>
        /// Complete WebAuthn authentication
        /// </summary>
        /// <param name="response">The authenticator response JSON from the client</param>
        /// <returns>Success status, user if successful, and any error message</returns>
        Task<(bool success, User? user, string message)> CompleteLoginAsync(string response);

        /// <summary>
        /// Get all WebAuthn credentials for a user
        /// </summary>
        /// <param name="userId">The user's GUID ID</param>
        /// <returns>List of user's WebAuthn credentials</returns>
        Task<IEnumerable<WebAuthnCredential>> GetUserCredentialsAsync(Guid userId);

        /// <summary>
        /// Delete a WebAuthn credential
        /// </summary>
        /// <param name="credentialId">The credential ID to delete</param>
        /// <param name="userId">The user's GUID ID (for authorization)</param>
        /// <returns>Success status and any error message</returns>
        Task<(bool success, string message)> DeleteCredentialAsync(long credentialId, Guid userId);

        /// <summary>
        /// Update the friendly name of a WebAuthn credential
        /// </summary>
        /// <param name="credentialId">The credential ID to update</param>
        /// <param name="userId">The user's GUID ID (for authorization)</param>
        /// <param name="friendlyName">The new friendly name</param>
        /// <returns>Success status and any error message</returns>
        Task<(bool success, string message)> UpdateCredentialNameAsync(long credentialId, Guid userId, string friendlyName);
    }
}