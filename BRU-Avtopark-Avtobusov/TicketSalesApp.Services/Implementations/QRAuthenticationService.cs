using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;
using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.Services.Implementations
{
    public class QRAuthenticationService : IQRAuthenticationService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<QRAuthenticationService> _logger;
        private readonly IMemoryCache _cache;

        public QRAuthenticationService(
            IConfiguration configuration,
            AppDbContext context,
            ILogger<QRAuthenticationService> logger,
            IMemoryCache cache)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        public async Task<string> GenerateQRLoginTokenAsync(User user)
        {
            try
            {
                var sessionId = Guid.NewGuid().ToString("N");
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var validationCode = GenerateValidationCode();

                // Store session data
                var cacheKey = $"qr_login_{sessionId}";
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                
                _cache.Set(cacheKey, new { 
                    UserId = user.UserId,
                    ValidationCode = validationCode,
                    Timestamp = timestamp
                }, cacheOptions);

                // Create QR code using RussiaPaymentOrder format
                var generator = new PayloadGenerator.RussiaPaymentOrder(
                    name: EncryptData(user.Login),                // Encrypted username
                    personalAcc: EncryptData(sessionId),          // Encrypted session ID
                    bankName: EncryptData(timestamp),             // Encrypted timestamp
                    BIC: EncryptData(validationCode),             // Encrypted validation code
                    correspAcc: EncryptData(user.Role.ToString()) // Encrypted role
                );

                return generator.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR login token");
                throw;
            }
        }

        public async Task<(bool success, User? user)> ValidateQRLoginTokenAsync(string token)
        {
            try
            {
                _logger.LogDebug("Validating QR token: {Token}", token);

                // Split the token into its parts
                var parts = token.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 6) // ST00012 + 5 fields
                {
                    _logger.LogWarning("Invalid QR token format - not enough parts. Found {Count} parts", parts.Length);
                    return (false, null);
                }

                // Extract fields from token
                // Format: ST00012|Name={username}|PersonalAcc={sessionId}|BankName={timestamp}|BIC={validationCode}|CorrespAcc={role}|
                var fields = new Dictionary<string, string>();
                foreach (var part in parts.Skip(1)) // Skip ST00012
                {
                    var fieldParts = part.Split('=', 2);
                    if (fieldParts.Length == 2)
                    {
                        fields[fieldParts[0]] = fieldParts[1];
                        _logger.LogDebug("Extracted field {Field}={Value}", fieldParts[0], fieldParts[1]);
                    }
                }

                if (!fields.ContainsKey("PersonalAcc"))
                {
                    _logger.LogWarning("Missing PersonalAcc field in token. Available fields: {Fields}", 
                        string.Join(", ", fields.Keys));
                    return (false, null);
                }

                var sessionId = DecryptData(fields["PersonalAcc"]);
                _logger.LogDebug("Decrypted session ID: {SessionId}", sessionId);
                var cacheKey = $"qr_login_{sessionId}";

                if (!_cache.TryGetValue(cacheKey, out dynamic? sessionData))
                {
                    _logger.LogWarning("QR login session not found or expired for key {Key}", cacheKey);
                    return (false, null);
                }

                var user = await _context.Users.FindAsync(sessionData.UserId);
                if (user == null)
                {
                    // Cast dynamic to long to avoid extension method dispatch issue
                    var userId = (long)sessionData.UserId;
                    _logger.LogWarning("User not found for QR login session. User ID: {UserId}", userId);
                    return (false, null);
                }

                // Validate timestamp
                var timestamp = DateTime.ParseExact(
                    DecryptData(fields["BankName"]),
                    "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture);

                if (DateTime.UtcNow - timestamp > TimeSpan.FromMinutes(5))
                {
                    _logger.LogWarning("QR login token expired. Token timestamp: {Timestamp}, Current time: {Now}", 
                        timestamp, DateTime.UtcNow);
                    return (false, null);
                }

                // Validate code
                var validationCode = DecryptData(fields["BIC"]);
                // Cast dynamic to string to avoid extension method dispatch issue
                var expectedCode = (string)sessionData.ValidationCode;
                if (validationCode != expectedCode)
                {
                    _logger.LogWarning("Invalid validation code. Expected: {Expected}, Got: {Actual}", 
                        expectedCode, validationCode);
                    return (false, null);
                }

                // Cast to string to avoid extension method dispatch issue with user.Login
                var userLogin = user.Login as string; // Use 'as' for safe casting
                _logger.LogInformation("QR login validation successful for user {Login}", userLogin ?? "Unknown"); // Handle potential null
                return (true, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating QR login token");
                return (false, null);
            }
        }

        public async Task<string> GenerateQRCodeAsync(User user)
        {
            var token = await GenerateQRLoginTokenAsync(user);
            
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                return Convert.ToBase64String(qrCodeImage);
            }
        }

        public async Task<(string qrCode, string rawData)> GenerateQRCodeWithDataAsync(User user)
        {
            var token = await GenerateQRLoginTokenAsync(user);
            
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q))
            using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
            {
                byte[] qrCodeImage = qrCode.GetGraphic(20);
                return (
                    qrCode: Convert.ToBase64String(qrCodeImage),
                    rawData: token
                );
            }
        }

        private string EncryptData(string data)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException(nameof(data));

            var key = _configuration["QRLogin:EncryptionKey"] ?? 
                throw new InvalidOperationException("QRLogin:EncryptionKey not configured");

            // Ensure key is proper length for AES
            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < 32)
            {
                Array.Resize(ref keyBytes, 32);
            }
            else if (keyBytes.Length > 32)
            {
                Array.Resize(ref keyBytes, 32);
            }

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV(); // Generate a new IV for each encryption

            var encrypted = aes.EncryptCbc(
                Encoding.UTF8.GetBytes(data),
                aes.IV
            );

            // Combine IV and encrypted data
            var combined = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, combined, aes.IV.Length, encrypted.Length);

            return Convert.ToBase64String(combined);
        }

        private string DecryptData(string encryptedData)
        {
            if (string.IsNullOrEmpty(encryptedData))
                throw new ArgumentNullException(nameof(encryptedData));

            var key = _configuration["QRLogin:EncryptionKey"] ?? 
                throw new InvalidOperationException("QRLogin:EncryptionKey not configured");

            // Ensure key is proper length for AES
            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length < 32)
            {
                Array.Resize(ref keyBytes, 32);
            }
            else if (keyBytes.Length > 32)
            {
                Array.Resize(ref keyBytes, 32);
            }

            var combined = Convert.FromBase64String(encryptedData);
            
            using var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = keyBytes;

            // Extract IV from the combined data
            var iv = new byte[aes.BlockSize / 8];
            var cipherText = new byte[combined.Length - iv.Length];
            Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(combined, iv.Length, cipherText, 0, cipherText.Length);

            var decrypted = aes.DecryptCbc(cipherText, iv);
            return Encoding.UTF8.GetString(decrypted);
        }

        private string GenerateValidationCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        // Add new methods for direct QR login
        public async Task<(string qrCode, string rawData)> GenerateDirectLoginQRCodeAsync(string username, string deviceType)
        {
            try
            {
                var sessionId = Guid.NewGuid().ToString("N");
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var validationCode = GenerateValidationCode();

                // Store session data
                var cacheKey = $"direct_login_{sessionId}";
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // Longer expiration for direct login
                
                _cache.Set(cacheKey, new { 
                    Username = username,
                    ValidationCode = validationCode,
                    Timestamp = timestamp,
                    DeviceType = deviceType,
                    DeviceId = sessionId
                }, cacheOptions);

                // Create QR code using RussiaPaymentOrder format
                var generator = new PayloadGenerator.RussiaPaymentOrder(
                    name: EncryptData(username),                // Encrypted username
                    personalAcc: EncryptData(sessionId),        // Encrypted session ID
                    bankName: EncryptData(timestamp),           // Encrypted timestamp
                    BIC: EncryptData(validationCode),          // Encrypted validation code
                    correspAcc: EncryptData(deviceType)        // Encrypted device type
                );

                var token = generator.ToString();

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q))
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImage = qrCode.GetGraphic(20);
                    return (
                        qrCode: Convert.ToBase64String(qrCodeImage),
                        rawData: token
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating direct login QR code for user {Username}", username);
                throw;
            }
        }

        public async Task<(bool success, User? user, string deviceId)> ValidateDirectLoginTokenAsync(string token, string deviceType)
        {
            try
            {
                _logger.LogDebug("Validating direct login QR token: {Token}", token);

                var parts = token.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 6)
                {
                    _logger.LogWarning("Invalid QR token format - not enough parts");
                    return (false, null, string.Empty);
                }

                var fields = new Dictionary<string, string>();
                foreach (var part in parts.Skip(1))
                {
                    var fieldParts = part.Split('=', 2);
                    if (fieldParts.Length == 2)
                    {
                        fields[fieldParts[0]] = fieldParts[1];
                    }
                }

                var sessionId = DecryptData(fields["PersonalAcc"]);
                var cacheKey = $"direct_login_{sessionId}";

                if (!_cache.TryGetValue(cacheKey, out dynamic? sessionData))
                {
                    _logger.LogWarning("Direct login session not found or expired");
                    return (false, null, string.Empty);
                }

                var username = DecryptData(fields["Name"]);
                var user = await _context.Users
                    .Include(u => u.UserRoles!)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Login == username);

                if (user == null)
                {
                    _logger.LogWarning("User not found for direct login: {Username}", username);
                    return (false, null, string.Empty);
                }

                // Validate timestamp
                var timestamp = DateTime.ParseExact(
                    DecryptData(fields["BankName"]),
                    "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture);

                if (DateTime.UtcNow - timestamp > TimeSpan.FromMinutes(30))
                {
                    _logger.LogWarning("Direct login token expired");
                    return (false, null, string.Empty);
                }

                // Validate code
                var validationCode = DecryptData(fields["BIC"]);
                var expectedCode = (string)sessionData.ValidationCode;
                if (validationCode != expectedCode)
                {
                    _logger.LogWarning("Invalid validation code for direct login");
                    return (false, null, string.Empty);
                }

                // Validate device type
                var tokenDeviceType = DecryptData(fields["CorrespAcc"]);
                if (tokenDeviceType != deviceType)
                {
                    _logger.LogWarning("Device type mismatch. Expected: {Expected}, Got: {Actual}", 
                        deviceType, tokenDeviceType);
                    return (false, null, string.Empty);
                }

                // Update user's last login time
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Direct login validation successful for user {Username}", username);
                return (true, user, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating direct login token");
                return (false, null, string.Empty);
            }
        }

        public async Task<bool> NotifyDeviceLoginSuccessAsync(string deviceId, string token)
        {
            try
            {
                var cacheKey = $"direct_login_{deviceId}";
                if (!_cache.TryGetValue(cacheKey, out dynamic? sessionData))
                {
                    _logger.LogWarning("Device session not found: {DeviceId}", deviceId);
                    return false;
                }

                // Store the JWT token for the device to retrieve
                var loginSuccessKey = $"login_success_{deviceId}";
                _cache.Set(loginSuccessKey, token, TimeSpan.FromMinutes(5));

                _logger.LogInformation("Device login success notification stored for device {DeviceId}", deviceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying device login success");
                return false;
            }
        }
    }
}