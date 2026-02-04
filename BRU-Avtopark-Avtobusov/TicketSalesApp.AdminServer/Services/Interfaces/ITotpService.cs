using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TicketSalesApp.AdminServer.Services.Interfaces
{
    public interface ITotpService
    {
        /// <summary>
        /// Generates TOTP setup information for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>TOTP setup result with secret, QR code, and manual entry key</returns>
        Task<TotpSetupResult> GenerateSetupAsync(long userId);

        /// <summary>
        /// Validates a TOTP code for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="code">6-digit TOTP code</param>
        /// <returns>True if code is valid</returns>
        Task<bool> ValidateCodeAsync(long userId, string code);

        /// <summary>
        /// Enables TOTP for a user after verifying the setup code
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="verificationCode">6-digit verification code</param>
        /// <returns>True if TOTP was successfully enabled</returns>
        Task<bool> EnableTotpAsync(long userId, string verificationCode);

        /// <summary>
        /// Disables TOTP for a user after verifying a code
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="verificationCode">6-digit verification code or recovery code</param>
        /// <returns>True if TOTP was successfully disabled</returns>
        Task<bool> DisableTotpAsync(long userId, string verificationCode);

        /// <summary>
        /// Generates new recovery codes for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of recovery codes</returns>
        Task<IEnumerable<string>> GenerateRecoveryCodesAsync(long userId);

        /// <summary>
        /// Validates a recovery code for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="recoveryCode">Recovery code</param>
        /// <returns>True if recovery code is valid and unused</returns>
        Task<bool> ValidateRecoveryCodeAsync(long userId, string recoveryCode);

        /// <summary>
        /// Checks if TOTP is enabled for a user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if TOTP is enabled</returns>
        Task<bool> IsTotpEnabledAsync(long userId);
    }

    public class TotpSetupResult
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeUri { get; set; } = string.Empty;
        public string QrCodeDataUrl { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
    }
}