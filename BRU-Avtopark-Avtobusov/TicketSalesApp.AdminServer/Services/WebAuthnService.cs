using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using TicketSalesApp.AdminServer.Configuration;
using TicketSalesApp.AdminServer.Services.Interfaces;
using TicketSalesApp.Core.Data;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.AdminServer.Services
{
    /// <summary>
    /// Implementation of WebAuthn (FIDO2) authentication service
    /// </summary>
    public class WebAuthnService : IWebAuthnService
    {
        private readonly IFido2 _fido2;
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly WebAuthnSettings _settings;
        private readonly ILogger<WebAuthnService> _logger;

        public WebAuthnService(
            IFido2 fido2,
            AppDbContext context,
            IMemoryCache cache,
            IOptions<WebAuthnSettings> settings,
            ILogger<WebAuthnService> logger)
        {
            _fido2 = fido2;
            _context = context;
            _cache = cache;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<CredentialCreateOptions> BeginRegistrationAsync(Guid userId, string username, string displayName)
        {
            try
            {
                _logger.LogInformation("Beginning WebAuthn registration for user {UserId} ({Username})", userId, username);

                // Get existing credentials for this user to exclude them
                var existingCredentials = await _context.WebAuthnCredentials
                    .Where(c => c.UserId == userId && c.IsActive)
                    .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
                    .ToListAsync();

                // Create user entity for FIDO2
                var user = new Fido2User
                {
                    DisplayName = displayName,
                    Name = username,
                    Id = userId.ToByteArray()
                };

                // Create credential creation options
                var options = _fido2.RequestNewCredential(
                    user, 
                    existingCredentials, 
                    AuthenticatorSelection.Default,
                    AttestationConveyancePreference.None);

                // Cache the options for verification later
                var cacheKey = $"webauthn_registration_{userId}";
                _cache.Set(cacheKey, options, TimeSpan.FromMinutes(5));

                _logger.LogInformation("WebAuthn registration options generated for user {UserId}", userId);
                return options;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error beginning WebAuthn registration for user {UserId}", userId);
                throw;
            }
        }

        public async Task<(bool success, string message)> CompleteRegistrationAsync(
            Guid userId, 
            string responseJson, 
            string? friendlyName = null)
        {
            try
            {
                _logger.LogInformation("Completing WebAuthn registration for user {UserId}", userId);

                // Retrieve cached options
                var cacheKey = $"webauthn_registration_{userId}";
                if (!_cache.TryGetValue(cacheKey, out CredentialCreateOptions? options) || options == null)
                {
                    return (false, "Registration session expired or not found");
                }

                // Parse the JSON response from the client
                AuthenticatorAttestationRawResponse response;
                try
                {
                    response = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(responseJson)!;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse WebAuthn registration response for user {UserId}", userId);
                    return (false, "Invalid response format");
                }

                // Verify the attestation response
                var success = await _fido2.MakeNewCredentialAsync(response, options, async (args, cancellationToken) =>
                {
                    // Check if credential ID already exists
                    var existingCredential = await _context.WebAuthnCredentials
                        .FirstOrDefaultAsync(c => c.CredentialId == args.CredentialId, cancellationToken);
                    
                    return existingCredential == null;
                });

                if (success.Status != "ok")
                {
                    _logger.LogWarning("WebAuthn registration failed for user {UserId}: {ErrorMessage}", 
                        userId, success.ErrorMessage);
                    return (false, success.ErrorMessage ?? "Registration failed");
                }

                // Save the credential to database
                var credential = new WebAuthnCredential
                {
                    UserId = userId,
                    CredentialId = success.Result!.CredentialId,
                    PublicKey = success.Result.PublicKey,
                    UserHandle = success.Result.User.Id,
                    SignatureCounter = success.Result.Counter,
                    CredType = success.Result.CredType,
                    RegisteredAt = DateTime.UtcNow,
                    AaGuid = success.Result.Aaguid.ToByteArray(),
                    FriendlyName = friendlyName ?? $"Security Key {DateTime.Now:yyyy-MM-dd HH:mm}",
                    IsActive = true
                };

                _context.WebAuthnCredentials.Add(credential);
                await _context.SaveChangesAsync();

                // Remove from cache
                _cache.Remove(cacheKey);

                _logger.LogInformation("WebAuthn credential registered successfully for user {UserId}", userId);
                return (true, "Credential registered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing WebAuthn registration for user {UserId}", userId);
                return (false, "An error occurred during registration");
            }
        }

        public async Task<AssertionOptions> BeginLoginAsync(string username)
        {
            try
            {
                _logger.LogInformation("Beginning WebAuthn login for username {Username}", username);

                // Find user by username
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == username);

                if (user == null)
                {
                    // Don't reveal that user doesn't exist - return generic options
                    var emptyOptions = _fido2.GetAssertionOptions(
                        new List<PublicKeyCredentialDescriptor>(),
                        UserVerificationRequirement.Preferred);
                    
                    return emptyOptions;
                }

                // Get user's credentials
                var credentials = await _context.WebAuthnCredentials
                    .Where(c => c.UserId == user.GuidId && c.IsActive)
                    .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
                    .ToListAsync();

                if (!credentials.Any())
                {
                    // User has no WebAuthn credentials
                    var emptyOptions = _fido2.GetAssertionOptions(
                        new List<PublicKeyCredentialDescriptor>(),
                        UserVerificationRequirement.Preferred);
                    
                    return emptyOptions;
                }

                // Create assertion options
                var options = _fido2.GetAssertionOptions(
                    credentials,
                    UserVerificationRequirement.Preferred);

                // Cache the options for verification later
                var cacheKey = $"webauthn_login_{username}";
                _cache.Set(cacheKey, options, TimeSpan.FromMinutes(5));

                _logger.LogInformation("WebAuthn login options generated for username {Username}", username);
                return options;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error beginning WebAuthn login for username {Username}", username);
                throw;
            }
        }

        public async Task<(bool success, User? user, string message)> CompleteLoginAsync(string responseJson)
        {
            try
            {
                _logger.LogInformation("Completing WebAuthn login");

                // Parse the JSON response from the client
                AuthenticatorAssertionRawResponse response;
                try
                {
                    response = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(responseJson)!;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse WebAuthn login response");
                    return (false, null, "Invalid response format");
                }

                // Find the credential by ID
                var credential = await _context.WebAuthnCredentials
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CredentialId == response.Id && c.IsActive);

                if (credential?.User == null)
                {
                    return (false, null, "Invalid credential");
                }

                // Retrieve cached options
                var cacheKey = $"webauthn_login_{credential.User.Login}";
                if (!_cache.TryGetValue(cacheKey, out AssertionOptions? options) || options == null)
                {
                    return (false, null, "Login session expired or not found");
                }

                // Verify the assertion
                var success = await _fido2.MakeAssertionAsync(response, options, credential.PublicKey, credential.SignatureCounter, async (args, cancellationToken) =>
                {
                    // Verify that the credential belongs to the user
                    var storedCredential = await _context.WebAuthnCredentials
                        .FirstOrDefaultAsync(c => c.CredentialId.SequenceEqual(args.CredentialId), cancellationToken);
                    
                    return storedCredential?.UserHandle?.SequenceEqual(args.UserHandle) == true;
                });

                if (success.Status != "ok")
                {
                    _logger.LogWarning("WebAuthn login failed: {ErrorMessage}", success.ErrorMessage);
                    return (false, null, success.ErrorMessage ?? "Authentication failed");
                }

                // Update signature counter and last used timestamp
                credential.SignatureCounter = success.Counter;
                credential.LastUsedAt = DateTime.UtcNow;
                
                // Update user's last login
                credential.User.LastLoginAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                // Remove from cache
                _cache.Remove(cacheKey);

                _logger.LogInformation("WebAuthn login successful for user {UserId}", credential.UserId);
                return (true, credential.User, "Authentication successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing WebAuthn login");
                return (false, null, "An error occurred during authentication");
            }
        }

        public async Task<IEnumerable<WebAuthnCredential>> GetUserCredentialsAsync(Guid userId)
        {
            try
            {
                var credentials = await _context.WebAuthnCredentials
                    .Where(c => c.UserId == userId && c.IsActive)
                    .OrderByDescending(c => c.RegisteredAt)
                    .ToListAsync();

                return credentials;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving WebAuthn credentials for user {UserId}", userId);
                throw;
            }
        }

        public async Task<(bool success, string message)> DeleteCredentialAsync(long credentialId, Guid userId)
        {
            try
            {
                var credential = await _context.WebAuthnCredentials
                    .FirstOrDefaultAsync(c => c.Id == credentialId && c.UserId == userId);

                if (credential == null)
                {
                    return (false, "Credential not found");
                }

                // Soft delete by marking as inactive
                credential.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("WebAuthn credential {CredentialId} deleted for user {UserId}", 
                    credentialId, userId);
                
                return (true, "Credential deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting WebAuthn credential {CredentialId} for user {UserId}", 
                    credentialId, userId);
                return (false, "An error occurred while deleting the credential");
            }
        }

        public async Task<(bool success, string message)> UpdateCredentialNameAsync(long credentialId, Guid userId, string friendlyName)
        {
            try
            {
                var credential = await _context.WebAuthnCredentials
                    .FirstOrDefaultAsync(c => c.Id == credentialId && c.UserId == userId && c.IsActive);

                if (credential == null)
                {
                    return (false, "Credential not found");
                }

                credential.FriendlyName = friendlyName;
                await _context.SaveChangesAsync();

                _logger.LogInformation("WebAuthn credential {CredentialId} name updated for user {UserId}", 
                    credentialId, userId);
                
                return (true, "Credential name updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating WebAuthn credential {CredentialId} name for user {UserId}", 
                    credentialId, userId);
                return (false, "An error occurred while updating the credential name");
            }
        }
    }
}