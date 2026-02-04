using System.Threading.Tasks;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    /// <summary>
    /// Business logic service for authentication operations
    /// Separates business logic from controller concerns
    /// </summary>
    public interface IAuthenticationBusinessService
    {
        /// <summary>
        /// Authenticate user and generate JWT token
        /// </summary>
        Task<(bool success, string token, string message)> AuthenticateUserAsync(string login, string password);

        /// <summary>
        /// Register new user with admin validation
        /// </summary>
        Task<(bool success, User user, string message)> RegisterUserAsync(string login, string password, int role, string adminToken);

        /// <summary>
        /// Generate JWT token for user
        /// </summary>
        string GenerateJwtToken(User user);

        /// <summary>
        /// Validate admin token for registration
        /// </summary>
        Task<(bool isValid, string message)> ValidateAdminTokenAsync(string token);
    }
}