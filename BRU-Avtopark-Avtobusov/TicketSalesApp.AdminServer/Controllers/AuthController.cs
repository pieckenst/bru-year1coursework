using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.Core.Data;
using Serilog;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace TicketSalesApp.AdminServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TicketSalesApp.Services.Interfaces.IAuthenticationService _authService; // only to fix error caused by using Microsoft.AspNetCore.Authentication;
        private readonly IQRAuthenticationService _qrAuthService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public AuthController(
            TicketSalesApp.Services.Interfaces.IAuthenticationService authService,
            IQRAuthenticationService qrAuthService,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<AuthController> logger,
            AppDbContext context,
            IMemoryCache cache)
        {
            _authService = authService;
            _qrAuthService = qrAuthService;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        private bool HasBlankPassword(string username, bool isMachine = false)
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
                // Treat any exception as “insecure/no password” so we block the login
                return true;
            }
        }





        [Route("check-windows-link-status")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckWindowsLinkStatus()
        {
            try
            {
                var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new 
                { 
                    isLinked = !string.IsNullOrEmpty(user.WindowsIdentity),
                    windowsIdentity = user.WindowsIdentity,
                    needsLinking = user.DoesWindowsAccountNeedLinking
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Windows account link status");
                return StatusCode(500, new { message = "An error occurred while checking Windows account link status" });
            }
        }

        [Route("link-windows-account")]
        [Authorize(AuthenticationSchemes = "Windows")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LinkWindowsAccount([FromBody] LinkWindowsAccountModel model)
        {
            try
            {
                string windowsUsername;
                
                // If we have a Windows identity from authentication
                if (User.Identity is WindowsIdentity wi && wi.IsAuthenticated)
                {
                    windowsUsername = wi.Name;
                }
                // Otherwise use the provided WindowsUsername
                else if (!string.IsNullOrEmpty(model?.WindowsUsername))
                {
                    windowsUsername = model.WindowsUsername;
                }
                else
                {
                    return BadRequest(new { message = "Windows authentication or WindowsUsername is required" });
                }

                // If we have an authenticated user (from JWT)
                User user = null;
                if (long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                {
                    user = await _context.Users.FindAsync(userId);
                }
                // Otherwise try to find by username
                else if (!string.IsNullOrEmpty(model?.Username))
                {
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Login == model.Username);
                }

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Find or create Windows account
                var windowsAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsUsername);

                if (windowsAccount == null)
                {
                    // Create a new Windows account if it doesn't exist
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
                    return BadRequest(new { message = "This Windows account is already linked to another account" });
                }

                // Generate a secure token with PIN, username prefix, and random string
                var random = new Random();
                var pin = random.Next(1000, 10000).ToString();
                var usernamePrefix = user.Login.Length >= 3 
                    ? user.Login.Substring(0, 3) 
                    : user.Login.PadRight(3, '_');
                var randomString = Path.GetRandomFileName().Replace(".", "");
                
                // Combine components and hash with BCrypt
                var token = $"{pin}{usernamePrefix}{randomString}";
                var hashedToken = BCrypt.Net.BCrypt.HashString(token);
                
                // Store linking info on the Windows account
                windowsAccount.LinkedAccountToken = hashedToken;
                windowsAccount.DoesWindowsAccountNeedLinking = true;
                windowsAccount.LinkedRegularAccountUsername = user.Login;
                
                // For debugging/logging only - don't log the actual token in production
                _logger.LogInformation("Generated token for user {Username}", user.Login);
                
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    verificationToken = token,
                    message = "Please complete the linking process by signing in with your Windows account"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Windows account linking");
                return StatusCode(500, new { message = "An error occurred while initiating Windows account linking" });
            }
        }

        [Route("complete-windows-link")]
        [Authorize(AuthenticationSchemes = "Windows")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> CompleteWindowsLink([FromBody] CompleteWindowsLinkModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { message = "Invalid request data" });
                }

                string windowsUsername;
                
                // If we have a Windows identity from authentication
                if (User.Identity is WindowsIdentity wi && wi.IsAuthenticated)
                {
                    windowsUsername = wi.Name;
                }
                // Otherwise use the provided WindowsUsername
                else if (!string.IsNullOrEmpty(model.WindowsUsername))
                {
                    windowsUsername = model.WindowsUsername;
                }
                else
                {
                    return BadRequest(new { message = "Windows authentication or WindowsUsername is required" });
                }
                
                // Find the Windows account that needs linking
                var windowsAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsUsername &&
                                           u.DoesWindowsAccountNeedLinking &&
                                           u.LinkedAccountToken != null &&
                                           u.LinkedRegularAccountUsername == model.Username);

                if (windowsAccount == null)
                {
                    return BadRequest(new { message = "No pending Windows account link found for this user" });
                }

                // Verify the token
                if (!BCrypt.Net.BCrypt.Verify(model.Token, windowsAccount.LinkedAccountToken))
                {
                    return BadRequest(new { message = "Invalid or expired token" });
                }

                // Find the regular account being linked
                var regularAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == model.Username);

                if (regularAccount == null)
                {
                    return BadRequest(new { message = "Linked regular account not found" });
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
                // Keep the LinkedRegularAccountUsername to maintain the relationship
                
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Windows account linked successfully",
                    username = windowsAccount.Login,
                    windowsIdentity = windowsAccount.WindowsIdentity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing Windows account linking");
                return StatusCode(500, new { message = "An error occurred while completing Windows account linking" });
            }
        }

       

        [Route("decline-windows-link")]
        [Authorize(AuthenticationSchemes = "Windows")]
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> DeclineWindowsLink([FromBody] DeclineWindowsLinkModel model = null)
        {
            try
            {
                string windowsUsername = null;
                
                // Get Windows identity from authentication if available
                if (User.Identity is WindowsIdentity wi && wi.IsAuthenticated)
                {
                    windowsUsername = wi.Name;
                }
                // Otherwise use the provided WindowsUsername from the model
                else if (model != null && !string.IsNullOrEmpty(model.WindowsUsername))
                {
                    windowsUsername = model.WindowsUsername;
                }
                else
                {
                    return BadRequest(new { message = "Windows authentication or WindowsUsername is required" });
                }

                // Find the Windows account
                var windowsAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsUsername);

                if (windowsAccount == null)
                {
                    return NotFound(new { message = "Windows account not found" });
                }

                // Set a special token to indicate the user has explicitly declined
                windowsAccount.LinkedAccountToken = "DECLINED";
                windowsAccount.DoesWindowsAccountNeedLinking = false;
                
                
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} has declined Windows account linking", windowsAccount.UserId);
                return Ok(new { message = "Windows account linking prompt has been dismissed. You can still link your account manually later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Windows account linking decline");
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        [Route("unlink-windows-account")]
        [Authorize] // This will be called with JWT from the regular account
        [HttpPost]
        public async Task<IActionResult> UnlinkWindowsAccount()
        {
            try
            {
                var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var regularAccount = await _context.Users.FindAsync(userId);

                if (regularAccount == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Find the Windows account that's linked to this regular account
                var windowsAccount = await _context.Users
                    .FirstOrDefaultAsync(u => u.LinkedRegularAccountUsername == regularAccount.Login);

                if (windowsAccount == null)
                {
                    return BadRequest(new { message = "No linked Windows account found" });
                }

                // Clear the linking information from the Windows account
                windowsAccount.LinkedAccountToken = string.Empty;
                windowsAccount.LinkedRegularAccountUsername = string.Empty;
                windowsAccount.DoesWindowsAccountNeedLinking = false;
                
                await _context.SaveChangesAsync();

                return Ok(new { message = "Windows account unlinked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking Windows account");
                return StatusCode(500, new { message = "An error occurred while unlinking Windows account" });
            }
        }

        [Route("windows-login")]
        [Authorize(AuthenticationSchemes = "Windows")]
        [HttpGet]
        public async Task<IActionResult> WindowsLogin()
        {
            Console.WriteLine("[WindowsLogin] Starting Windows authentication flow");
            try
            {
                Console.WriteLine("[WindowsLogin] Checking Windows authentication status");
                if (!(User.Identity is WindowsIdentity wi) || !wi.IsAuthenticated)
                {
                    Console.WriteLine("[WindowsLogin] User not authenticated, triggering Windows auth challenge");
                    return Challenge(
                        new AuthenticationProperties(),
                        NegotiateDefaults.AuthenticationScheme
                    );
                }

                // 2) User is now authenticated by Windows
                var windowsUsername = wi.Name;
                Console.WriteLine($"[WindowsLogin] Windows user authenticated: {windowsUsername}");
                
                var isMachineAccount = windowsUsername.StartsWith(
                    Environment.MachineName + "\\",
                    StringComparison.OrdinalIgnoreCase
                );
                Console.WriteLine($"[WindowsLogin] Is machine account: {isMachineAccount}");

                _logger.LogInformation("Windows user authenticated: {WindowsUsername}", windowsUsername);

                // 3) Lookup or provision in your DB
                Console.WriteLine($"[WindowsLogin] Looking up user in database for Windows identity: {windowsUsername}");
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsUsername);
                Console.WriteLine($"[WindowsLogin] User found in database: {user != null}");

                // 4) Blank‑password block → Teapot 418
                Console.WriteLine("[WindowsLogin] Checking for blank password");
                bool hasBlankPassword = HasBlankPassword(windowsUsername, isMachineAccount);
                Console.WriteLine($"[WindowsLogin] Has blank password: {hasBlankPassword}");
                
                if (hasBlankPassword)
                {
                    string warningMessage = $"User {windowsUsername} blocked: blank or unset password";
                    Console.WriteLine($"[WindowsLogin] {warningMessage}");
                    _logger.LogWarning(warningMessage);

                    return StatusCode(418, new
                    {
                        message = "Access denied: Your account is not securely configured. " +
                                        "Please set a password in Windows settings and try again.",
                        codedtest = 418,
                        secondaryText = "I’m a teapot—not really, but your account looks like one."
                    });
                }

                var isNewUser = false;
                if (user == null)
                {
                    Console.WriteLine($"[WindowsLogin] Provisioning new user for Windows identity: {windowsUsername}");
                    var username = windowsUsername.Split('\\', 2).Last();
                    Console.WriteLine($"[WindowsLogin] Generated username: {username}");
                    
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
                    
                    Console.WriteLine($"[WindowsLogin] New user created with DoesWindowsAccountNeedLinking: {user.DoesWindowsAccountNeedLinking}");
                    _logger.LogInformation(
                        "New user '{Username}' created for Windows identity '{WindowsUsername}'",
                        user.Login, windowsUsername
                    );
                }
                else
                {
                    Console.WriteLine($"[WindowsLogin] Found existing user: {user.Login}");
                    Console.WriteLine($"[WindowsLogin] User details - " +
                                    $"DoesWindowsAccountNeedLinking: {user.DoesWindowsAccountNeedLinking}, " +
                                    $"LastLoginAt: {user.LastLoginAt}, " +
                                    $"LinkedAccountToken: {!string.IsNullOrEmpty(user.LinkedAccountToken)}");
                    
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
                    
                    Console.WriteLine($"[WindowsLogin] Link check - " +
                                    $"HasNoLinkedAccount: {hasNoLinkedAccount}, " +
                                    $"HasNoToken: {hasNoToken}, " +
                                    $"NeedsLinkingByFlag: {needsLinkingByFlag}, " +
                                    $"IsFirstLogin: {isFirstLogin}, " +
                                    $"LastLoginBeforeFeature: {lastLoginBeforeFeature}, " +
                                    $"CreatedBeforeFeature: {createdBeforeFeature}");
                    
                    // User needs linking if they have no linked account, no token, and either:
                    // 1. The linking flag is set, OR
                    // 2. It's their first login, OR
                    // 3. They last logged in before the feature was implemented, OR
                    // 4. They were created before the feature was implemented
                    bool needsLinkingPrompt = hasNoLinkedAccount && 
                                           hasNoToken &&
                                           (needsLinkingByFlag || 
                                            isFirstLogin || 
                                            lastLoginBeforeFeature ||
                                            createdBeforeFeature);
                    
                    Console.WriteLine($"[WindowsLogin] Needs linking prompt: {needsLinkingPrompt}");
                    
                    if (needsLinkingPrompt && user.LinkedAccountToken != "DECLINED")
                    {
                        Console.WriteLine($"[DEBUG] Prompting existing user '{user.Login}' for Windows account linking");
                        // Check if this is their first login after the linking feature was implemented
                        // or if they've never been prompted before
                        if (user.LastLoginAt == null || user.LastLoginAt < new DateTime(2025, 7, 30) || needsLinkingPrompt) // Assuming linking was implemented in July 2025
                        {
                            user.DoesWindowsAccountNeedLinking = true;
                            _logger.LogInformation(
                                "Prompting existing user '{Username}' for Windows account linking (first login after feature implementation)",
                                user.Login
                            );
                        }
                    }
                    // If they've been prompted before but haven't completed linking
                    else if (user.DoesWindowsAccountNeedLinking && 
                             string.IsNullOrEmpty(user.LinkedRegularAccountUsername) && 
                             string.IsNullOrEmpty(user.LinkedAccountToken))
                    {
                        Console.WriteLine($"[DEBUG] User '{user.Login}' still needs to complete Windows account linking");
                        _logger.LogInformation(
                            "User '{Username}' still needs to complete Windows account linking",
                            user.Login
                        );
                    }
                }

                // 3) Ensure role consistency for Windows accounts
                if (user.IsWindowsAuth && !string.IsNullOrEmpty(user.WindowsIdentity))
                {
                    // Get all roles for this user
                    var roles = await _context.UserRoles
                        .Include(ur => ur.Role)
                        .Where(ur => ur.UserId == user.GuidId)
                        .ToListAsync();

                    // Update legacy role based on current roles
                    var highestLegacyRole = roles.Any(r => r.Role?.LegacyRoleId == 1) ? 1 : 0;
                    if (user.Role != highestLegacyRole)
                    {
                        _logger.LogInformation("Updating legacy role for user {UserId} from {OldRole} to {NewRole}", 
                            user.UserId, user.Role, highestLegacyRole);
                        user.Role = highestLegacyRole;
                    }
                }

                // 4) Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                Console.WriteLine($"[WindowsLogin] Database changes saved successfully");

                // 6) Generate JWT token with appropriate claims
                Console.WriteLine("[WindowsLogin] Generating JWT token");
                var token = GenerateJwtToken(user);

                Console.WriteLine("[WindowsLogin] JWT token generated successfully - Showing user information:" +
                                $"{user}");
                
                Console.WriteLine($"[WindowsLogin] JWT token generated for user: {user.Login}" +
                                $" (DoesWindowsAccountNeedLinking: {user.DoesWindowsAccountNeedLinking})");
                
                _logger.LogInformation("JWT token generated for user '{Username}'", user.Login);

                var response = new
                {
                    token,
                    user = new
                    {
                        user.UserId,
                        user.Login,
                        user.Email,
                        user.Role,
                        IsWindowsAuth = true,
                        DoesWindowsAccountNeedLinking = user.DoesWindowsAccountNeedLinking
                    }
                };
                
                Console.WriteLine($"[WindowsLogin] Returning successful authentication response");
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Unexpected exception in WindowsLogin: {ex.Message}");
                _logger.LogError(ex, "An unexpected error occurred during Windows authentication.");
                return StatusCode(500, new
                {
                    message = "An internal server error occurred during authentication."
                });
            }
        }



        [Route("login")]
        [HttpGet]
        [AllowAnonymous]
        public ContentResult LoginPage()
        {
            Log.Information("Login page accessed");
            if (!_environment.IsDevelopment())
            {
                Log.Warning("Login page accessed in non-development environment");
                return Content("Not available in production", "text/plain");
            }

            var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Login</title>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body { 
            font-family: Arial, sans-serif;
            line-height: 1.6;
            padding: 20px;
            max-width: 1200px;
            margin: 0 auto;
            background-color: #f5f5f5;
        }

        .container {
            background: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            margin: 20px auto;
            width: 100%;
            max-width: 500px;
        }

        h2 {
            text-align: center;
            color: #333;
            margin-bottom: 20px;
        }

        .form-group { 
            margin-bottom: 15px; 
        }

        label { 
            display: block; 
            margin-bottom: 5px;
            color: #555;
            font-weight: bold;
        }

        input { 
            width: 100%;
            padding: 10px;
            border: 1px solid #ddd;
            border-radius: 4px;
            font-size: 16px;
        }

        input:focus {
            outline: none;
            border-color: #007bff;
            box-shadow: 0 0 0 2px rgba(0,123,255,0.25);
        }

        button { 
            width: 100%;
            padding: 12px;
            background-color: #007bff;
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
            transition: background-color 0.2s;
        }

        button:hover {
            background-color: #0056b3;
        }

        #result { 
            margin-top: 20px;
            padding: 15px;
            border-radius: 4px;
        }

        .success { 
            background-color: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
            padding: 15px;
            border-radius: 4px;
        }

        .error { 
            background-color: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
            padding: 15px;
            border-radius: 4px;
        }

        .role-admin { 
            color: #dc3545;
            font-weight: bold;
        }

        .role-user { 
            color: #28a745;
            font-weight: bold;
        }

        .debug-info { 
            background: #f8f9fa; 
            padding: 15px; 
            border: 1px solid #ddd; 
            margin-top: 20px;
            white-space: pre-wrap;
            font-family: monospace;
            overflow-x: auto;
        }

        .json-view {
            background: #2d2d2d;
            color: #fff;
            padding: 15px;
            border-radius: 4px;
            margin-top: 10px;
            overflow-x: auto;
            font-size: 14px;
        }

        #qrCodeSection {
            display: none;
            margin-top: 20px;
            padding: 20px;
            border: 1px solid #ddd;
            border-radius: 4px;
            background: white;
        }

        #qrCode {
            display: block;
            margin: 20px auto;
            max-width: 100%;
            height: auto;
        }

        .qr-title {
            font-size: 1.2em;
            font-weight: bold;
            margin-bottom: 10px;
            text-align: center;
            color: #333;
        }

        .qr-description {
            color: #666;
            text-align: center;
            margin-bottom: 15px;
            font-size: 0.9em;
        }

        .refresh-button {
            display: block;
            width: 200px;
            margin: 10px auto;
            padding: 8px 16px;
            background-color: #28a745;
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }

        .refresh-button:hover {
            background-color: #218838;
        }

        .test-qr-section {
            margin-top: 20px;
            padding: 15px;
            background-color: #f8f9fa;
            border: 1px solid #ddd;
            border-radius: 4px;
        }

        .test-qr-title {
            font-weight: bold;
            margin-bottom: 10px;
            color: #666;
            text-align: center;
        }

        .test-qr-button {
            display: block;
            width: 200px;
            margin: 10px auto;
            padding: 8px 16px;
            background-color: #17a2b8;
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }

        .test-qr-button:hover {
            background-color: #138496;
        }

        .test-qr-result {
            margin-top: 10px;
            padding: 10px;
            border-radius: 4px;
        }

        .test-qr-success {
            background-color: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }

        .test-qr-error {
            background-color: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }

        textarea {
            width: 100%;
            max-width: 100%;
            min-height: 60px;
            margin-top: 5px;
            padding: 8px;
            border: 1px solid #ddd;
            border-radius: 4px;
            font-family: monospace;
            font-size: 14px;
        }

        @media (max-width: 768px) {
            body {
                padding: 10px;
            }

            .container {
                padding: 15px;
                margin: 10px auto;
            }

            input, button {
                font-size: 14px;
                padding: 8px;
            }

            .json-view {
                font-size: 12px;
            }

            textarea {
                font-size: 12px;
            }

            .test-qr-button, .refresh-button {
                width: 100%;
            }
        }

        @media (max-width: 480px) {
            body {
                padding: 5px;
            }

            .container {
                padding: 10px;
                margin: 5px auto;
            }

            h2 {
                font-size: 1.5em;
            }

            .debug-info {
                font-size: 12px;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
    <h2>Login</h2>
    <div class='form-group'>
        <label for='login'>Login:</label>
        <input type='text' id='login' name='login' required>
    </div>
    <div class='form-group'>
        <label for='password'>Password:</label>
        <input type='password' id='password' name='password' required>
    </div>
    <button onclick='submitLogin()'>Login</button>
    <div id='result'></div>

        <div id='qrCodeSection'>
            <div class='qr-title'>Quick Login QR Code</div>
            <div class='qr-description'>Scan this QR code with the mobile app to quickly log in next time</div>
            <img id='qrCode' alt='QR Code for quick login'>
            <button class='refresh-button' onclick='refreshQRCode()'>Refresh QR Code</button>
            
            <!-- Debug section - only shown in development -->
            <div class='debug-info'>
                <h4>QR Code Debug Data</h4>
                <div id='qrCodeDebugData' class='json-view'></div>
            </div>

            <!-- QR Code Testing Section -->
            <div class='test-qr-section'>
                <div class='test-qr-title'>Test QR Code Login</div>
                <p>This section simulates scanning the QR code with a mobile device.</p>
                <button class='test-qr-button' onclick='testQRLogin()'>Simulate QR Code Scan</button>
                <div id='testQrResult' class='test-qr-result' style='display: none;'></div>
            </div>
        </div>

    <div id='debug-info' class='debug-info' style='display: none;'>
        <h3>Debug Information</h3>
        <div id='request-info'>
            <h4>Request</h4>
            <div id='request-json' class='json-view'></div>
        </div>
        <div id='response-info'>
            <h4>Response</h4>
            <div id='response-json' class='json-view'></div>
        </div>
        <div id='token-info'>
            <h4>Decoded Token</h4>
            <div id='token-json' class='json-view'></div>
            </div>
        </div>
    </div>

    <script>
        let authToken = '';
        let lastQrData = null;

        function formatJson(obj) {
            return JSON.stringify(obj, null, 2)
                .replace(/""([^""]+)""/g, '<span class=""key"">""$1""</span>')
                .replace(/"": ""([^""]+)""/g, '"": <span class=""string"">""$1""</span>')
                .replace(/"": (\d+)/g, '"": <span class=""number"">$1</span>')
                .replace(/"": (true|false)/g, '"": <span class=""boolean"">$1</span>');
        }

        async function fetchQRCode() {
            try {
                const response = await fetch('/api/auth/qr/generate', {
                    headers: {
                        'Authorization': `Bearer ${authToken}`
                    }
                });

                if (!response.ok) {
                    throw new Error('Failed to generate QR code');
                }

                const data = await response.json();
                document.getElementById('qrCode').src = `data:image/png;base64,${data.qrCode}`;
                document.getElementById('qrCodeSection').style.display = 'block';
                lastQrData = data.rawData; // Store the raw data for testing

                // Add debug information in development mode
                const debugQrData = document.getElementById('qrCodeDebugData');
                if (debugQrData) {
                    debugQrData.innerHTML = formatJson({
                        qrCodeBase64: data.qrCode.substring(0, 100) + '...', // Show first 100 chars
                        rawData: data.rawData || 'Not available'
                    });
                }
            } catch (error) {
                console.error('Error generating QR code:', error);
            }
        }

        async function testQRLogin() {
            if (!lastQrData) {
                alert('Please generate a QR code first');
                return;
            }

            const resultDiv = document.getElementById('testQrResult');
            resultDiv.style.display = 'block';
            resultDiv.className = 'test-qr-result';

            try {
                const response = await fetch('/api/auth/qr/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ token: lastQrData })
                });

                const data = await response.json();

                if (response.ok) {
                    resultDiv.className = 'test-qr-result test-qr-success';
                    resultDiv.innerHTML = `
                        <h4>QR Login Successful!</h4>
                        <p>New JWT Token received:</p>
                        <textarea rows='3' cols='50'>${data.token}</textarea>
                        <p>This simulates what would happen when scanning the QR code with a mobile device.</p>
                    `;

                    // Decode and show token info
                    const tokenParts = data.token.split('.');
                    const payload = JSON.parse(atob(tokenParts[1]));
                    const tokenInfo = document.createElement('div');
                    tokenInfo.className = 'debug-info';
                    tokenInfo.innerHTML = `
                        <h4>Token Payload:</h4>
                        <div class='json-view'>${formatJson(payload)}</div>
                    `;
                    resultDiv.appendChild(tokenInfo);
                } else {
                    resultDiv.className = 'test-qr-result test-qr-error';
                    resultDiv.innerHTML = `
                        <h4>QR Login Failed</h4>
                        <p>Error: ${data.message || 'Unknown error'}</p>
                    `;
                }
            } catch (error) {
                resultDiv.className = 'test-qr-result test-qr-error';
                resultDiv.innerHTML = `
                    <h4>QR Login Error</h4>
                    <p>Error: ${error.message}</p>
                `;
            }
        }

        async function refreshQRCode() {
            if (authToken) {
                await fetchQRCode();
            }
        }

        async function submitLogin() {
            const login = document.getElementById('login').value;
            const password = document.getElementById('password').value;
            const resultDiv = document.getElementById('result');
            const debugInfo = document.getElementById('debug-info');
            const requestJson = document.getElementById('request-json');
            const responseJson = document.getElementById('response-json');
            const tokenJson = document.getElementById('token-json');

            try {
                const requestData = { login, password };
                requestJson.innerHTML = formatJson(requestData);
                debugInfo.style.display = 'block';

                const response = await fetch('/api/auth/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(requestData)
                });

                const data = await response.json();
                responseJson.innerHTML = formatJson(data);

                if (response.ok) {
                    const tokenParts = data.token.split('.');
                    const payload = JSON.parse(atob(tokenParts[1]));
                    tokenJson.innerHTML = formatJson(payload);
                    
                    const role = payload['role'];
                    const isAdmin = role === '1';
                    
                    resultDiv.innerHTML = `
                        <div class='success'>
                            <p>Login successful!</p>
                            <p>Role: <span class='${isAdmin ? 'role-admin' : 'role-user'}'>${isAdmin ? 'Administrator' : 'Regular User'}</span></p>
                            <p>Token:</p>
                            <textarea rows='3' cols='50'>${data.token}</textarea>
                        </div>`;

                    // Store token and fetch QR code
                    authToken = data.token;
                    await fetchQRCode();
                } else {
                    resultDiv.innerHTML = `<div class='error'>Error: ${data.message || 'Login failed'}</div>`;
                    document.getElementById('qrCodeSection').style.display = 'none';
                }
            } catch (error) {
                resultDiv.innerHTML = `<div class='error'>Error: ${error.message}</div>`;
                responseJson.innerHTML = formatJson({ error: error.message });
                document.getElementById('qrCodeSection').style.display = 'none';
            }
        }
    </script>
