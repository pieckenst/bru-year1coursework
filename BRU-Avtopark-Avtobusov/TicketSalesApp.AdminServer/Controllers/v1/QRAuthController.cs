using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.Core.Data;
using Serilog;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;

namespace TicketSalesApp.AdminServer.Controllers.v1
{
    /// <summary>
    /// Handles QR Code authentication operations
    /// </summary>
    [ApiController]
    [Route("api/v1/auth/qr")]
    public class QRAuthController : ControllerBase
    {
        private readonly IQRAuthenticationService _qrAuthService;
        private readonly TicketSalesApp.Services.Interfaces.IAuthenticationService _authService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<QRAuthController> _logger;
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public QRAuthController(
            IQRAuthenticationService qrAuthService,
            TicketSalesApp.Services.Interfaces.IAuthenticationService authService,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            ILogger<QRAuthController> logger,
            AppDbContext context,
            IMemoryCache cache)
        {
            _qrAuthService = qrAuthService;
            _authService = authService;
            _configuration = configuration;
            _environment = environment;
            _logger = logger;
            _context = context;
            _cache = cache;
        }

        /// <summary>
        /// Generate QR code for authenticated user
        /// </summary>
        [Route("generate")]
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

        /// <summary>
        /// Login using QR code token
        /// </summary>
        [Route("login")]
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

        /// <summary>
        /// Generate direct login QR code (no prior authentication required)
        /// </summary>
        [Route("direct/generate")]
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

        /// <summary>
        /// Login using direct QR code token
        /// </summary>
        [Route("direct/login")]
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

        /// <summary>
        /// Check direct login status for desktop polling
        /// </summary>
        [Route("direct/check")]
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

        /// <summary>
        /// Generate JWT token for QR authenticated user
        /// </summary>
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

    public class QRLoginModel
    {
        public required string Token { get; set; }
    }

    public class DirectQRLoginModel
    {
        public required string Token { get; set; }
        public string? DeviceId { get; set; }
        public bool IsDesktopLogin { get; set; }
        public required string DeviceType { get; set; }
    }
}