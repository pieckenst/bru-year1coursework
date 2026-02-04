using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TicketSalesApp.AdminServer.Services.Interfaces;

namespace TicketSalesApp.AdminServer.Controllers.v1
{
    [ApiController]
    [Route("api/v1/auth/2fa")]
    [Authorize]
    public class TwoFactorController : ControllerBase
    {
        private readonly ITotpService _totpService;
        private readonly ILogger<TwoFactorController> _logger;

        public TwoFactorController(ITotpService totpService, ILogger<TwoFactorController> logger)
        {
            _totpService = totpService;
            _logger = logger;
        }

        /// <summary>
        /// Get TOTP status for the current user
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetTotpStatus()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var isEnabled = await _totpService.IsTotpEnabledAsync(userId.Value);

                return Ok(new
                {
                    isEnabled = isEnabled,
                    userId = userId.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TOTP status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Generate TOTP setup information (QR code, secret key, etc.)
        /// </summary>
        [HttpPost("setup")]
        public async Task<IActionResult> SetupTotp()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if TOTP is already enabled
                var isEnabled = await _totpService.IsTotpEnabledAsync(userId.Value);
                if (isEnabled)
                {
                    return BadRequest(new { error = "TOTP is already enabled for this user" });
                }

                var setupResult = await _totpService.GenerateSetupAsync(userId.Value);

                return Ok(new
                {
                    secretKey = setupResult.SecretKey,
                    qrCodeDataUrl = setupResult.QrCodeDataUrl,
                    manualEntryKey = setupResult.ManualEntryKey,
                    username = setupResult.Username,
                    issuer = setupResult.Issuer,
                    instructions = new[]
                    {
                        "1. Install an authenticator app (Google Authenticator, Authy, Microsoft Authenticator, etc.)",
                        "2. Scan the QR code or manually enter the secret key",
                        "3. Enter the 6-digit code from your authenticator app to verify setup",
                        "4. Save your recovery codes in a secure location"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up TOTP");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Enable TOTP after verifying the setup code
        /// </summary>
        [HttpPost("enable")]
        public async Task<IActionResult> EnableTotp([FromBody] EnableTotpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var success = await _totpService.EnableTotpAsync(userId.Value, request.VerificationCode);
                if (!success)
                {
                    return BadRequest(new { error = "Invalid verification code or TOTP setup not found" });
                }

                // Generate recovery codes
                var recoveryCodes = await _totpService.GenerateRecoveryCodesAsync(userId.Value);

                return Ok(new
                {
                    message = "TOTP enabled successfully",
                    recoveryCodes = recoveryCodes.ToArray(),
                    warning = "Save these recovery codes in a secure location. They can be used to access your account if you lose your authenticator device."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling TOTP");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Disable TOTP using verification code or recovery code
        /// </summary>
        [HttpPost("disable")]
        public async Task<IActionResult> DisableTotp([FromBody] DisableTotpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var success = await _totpService.DisableTotpAsync(userId.Value, request.VerificationCode);
                if (!success)
                {
                    return BadRequest(new { error = "Invalid verification code or recovery code" });
                }

                return Ok(new { message = "TOTP disabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling TOTP");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate a TOTP code
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateTotp([FromBody] ValidateTotpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var isValid = await _totpService.ValidateCodeAsync(userId.Value, request.Code);

                return Ok(new
                {
                    isValid = isValid,
                    message = isValid ? "Code is valid" : "Code is invalid"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating TOTP code");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Generate new recovery codes (requires TOTP code verification)
        /// </summary>
        [HttpPost("recovery-codes")]
        public async Task<IActionResult> GenerateRecoveryCodes([FromBody] ValidateTotpRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Verify TOTP code before generating new recovery codes
                var isValid = await _totpService.ValidateCodeAsync(userId.Value, request.Code);
                if (!isValid)
                {
                    return BadRequest(new { error = "Invalid TOTP code" });
                }

                var recoveryCodes = await _totpService.GenerateRecoveryCodesAsync(userId.Value);

                return Ok(new
                {
                    recoveryCodes = recoveryCodes.ToArray(),
                    message = "New recovery codes generated successfully",
                    warning = "These new recovery codes replace your previous ones. Save them in a secure location."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recovery codes");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Validate a recovery code
        /// </summary>
        [HttpPost("validate-recovery")]
        public async Task<IActionResult> ValidateRecoveryCode([FromBody] ValidateRecoveryCodeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var isValid = await _totpService.ValidateRecoveryCodeAsync(userId.Value, request.RecoveryCode);

                return Ok(new
                {
                    isValid = isValid,
                    message = isValid ? "Recovery code is valid and has been consumed" : "Recovery code is invalid or already used"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating recovery code");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return null;
        }
    }

    // Request/Response DTOs
    public class EnableTotpRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class DisableTotpRequest
    {
        [Required]
        [StringLength(8, MinimumLength = 6)]
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class ValidateTotpRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        public string Code { get; set; } = string.Empty;
    }

    public class ValidateRecoveryCodeRequest
    {
        [Required]
        [StringLength(8, MinimumLength = 8)]
        public string RecoveryCode { get; set; } = string.Empty;
    }
}