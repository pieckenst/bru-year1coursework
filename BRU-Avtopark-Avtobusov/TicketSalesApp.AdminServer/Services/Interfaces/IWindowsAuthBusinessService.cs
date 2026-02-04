using System.Threading.Tasks;
using TicketSalesApp.Core.Models;
using System.Security.Principal;

namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    /// <summary>
    /// Business logic service for Windows authentication operations
    /// </summary>
    public interface IWindowsAuthBusinessService
    {
        /// <summary>
        /// Authenticate Windows user and generate JWT token
        /// </summary>
        Task<(bool success, string token, User user, string message)> AuthenticateWindowsUserAsync(string windowsUsername);

        /// <summary>
        /// Check if user has blank password (security validation)
        /// </summary>
        bool HasBlankPassword(string username, bool isMachine = false);

        /// <summary>
        /// Initiate Windows account linking process
        /// </summary>
        Task<(bool success, string verificationToken, string message)> InitiateAccountLinkingAsync(string windowsUsername, string regularUsername);

        /// <summary>
        /// Complete Windows account linking process
        /// </summary>
        Task<(bool success, string message)> CompleteAccountLinkingAsync(string windowsUsername, string regularUsername, string token);

        /// <summary>
        /// Decline Windows account linking
        /// </summary>
        Task<(bool success, string message)> DeclineAccountLinkingAsync(string windowsUsername);

        /// <summary>
        /// Unlink Windows account from regular account
        /// </summary>
        Task<(bool success, string message)> UnlinkAccountAsync(string regularUsername);

        /// <summary>
        /// Check Windows account link status
        /// </summary>
        Task<(bool success, bool isLinked, string windowsIdentity, bool needsLinking, string message)> CheckLinkStatusAsync(long userId);
    }
}