using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Business logic service for Windows authentication operations
    /// </summary>
    public class WindowsAuthBusinessService : IWindowsAuthBusinessService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WindowsAuthBusinessService> _logger;
        private readonly AppDbContext _context;

        public WindowsAuthBusinessService(
            IConfiguration configuration,
            ILogger<WindowsAuthBusinessService> logger,
            AppDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task<(bool success, string token, User user, string message)> AuthenticateWindowsUserAsync(string windowsUsername)
        {
            try
            {
                var isMachineAccount = windowsUsername.StartsWith(
                    Environment.MachineName + "\\",
                    StringComparison.OrdinalIgnoreCase
                );

                _logger.LogInformation("Windows user authenticated: {WindowsUsername}", windowsUsername);

                // Lookup or provision in database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsUsername);

                // Security check for blank passwords
                bool hasBlankPassword = HasBlankPassword(windowsUsername, isMachineAccount);
                if (hasBlankPassword)
                {
                    string warningMessage = $"User {windowsUsername} blocked: blank or unset password";
                    _logger.LogWarning(warningMessage);
                    return (false, null, null, "Access denied: Your account is not securely configured. Please set a password in Windows settings and try again.");
                }

                var isNewUser = false;
                if (user == null)
                {
                    var username = windowsUsername.Split('\\', 2).Last();
                    
                    user = new User
                    {
                        Login = username,
                        WindowsIdentity = windowsUsername,
                        IsWindowsAuth = true,
                        Role = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        DoesWindowsAccountNeedLinking = true // New users need to link their account
                    };
                    
                    _context.Users.Add(user);
                    isNewUser = true;
                    
                    _logger.LogInformation(
                        "New user '{Username}' created for Windows identity '{WindowsUsername}'",
                        user.Login, windowsUsername
                    );
                }
                else
                {
                    _logger.LogInformation(
                        "Found existing user '{Username}' for Windows identity '{WindowsUsername}'",
                        user.Login, windowsUsername
                    );
                    
                    // Check for users who need to be prompted for linking
                    bool hasNoLinkedAccount = string.IsNullOrEmpty(user.LinkedRegularAccountUsername);
                    bool hasNoToken = string.IsNullOrEmpty(user.LinkedAccountToken);
                    bool needsLinkingByFlag = user.DoesWindowsAccountNeedLinking;
                    bool isFirstLogin = user.LastLoginAt == null;
                    bool lastLoginBeforeFeature = user.LastLoginAt < new DateTime(2025, 7, 30);
                    bool createdBeforeFeature = user.CreatedAt < new DateTime(2025, 7, 30);
                    
                    bool needsLinkingPrompt = hasNoLinkedAccount && 
                                           hasNoToken &&
                                           (needsLinkingByFlag || 
                                            isFirstLogin || 
                                            lastLoginBeforeFeature ||
                                            createdBeforeFeature);
                    
                    if (needsLinkingPrompt && user.LinkedAccountToken != "DECLINED")
                    {
                        if (user.LastLoginAt == null || user.LastLoginAt < new DateTime(2025, 7, 30) || needsLinkingPrompt)
                        {
                            user.DoesWindowsAccountNeedLinking = true;
                            _logger.LogInformation(
                                "Prompting existing user '{Username}' for Windows account linking (first login after feature implementation)",
                                user.Login
                            );
                        }
                    }
                    else if (user.DoesWindowsAccountNeedLinking && 
                             string.IsNullOrEmpty(user.LinkedRegularAccountUsername) && 
                             string.IsNullOrEmpty(user.LinkedAccountToken))
                    {
                        _logger.LogInformation(
                            "User '{Username}' still needs to complete Windows account linking",
                            user.Login
                        );
                    }
                }

                // Ensure role consistency for Windows accounts
                if (user.IsWindowsAuth && !string.IsNullOrEmpty(user.WindowsIdentity))
                {
                    var roles = await _context.UserRoles
                        .Include(ur => ur.Role)
                        .Where(ur => ur.UserId == user.GuidId)
                        .ToListAsync();

                    var highestLegacyRole = roles.Any(r => r.Role?.LegacyRoleId == 1) ? 1 : 0;
                    if (user.Role != highestLegacyRole)
                    {
                        _logger.LogInformation("Updating legacy role for user {UserId} from {OldRole} to {NewRole}", 
                            user.UserId, user.Role, highestLegacyRole);
                        user.Role = highestLegacyRole;
                    }
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = GenerateJwtToken(user);

                _logger.LogInformation("JWT token generated for user '{Username}'", user.Login);

                return (true, token, user, "Windows authentication successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred during Windows authentication.");
                return (false, null, null, "An internal server error occurred during authentication.");
            }
        }

        public bool HasBlankPassword(string username, bool isMachine = false)
        {
            try
            {
                var contextType = isMachine ? ContextType.Machine : ContextType.Domain;
                Console.WriteLine($"[DEBUG] Checking blank password for '{username}' (IsMachine: {isMachine})");
                
                using var context = new PrincipalContext(contextType);
                using var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);
                
                if (user == null)
                {
                    Console.WriteLine($"[DEBUG] User not found: {username}");
                    _logger.LogWarning("User not found: {Username}", username);
                    return false;
                }

                if (!isMachine)
                {
                    using var de = (DirectoryEntry)user.GetUnderlyingObject();
                    if (de.Properties.Contains("userAccountControl"))
                    {
                        int uac = (int)de.Properties["userAccountControl"].Value;
                        Console.WriteLine($"[DEBUG] UAC for domain user {username}: {uac}");
                        const int PASSWD_NOTREQD = 0x0020;
                        if ((uac & PASSWD_NOTREQD) != 0)
                        {
                            Console.WriteLine($"[DEBUG] PASSWD_NOTREQD flag detected for user {username}");
                            _logger.LogWarning("Domain user {Username} has PASSWD_NOTREQD flag set", username);
                            return true; // пароль не требуется
                        }
                    }
                }

                // Проверка с пустым паролем (без Sign/Seal для локальных)
                var options = isMachine
                    ? ContextOptions.Negotiate // fix: only option allowed for local
                    : ContextOptions.Negotiate | ContextOptions.Signing | ContextOptions.Sealing;

                bool acceptsBlankPassword = context.ValidateCredentials(
                    user.SamAccountName,
                    string.Empty,
                    options);

                Console.WriteLine($"[DEBUG] ValidateCredentials(empty) returned {acceptsBlankPassword} for {username}");

                if (acceptsBlankPassword)
                {
                    _logger.LogWarning("User {Username} accepted empty password", username);
                }

                return acceptsBlankPassword;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception during blank password check for {username}: {ex.Message}");
                _logger.LogError(ex, "Error in HasBlankPassword for {Username}", username);
                // Treat any exception as "insecure/no password" so we block the login
                return true;
            }
        }

        public async Task<(bool success, string verificationToken, string message)> InitiateAccountLinkingAsync(string windowsUsername, string regularUsername)
        {
            try
            {
                // Find the regular account
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == regularUsername);
                if (user == null)
                {
                    return (false, null, "User not found");
                }

                // Find or create Windows account
                var windowsAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsUsername);

                if (windowsAccount == null)
                {
                    windowsAccount = new User
                    {
                        Login = $"win_{windowsUsername.Replace("\\", "_")}",
                        WindowsIdentity = windowsUsername,
                        IsWindowsAuth = true,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Users.Add(windowsAccount);
                }

                // Check if Windows account is already linked to another account
                if (!string.IsNullOrEmpty(windowsAccount.LinkedRegularAccountUsername))
                {
                    return (false, null, "This Windows account is already linked to another account");
                }

                // Generate a secure token
                var random = new Random();
                var pin = random.Next(1000, 10000).ToString();
                var usernamePrefix = user.Login.Length >= 3
                    ? user.Login.Substring(0, 3)
                    : user.Login.PadRight(3, '_');
                var randomString = Path.GetRandomFileName().Replace(".", "");

                var token = $"{pin}{usernamePrefix}{randomString}";
                var hashedToken = BCrypt.Net.BCrypt.HashString(token);

                // Store linking info on the Windows account
                windowsAccount.LinkedAccountToken = hashedToken;
                windowsAccount.DoesWindowsAccountNeedLinking = true;
                windowsAccount.LinkedRegularAccountUsername = user.Login;

                _logger.LogInformation("Generated token for user {Username}", user.Login);
                await _context.SaveChangesAsync();

                return (true, token, "Please complete the linking process by signing in with your Windows account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Windows account linking");
                return (false, null, "An error occurred while initiating Windows account linking");
            }
        }

        public async Task<(bool success, string message)> CompleteAccountLinkingAsync(string windowsUsername, string regularUsername, string token)
        {
            try
            {
                // Find the Windows account that needs linking
                var windowsAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsUsername &&
                                           u.DoesWindowsAccountNeedLinking &&
                                           u.LinkedAccountToken != null &&
                                           u.LinkedRegularAccountUsername == regularUsername);

                if (windowsAccount == null)
                {
                    return (false, "No pending Windows account link found for this user");
                }

                // Verify the token
                if (!BCrypt.Net.BCrypt.Verify(token, windowsAccount.LinkedAccountToken))
                {
                    return (false, "Invalid or expired token");
                }

                // Find the regular account being linked
                var regularAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == regularUsername);

                if (regularAccount == null)
                {
                    return (false, "Linked regular account not found");
                }

                // Get all roles from the regular account
                var regularAccountRoles = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == regularAccount.GuidId)
                    .ToListAsync();

                // Determine the highest legacy role (1 for admin, 0 for user)
                var highestLegacyRole = regularAccountRoles.Any(ur => ur.Role?.LegacyRoleId == 1) ? 1 : 0;

                // Remove any existing roles from the Windows account
                var existingWindowsRoles = _context.UserRoles
                    .Where(ur => ur.UserId == windowsAccount.GuidId);
                _context.UserRoles.RemoveRange(existingWindowsRoles);

                // Copy all roles from the regular account to the Windows account
                foreach (var role in regularAccountRoles)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = windowsAccount.GuidId,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = "System"
                    });
                }

                // Complete the linking
                windowsAccount.Role = highestLegacyRole;
                windowsAccount.DoesWindowsAccountNeedLinking = false;

                await _context.SaveChangesAsync();

                return (true, "Windows account linked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing Windows account linking");
                return (false, "An error occurred while completing Windows account linking");
            }
        }

        public async Task<(bool success, string message)> DeclineAccountLinkingAsync(string windowsUsername)
        {
            try
            {
                var windowsAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsUsername);

                if (windowsAccount == null)
                {
                    return (false, "Windows account not found");
                }

                // Set a special token to indicate the user has explicitly declined
                windowsAccount.LinkedAccountToken = "DECLINED";
                windowsAccount.DoesWindowsAccountNeedLinking = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} has declined Windows account linking", windowsAccount.UserId);
                return (true, "Windows account linking prompt has been dismissed. You can still link your account manually later.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Windows account linking decline");
                return (false, "An error occurred while processing your request");
            }
        }

        public async Task<(bool success, string message)> UnlinkAccountAsync(string regularUsername)
        {
            try
            {
                var regularAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == regularUsername);

                if (regularAccount == null)
                {
                    return (false, "User not found");
                }

                // Find the Windows account that's linked to this regular account
                var windowsAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.LinkedRegularAccountUsername == regularAccount.Login);

                if (windowsAccount == null)
                {
                    return (false, "No linked Windows account found");
                }

                // Clear the linking information from the Windows account
                windowsAccount.LinkedAccountToken = string.Empty;
                windowsAccount.LinkedRegularAccountUsername = string.Empty;
                windowsAccount.DoesWindowsAccountNeedLinking = false;

                await _context.SaveChangesAsync();

                return (true, "Windows account unlinked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking Windows account");
                return (false, "An error occurred while unlinking Windows account");
            }
        }

        public async Task<(bool success, bool isLinked, string windowsIdentity, bool needsLinking, string message)> CheckLinkStatusAsync(long userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return (false, false, null, false, "User not found");
                }

                return (true, !string.IsNullOrEmpty(user.WindowsIdentity), user.WindowsIdentity, user.DoesWindowsAccountNeedLinking, "Success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Windows account link status");
                return (false, false, null, false, "An error occurred while checking Windows account link status");
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = _configuration["JwtSettings:Secret"] ??
                throw new InvalidOperationException("JWT secret is not configured");

            // Ensure the key is at least 32 bytes
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            if (keyBytes.Length < 32)
            {
                Array.Resize(ref keyBytes, 32);
            }
            else if (keyBytes.Length > 64)
            {
                Array.Resize(ref keyBytes, 64);
            }

            var key = new SymmetricSecurityKey(keyBytes);

            // Determine if the user needs to be prompted for Windows account linking
            bool needsLinking = user.IsWindowsAuth &&
                              string.IsNullOrEmpty(user.LinkedRegularAccountUsername) &&
                              string.IsNullOrEmpty(user.LinkedAccountToken) &&
                              (user.DoesWindowsAccountNeedLinking ||
                               user.LastLoginAt == null ||
                               user.LastLoginAt < new DateTime(2025, 7, 30) ||
                               user.CreatedAt < new DateTime(2025, 7, 30));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim("role", user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("is_windows_auth", user.IsWindowsAuth ? "true" : "false", ClaimValueTypes.Boolean),
                new Claim("does_windows_account_need_linking", needsLinking ? "true" : "false", ClaimValueTypes.Boolean)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "120")),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}