</body>
</html>";
            return Content(html, "text/html");
        }

        [Route("register")]
        [HttpGet]
        [AllowAnonymous]
        public ContentResult RegisterPage()
        {
            if (!_environment.IsDevelopment())
                return Content("Not available in production", "text/plain");

            var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Register</title>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body { 
            font-family: Arial, sans-serif;
            line-height: 1.6;
            padding: 20px;
            max-width: 1200px;
            margin: 0 auto;
            background-color: #f5f5f5;
        }

        .container {
            background: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            margin: 20px auto;
            width: 100%;
            max-width: 500px;
        }

        h2 {
            text-align: center;
            color: #333;
            margin-bottom: 20px;
        }

        .form-group { 
            margin-bottom: 15px; 
        }

        label { 
            display: block; 
            margin-bottom: 5px;
            color: #555;
            font-weight: bold;
        }

        input, select { 
            width: 100%;
            padding: 10px;
            border: 1px solid #ddd;
            border-radius: 4px;
            font-size: 16px;
        }

        input:focus, select:focus {
            outline: none;
            border-color: #28a745;
            box-shadow: 0 0 0 2px rgba(40,167,69,0.25);
        }

        .role-select {
            background-color: white;
        }

        button { 
            width: 100%;
            padding: 12px;
            background-color: #28a745;
            color: white;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 16px;
            transition: background-color 0.2s;
        }

        button:hover {
            background-color: #218838;
        }

        #result { 
            margin-top: 20px;
            padding: 15px;
            border-radius: 4px;
        }

        .success { 
            background-color: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
            padding: 15px;
            border-radius: 4px;
        }

        .error { 
            background-color: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
            padding: 15px;
            border-radius: 4px;
        }

        .debug-info { 
            background: #f8f9fa; 
            padding: 15px; 
            border: 1px solid #ddd; 
            margin-top: 20px;
            white-space: pre-wrap;
            font-family: monospace;
            overflow-x: auto;
        }

        .json-view {
            background: #2d2d2d;
            color: #fff;
            padding: 15px;
            border-radius: 4px;
            margin-top: 10px;
            overflow-x: auto;
            font-size: 14px;
        }

        @media (max-width: 768px) {
            body {
            padding: 10px;
            }

            .container {
                padding: 15px;
                margin: 10px auto;
            }

            input, select, button {
                font-size: 14px;
                padding: 8px;
            }

            .json-view {
                font-size: 12px;
            }
        }

        @media (max-width: 480px) {
            body {
                padding: 5px;
            }

            .container {
                padding: 10px;
                margin: 5px auto;
            }

            h2 {
                font-size: 1.5em;
            }

            .debug-info {
                font-size: 12px;
            }
        }
    </style>
