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
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace TicketSalesApp.AdminServer.Controllers.v1
{
    /// <summary>
    /// Handles basic authentication operations (login, register, JWT token management)
    /// </summary>
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly TicketSalesApp.AdminServer.Services.Interfaces.IAuthenticationBusinessService _authBusinessService;
        private readonly ILogger<AuthenticationController> _logger;
        private readonly AppDbContext _context;

        public AuthenticationController(
            TicketSalesApp.AdminServer.Services.Interfaces.IAuthenticationBusinessService authBusinessService,
            ILogger<AuthenticationController> logger,
            AppDbContext context)
        {
            _authBusinessService = authBusinessService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        [Route("login")]
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<string>> Login([FromBody] LoginModel model)
        {
            Log.Information("Login attempt started for user {Login} from IP {RemoteIP}", 
                model.Login, HttpContext.Connection.RemoteIpAddress);

            // Server-side validation
            if (!ModelState.IsValid)
            {
                Log.Warning("Invalid model state for login request: {ValidationErrors}",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(new { message = "Invalid request data" });
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(model.Login) || string.IsNullOrWhiteSpace(model.Password))
            {
                Log.Warning("Login attempt with empty credentials from IP {RemoteIP}", 
                    HttpContext.Connection.RemoteIpAddress);
                return BadRequest(new { message = "Username and password are required" });
            }

            //if (model.Login.Length < 3)
            //{
                //Log.Warning("Login attempt with username too short: {Login}", model.Login);
                //return BadRequest(new { message = "Username must be at least 3 characters" });
            //}

            //if (model.Password.Length < 6)
            //{
                //Log.Warning("Login attempt with password too short for user {Login}", model.Login);
                //return BadRequest(new { message = "Password must be at least 6 characters" });
            //}

            // Check for SQL injection attempts or suspicious patterns
            if (model.Login.Contains("'") || model.Login.Contains("--") || 
                model.Login.Contains(";") || model.Login.Contains("/*"))
            {
                Log.Warning("Suspicious login attempt detected for user {Login} from IP {RemoteIP}", 
                    model.Login, HttpContext.Connection.RemoteIpAddress);
                return BadRequest(new { message = "Invalid characters in username" });
            }

            var (success, token, message) = await _authBusinessService.AuthenticateUserAsync(model.Login, model.Password);
            if (!success)
            {
                Log.Warning("Failed login attempt for user {Login} from IP {RemoteIP}: {Message}", 
                    model.Login, HttpContext.Connection.RemoteIpAddress, message);
                return Unauthorized(new { message = "Invalid username or password" });
            }

            Log.Information("Successful login for user {Login} from IP {RemoteIP}", 
                model.Login, HttpContext.Connection.RemoteIpAddress);
            return Ok(new { token });
        }

        /// <summary>
        /// Validate JWT token and return user information
        /// </summary>
        [Route("validate")]
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<object>> ValidateToken()
        {
            try
            {
                Log.Information("Token validation requested for user {UserName}", User.Identity?.Name);

                // Get user information from claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usernameClaim = User.FindFirst(ClaimTypes.Name)?.Value;
                var roleClaim = User.FindFirst("role")?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(usernameClaim))
                {
                    Log.Warning("Invalid token claims - missing user identifier or name");
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Get full user details from database
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == long.Parse(userIdClaim));
                if (user == null)
                {
                    Log.Warning("User {UserId} not found in database", userIdClaim);
                    return Unauthorized(new { message = "User not found" });
                }

                Log.Information("Token validated successfully for user {Login}", user.Login);

                return Ok(new
                {
                    isAuthenticated = true,
                    user = new
                    {
                        userId = user.UserId,
                        login = user.Login,
                        username = user.Login,
                        role = user.Role,
                        email = user.Email,
                        phoneNumber = user.PhoneNumber,
                        isWindowsAuth = user.IsWindowsAuth,
                        createdAt = user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                Log.Error(ex, "Token validation failed. Error: {ErrorMessage}", ex.Message);
                return Unauthorized(new { message = "Token validation failed" });
            }
        }

        /// <summary>
        /// Check if Windows authentication is available on the server
        /// </summary>
        [Route("windows-available")]
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<object> CheckWindowsAuthAvailability()
        {
            try
            {
                // Check if running on Windows
                bool isWindows = OperatingSystem.IsWindows();
                
                // Check if Windows authentication is configured
                bool isConfigured = HttpContext.User.Identity?.AuthenticationType == "Negotiate" ||
                                   HttpContext.User.Identity?.AuthenticationType == "NTLM" ||
                                   HttpContext.User.Identity?.AuthenticationType == "Windows";

                // Get client platform from User-Agent
                var userAgent = Request.Headers.UserAgent.ToString().ToLower();
                bool clientIsWindows = userAgent.Contains("windows") || userAgent.Contains("win64") || userAgent.Contains("win32");

                Log.Information("Windows auth availability check - Server OS: {IsWindows}, Configured: {IsConfigured}, Client: {ClientIsWindows}",
                    isWindows, isConfigured, clientIsWindows);

                return Ok(new
                {
                    serverSupportsWindows = isWindows,
                    windowsAuthConfigured = isConfigured,
                    clientIsWindows = clientIsWindows,
                    available = isWindows && clientIsWindows,
                    message = isWindows && clientIsWindows 
                        ? "Windows authentication is available" 
                        : "Windows authentication is not available on this platform"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Windows authentication availability");
                Log.Error(ex, "Error checking Windows authentication availability");
                return Ok(new
                {
                    serverSupportsWindows = false,
                    windowsAuthConfigured = false,
                    clientIsWindows = false,
                    available = false,
                    message = "Unable to determine Windows authentication availability"
                });
            }
        }

        /// <summary>
        /// Register a new user (requires admin token)
        /// </summary>
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

                // Use business service to register user
                var (success, user, message) = await _authBusinessService.RegisterUserAsync(model.Login, model.Password, model.Role, token);
                if (!success)
                {
                    Log.Warning("Registration failed for {Login}: {Message}", model.Login, message);
                    return BadRequest(new
                    {
                        success = false,
                        message,
                        details = new
                        {
                            error = message,
                            attemptedUser = new { model.Login, role = model.Role }
                        }
                    });
                }

                Log.Information("User {Login} successfully registered with role {Role}", model.Login, model.Role);

                return Ok(new
                {
                    success = true,
                    message = "User registered successfully",
                    details = new
                    {
                        user = new
                        {
                            user.UserId,
                            user.Login,
                            user.Role,
                            user.PhoneNumber,
                            user.Email,
                            user.CreatedAt
                        }
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
                        time = DateTime.UtcNow,
                        requestData = new
                        {
                            model.Login,
                            role = model.Role
                        }
                    }
                });
            }
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Username is required")]
        
        [RegularExpression(@"^[a-zA-Z0-9_\-\.@]+$", ErrorMessage = "Username contains invalid characters")]
        public required string Login { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        
        public required string Password { get; set; }
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Username is required")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_\-\.@]+$", ErrorMessage = "Username contains invalid characters")]
        public required string Login { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
        public required string Password { get; set; }
        
        [Range(1, 4, ErrorMessage = "Role must be between 1 and 4")]
        public int Role { get; set; }
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        [MaxLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string? PhoneNumber { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }
    }
}