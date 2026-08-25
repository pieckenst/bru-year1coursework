using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace TicketSalesApp.AdminServer.Authentication;

/// <summary>
/// Custom NegotiateHandler that logs NTLM/Negotiate Type 1, 2, 3 challenges during Windows authentication
/// </summary>
public sealed class WindowsAuthLoggingHandler : NegotiateHandler
{
    private readonly ILogger<WindowsAuthLoggingHandler> _logger;

    public WindowsAuthLoggingHandler(
        IOptionsMonitor<NegotiateOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        _logger = logger.CreateLogger<WindowsAuthLoggingHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(authHeader))
        {
            _logger.LogInformation("[WindowsAuthHandler] Authorization header present: {Header}", 
                authHeader.Substring(0, Math.Min(50, authHeader.Length)));
            
            // Parse NTLM/Negotiate tokens
            if (authHeader.StartsWith("NTLM ", StringComparison.OrdinalIgnoreCase))
            {
                var ntlmToken = authHeader.Substring(5).Trim();
                var messageType = GetNtlmMessageType(ntlmToken);
                
                _logger.LogInformation("[WindowsAuthHandler] NTLM {MessageType} message detected - Token length: {TokenLength}", 
                    messageType, ntlmToken.Length);
                
                // Store authentication flow info in HttpContext
                Context.Items["WindowsAuthFlow"] = new WindowsAuthFlowInfo
                {
                    Protocol = "NTLM",
                    MessageType = messageType,
                    Token = ntlmToken,
                    Timestamp = DateTime.UtcNow
                };
            }
            else if (authHeader.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
            {
                var negotiateToken = authHeader.Substring(9).Trim();
                var messageType = GetNegotiateMessageType(negotiateToken);
                
                _logger.LogInformation("[WindowsAuthHandler] Negotiate {MessageType} message detected - Token length: {TokenLength}", 
                    messageType, negotiateToken.Length);
                
                // Store authentication flow info in HttpContext
                Context.Items["WindowsAuthFlow"] = new WindowsAuthFlowInfo
                {
                    Protocol = "Negotiate",
                    MessageType = messageType,
                    Token = negotiateToken,
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        else
        {
            _logger.LogInformation("[WindowsAuthHandler] No Authorization header - initial challenge expected");
            Context.Items["WindowsAuthFlow"] = new WindowsAuthFlowInfo
            {
                Protocol = "Initial",
                MessageType = "Challenge",
                Token = null,
                Timestamp = DateTime.UtcNow
            };
        }

        var result = await base.HandleAuthenticateAsync();
        
        if (result.Succeeded && result.Principal != null)
        {
            _logger.LogInformation("[WindowsAuthHandler] Authentication succeeded for user: {User}", 
                result.Principal.Identity?.Name);
            
            // Update flow info with success
            if (Context.Items["WindowsAuthFlow"] is WindowsAuthFlowInfo flowInfo)
            {
                flowInfo.AuthenticationSucceeded = true;
                flowInfo.AuthenticatedUser = result.Principal.Identity?.Name;
                flowInfo.AuthenticationType = result.Principal.Identity?.AuthenticationType;
            }
        }
        else if (result.Failure != null)
        {
            _logger.LogWarning("[WindowsAuthHandler] Authentication failed: {Error}", result.Failure.Message);
            
            // Update flow info with failure
            if (Context.Items["WindowsAuthFlow"] is WindowsAuthFlowInfo flowInfo)
            {
                flowInfo.AuthenticationSucceeded = false;
            }
        }
        
        return result;
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        _logger.LogInformation("[WindowsAuthHandler] Sending challenge response");
        
        await base.HandleChallengeAsync(properties);
        
        // Log WWW-Authenticate header after base handler sets it
        var wwwAuthenticate = Response.Headers["Www-Authenticate"].ToString();
        if (!string.IsNullOrEmpty(wwwAuthenticate))
        {
            _logger.LogInformation("[WindowsAuthHandler] WWW-Authenticate header: {Header}", wwwAuthenticate);
            
            // Try to parse the challenge token
            if (wwwAuthenticate.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
            {
                var challengeToken = wwwAuthenticate.Substring(9).Trim();
                var messageType = GetNtlmMessageType(challengeToken);
                _logger.LogInformation("[WindowsAuthHandler] Server challenge - {MessageType} - Token length: {TokenLength}", 
                    messageType, challengeToken.Length);
            }
        }
    }

    private string GetNtlmMessageType(string token)
    {
        if (string.IsNullOrEmpty(token))
            return "Unknown";

        try
        {
            // NTLM messages start with signature "NTLMSSP\0"
            // Byte 8 indicates message type: 1=Type1, 2=Type2, 3=Type3
            var bytes = Convert.FromBase64String(token);
            
            if (bytes.Length < 12)
                return "Invalid";

            // Check for NTLMSSP signature (bytes 0-7: 4E 54 4C 4D 53 53 50 00)
            if (bytes[0] == 0x4E && bytes[1] == 0x54 && bytes[2] == 0x4C && bytes[3] == 0x4D &&
                bytes[4] == 0x53 && bytes[5] == 0x53 && bytes[6] == 0x50 && bytes[7] == 0x00)
            {
                var messageType = BitConverter.ToInt32(bytes, 8);
                return messageType switch
                {
                    1 => "Type1",
                    2 => "Type2",
                    3 => "Type3",
                    _ => $"Type{messageType}"
                };
            }
            
            return "Invalid";
        }
        catch
        {
            return "ParseError";
        }
    }

    private string GetNegotiateMessageType(string token)
    {
        if (string.IsNullOrEmpty(token))
            return "Unknown";

        try
        {
            var bytes = Convert.FromBase64String(token);
            
            // Negotiate/SPNEGO can wrap NTLM or Kerberos
            // Check if it's wrapped NTLM
            if (bytes.Length > 12 && bytes[0] == 0x4E && bytes[1] == 0x54 && bytes[2] == 0x4C && bytes[3] == 0x4D &&
                bytes[4] == 0x53 && bytes[5] == 0x53 && bytes[6] == 0x50 && bytes[7] == 0x00)
            {
                var messageType = BitConverter.ToInt32(bytes, 8);
                return $"Negotiate-NTLM-Type{messageType}";
            }
            
            // Kerberos tokens start with different OID patterns
            // For simplicity, we'll just indicate it's a Kerberos token
            return "Kerberos";
        }
        catch
        {
            return "ParseError";
        }
    }
}

/// <summary>
/// Information about Windows authentication flow
/// </summary>
public class WindowsAuthFlowInfo
{
    public string Protocol { get; set; }
    public string MessageType { get; set; }
    public string Token { get; set; }
    public DateTime Timestamp { get; set; }
    public bool AuthenticationSucceeded { get; set; }
    public string AuthenticatedUser { get; set; }
    public string AuthenticationType { get; set; }
}