</head>
<body>
    <div class='container'>
    <h2>Register New User</h2>
    <div class='form-group'>
        <label for='login'>Login:</label>
        <input type='text' id='login' name='login' required>
    </div>
    <div class='form-group'>
        <label for='password'>Password:</label>
        <input type='password' id='password' name='password' required>
    </div>
    <div class='form-group'>
        <label for='role'>Role:</label>
        <select id='role' name='role' class='role-select'>
            <option value='0'>Regular User</option>
            <option value='1'>Administrator</option>
        </select>
    </div>
    <div class='form-group'>
        <label for='token'>Admin Token:</label>
        <input type='text' id='token' name='token' placeholder='Required for registration' required>
    </div>
    <button onclick='submitRegister()'>Register</button>
    <div id='result'></div>

    <div id='debug-info' class='debug-info' style='display: none;'>
        <h3>Debug Information</h3>
        <div id='request-info'>
            <h4>Request</h4>
            <div class='headers-info'>
                <strong>Headers:</strong>
                <div id='request-headers'></div>
            </div>
            <div id='request-json' class='json-view'></div>
        </div>
        <div id='response-info'>
            <h4>Response</h4>
            <div class='headers-info'>
                <strong>Response Status:</strong>
                <div id='response-status'></div>
            </div>
            <div id='response-json' class='json-view'></div>
        </div>
        <div id='error-details' class='debug-info' style='display: none;'>
            <h4>Error Details</h4>
            <div id='error-info' class='json-view'></div>
            </div>
        </div>
    </div>

    <script>
        function formatJson(obj) {
            return JSON.stringify(obj, null, 2)
                .replace(/""([^""]+)""/g, '<span class=""key"">""$1""</span>')
                .replace(/"": ""([^""]+)""/g, '"": <span class=""string"">""$1""</span>')
                .replace(/"": (\d+)/g, '"": <span class=""number"">$1</span>')
                .replace(/"": (true|false)/g, '"": <span class=""boolean"">$1</span>');
        }

        async function submitRegister() {
            const login = document.getElementById('login').value;
            const password = document.getElementById('password').value;
            const role = parseInt(document.getElementById('role').value);
            const token = document.getElementById('token').value;
            const resultDiv = document.getElementById('result');
            const debugInfo = document.getElementById('debug-info');
            const requestJson = document.getElementById('request-json');
            const responseJson = document.getElementById('response-json');
            const requestHeaders = document.getElementById('request-headers');
            const responseStatus = document.getElementById('response-status');
            const errorInfo = document.getElementById('error-info');
            const errorDetails = document.getElementById('error-details');

            try {
                const requestData = { login, password, role };
                const headers = {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                };

                requestJson.innerHTML = formatJson(requestData);
                requestHeaders.innerHTML = formatJson(headers);
                debugInfo.style.display = 'block';
                errorDetails.style.display = 'none';

                const response = await fetch('/api/auth/register', {
                    method: 'POST',
                    headers: headers,
                    body: JSON.stringify(requestData)
                });

                responseStatus.innerHTML = `HTTP ${response.status} ${response.statusText}`;
                
                let responseData;
                const responseText = await response.text();
                try {
                    responseData = JSON.parse(responseText);
                    responseJson.innerHTML = formatJson(responseData);
                } catch (parseError) {
                    errorDetails.style.display = 'block';
                    errorInfo.innerHTML = formatJson({
                        error: 'Failed to parse response as JSON',
                        responseText: responseText,
                        parseError: parseError.message
                    });
                    throw new Error(`Failed to parse response: ${parseError.message}. Raw response: ${responseText}`);
                }

                if (response.ok) {
                    resultDiv.innerHTML = `
                        <div class='success'>
                            <p>Registration successful!</p>
                            <p>Role: <span class='${role === 1 ? 'role-admin' : 'role-user'}'>${role === 1 ? 'Administrator' : 'Regular User'}</span></p>
                        </div>`;
                } else {
                    resultDiv.innerHTML = `<div class='error'>Error: ${responseData.message || 'Registration failed'}</div>`;
                }
            } catch (error) {
                errorDetails.style.display = 'block';
                errorInfo.innerHTML = formatJson({
                    error: error.message,
                    stack: error.stack,
                    type: error.name
                });
                resultDiv.innerHTML = `<div class='error'>Error: ${error.message}</div>`;
            }
        }
    </script>
