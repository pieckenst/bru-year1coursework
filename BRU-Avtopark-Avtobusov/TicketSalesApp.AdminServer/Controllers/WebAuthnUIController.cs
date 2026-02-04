using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketSalesApp.AdminServer.Controllers
{
    /// <summary>
    /// Controller for serving WebAuthn management UI
    /// </summary>
    [Route("webauthn")]
    public class WebAuthnUIController : Controller
    {
        private readonly ILogger<WebAuthnUIController> _logger;
        private readonly IWebHostEnvironment _environment;

        public WebAuthnUIController(ILogger<WebAuthnUIController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Serve the WebAuthn management page
        /// </summary>
        /// <returns>WebAuthn management HTML page</returns>
        [HttpGet("manage")]
        [AllowAnonymous]
        public IActionResult Manage()
        {
            try
            {
                var filePath = Path.Combine(_environment.ContentRootPath, "Views", "WebAuthn", "manage.html");
                
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogError("WebAuthn management page not found at {FilePath}", filePath);
                    return NotFound("WebAuthn management page not found");
                }

                var content = System.IO.File.ReadAllText(filePath);
                return Content(content, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving WebAuthn management page");
                return StatusCode(500, "Error loading WebAuthn management page");
            }
        }

        /// <summary>
        /// Get WebAuthn configuration for client-side JavaScript
        /// </summary>
        /// <returns>WebAuthn configuration</returns>
        [HttpGet("config")]
        [AllowAnonymous]
        public IActionResult GetConfig()
        {
            try
            {
                var config = new
                {
                    serverDomain = Request.Host.Host,
                    serverName = "TicketSales Admin Server",
                    origins = new[] { $"{Request.Scheme}://{Request.Host}" },
                    apiBaseUrl = "/api/v1/auth/webauthn",
                    supportedAlgorithms = new[] { -7, -35, -36, -257, -258, -259 }, // ES256, ES384, ES512, RS256, RS384, RS512
                    userVerification = "preferred",
                    authenticatorAttachment = "cross-platform", // Allow both platform and cross-platform authenticators
                    requireResidentKey = false,
                    timeout = 60000 // 60 seconds
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting WebAuthn configuration");
                return StatusCode(500, new { error = "Error getting WebAuthn configuration" });
            }
        }

        /// <summary>
        /// Health check endpoint for WebAuthn functionality
        /// </summary>
        /// <returns>WebAuthn service health status</returns>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            try
            {
                var health = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    webAuthnSupported = true,
                    serverDomain = Request.Host.Host,
                    environment = _environment.EnvironmentName,
                    endpoints = new
                    {
                        register = "/api/v1/auth/webauthn/register/begin",
                        login = "/api/v1/auth/webauthn/login/begin",
                        credentials = "/api/v1/auth/webauthn/credentials",
                        management = "/webauthn/manage"
                    }
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking WebAuthn health");
                return StatusCode(500, new { status = "unhealthy", error = ex.Message });
            }
        }
    }
}