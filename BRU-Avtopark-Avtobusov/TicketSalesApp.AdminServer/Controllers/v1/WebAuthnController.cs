using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;

namespace TicketSalesApp.AdminServer.Controllers.v1
{
    /// <summary>
    /// Controller for WebAuthn (FIDO2) authentication operations
    /// </summary>
    [ApiController]
    [Route("api/v1/auth/webauthn")]
    [Produces("application/json")]
    public class WebAuthnController : ControllerBase
    {
        private readonly IWebAuthnService _webAuthnService;
        private readonly IAuthenticationBusinessService _authService;
        private readonly ILogger<WebAuthnController> _logger;
        private readonly AppDbContext _context;

        public WebAuthnController(
            IWebAuthnService webAuthnService,
            IAuthenticationBusinessService authService,
            ILogger<WebAuthnController> logger,
            AppDbContext context)
        {
            _webAuthnService = webAuthnService;
            _authService = authService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Begin WebAuthn credential registration
        /// </summary>
        /// <param name="request">Registration request containing display name</param>
        /// <returns>Credential creation options for the client</returns>
        [HttpPost("register/begin")]
        [Authorize]
        [ProducesResponseType(typeof(CredentialCreateOptions), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> BeginRegistration([FromBody] BeginRegistrationRequest request)
        {
            try
            {
                _logger.LogInformation("WebAuthn registration attempt started");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usernameClaim = User.FindFirst(ClaimTypes.Name)?.Value;

                _logger.LogInformation("User claims - ID: {UserId}, Name: {Username}", userIdClaim, usernameClaim);

                if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(usernameClaim))
                {
                    _logger.LogWarning("Invalid user claims - ID: {UserId}, Name: {Username}", userIdClaim, usernameClaim);
                    return Unauthorized(new { error = "Invalid user claims" });
                }

                if (!long.TryParse(userIdClaim, out var userIdLong))
                {
                    _logger.LogWarning("Invalid user ID format: {UserId}", userIdClaim);
                    return BadRequest(new { error = "Invalid user ID format" });
                }

                _logger.LogInformation("Looking up user with ID: {UserId}", userIdLong);

                // Get the user to retrieve the GuidId
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userIdLong);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {UserId}", userIdLong);
                    return BadRequest(new { error = "User not found" });
                }

                _logger.LogInformation("Found user: {Login} with GuidId: {GuidId}", user.Login, user.GuidId);

                var displayName = string.IsNullOrEmpty(request.DisplayName) ? usernameClaim : request.DisplayName;
                
                _logger.LogInformation("Calling WebAuthn service with GuidId: {GuidId}, Username: {Username}, DisplayName: {DisplayName}", 
                    user.GuidId, usernameClaim, displayName);
                
                var options = await _webAuthnService.BeginRegistrationAsync(user.GuidId, usernameClaim, displayName);

                _logger.LogInformation("WebAuthn registration options generated successfully");
                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error beginning WebAuthn registration");
                return StatusCode(500, new { error = "An error occurred during registration setup", details = ex.Message });
            }
        }

        /// <summary>
        /// Complete WebAuthn credential registration
        /// </summary>
        /// <param name="request">Registration completion request</param>
        /// <returns>Registration result</returns>
        [HttpPost("register/complete")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { error = "Invalid user claims" });
                }

                if (!long.TryParse(userIdClaim, out var userIdLong))
                {
                    return BadRequest(new { error = "Invalid user ID format" });
                }

