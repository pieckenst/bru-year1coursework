using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.AspNetCore.Authentication;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace TicketSalesApp.AdminServer.Controllers.v1
{
    /// <summary>
    /// Handles Windows Authentication and account linking operations
    /// </summary>
    [ApiController]
    [Route("api/v1/auth/windows")]
    public class WindowsAuthController : ControllerBase
    {
        private readonly TicketSalesApp.AdminServer.Services.Interfaces.IWindowsAuthBusinessService _windowsAuthBusinessService;
        private readonly ILogger<WindowsAuthController> _logger;

        public WindowsAuthController(
            TicketSalesApp.AdminServer.Services.Interfaces.IWindowsAuthBusinessService windowsAuthBusinessService,
            ILogger<WindowsAuthController> logger)
        {
            _windowsAuthBusinessService = windowsAuthBusinessService;
            _logger = logger;
        }

        /// <summary>
        /// Windows authentication login endpoint
        /// </summary>
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
                    return Challenge(new AuthenticationProperties(),
                        NegotiateDefaults.AuthenticationScheme);
                }

                var windowsUsername = wi.Name;
                Console.WriteLine($"[WindowsLogin] Windows user authenticated: {windowsUsername}");

                // Use business service for authentication
                var (success, token, user, message) = await _windowsAuthBusinessService.AuthenticateWindowsUserAsync(windowsUsername);

                if (!success)
                {
                    Console.WriteLine($"[WindowsLogin] Authentication failed: {message}");
                    
                    // Check if it's a security issue (blank password)
                    if (message.Contains("not securely configured"))
                    {
                        return StatusCode(418, new
                        {
                            message = message,
                            codedtest = 418,
                            secondaryText = "I'm a teapot—not really, but your account looks like one."
                        });
                    }
                    
                    return BadRequest(new { message });
                }

                Console.WriteLine($"[WindowsLogin] Authentication successful for user: {user.Login}");
                
                // Extract Windows authentication protocol information
                var authInfo = new
                {
                    AuthenticationType = wi.AuthenticationType,
                    IsAuthenticated = wi.IsAuthenticated,
                    IsGuest = wi.IsGuest,
                    IsSystem = wi.IsSystem,
                    IsAnonymous = wi.IsAnonymous,
                    UserSid = wi.User?.ToString(),
                    OwnerSid = wi.Owner?.ToString(),
                    ImpersonationLevel = wi.ImpersonationLevel.ToString(),
                    Token = wi.Token.ToString(),
                    Groups = wi.Groups?.Select(g => g.ToString()).ToArray()
                };

                // Extract HTTP authentication headers
                var authHeaders = new
                {
                    Authorization = HttpContext.Request.Headers["Authorization"].ToString(),
                    WwwAuthenticate = HttpContext.Request.Headers["Www-Authenticate"].ToString()
                };

                // Extract authentication flow information from custom handler
                TicketSalesApp.AdminServer.Authentication.WindowsAuthFlowInfo flowInfo = null;
                if (HttpContext.Items.TryGetValue("WindowsAuthFlow", out var flowInfoObj) && flowInfoObj is TicketSalesApp.AdminServer.Authentication.WindowsAuthFlowInfo)
                {
                    flowInfo = (TicketSalesApp.AdminServer.Authentication.WindowsAuthFlowInfo)flowInfoObj;
                    Console.WriteLine($"[WindowsLogin] Auth flow info: Protocol={flowInfo.Protocol}, MessageType={flowInfo.MessageType}");
                }

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
                    },
                    windowsAuth = new
                    {
                        protocol = authInfo.AuthenticationType,
                        isNtlm = authInfo.AuthenticationType?.Equals("NTLM", StringComparison.OrdinalIgnoreCase) ?? false,
                        isNegotiate = authInfo.AuthenticationType?.Equals("Negotiate", StringComparison.OrdinalIgnoreCase) ?? false,
                        isKerberos = authInfo.AuthenticationType?.Equals("Kerberos", StringComparison.OrdinalIgnoreCase) ?? false,
                        authenticationDetails = authInfo,
                        httpHeaders = authHeaders,
                        authenticationFlow = flowInfo != null ? new
                        {
                            protocol = flowInfo.Protocol,
                            messageType = flowInfo.MessageType,
                            token = flowInfo.Token,
                            timestamp = flowInfo.Timestamp,
                            authenticationSucceeded = flowInfo.AuthenticationSucceeded,
                            authenticatedUser = flowInfo.AuthenticatedUser,
                            authenticationType = flowInfo.AuthenticationType
                        } : null
                    }
                };

                Console.WriteLine($"[WindowsLogin] Returning successful authentication response with protocol info: {authInfo.AuthenticationType}");
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

        /// <summary>
        /// Check Windows account link status
        /// </summary>
        [Route("check-windows-link-status")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckWindowsLinkStatus()
        {
            try
            {
                var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var (success, isLinked, windowsIdentity, needsLinking, message) = await _windowsAuthBusinessService.CheckLinkStatusAsync(userId);

                if (!success)
                {
                    return NotFound(new { message });
                }

                return Ok(new
                {
                    isLinked,
                    windowsIdentity,
                    needsLinking
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Windows account link status");
                return StatusCode(500, new { message = "An error occurred while checking Windows account link status" });
            }
        }

        /// <summary>
        /// Initiate Windows account linking
        /// </summary>
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

                string regularUsername = model?.Username;
                if (string.IsNullOrEmpty(regularUsername))
                {
                    // Try to get from JWT if authenticated
                    if (long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                    {
                        var (success, _, _, _, message) = await _windowsAuthBusinessService.CheckLinkStatusAsync(userId);
                        if (!success)
                        {
                            return NotFound(new { message });
                        }
                        // We would need to get the username from the user ID, but let's require it in the model for now
                        return BadRequest(new { message = "Username is required" });
                    }
                    else
                    {
                        return BadRequest(new { message = "Username is required" });
                    }
                }

                var (linkSuccess, verificationToken, linkMessage) = await _windowsAuthBusinessService.InitiateAccountLinkingAsync(windowsUsername, regularUsername);

                if (!linkSuccess)
                {
                    return BadRequest(new { message = linkMessage });
                }

                return Ok(new
                {
                    verificationToken,
                    message = linkMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Windows account linking");
                return StatusCode(500, new { message = "An error occurred while initiating Windows account linking" });
            }
        }

        /// <summary>
        /// Complete Windows account linking
        /// </summary>
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

                var (success, message) = await _windowsAuthBusinessService.CompleteAccountLinkingAsync(windowsUsername, model.Username, model.Token);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing Windows account linking");
                return StatusCode(500, new { message = "An error occurred while completing Windows account linking" });
            }
        }

        /// <summary>
        /// Decline Windows account linking
        /// </summary>
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

                var (success, message) = await _windowsAuthBusinessService.DeclineAccountLinkingAsync(windowsUsername);

                if (!success)
                {
                    return NotFound(new { message });
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Windows account linking decline");
                return StatusCode(500, new { message = "An error occurred while processing your request" });
            }
        }

        /// <summary>
        /// Unlink Windows account
        /// </summary>
        [Route("unlink-windows-account")]
        [Authorize] // This will be called with JWT from the regular account
        [HttpPost]
        public async Task<IActionResult> UnlinkWindowsAccount()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usernameClaim = User.FindFirst(ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(usernameClaim))
                {
                    return BadRequest(new { message = "Unable to determine username from token" });
                }

                var (success, message) = await _windowsAuthBusinessService.UnlinkAccountAsync(usernameClaim);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlinking Windows account");
                return StatusCode(500, new { message = "An error occurred while unlinking Windows account" });
            }
        }
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