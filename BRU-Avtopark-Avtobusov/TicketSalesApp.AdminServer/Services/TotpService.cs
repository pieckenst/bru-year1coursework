using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;
using QRCoder;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using BCrypt.Net;

namespace TicketSalesApp.AdminServer.Services
{
    public class TotpService : ITotpService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TotpService> _logger;
        private const string Issuer = "TicketSalesApp";
        private const int RecoveryCodeCount = 10;
        private const int RecoveryCodeLength = 8;

        public TotpService(AppDbContext context, ILogger<TotpService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TotpSetupResult> GenerateSetupAsync(long userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("User not found", nameof(userId));
                }

                // Generate a new secret key
                var secretKey = KeyGeneration.GenerateRandomKey(20); // 160-bit key
                var secretBase32 = Base32Encoding.ToString(secretKey);

                // Store the secret temporarily (not enabled yet)
                user.TotpSecret = secretBase32;
                await _context.SaveChangesAsync();

                // Generate QR code URI
                var totpUri = $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(user.Login)}?secret={secretBase32}&issuer={Uri.EscapeDataString(Issuer)}";

                // Generate QR code image
                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(totpUri, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);
                var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);
                var qrCodeDataUrl = $"data:image/png;base64,{qrCodeBase64}";

                _logger.LogInformation("Generated TOTP setup for user {UserId}", userId);

                return new TotpSetupResult
                {
                    SecretKey = secretBase32,
                    QrCodeUri = totpUri,
                    QrCodeDataUrl = qrCodeDataUrl,
                    ManualEntryKey = FormatSecretForManualEntry(secretBase32),
                    Username = user.Login,
                    Issuer = Issuer
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating TOTP setup for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateCodeAsync(long userId, string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
                {
                    return false;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.TotpSecret) || !user.IsTotpEnabled)
                {
                    return false;
                }

                var secretBytes = Base32Encoding.ToBytes(user.TotpSecret);
                var totp = new Totp(secretBytes);

                // Verify the code with a window of ±1 time step (30 seconds each)
                var isValid = totp.VerifyTotp(code, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);

                if (isValid)
                {
                    _logger.LogInformation("TOTP code validated successfully for user {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("Invalid TOTP code attempt for user {UserId}", userId);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating TOTP code for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> EnableTotpAsync(long userId, string verificationCode)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.TotpSecret))
                {
                    return false;
                }

                // Validate the verification code
                var secretBytes = Base32Encoding.ToBytes(user.TotpSecret);
                var totp = new Totp(secretBytes);
                var isValid = totp.VerifyTotp(verificationCode, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);

                if (!isValid)
                {
                    _logger.LogWarning("Invalid verification code during TOTP enable for user {UserId}", userId);
                    return false;
                }

                // Enable TOTP and generate recovery codes
                user.IsTotpEnabled = true;
                user.TotpEnabledAt = DateTime.UtcNow;
                
                // Generate initial recovery codes
                var recoveryCodes = GenerateRecoveryCodes();
                var hashedRecoveryCodes = recoveryCodes.Select(code => BCrypt.Net.BCrypt.HashPassword(code)).ToList();
                user.TotpRecoveryCodes = JsonSerializer.Serialize(hashedRecoveryCodes);

                await _context.SaveChangesAsync();

                _logger.LogInformation("TOTP enabled successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling TOTP for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DisableTotpAsync(long userId, string verificationCode)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsTotpEnabled)
                {
                    return false;
                }

                bool isValid = false;

                // Check if it's a TOTP code
                if (verificationCode.Length == 6 && verificationCode.All(char.IsDigit))
                {
                    isValid = await ValidateCodeAsync(userId, verificationCode);
                }
                // Check if it's a recovery code
                else if (verificationCode.Length == RecoveryCodeLength)
                {
                    isValid = await ValidateRecoveryCodeAsync(userId, verificationCode);
                }

                if (!isValid)
                {
                    _logger.LogWarning("Invalid verification code during TOTP disable for user {UserId}", userId);
                    return false;
                }

                // Disable TOTP and clear related data
                user.IsTotpEnabled = false;
                user.TotpSecret = null;
                user.TotpEnabledAt = null;
                user.TotpRecoveryCodes = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("TOTP disabled successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling TOTP for user {UserId}", userId);
                return false;
            }
        }

        public async Task<IEnumerable<string>> GenerateRecoveryCodesAsync(long userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsTotpEnabled)
                {
                    throw new ArgumentException("User not found or TOTP not enabled", nameof(userId));
                }

                var recoveryCodes = GenerateRecoveryCodes();
                var hashedRecoveryCodes = recoveryCodes.Select(code => BCrypt.Net.BCrypt.HashPassword(code)).ToList();
                user.TotpRecoveryCodes = JsonSerializer.Serialize(hashedRecoveryCodes);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated new recovery codes for user {UserId}", userId);
                return recoveryCodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recovery codes for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateRecoveryCodeAsync(long userId, string recoveryCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recoveryCode))
                {
                    return false;
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsTotpEnabled || string.IsNullOrEmpty(user.TotpRecoveryCodes))
                {
                    return false;
                }

                var hashedRecoveryCodes = JsonSerializer.Deserialize<List<string>>(user.TotpRecoveryCodes);
                if (hashedRecoveryCodes == null)
                {
                    return false;
                }

                // Check if the recovery code matches any of the stored hashed codes
                for (int i = 0; i < hashedRecoveryCodes.Count; i++)
                {
                    if (BCrypt.Net.BCrypt.Verify(recoveryCode, hashedRecoveryCodes[i]))
                    {
                        // Remove the used recovery code
                        hashedRecoveryCodes.RemoveAt(i);
                        user.TotpRecoveryCodes = JsonSerializer.Serialize(hashedRecoveryCodes);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Recovery code used successfully for user {UserId}", userId);
                        return true;
                    }
                }

                _logger.LogWarning("Invalid recovery code attempt for user {UserId}", userId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating recovery code for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsTotpEnabledAsync(long userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                return user?.IsTotpEnabled ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking TOTP status for user {UserId}", userId);
                return false;
            }
        }

        private List<string> GenerateRecoveryCodes()
        {
            var recoveryCodes = new List<string>();
            using var rng = RandomNumberGenerator.Create();

            for (int i = 0; i < RecoveryCodeCount; i++)
            {
                var bytes = new byte[RecoveryCodeLength / 2];
                rng.GetBytes(bytes);
                var code = Convert.ToHexString(bytes).ToLowerInvariant();
                recoveryCodes.Add(code);
            }

            return recoveryCodes;
        }

        private string FormatSecretForManualEntry(string secret)
        {
            // Format the secret key for manual entry (groups of 4 characters)
            var formatted = new StringBuilder();
            for (int i = 0; i < secret.Length; i += 4)
            {
                if (i > 0) formatted.Append(' ');
                formatted.Append(secret.Substring(i, Math.Min(4, secret.Length - i)));
            }
            return formatted.ToString();
        }
    }
}