                // Get the user to retrieve the GuidId
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userIdLong);
                if (user == null)
                {
                    return BadRequest(new { error = "User not found" });
                }

                var (success, message) = await _webAuthnService.CompleteRegistrationAsync(
                    user.GuidId, 
                    request.Response, 
                    request.FriendlyName);

                if (success)
                {
                    return Ok(new { success = true, message });
                }

                return BadRequest(new { success = false, error = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing WebAuthn registration");
                return StatusCode(500, new { error = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Begin WebAuthn authentication
        /// </summary>
        /// <param name="request">Login request containing username</param>
        /// <returns>Assertion options for the client</returns>
        [HttpPost("login/begin")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AssertionOptions), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> BeginLogin([FromBody] BeginLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Username))
                {
                    return BadRequest(new { error = "Username is required" });
                }

                var options = await _webAuthnService.BeginLoginAsync(request.Username);
                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error beginning WebAuthn login for username {Username}", request.Username);
                return StatusCode(500, new { error = "An error occurred during login setup" });
            }
        }

        /// <summary>
        /// Complete WebAuthn authentication
        /// </summary>
        /// <param name="request">Login completion request</param>
        /// <returns>Authentication result with JWT token</returns>
        [HttpPost("login/complete")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> CompleteLogin([FromBody] CompleteLoginRequest request)
        {
            try
            {
                var (success, user, message) = await _webAuthnService.CompleteLoginAsync(request.Response);

                if (success && user != null)
                {
                    // Generate JWT token using existing authentication service
                    var token = _authService.GenerateJwtToken(user);

                    return Ok(new
                    {
                        success = true,
                        message = "Authentication successful",
                        token,
                        user = new
                        {
                            user.UserId,
                            user.GuidId,
                            user.Login,
                            user.Email,
                            user.Role,
                            user.LastLoginAt
                        }
                    });
                }

                return BadRequest(new { success = false, error = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing WebAuthn login");
                return StatusCode(500, new { error = "An error occurred during authentication" });
            }
        }

        /// <summary>
        /// Get user's WebAuthn credentials
        /// </summary>
        /// <returns>List of user's WebAuthn credentials</returns>
        [HttpGet("credentials")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetCredentials()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { error = "Invalid user claims" });
                }

                if (!long.TryParse(userIdClaim, out var userIdLong))
                {
                    return BadRequest(new { error = "Invalid user ID format" });
                }

                // Get the user to retrieve the GuidId
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userIdLong);
                if (user == null)
                {
                    return BadRequest(new { error = "User not found" });
                }

                var credentials = await _webAuthnService.GetUserCredentialsAsync(user.GuidId);

                var result = credentials.Select(c => new
                {
                    c.Id,
                    c.FriendlyName,
                    c.RegisteredAt,
                    c.LastUsedAt,
                    c.IsActive,
                    CredentialId = Convert.ToBase64String(c.CredentialId)
                });

                return Ok(new { credentials = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving WebAuthn credentials");
                return StatusCode(500, new { error = "An error occurred while retrieving credentials" });
            }
        }

        /// <summary>
        /// Delete a WebAuthn credential
        /// </summary>
        /// <param name="credentialId">The credential ID to delete</param>
        /// <returns>Deletion result</returns>
        [HttpDelete("credentials/{credentialId}")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCredential(long credentialId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { error = "Invalid user claims" });
                }

                if (!long.TryParse(userIdClaim, out var userIdLong))
                {
                    return BadRequest(new { error = "Invalid user ID format" });
                }

                // Get the user to retrieve the GuidId
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userIdLong);
                if (user == null)
                {
                    return BadRequest(new { error = "User not found" });
                }

                var (success, message) = await _webAuthnService.DeleteCredentialAsync(credentialId, user.GuidId);

                if (success)
                {
                    return Ok(new { success = true, message });
                }

                return NotFound(new { success = false, error = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting WebAuthn credential {CredentialId}", credentialId);
                return StatusCode(500, new { error = "An error occurred while deleting the credential" });
            }
        }

        /// <summary>
        /// Update a WebAuthn credential's friendly name
        /// </summary>
        /// <param name="credentialId">The credential ID to update</param>
        /// <param name="request">Update request containing new friendly name</param>
        /// <returns>Update result</returns>
        [HttpPut("credentials/{credentialId}")]
        [Authorize]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateCredential(long credentialId, [FromBody] UpdateCredentialRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { error = "Invalid user claims" });
                }

                if (!long.TryParse(userIdClaim, out var userIdLong))
                {
                    return BadRequest(new { error = "Invalid user ID format" });
                }

                if (string.IsNullOrEmpty(request.FriendlyName))
                {
                    return BadRequest(new { error = "Friendly name is required" });
                }

                // Get the user to retrieve the GuidId
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userIdLong);
                if (user == null)
                {
                    return BadRequest(new { error = "User not found" });
                }

                var (success, message) = await _webAuthnService.UpdateCredentialNameAsync(
                    credentialId, 
                    user.GuidId, 
                    request.FriendlyName);

                if (success)
                {
                    return Ok(new { success = true, message });
                }

                return NotFound(new { success = false, error = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating WebAuthn credential {CredentialId}", credentialId);
                return StatusCode(500, new { error = "An error occurred while updating the credential" });
            }
        }
    }

    // Request/Response DTOs
    public class BeginRegistrationRequest
    {
        public string? DisplayName { get; set; }
    }

    public class CompleteRegistrationRequest
    {
        public string Response { get; set; } = string.Empty; // JSON string from client
        public string? FriendlyName { get; set; }
    }

    public class BeginLoginRequest
    {
        public string Username { get; set; } = string.Empty;
    }

    public class CompleteLoginRequest
    {
        public string Response { get; set; } = string.Empty; // JSON string from client
    }

    public class UpdateCredentialRequest
    {
        public string FriendlyName { get; set; } = string.Empty;
    }
}