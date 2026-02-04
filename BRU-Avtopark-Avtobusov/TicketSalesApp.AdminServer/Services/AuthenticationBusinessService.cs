using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Business logic service for authentication operations
    /// </summary>
    public class AuthenticationBusinessService : IAuthenticationBusinessService
    {
        private readonly TicketSalesApp.Services.Interfaces.IAuthenticationService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationBusinessService> _logger;
        private readonly AppDbContext _context;

        public AuthenticationBusinessService(
            TicketSalesApp.Services.Interfaces.IAuthenticationService authService,
            IConfiguration configuration,
            ILogger<AuthenticationBusinessService> logger,
            AppDbContext context)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task<(bool success, string token, string message)> AuthenticateUserAsync(string login, string password)
        {
            try
            {
                Log.Information("Authentication attempt started for user {Login}", login);

                var user = await _authService.AuthenticateAsync(login, password);
                if (user == null)
                {
                    Log.Warning("Failed authentication attempt for user {Login}: Invalid credentials", login);
                    return (false, null, "Invalid username or password");
                }

                Log.Debug("User {Login} successfully authenticated, generating JWT token", login);
                var token = GenerateJwtToken(user);

                Log.Information("Successful authentication for user {Login} with role {Role}", login, user.Role);
                return (true, token, "Authentication successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication for user {Login}", login);
                return (false, null, "An error occurred during authentication");
            }
        }

        public async Task<(bool success, User user, string message)> RegisterUserAsync(string login, string password, int role, string adminToken)
        {
            try
            {
                Log.Information("Starting user registration process for {Login}", login);

                // Validate admin token
                var (isValidToken, tokenMessage) = await ValidateAdminTokenAsync(adminToken);
                if (!isValidToken)
                {
                    Log.Warning("Invalid admin token provided for registration of user {Login}", login);
                    return (false, null, tokenMessage);
                }

                // Create user with new fields
                var user = new User
                {
                    Login = login,
                    Role = role,
                    PhoneNumber = "+375333000000",
                    Email = "placeholderemail@mogilev.by",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Attempt to register the user
                var success = await _authService.RegisterAsync(login, password, role);
                if (!success)
                {
                    Log.Warning("Registration failed for {Login}. User may already exist.", login);
                    return (false, null, "Registration failed - user may already exist");
                }

                // Get the created user to return in response
                var createdUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == login);

                if (createdUser != null)
                {
                    // Update the additional fields
                    createdUser.PhoneNumber = user.PhoneNumber;
                    createdUser.Email = user.Email;
                    await _context.SaveChangesAsync();
                }

                Log.Information("User {Login} successfully registered with role {Role}", login, role);
                return (true, createdUser, "User registered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {Login}", login);
                Log.Error(ex, "Registration failed for {Login}. Error: {ErrorMessage}", login, ex.Message);
                return (false, null, "Registration failed due to server error");
            }
        }

        public string GenerateJwtToken(User user)
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
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            Log.Information("Successfully generated JWT token for user {Login} with expiration at {Expiration}",
                user.Login, tokenDescriptor.Expires);

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<(bool isValid, string message)> ValidateAdminTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return (false, "No token provided");
                }

                var tokenHandler = new JwtSecurityTokenHandler();

                // First just read the token without validation to check the role
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "role");

                if (roleClaim?.Value != "1")
                {
                    Log.Warning("Unauthorized token validation attempt. Required role: 1, Provided role: {Role}", roleClaim?.Value ?? "none");
                    return (false, "Not authorized - admin role required");
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

                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                Log.Debug("Token validation successful for admin operation by {Username}",
                    principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value);

                return (true, "Token is valid");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Token validation failed");
                return (false, "Invalid token");
            }
        }
    }
}