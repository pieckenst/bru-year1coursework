using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace TicketSalesApp.AdminServer.Security
{
    public class WindowsAuthSecurityRequirement : IAuthorizationRequirement { }

    public class WindowsAuthSecurityHandler : AuthorizationHandler<WindowsAuthSecurityRequirement>
    {
        private readonly ILogger<WindowsAuthSecurityHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WindowsAuthSecurityHandler(
            ILogger<WindowsAuthSecurityHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            WindowsAuthSecurityRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HTTP context is not available");
                return;
            }

            var windowsIdentity = httpContext.User.Identity as WindowsIdentity;
            if (windowsIdentity == null || !windowsIdentity.IsAuthenticated)
            {
                _logger.LogWarning("User is not authenticated with Windows Authentication");
                return;
            }

            // Check if the authentication was done with a password or Windows Hello
            var authType = windowsIdentity.AuthenticationType?.ToUpperInvariant() ?? string.Empty;
            
            // Authentication types that indicate password or Windows Hello
            var secureAuthTypes = new[] { "NEGOTIATE", "KERBEROS", "NTLM", "PASSPORT" };
            
            if (secureAuthTypes.Contains(authType))
            {
                // Additional check for blank passwords if using NTLM
                if (authType == "NTLM" && HasBlankPassword(windowsIdentity.Name))
                {
                    _logger.LogWarning("Windows Authentication failed: User has a blank password which is not allowed due to security purposes");
                    return;
                }
                
                context.Succeed(requirement);
                return;
            }

            _logger.LogWarning("Windows Authentication failed: Unsecure authentication method detected: {AuthType}", authType);
        }

        private bool HasBlankPassword(string username)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Domain))
                using (var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username))
                {
                    if (user == null)
                    {
                        _logger.LogWarning("Could not find user in Active Directory: {Username}", username);
                        return false;
                    }

                    // Try to authenticate with a blank password
                    return context.ValidateCredentials(user.SamAccountName, "", ContextOptions.Negotiate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for blank password for user: {Username}", username);
                return false; // Fail securely by not allowing access
            }
        }
    }
}
