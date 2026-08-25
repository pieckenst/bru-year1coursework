using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TicketSalesApp.AdminServer.Authentication;

/// <summary>
/// Middleware to log NTLM/Negotiate Type 1, 2, 3 challenges during Windows authentication
/// </summary>
public class WindowsAuthLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WindowsAuthLoggingMiddleware> _logger;

    public WindowsAuthLoggingMiddleware(RequestDelegate next, ILogger<WindowsAuthLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (!string.IsNullOrEmpty(authHeader))
        {
            _logger.LogInformation("[WindowsAuthMiddleware] Authorization header present: {Header}", 
                authHeader.Substring(0, Math.Min(50, authHeader.Length)));
            
            // Parse NTLM/Negotiate tokens
            if (authHeader.StartsWith("NTLM ", StringComparison.OrdinalIgnoreCase))
            {
                var ntlmToken = authHeader.Substring(5).Trim();
                var messageType = GetNtlmMessageType(ntlmToken);
                
                _logger.LogInformation("[WindowsAuthMiddleware] NTLM {MessageType} message detected - Token length: {TokenLength}", 
                    messageType, ntlmToken.Length);
                
                // Store authentication flow info in HttpContext
                context.Items["WindowsAuthFlow"] = new WindowsAuthFlowInfo
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
                
                _logger.LogInformation("[WindowsAuthMiddleware] Negotiate {MessageType} message detected - Token length: {TokenLength}", 
                    messageType, negotiateToken.Length);
                
                // Store authentication flow info in HttpContext
                context.Items["WindowsAuthFlow"] = new WindowsAuthFlowInfo
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
            _logger.LogInformation("[WindowsAuthMiddleware] No Authorization header - initial challenge expected");
            context.Items["WindowsAuthFlow"] = new WindowsAuthFlowInfo
            {
                Protocol = "Initial",
                MessageType = "Challenge",
                Token = null,
                Timestamp = DateTime.UtcNow
            };
        }

        // Log response headers when they're being sent
        context.Response.OnStarting(() =>
        {
            var wwwAuthenticate = context.Response.Headers["Www-Authenticate"].ToString();
            if (!string.IsNullOrEmpty(wwwAuthenticate))
            {
                _logger.LogInformation("[WindowsAuthMiddleware] WWW-Authenticate header: {Header}", wwwAuthenticate);
                
                // Try to parse the challenge token
                if (wwwAuthenticate.StartsWith("Negotiate ", StringComparison.OrdinalIgnoreCase))
                {
                    var challengeToken = wwwAuthenticate.Substring(9).Trim();
                    var messageType = GetNtlmMessageType(challengeToken);
                    _logger.LogInformation("[WindowsAuthMiddleware] Server challenge - {MessageType} - Token length: {TokenLength}", 
                        messageType, challengeToken.Length);
                }
            }
            
            // Update flow info with authentication result
            if (context.Items["WindowsAuthFlow"] is WindowsAuthFlowInfo flowInfo)
            {
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    flowInfo.AuthenticationSucceeded = true;
                    flowInfo.AuthenticatedUser = context.User.Identity.Name;
                    flowInfo.AuthenticationType = context.User.Identity.AuthenticationType;
                    _logger.LogInformation("[WindowsAuthMiddleware] Authentication succeeded for user: {User} - Type: {AuthType}", 
                        flowInfo.AuthenticatedUser, flowInfo.AuthenticationType);
                }
                else
                {
                    flowInfo.AuthenticationSucceeded = false;
                    _logger.LogWarning("[WindowsAuthMiddleware] Authentication failed or not completed");
                }
            }
            
            return Task.CompletedTask;
        });

        await _next(context);
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