</body>
</html>";
            return Content(html, "text/html");
        }

        [Route("login")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Login([FromBody] LoginModel model)
        {
            Log.Information("Login attempt started for user {Login}", model.Login);

            if (!ModelState.IsValid)
            {
                Log.Warning("Invalid model state for login request: {ValidationErrors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            var user = await _authService.AuthenticateAsync(model.Login, model.Password);
            if (user == null)
            {
                Log.Warning("Failed login attempt for user {Login}: Invalid credentials", model.Login);
                return Unauthorized(new { message = "Invalid username or password" });
            }

            Log.Debug("User {Login} successfully authenticated, generating JWT token", model.Login);
            var token = GenerateJwtToken(user);

            Log.Information("Successful login for user {Login} with role {Role}", model.Login, user.Role);
            return Ok(new { token });
        }

        [Route("register")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<object>> Register([FromBody] RegisterModel model)
        {
            try
            {
                Log.Information("Starting user registration process for {Login}", model.Login);

                // Get the token from the Authorization header
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    Log.Warning("Missing or invalid Authorization header in registration request");
                    return Unauthorized(new
                    {
                        success = false,
                        message = "No token provided",
                        details = new { error = "Authorization header missing or invalid" }
                    });
                }

                var token = authHeader.Substring("Bearer ".Length);
                var tokenHandler = new JwtSecurityTokenHandler();

                // First just read the token without validation to check the role
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");

                if (roleClaim?.Value != "1")
                {
                    Log.Warning("Unauthorized registration attempt. Required role: 1, Provided role: {Role}", roleClaim?.Value ?? "none");
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Not authorized to register users",
                        details = new
                        {
                            requiredRole = "1",
                            providedRole = roleClaim?.Value ?? "none",
                            claims = jwtToken.Claims.Select(c => new { c.Type, c.Value })
                        }
                    });
                }

                // Now validate the token properly
                var keyString = _configuration["JwtSettings:Secret"] ??
                    throw new InvalidOperationException("JWT secret is not configured");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                try
                {
                    var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                    Log.Debug("Token validation successful for registration request by {Username}",
                        principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Token validation failed during registration");
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token",
                        details = new
                        {
                            error = ex.Message,
                            stack = ex.StackTrace,
                            innerException = ex.InnerException?.Message
                        }
                    });
                }

                // Token is valid and user is admin, proceed with registration
                if (!ModelState.IsValid)
                {
                    Log.Warning("Invalid registration data for {Login}", model.Login);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid registration data",
                        details = new
                        {
                            modelState = ModelState.ToDictionary(
                                kvp => kvp.Key,
                                kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                            )
                        }
                    });
                }

                // Create user with new fields
                var user = new User
                {
                    Login = model.Login,
                    Role = model.Role,
                    PhoneNumber = model.PhoneNumber ?? "+375333000000",
                    Email = model.Email ?? "placeholderemail@mogilev.by",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Attempt to register the user
                var success = await _authService.RegisterAsync(model.Login, model.Password, model.Role);
                if (!success)
                {
                    Log.Warning("Registration failed for {Login}. User may already exist.", model.Login);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Registration failed",
                        details = new
                        {
                            error = "User already exists or database constraints violated",
                            attemptedUser = new { model.Login, role = model.Role }
                        }
                    });
                }

                // Get the created user to return in response
                var createdUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == model.Login);

                if (createdUser != null)
                {
                    // Update the additional fields
                    createdUser.PhoneNumber = user.PhoneNumber;
                    createdUser.Email = user.Email;
                    await _context.SaveChangesAsync();
                }

                Log.Information("User {Login} successfully registered with role {Role} by {RegisteredBy}",
                    model.Login, model.Role, jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value);

                return Ok(new
                {
                    success = true,
                    message = "User registered successfully",
                    details = new
                    {
                        user = new
                        {
                            createdUser.UserId,
                            createdUser.Login,
                            createdUser.Role,
                            createdUser.PhoneNumber,
                            createdUser.Email,
                            createdAt = DateTime.UtcNow
                        },
                        registeredBy = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                Log.Error(ex, "Registration failed for {Login}. Error: {ErrorMessage}", model.Login, ex.Message);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Registration failed due to server error",
                    details = new
                    {
                        error = ex.Message,
                        stackTrace = ex.StackTrace,
                        innerException = ex.InnerException?.Message,
                        data = ex.Data,
                        source = ex.Source,
                        targetSite = ex.TargetSite?.Name,
                        time = DateTime.UtcNow,
                        requestData = new
                        {
                            model.Login,
                            role = model.Role,
                            headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
                        }
                    }
                });
            }
        }

        private string GenerateJwtToken(User user)
        {
            Log.Information("Starting JWT token generation for user {Login}", user.Login);

            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = _configuration["JwtSettings:Secret"] ??
                throw new InvalidOperationException("JWT secret is not configured");

            // Ensure the key is at least 32 bytes
            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            if (keyBytes.Length < 32)
            {
                Log.Debug("JWT key was too short ({Length} bytes), padding to 32 bytes", keyBytes.Length);
                Array.Resize(ref keyBytes, 32);
            }
            else if (keyBytes.Length > 64)
            {
                Log.Debug("JWT key was too long ({Length} bytes), truncating to 64 bytes", keyBytes.Length);
                Array.Resize(ref keyBytes, 64);
            }

            var key = new SymmetricSecurityKey(keyBytes);
            var expirationMinutes = double.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "120");

            Log.Debug("Creating token descriptor for user {Login} with expiration in {ExpirationMinutes} minutes",
                user.Login, expirationMinutes);

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
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("is_windows_auth", user.IsWindowsAuth ? "true" : "false", ClaimValueTypes.Boolean),
                new Claim("does_windows_account_need_linking", needsLinking ? "true" : "false", ClaimValueTypes.Boolean)
            };
            
            Log.Debug("JWT claims for user {Login} - DoesWindowsAccountNeedLinking: {Value}", 
                    user.Login, needsLinking);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "120")),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            Log.Information("Successfully generated JWT token for user {Login} with expiration at {Expiration}",
                user.Login, tokenDescriptor.Expires);


            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [Route("qr/generate")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<string>> GenerateQRLogin()
        {
            try
            {
                Log.Information("Generating QR login code for authenticated user");

                // Get current user from claims
                var userLogin = User.Identity?.Name;
                if (string.IsNullOrEmpty(userLogin))
                {
                    Log.Warning("No user identity found in token for QR code generation");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == userLogin);
                if (user == null)
                {
                    Log.Warning("User {Login} not found for QR code generation", userLogin);
                    return NotFound(new { message = "User not found" });
                }

                // Generate QR code
                var (qrCodeBase64, rawData) = await _qrAuthService.GenerateQRCodeWithDataAsync(user);

                var response = new
                {
                    qrCode = qrCodeBase64,
                    rawData = _environment.IsDevelopment() ? rawData : null
                };

                Log.Information("Successfully generated QR code for user {Login}", userLogin);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating QR login code");
                return StatusCode(500, new { message = "Error generating QR code", error = ex.Message });
            }
        }

        [Route("qr/login")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<string>> QRLogin([FromBody] QRLoginModel model)
        {
            try
            {
                Log.Information("QR login attempt started");

                var (success, user) = await _qrAuthService.ValidateQRLoginTokenAsync(model.Token);
                if (!success || user == null)
                {
                    Log.Warning("QR login validation failed");
                    return Unauthorized(new { message = "Invalid QR login token" });
                }

                Log.Debug("QR login successful for user {Login}, generating JWT token", user.Login);
                var token = GenerateJwtToken(user);

                Log.Information("Successful QR login for user {Login} with role {Role}", user.Login, user.Role);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during QR login");
                return StatusCode(500, new { message = "Error during QR login", error = ex.Message });
            }
        }

        // New direct QR login endpoints
        [Route("qr/direct/generate")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<object>> GenerateDirectLoginQRCode([FromQuery] string username, [FromQuery] string deviceType)
        {
            try
            {
                Log.Information("Generating direct login QR code for user {Username} on device type {DeviceType}",
                    username, deviceType);

                // Validate user exists
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == username);
                if (user == null)
                {
                    Log.Warning("User {Username} not found for QR code generation", username);
                    return NotFound(new { message = "User not found" });
                }

                var (qrCode, rawData) = await _qrAuthService.GenerateDirectLoginQRCodeAsync(username, deviceType);

                var response = new
                {
                    qrCode,
                    rawData = _environment.IsDevelopment() ? rawData : null
                };

                Log.Information("Successfully generated direct login QR code for user {Username}", username);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating direct login QR code");
                return StatusCode(500, new { message = "Error generating QR code", error = ex.Message });
            }
        }

        [Route("qr/direct/login")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<object>> DirectQRLogin([FromBody] DirectQRLoginModel model)
        {
            try
            {
                Log.Information("Direct QR login attempt started for device type {DeviceType}", model.DeviceType);

                var (success, user, deviceId) = await _qrAuthService.ValidateDirectLoginTokenAsync(model.Token, model.DeviceType);
                if (!success || user == null)
                {
                    Log.Warning("Direct QR login validation failed");
                    return Unauthorized(new { message = "Invalid QR login token" });
                }

                // Authenticate user without password
                var authenticatedUser = await _authService.AuthenticateDirectQRAsync(user.Login, deviceId);
                if (authenticatedUser == null)
                {
                    Log.Warning("Direct QR login authentication failed for user {Login}", user.Login);
                    return Unauthorized(new { message = "Authentication failed" });
                }

                Log.Debug("Direct QR login successful for user {Login}, generating JWT token", user.Login);
                var token = GenerateJwtToken(authenticatedUser);

                // If this is a mobile device scanning a desktop QR code, notify the desktop
                if (model.DeviceType == "mobile" && model.IsDesktopLogin)
                {
                    await _qrAuthService.NotifyDeviceLoginSuccessAsync(deviceId, token);
                    Log.Information("Notified desktop of successful login for device {DeviceId}", deviceId);
                }

                Log.Information("Successful direct QR login for user {Login} with role {Role}", user.Login, user.Role);
                return Ok(new { token, deviceId });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during direct QR login");
                return StatusCode(500, new { message = "Error during QR login", error = ex.Message });
            }
        }

        [Route("qr/direct/check")]
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<object>> CheckDirectLoginStatus([FromQuery] string deviceId)
        {
            try
            {
                var loginSuccessKey = $"login_success_{deviceId}";
                if (_cache.TryGetValue(loginSuccessKey, out string token))
                {
                    _cache.Remove(loginSuccessKey); // One-time use
                    return Ok(new { success = true, token });
                }

                return Ok(new { success = false });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking direct login status");
                return StatusCode(500, new { message = "Error checking login status", error = ex.Message });
            }
        }
    }

    public class LoginModel
    {
        public required string Login { get; set; }
        public required string Password { get; set; }
    }

    public class RegisterModel
    {
        public required string Login { get; set; }
        public required string Password { get; set; }
        public int Role { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }

    public class QRLoginModel
    {
        public required string Token { get; set; }
    }

    public class DirectQRLoginModel
    {
        public string Token { get; set; }
        public string DeviceId { get; set; }
        public bool IsDesktopLogin { get; set; }
        public string DeviceType { get; internal set; }
    }

    public class LinkWindowsAccountModel
    {
        public string WindowsUsername { get; set; }
        public string Username { get; set; }
    }

    public class CompleteWindowsLinkModel
    {
        public string WindowsUsername { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }
    }
    public class DeclineWindowsLinkModel
    {
        public string WindowsUsername { get; set; }
        public string Username { get; set; }
    }
}