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
    /// <summary>
    /// Legacy AuthController - provides backward compatibility by redirecting to new v1 controllers
    /// This controller maintains existing endpoints for backward compatibility while delegating
    /// actual functionality to the new focused controllers (AuthenticationController, WindowsAuthController, QRAuthController)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Obsolete("This controller is deprecated. Use the new v1 controllers: /api/v1/auth, /api/v1/auth/windows, /api/v1/auth/qr")]
    public class AuthController : ControllerBase
    {
        private readonly TicketSalesApp.Services.Interfaces.IAuthenticationService _authService;
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

        /// <summary>
        /// Redirect login POST requests to new v1 authentication controller
        /// </summary>
        [Route("login")]
        [HttpPost]
        [AllowAnonymous]
        public IActionResult LoginPost()
        {
            return RedirectPermanent("/api/v1/auth/login");
        }

        /// <summary>
        /// Redirect register POST requests to new v1 authentication controller
        /// </summary>
        [Route("register")]
        [HttpPost]
        [AllowAnonymous]
        public IActionResult RegisterPost()
        {
            return RedirectPermanent("/api/v1/auth/register");
        }

        /// <summary>
        /// Redirect Windows login requests to new v1 Windows auth controller
        /// </summary>
        [Route("windows-login")]
        [Authorize(AuthenticationSchemes = "Windows")]
        [HttpGet]
        public IActionResult WindowsLogin()
        {
            return RedirectPermanent("/api/v1/auth/windows/login");
        }

        /// <summary>
        /// Redirect Windows account linking requests to new v1 Windows auth controller
        /// </summary>
        [Route("link-windows-account")]
        [Authorize(AuthenticationSchemes = "Windows")]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult LinkWindowsAccount()
        {
            return RedirectPermanent("/api/v1/auth/windows/link");
        }

        /// <summary>
        /// Redirect Windows link completion requests to new v1 Windows auth controller
        /// </summary>
        [Route("complete-windows-link")]
        [Authorize(AuthenticationSchemes = "Windows")]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult CompleteWindowsLink()
        {
            return RedirectPermanent("/api/v1/auth/windows/complete-link");
        }

        /// <summary>
        /// Redirect Windows link decline requests to new v1 Windows auth controller
        /// </summary>
        [Route("decline-windows-link")]
        [Authorize(AuthenticationSchemes = "Windows")]
        [AllowAnonymous]
        [HttpPost]
        public IActionResult DeclineWindowsLink()
        {
            return RedirectPermanent("/api/v1/auth/windows/decline-link");
        }

        /// <summary>
        /// Redirect Windows unlink requests to new v1 Windows auth controller
        /// </summary>
        [Route("unlink-windows-account")]
        [Authorize]
        [HttpPost]
        public IActionResult UnlinkWindowsAccount()
        {
            return RedirectPermanent("/api/v1/auth/windows/unlink");
        }

        /// <summary>
        /// Redirect Windows link status check to new v1 Windows auth controller
        /// </summary>
        [Route("check-windows-link-status")]
        [Authorize]
        [HttpGet]
        public IActionResult CheckWindowsLinkStatus()
        {
            return RedirectPermanent("/api/v1/auth/windows/link-status");
        }

        /// <summary>
        /// Redirect QR generation requests to new v1 QR auth controller
        /// </summary>
        [Route("qr/generate")]
        [HttpGet]
        [Authorize]
        public IActionResult GenerateQR()
        {
            return RedirectPermanent("/api/v1/auth/qr/generate");
        }

        /// <summary>
        /// Redirect QR login requests to new v1 QR auth controller
        /// </summary>
        [Route("qr/login")]
        [HttpPost]
        [AllowAnonymous]
        public IActionResult QRLogin()
        {
            return RedirectPermanent("/api/v1/auth/qr/login");
        }

        /// <summary>
        /// Redirect direct QR generation requests to new v1 QR auth controller
        /// </summary>
        [Route("qr/direct/generate")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GenerateDirectQR()
        {
            return RedirectPermanent("/api/v1/auth/qr/direct/generate");
        }

        /// <summary>
        /// Redirect direct QR login requests to new v1 QR auth controller
        /// </summary>
        [Route("qr/direct/login")]
        [HttpPost]
        [AllowAnonymous]
        public IActionResult DirectQRLogin()
        {
            return RedirectPermanent("/api/v1/auth/qr/direct/login");
        }

        /// <summary>
        /// Redirect direct QR check requests to new v1 QR auth controller
        /// </summary>
        [Route("qr/direct/check")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult CheckDirectQR()
        {
            return RedirectPermanent("/api/v1/auth/qr/direct/check");
        }

        // Legacy GET endpoints for login/register forms (if needed for backward compatibility)
        [Route("login")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult LoginGet()
        {
            if (_environment.IsDevelopment())
            {
                // Redirect to development controller for debug pages
                return RedirectPermanent("/api/dev/auth/login");
            }
            
            return Ok(new { message = "Please use POST method for login. This endpoint has been moved to /api/v1/auth/login" });
        }

        [Route("register")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegisterGet()
        {
            if (_environment.IsDevelopment())
            {
                // Redirect to development controller for debug pages
                return RedirectPermanent("/api/dev/auth/register");
            }
            
            return Ok(new { message = "Please use POST method for registration. This endpoint has been moved to /api/v1/auth/register" });
        }
    }
}