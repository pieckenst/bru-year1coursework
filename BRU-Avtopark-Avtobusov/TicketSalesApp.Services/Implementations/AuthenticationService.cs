using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Configuration;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly AppDbContext _context;
        private readonly IRoleService _roleService;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly Configuration.WindowsAuthSettings _windowsAuthSettings;

        public AuthenticationService(
            AppDbContext context, 
            IRoleService roleService, 
            ILogger<AuthenticationService> logger,
            IOptions<WindowsAuthSettings> windowsAuthSettings = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _windowsAuthSettings = windowsAuthSettings?.Value ?? new WindowsAuthSettings();
        }

        public async Task<User?> AuthenticateAsync(string login, string password)
        {
            try
            {
                _logger.LogInformation("Attempting to authenticate user: {Login}", login);

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Authentication attempt with empty login or password");
                    return null;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed: User not found for login: {Login}", login);
                    return null;
                }

                if (VerifyPassword(password, user.PasswordHash))
                {
                    _logger.LogInformation("User {Login} authenticated successfully", login);
                    return user;
                }

                _logger.LogWarning("Authentication failed: Invalid password for user: {Login}", login);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while authenticating user: {Login}", login);
                throw new Exception($"Authentication failed for user: {login}", ex);
            }
        }

        public async Task<User> AuthenticateWindowsUserAsync(string windowsIdentity)
        {
            try
            {
                _logger.LogInformation("Attempting to authenticate Windows user: {WindowsIdentity}", windowsIdentity);

                if (string.IsNullOrEmpty(windowsIdentity))
                {
                    _logger.LogWarning("Windows authentication failed: Empty Windows identity provided");
                    return null;
                }

                // Find existing user by Windows identity
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsIdentity);

                if (user != null)
                {
                    _logger.LogInformation("Found existing user for Windows identity: {WindowsIdentity}", windowsIdentity);
                    return user;
                }

                // Auto-provision new user if enabled
                if (!_windowsAuthSettings.Enabled || !_windowsAuthSettings.AutoProvisionUsers)
                {
                    _logger.LogWarning("Auto-provisioning is disabled for Windows user: {WindowsIdentity}", windowsIdentity);
                    return null;
                }

                // Create a new user for Windows authentication
                var username = GetUsernameFromWindowsIdentity(windowsIdentity);
                var defaultRole = await _roleService.GetRoleByLegacyIdAsync(_windowsAuthSettings.DefaultRole)
                    ?? await _roleService.GetRoleByLegacyIdAsync(0); // Fallback to user role

                if (defaultRole == null)
                {
                    _logger.LogError("Windows authentication failed: Could not find default role");
                    return null;
                }

                user = new User
                {
                    Login = username,
                    WindowsIdentity = windowsIdentity,
                    IsWindowsAuth = true,
                    Role = defaultRole.LegacyRoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Auto-provisioned new user for Windows identity: {WindowsIdentity}", windowsIdentity);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Windows authentication for identity: {WindowsIdentity}", windowsIdentity);
                throw new ApplicationException("An error occurred during Windows authentication", ex);
            }
        }

        public async Task<bool> RegisterAsync(string login, string password, int role)
        {
            try
            {
                _logger.LogInformation("Attempting to register new user with login: {Login}", login);

                if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Registration attempt with empty login or password");
                    return false;
                }

                if (await _context.Users.AnyAsync(u => u.Login == login))
                {
                    _logger.LogWarning("Registration failed: User already exists with login: {Login}", login);
                    return false;
                }

                var defaultRole = await _roleService.GetRoleByLegacyIdAsync(role)
                    ?? await _roleService.GetRoleByLegacyIdAsync(0); // Fallback to user role

                if (defaultRole == null)
                {
                    _logger.LogError("Registration failed: Could not find default role for role ID: {RoleId}", role);
                    return false;
                }

                var user = new User
                {
                    Login = login,
                    PasswordHash = HashPassword(password),
                    Role = role,
                    IsActive = true,
                    IsWindowsAuth = false
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Assign the role using the new system
                await _roleService.AssignRoleToUserAsync(user.UserId, defaultRole.RoleId);

                _logger.LogInformation("User {Login} registered successfully with role {RoleName}", 
                    login, defaultRole.Name);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while registering user: {Login}", login);
                throw new Exception($"Registration failed for user: {login}", ex);
            }
        }

        private string HashPassword(string password)
        {
            try
            {
                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(password);
                var hashBytes = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hashBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while hashing password");
                throw new Exception("Failed to hash password", ex);
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                return HashPassword(password) == storedHash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while verifying password");
                throw new Exception("Failed to verify password", ex);
            }
        }

        private string GetUsernameFromWindowsIdentity(string windowsIdentity)
        {
            // Extract the username part from DOMAIN\username format
            var parts = windowsIdentity.Split('\\');
            return parts.Length > 1 ? parts[1] : windowsIdentity;
        }

        public async Task<bool> LinkWindowsIdentityAsync(int userId, string windowsIdentity)
        {
            try
            {
                _logger.LogInformation("Linking Windows identity {WindowsIdentity} to user ID {UserId}", 
                    windowsIdentity, userId);

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for Windows identity linking", userId);
                    return false;
                }

                // Check if the Windows identity is already linked to another user
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.WindowsIdentity == windowsIdentity && u.UserId != userId);

                if (existingUser != null)
                {
                    _logger.LogWarning("Windows identity {WindowsIdentity} is already linked to user ID {ExistingUserId}", 
                        windowsIdentity, existingUser.UserId);
                    return false;
                }

                user.WindowsIdentity = windowsIdentity;
                user.IsWindowsAuth = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully linked Windows identity {WindowsIdentity} to user ID {UserId}", 
                    windowsIdentity, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking Windows identity {WindowsIdentity} to user ID {UserId}", 
                    windowsIdentity, userId);
                throw new ApplicationException("An error occurred while linking Windows identity", ex);
            }
        }

        public async Task<User?> AuthenticateDirectQRAsync(string login, string validationToken)
        {
            try
            {
                _logger.LogInformation("Attempting direct QR authentication for user: {Login}", login);

                if (string.IsNullOrEmpty(login))
                {
                    _logger.LogWarning("Direct QR authentication attempt with empty login");
                    return null;
                }

                var user = await _context.Users
                    .Include(u => u.UserRoles!)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Login == login);

                if (user == null)
                {
                    _logger.LogWarning("Direct QR authentication failed: User not found for login: {Login}", login);
                    return null;
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Direct QR authentication failed: User {Login} is not active", login);
                    return null;
                }

                // Update last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {Login} authenticated successfully via direct QR", login);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while authenticating user via direct QR: {Login}", login);
                throw new Exception($"Direct QR authentication failed for user: {login}", ex);
            }
        }
    }
}
