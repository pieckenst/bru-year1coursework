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

        public AuthenticationController(
            TicketSalesApp.AdminServer.Services.Interfaces.IAuthenticationBusinessService authBusinessService,
            ILogger<AuthenticationController> logger)
        {
            _authBusinessService = authBusinessService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
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

            var (success, token, message) = await _authBusinessService.AuthenticateUserAsync(model.Login, model.Password);
            if (!success)
            {
                Log.Warning("Failed login attempt for user {Login}: {Message}", model.Login, message);
                return Unauthorized(new { message });
            }

            Log.Information("Successful login for user {Login}", model.Login);
            return Ok(new { token });
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
}