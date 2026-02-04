using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace TicketSalesApp.AdminServer.Authentication;

/// <summary>
/// Custom authentication handler for SignalR WebSocket connections using JWT tokens
/// </summary>
public class SignalRJwtAuthenticationHandler : AuthenticationHandler<JwtBearerOptions>
{
    private readonly ILogger<SignalRJwtAuthenticationHandler> _logger;

    public SignalRJwtAuthenticationHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _logger = logger.CreateLogger<SignalRJwtAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Try to get token from query string (for WebSocket connections)
            var token = Request.Query["access_token"].FirstOrDefault();

            // If not in query string, try Authorization header
            if (string.IsNullOrEmpty(token))
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader?.StartsWith("Bearer ") == true)
                {
                    token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("No JWT token found in request");
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Validate the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = Options.TokenValidationParameters;

            try
            {
                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                
                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    // Additional validation for SignalR context
                    var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var role = principal.FindFirst("role")?.Value;

                    if (string.IsNullOrEmpty(userId))
                    {
                        _logger.LogWarning("JWT token is missing required user ID claim");
                        return Task.FromResult(AuthenticateResult.Fail("Invalid token: missing user ID"));
                    }

                    _logger.LogDebug("JWT token validated successfully for user {UserId} with role {Role}", userId, role);

                    var ticket = new AuthenticationTicket(principal, Scheme.Name);
                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }
                else
                {
                    _logger.LogWarning("Token validation succeeded but token is not a JWT");
                    return Task.FromResult(AuthenticateResult.Fail("Invalid token format"));
                }
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogDebug("JWT token has expired: {Message}", ex.Message);
                return Task.FromResult(AuthenticateResult.Fail("Token expired"));
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning("JWT token has invalid signature: {Message}", ex.Message);
                return Task.FromResult(AuthenticateResult.Fail("Invalid token signature"));
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("JWT token validation failed: {Message}", ex.Message);
                return Task.FromResult(AuthenticateResult.Fail($"Token validation failed: {ex.Message}"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during JWT authentication for SignalR");
            return Task.FromResult(AuthenticateResult.Fail("Authentication error"));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // For SignalR connections, we can't send a traditional challenge response
        // The connection will simply be rejected
        _logger.LogDebug("SignalR authentication challenge - connection will be rejected");
        
        Response.StatusCode = 401;
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // For SignalR connections, forbidden access results in connection rejection
        _logger.LogDebug("SignalR authentication forbidden - connection will be rejected");
        
        Response.StatusCode = 403;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for configuring SignalR JWT authentication
/// </summary>
public static class SignalRJwtAuthenticationExtensions
{
    /// <summary>
    /// Add SignalR JWT authentication to the service collection
    /// </summary>
    public static AuthenticationBuilder AddSignalRJwtAuthentication(
        this AuthenticationBuilder builder,
        string authenticationScheme,
        Action<JwtBearerOptions>? configureOptions = null)
    {
        return builder.AddScheme<JwtBearerOptions, SignalRJwtAuthenticationHandler>(
            authenticationScheme, configureOptions);
    }
}