using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSalesApp.Core.Data;
using System.Text;

namespace TicketSalesApp.AdminServer.Controllers.dev
{
    [ApiController]
    [Route("api/dev/auth")]
    [AllowAnonymous]
    public class AuthDevelopmentController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthDevelopmentController> _logger;
        private readonly AppDbContext _context;

        public AuthDevelopmentController(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<AuthDevelopmentController> logger,
            AppDbContext context)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        private bool IsDevelopmentEnvironment()
        {
            return _environment.IsDevelopment();
        }

        private IActionResult CheckDevelopmentEnvironment()
        {
            if (!IsDevelopmentEnvironment())
            {
                return NotFound("Development pages are only available in Development environment.");
            }
            return null;
        }

        [HttpGet("")]
        [AllowAnonymous]
        public IActionResult Index()
        {
            var devCheck = CheckDevelopmentEnvironment();
            if (devCheck != null) return devCheck;

            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Authentication Development Hub - BRU Avtopark</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }
        .container { max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #333; text-align: center; margin-bottom: 30px; }
        .auth-method { background: #f8f9fa; padding: 20px; margin: 15px 0; border-radius: 6px; border-left: 4px solid #007bff; }
        .auth-method h3 { margin-top: 0; color: #007bff; }
        .auth-method p { margin: 10px 0; color: #666; }
        .btn { display: inline-block; padding: 10px 20px; background: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 5px; }
        .btn:hover { background: #0056b3; }
        .btn-secondary { background: #6c757d; }
        .btn-secondary:hover { background: #545b62; }
        .status { padding: 10px; margin: 10px 0; border-radius: 4px; }
        .status.success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .status.info { background: #d1ecf1; color: #0c5460; border: 1px solid #bee5eb; }
        .status.warning { background: #fff3cd; color: #856404; border: 1px solid #ffeaa7; }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🔐 Authentication Development Hub</h1>
        <div class=""status warning"">
            <strong>Development Environment Only</strong><br>
            This page is only available in Development environment for testing authentication methods.
        </div>
        <div class=""status info"">
            <strong>Environment:</strong> " + _environment.EnvironmentName + @"<br>
            <strong>Server:</strong> " + Request.Host + @"<br>
            <strong>Time:</strong> " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"
        </div>

        <div class=""auth-method"">
            <h3>🔑 Basic Authentication</h3>
            <p>Traditional username/password authentication with login and registration.</p>
            <a href=""/api/dev/auth/login"" class=""btn"">Login Page</a>
            <a href=""/api/dev/auth/register"" class=""btn btn-secondary"">Register Page</a>
        </div>

        <div class=""auth-method"">
            <h3>📱 QR Code Authentication</h3>
            <p>QR code-based authentication for mobile integration and quick access.</p>
            <a href=""/api/dev/auth/qr"" class=""btn"">QR Auth Test</a>
        </div>

        <div class=""auth-method"">
            <h3>🔐 WebAuthn (FIDO2)</h3>
            <p>Modern passwordless authentication using biometrics, security keys, or platform authenticators.</p>
            <a href=""/api/dev/auth/webauthn"" class=""btn"">WebAuthn Test</a>
        </div>

        <div class=""auth-method"">
            <h3>🖥️ Windows Authentication</h3>
            <p>Windows domain authentication and account linking for enterprise environments.</p>
            <a href=""/api/dev/auth/windows"" class=""btn"">Windows Auth Test</a>
        </div>

        <div class=""status success"">
            <strong>API Endpoints:</strong><br>
            • Authentication: <code>/api/v1/auth/*</code><br>
            • QR Auth: <code>/api/v1/auth/qr/*</code><br>
            • WebAuthn: <code>/api/v1/auth/webauthn/*</code><br>
            • Windows Auth: <code>/api/v1/windows-auth/*</code>
        </div>
    </div>
</body>
</html>";

            return Content(html, "text/html");
        }

        [HttpGet("login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            var devCheck = CheckDevelopmentEnvironment();
            if (devCheck != null) return devCheck;

            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Login - BRU Avtopark Development</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: Arial, sans-serif; line-height: 1.6; padding: 20px; max-width: 1200px; margin: 0 auto; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; }
        .container { background: white; padding: 40px; border-radius: 10px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); margin: 20px auto; width: 100%; max-width: 600px; }
        h2 { text-align: center; color: #333; margin-bottom: 30px; }
        .form-group { margin-bottom: 20px; }
        label { display: block; margin-bottom: 5px; color: #555; font-weight: bold; }
        input[type=""text""], input[type=""password""] { width: 100%; padding: 12px; border: 2px solid #ddd; border-radius: 6px; font-size: 16px; box-sizing: border-box; }
        input[type=""text""]:focus, input[type=""password""]:focus { border-color: #667eea; outline: none; box-shadow: 0 0 0 2px rgba(102,126,234,0.25); }
        .btn { width: 100%; padding: 12px; background: #667eea; color: white; border: none; border-radius: 6px; font-size: 16px; cursor: pointer; margin-top: 10px; transition: background-color 0.2s; }
        .btn:hover { background: #5a6fd8; }
        .result { margin-top: 20px; padding: 15px; border-radius: 6px; }
        .success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
        .role-admin { color: #dc3545; font-weight: bold; }
        .role-user { color: #28a745; font-weight: bold; }
        .back-link { text-align: center; margin-top: 20px; }
        .back-link a { color: #667eea; text-decoration: none; }
        
        #qrCodeSection { display: none; margin-top: 20px; padding: 20px; border: 1px solid #ddd; border-radius: 6px; background: #f8f9fa; }
        #qrCode { display: block; margin: 20px auto; max-width: 100%; height: auto; border: 2px solid #ddd; border-radius: 6px; }
        .qr-title { font-size: 1.2em; font-weight: bold; margin-bottom: 10px; text-align: center; color: #333; }
        .qr-description { color: #666; text-align: center; margin-bottom: 15px; font-size: 0.9em; }
        .refresh-button { display: block; width: 200px; margin: 10px auto; padding: 8px 16px; background-color: #28a745; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; }
        .refresh-button:hover { background-color: #218838; }
        
        .test-qr-section { margin-top: 20px; padding: 15px; background-color: #e9ecef; border: 1px solid #ddd; border-radius: 4px; }
        .test-qr-title { font-weight: bold; margin-bottom: 10px; color: #666; text-align: center; }
        .test-qr-button { display: block; width: 200px; margin: 10px auto; padding: 8px 16px; background-color: #17a2b8; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; }
        .test-qr-button:hover { background-color: #138496; }
        .test-qr-result { margin-top: 10px; padding: 10px; border-radius: 4px; }
        .test-qr-success { background-color: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .test-qr-error { background-color: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
        
        .debug-info { background: #f8f9fa; padding: 15px; border: 1px solid #ddd; margin-top: 20px; border-radius: 4px; }
        .json-view { background: #2d2d2d; color: #fff; padding: 15px; border-radius: 4px; margin-top: 10px; overflow-x: auto; font-size: 14px; font-family: monospace; }
        textarea { width: 100%; max-width: 100%; min-height: 60px; margin-top: 5px; padding: 8px; border: 1px solid #ddd; border-radius: 4px; font-family: monospace; font-size: 14px; }
        
        @media (max-width: 768px) {
            body { padding: 10px; }
            .container { padding: 20px; margin: 10px auto; }
            input, button { font-size: 14px; padding: 10px; }
            .json-view { font-size: 12px; }
            textarea { font-size: 12px; }
            .test-qr-button, .refresh-button { width: 100%; }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <h2>🔑 Login</h2>
        <div class=""form-group"">
            <label for=""login"">Login:</label>
            <input type=""text"" id=""login"" name=""login"" required>
        </div>
        <div class=""form-group"">
            <label for=""password"">Password:</label>
            <input type=""password"" id=""password"" name=""password"" required>
        </div>
        <button class=""btn"" onclick=""submitLogin()"">Login</button>
        <div id=""result""></div>
        
        <div id=""qrCodeSection"">
            <div class=""qr-title"">Quick Login QR Code</div>
            <div class=""qr-description"">Scan this QR code with the mobile app to quickly log in next time</div>
            <img id=""qrCode"" alt=""QR Code for quick login"">
            <button class=""refresh-button"" onclick=""refreshQRCode()"">Refresh QR Code</button>
            
            <div class=""debug-info"">
                <h4>QR Code Debug Data</h4>
                <div id=""qrCodeDebugData"" class=""json-view""></div>
            </div>
            
            <div class=""test-qr-section"">
                <div class=""test-qr-title"">Test QR Code Login</div>
                <p>This section simulates scanning the QR code with a mobile device.</p>
                <button class=""test-qr-button"" onclick=""testQRLogin()"">Simulate QR Code Scan</button>
                <div id=""testQrResult"" class=""test-qr-result"" style=""display: none;""></div>
            </div>
        </div>
        
        <div id=""debug-info"" class=""debug-info"" style=""display: none;"">
            <h3>Debug Information</h3>
            <div id=""request-info"">
                <h4>Request</h4>
                <div id=""request-json"" class=""json-view""></div>
            </div>
            <div id=""response-info"">
                <h4>Response</h4>
                <div id=""response-json"" class=""json-view""></div>
            </div>
            <div id=""token-info"">
                <h4>Decoded Token</h4>
                <div id=""token-json"" class=""json-view""></div>
            </div>
        </div>
        
        <div class=""back-link"">
            <a href=""/api/dev/auth"">← Back to Auth Hub</a>
        </div>
    </div>

    <script>
        let authToken = '';
        let lastQrData = null;

        function formatJson(obj) {
            return JSON.stringify(obj, null, 2)
                .replace(/""([^""]+)""/g, '<span style=""color: #9cdcfe;"">""$1""</span>')
                .replace(/"": ""([^""]+)""/g, '"": <span style=""color: #ce9178;"">""$1""</span>')
                .replace(/"": (\d+)/g, '"": <span style=""color: #b5cea8;"">$1</span>')
                .replace(/"": (true|false)/g, '"": <span style=""color: #569cd6;"">$1</span>');
        }

        async function fetchQRCode() {
            try {
                const response = await fetch('/api/v1/auth/qr/generate', {
                    method: 'GET',
                    headers: {
                        'Authorization': `Bearer ${authToken}`,
                        'Content-Type': 'application/json'
                    }
                });

                if (!response.ok) {
                    throw new Error('Failed to generate QR code');
                }

                const data = await response.json();
                document.getElementById('qrCode').src = `data:image/png;base64,${data.qrCode}`;
                document.getElementById('qrCodeSection').style.display = 'block';
                lastQrData = data.rawData;

                const debugQrData = document.getElementById('qrCodeDebugData');
                if (debugQrData) {
                    debugQrData.innerHTML = formatJson({
                        qrCodeBase64: data.qrCode.substring(0, 100) + '...',
                        rawData: data.rawData || 'Not available in production'
                    });
                }
            } catch (error) {
                console.error('Error generating QR code:', error);
            }
        }

        async function testQRLogin() {
            if (!lastQrData) {
                alert('Please generate a QR code first');
                return;
            }

            const resultDiv = document.getElementById('testQrResult');
            resultDiv.style.display = 'block';
            resultDiv.className = 'test-qr-result';

            try {
                // Simulate QR login using the token from the generated QR code
                const response = await fetch('/api/v1/auth/qr/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ token: lastQrData })
                });

                const data = await response.json();
                
                if (response.ok) {
                    resultDiv.className = 'test-qr-result test-qr-success';
                    resultDiv.innerHTML = `<h4>QR Login Successful!</h4><p>Token received: ${data.token.substring(0, 50)}...</p><p>This simulates what would happen when scanning the QR code with a mobile device.</p>`;
                } else {
                    resultDiv.className = 'test-qr-result test-qr-error';
                    resultDiv.innerHTML = `<h4>QR Login Failed</h4><p>Error: ${data.message || 'Unknown error'}</p>`;
                }
            } catch (error) {
                resultDiv.className = 'test-qr-result test-qr-error';
                resultDiv.innerHTML = `<h4>QR Login Error</h4><p>Error: ${error.message}</p>`;
            }
        }

        async function refreshQRCode() {
            if (authToken) {
                await fetchQRCode();
            }
        }

        async function submitLogin() {
            const login = document.getElementById('login').value;
            const password = document.getElementById('password').value;
            const resultDiv = document.getElementById('result');
            const debugInfo = document.getElementById('debug-info');
            const requestJson = document.getElementById('request-json');
            const responseJson = document.getElementById('response-json');
            const tokenJson = document.getElementById('token-json');

            try {
                const requestData = { login, password };
                requestJson.innerHTML = formatJson(requestData);
                debugInfo.style.display = 'block';

                const response = await fetch('/api/v1/auth/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(requestData)
                });

                const data = await response.json();
                responseJson.innerHTML = formatJson(data);

                if (response.ok) {
                    const tokenParts = data.token.split('.');
                    const payload = JSON.parse(atob(tokenParts[1]));
                    tokenJson.innerHTML = formatJson(payload);

                    const role = payload['role'] || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
                    const isAdmin = role === '1' || role === 'Admin';

                    // Save admin token to localStorage for use in registration
                    if (isAdmin) {
                        localStorage.setItem('adminToken', data.token);
                    }

                    resultDiv.innerHTML = `<div class=""result success"">
                        <p>Login successful!</p>
                        <p>Role: <span class=""${isAdmin ? 'role-admin' : 'role-user'}"">${isAdmin ? 'Administrator' : 'Regular User'}</span></p>
                        ${isAdmin ? '<p><em>Admin token saved for registration use</em></p>' : ''}
                        <p>Token:</p>
                        <textarea rows=""3"">${data.token}</textarea>
                    </div>`;

                    authToken = data.token;
                    await fetchQRCode();
                } else {
                    resultDiv.innerHTML = `<div class=""result error"">Error: ${data.message || data.title || 'Login failed'}</div>`;
                    document.getElementById('qrCodeSection').style.display = 'none';
                }
            } catch (error) {
                resultDiv.innerHTML = `<div class=""result error"">Error: ${error.message}</div>`;
                responseJson.innerHTML = formatJson({ error: error.message });
                document.getElementById('qrCodeSection').style.display = 'none';
            }
        }
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }

        [HttpGet("register")]
        [AllowAnonymous]
        public IActionResult Register()
        {
            var devCheck = CheckDevelopmentEnvironment();
            if (devCheck != null) return devCheck;

            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Register - BRU Avtopark Development</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: Arial, sans-serif; line-height: 1.6; padding: 20px; max-width: 1200px; margin: 0 auto; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; }
        .container { background: white; padding: 40px; border-radius: 10px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); margin: 20px auto; width: 100%; max-width: 600px; }
        h2 { text-align: center; color: #333; margin-bottom: 30px; }
        .form-group { margin-bottom: 20px; }
        label { display: block; margin-bottom: 5px; color: #555; font-weight: bold; }
        input[type=""text""], input[type=""email""], input[type=""password""], select { width: 100%; padding: 12px; border: 2px solid #ddd; border-radius: 6px; font-size: 16px; box-sizing: border-box; }
        input[type=""text""]:focus, input[type=""email""]:focus, input[type=""password""]:focus, select:focus { border-color: #667eea; outline: none; box-shadow: 0 0 0 2px rgba(102,126,234,0.25); }
        .btn { width: 100%; padding: 12px; background: #667eea; color: white; border: none; border-radius: 6px; font-size: 16px; cursor: pointer; margin-top: 10px; transition: background-color 0.2s; }
        .btn:hover { background: #5a6fd8; }
        .result { margin-top: 20px; padding: 15px; border-radius: 6px; }
        .success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
        .role-admin { color: #dc3545; font-weight: bold; }
        .role-user { color: #28a745; font-weight: bold; }
        .back-link { text-align: center; margin-top: 20px; }
        .back-link a { color: #667eea; text-decoration: none; }
        
        .admin-token-section { margin-bottom: 20px; padding: 15px; background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 6px; }
        .admin-token-title { font-weight: bold; margin-bottom: 10px; color: #856404; }
        .admin-token-description { color: #856404; font-size: 0.9em; margin-bottom: 10px; }
        
        .debug-info { background: #f8f9fa; padding: 15px; border: 1px solid #ddd; margin-top: 20px; border-radius: 4px; }
        .json-view { background: #2d2d2d; color: #fff; padding: 15px; border-radius: 4px; margin-top: 10px; overflow-x: auto; font-size: 14px; font-family: monospace; }
        textarea { width: 100%; max-width: 100%; min-height: 60px; margin-top: 5px; padding: 8px; border: 1px solid #ddd; border-radius: 4px; font-family: monospace; font-size: 14px; }
        
        @media (max-width: 768px) {
            body { padding: 10px; }
            .container { padding: 20px; margin: 10px auto; }
            input, button, select { font-size: 14px; padding: 10px; }
            .json-view { font-size: 12px; }
            textarea { font-size: 12px; }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <h2>📝 Register New User</h2>
        
        <div class=""admin-token-section"">
            <div class=""admin-token-title"">Admin Token Required</div>
            <div class=""admin-token-description"">Registration requires an admin token. Login as an admin first to get a token, then paste it below.</div>
            <div class=""form-group"">
                <label for=""adminToken"">Admin Token:</label>
                <textarea id=""adminToken"" placeholder=""Paste admin JWT token here..."" rows=""3""></textarea>
            </div>
        </div>
        
        <div class=""form-group"">
            <label for=""username"">Username:</label>
            <input type=""text"" id=""username"" name=""username"" required>
        </div>
        <div class=""form-group"">
            <label for=""email"">Email (optional):</label>
            <input type=""email"" id=""email"" name=""email"">
        </div>
        <div class=""form-group"">
            <label for=""phoneNumber"">Phone Number (optional):</label>
            <input type=""text"" id=""phoneNumber"" name=""phoneNumber"">
        </div>
        <div class=""form-group"">
            <label for=""password"">Password:</label>
            <input type=""password"" id=""password"" name=""password"" required>
        </div>
        <div class=""form-group"">
            <label for=""confirmPassword"">Confirm Password:</label>
            <input type=""password"" id=""confirmPassword"" name=""confirmPassword"" required>
        </div>
        <div class=""form-group"">
            <label for=""role"">Role:</label>
            <select id=""role"" name=""role"" required>
                <option value="""">Select Role</option>
                <option value=""0"">Regular User</option>
                <option value=""1"">Administrator</option>
            </select>
        </div>
        <button class=""btn"" onclick=""submitRegistration()"">Register User</button>
        <div id=""result""></div>
        
        <div id=""debug-info"" class=""debug-info"" style=""display: none;"">
            <h3>Debug Information</h3>
            <div id=""request-info"">
                <h4>Request</h4>
                <div id=""request-json"" class=""json-view""></div>
            </div>
            <div id=""response-info"">
                <h4>Response</h4>
                <div id=""response-json"" class=""json-view""></div>
            </div>
            <div id=""token-info"">
                <h4>Admin Token Decoded</h4>
                <div id=""token-json"" class=""json-view""></div>
            </div>
        </div>
        
        <div class=""back-link"">
            <a href=""/api/dev/auth"">← Back to Auth Hub</a>
        </div>
    </div>

    <script>
        function formatJson(obj) {
            return JSON.stringify(obj, null, 2)
                .replace(/""([^""]+)""/g, '<span style=""color: #9cdcfe;"">""$1""</span>')
                .replace(/"": ""([^""]+)""/g, '"": <span style=""color: #ce9178;"">""$1""</span>')
                .replace(/"": (\d+)/g, '"": <span style=""color: #b5cea8;"">$1</span>')
                .replace(/"": (true|false)/g, '"": <span style=""color: #569cd6;"">$1</span>');
        }

        function decodeToken(token) {
            try {
                const parts = token.split('.');
                if (parts.length !== 3) return null;
                const payload = JSON.parse(atob(parts[1]));
                return payload;
            } catch (error) {
                return null;
            }
        }

        async function submitRegistration() {
            const username = document.getElementById('username').value;
            const email = document.getElementById('email').value;
            const phoneNumber = document.getElementById('phoneNumber').value;
            const password = document.getElementById('password').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            const role = parseInt(document.getElementById('role').value);
            const adminToken = document.getElementById('adminToken').value.trim();
            const resultDiv = document.getElementById('result');
            const debugInfo = document.getElementById('debug-info');
            const requestJson = document.getElementById('request-json');
            const responseJson = document.getElementById('response-json');
            const tokenJson = document.getElementById('token-json');

            // Validation
            if (!username || !password || !confirmPassword || role === '' || !adminToken) {
                resultDiv.innerHTML = '<div class=""result error"">Please fill in all required fields including admin token</div>';
                return;
            }

            if (password !== confirmPassword) {
                resultDiv.innerHTML = '<div class=""result error"">Passwords do not match!</div>';
                return;
            }

            try {
                // Decode and display admin token
                const decodedToken = decodeToken(adminToken);
                if (decodedToken) {
                    tokenJson.innerHTML = formatJson(decodedToken);
                } else {
                    tokenJson.innerHTML = '<span style=""color: #dc3545;"">Invalid token format</span>';
                }

                const requestData = { 
                    login: username, 
                    email: email || null,
                    phoneNumber: phoneNumber || null,
                    password, 
                    role 
                };
                requestJson.innerHTML = formatJson(requestData);
                debugInfo.style.display = 'block';

                const response = await fetch('/api/v1/auth/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${adminToken}`
                    },
                    body: JSON.stringify(requestData)
                });

                const data = await response.json();
                responseJson.innerHTML = formatJson(data);

                if (response.ok) {
                    const roleText = role === 1 ? 'Administrator' : 'Regular User';
                    const roleClass = role === 1 ? 'role-admin' : 'role-user';
                    
                    resultDiv.innerHTML = `<div class=""result success"">
                        <p>Registration successful!</p>
                        <p>User: <strong>${username}</strong></p>
                        <p>Role: <span class=""${roleClass}"">${roleText}</span></p>
                        <p>User ID: ${data.details?.user?.userId || 'Generated'}</p>
                        <p>Created: ${data.details?.user?.createdAt ? new Date(data.details.user.createdAt).toLocaleString() : 'Now'}</p>
                        <p>The user can now login with their credentials.</p>
                    </div>`;

                    // Clear form
                    document.getElementById('username').value = '';
                    document.getElementById('email').value = '';
                    document.getElementById('phoneNumber').value = '';
                    document.getElementById('password').value = '';
                    document.getElementById('confirmPassword').value = '';
                    document.getElementById('role').value = '';
                } else {
                    let errorMessage = data.message || 'Registration failed';
                    if (data.details?.error) {
                        errorMessage += ': ' + data.details.error;
                    }
                    if (data.details?.modelState) {
                        errorMessage += '<br><br>Validation errors:<br>';
                        Object.keys(data.details.modelState).forEach(field => {
                            errorMessage += `• ${field}: ${data.details.modelState[field].join(', ')}<br>`;
                        });
                    }
                    resultDiv.innerHTML = `<div class=""result error"">${errorMessage}</div>`;
                }
            } catch (error) {
                resultDiv.innerHTML = `<div class=""result error"">Network error: ${error.message}</div>`;
                responseJson.innerHTML = formatJson({ error: error.message });
            }
        }

        // Auto-fill admin token from localStorage if available (from login page)
        window.addEventListener('load', function() {
            const savedToken = localStorage.getItem('adminToken');
            if (savedToken) {
                document.getElementById('adminToken').value = savedToken;
            }
        });

        // Save admin token to localStorage when entered
        document.getElementById('adminToken').addEventListener('input', function() {
            const token = this.value.trim();
            if (token) {
                localStorage.setItem('adminToken', token);
            }
        });
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }

        [HttpGet("qr")]
        [AllowAnonymous]
        public IActionResult QRAuth()
        {
            var devCheck = CheckDevelopmentEnvironment();
            if (devCheck != null) return devCheck;

            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>QR Authentication - BRU Avtopark Development</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; padding: 40px; background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); min-height: 100vh; }
        .container { max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 10px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); }
        h2 { text-align: center; color: #333; margin-bottom: 30px; }
        .qr-section { text-align: center; margin: 30px 0; }
        .qr-code { width: 200px; height: 200px; border: 2px solid #ddd; margin: 20px auto; display: flex; align-items: center; justify-content: center; background: #f8f9fa; }
        .btn { padding: 12px 24px; background: #11998e; color: white; border: none; border-radius: 6px; font-size: 16px; cursor: pointer; margin: 10px; }
        .btn:hover { background: #0d7377; }
        .btn-secondary { background: #6c757d; }
        .btn-secondary:hover { background: #545b62; }
        .form-group { margin-bottom: 15px; }
        .form-group label { display: block; margin-bottom: 5px; font-weight: bold; color: #333; }
        .form-group input { width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; font-size: 14px; }
        .form-group input:focus { outline: none; border-color: #11998e; box-shadow: 0 0 5px rgba(17, 153, 142, 0.3); }
        .result { margin-top: 20px; padding: 15px; border-radius: 6px; }
        .success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
        .info { background: #d1ecf1; color: #0c5460; border: 1px solid #bee5eb; }
        .back-link { text-align: center; margin-top: 20px; }
        .back-link a { color: #11998e; text-decoration: none; }
        .status-indicator { display: inline-block; width: 10px; height: 10px; border-radius: 50%; margin-right: 8px; }
        .status-waiting { background: #ffc107; }
        .status-success { background: #28a745; }
        .status-error { background: #dc3545; }
    </style>
</head>
<body>
    <div class=""container"">
        <h2>📱 QR Code Authentication</h2>
        
        <div class=""qr-section"">
            <h3>User Credentials</h3>
            <div class=""form-group"">
                <label for=""qrUsername"">Username:</label>
                <input type=""text"" id=""qrUsername"" placeholder=""Enter username"" value=""admin"">
            </div>
            <div class=""form-group"">
                <label for=""qrPassword"">Password:</label>
                <input type=""password"" id=""qrPassword"" placeholder=""Enter password"" value=""admin"">
            </div>
            
            <h3>QR Code Generation</h3>
            <div class=""qr-code"" id=""qrCode"">
                <span>Enter credentials and click ""Generate QR"" to start</span>
            </div>
            <div>
                <button class=""btn"" onclick=""generateQR()"">Generate QR Code</button>
                <button class=""btn btn-secondary"" onclick=""checkStatus()"">Check Status</button>
            </div>
        </div>

        <div id=""result""></div>
        
        <div class=""result info"">
            <strong>QR Authentication Flow:</strong><br>
            1. Enter username and password<br>
            2. Authenticate user credentials<br>
            3. Generate QR code for authenticated user<br>
            4. Test QR login simulation<br>
            5. Receive authentication token
        </div>

        <div class=""back-link"">
            <a href=""/api/dev/auth"">← Back to Auth Hub</a>
        </div>
    </div>

    <script>
        let currentQRId = null;
        let statusCheckInterval = null;

        async function generateQR() {
            const resultDiv = document.getElementById('result');
            const qrCodeDiv = document.getElementById('qrCode');
            const username = document.getElementById('qrUsername').value;
            const password = document.getElementById('qrPassword').value;
            
            if (!username || !password) {
                resultDiv.innerHTML = '<div class=""result error"">Please enter both username and password</div>';
                return;
            }
            
            try {
                // First authenticate the user to verify credentials
                const authResponse = await fetch('/api/v1/auth/login', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ login: username, password: password })
                });
                
                if (!authResponse.ok) {
                    const authError = await authResponse.json();
                    resultDiv.innerHTML = '<div class=""result error"">Authentication failed: ' + (authError.message || 'Invalid credentials') + '</div>';
                    return;
                }
                
                const authData = await authResponse.json();
                resultDiv.innerHTML = '<div class=""result success"">User authenticated successfully. Generating QR code...</div>';
                
                // Now generate QR code for the authenticated user
                const response = await fetch('/api/v1/auth/qr/direct/generate?username=' + encodeURIComponent(username) + '&deviceType=desktop', {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                });
                
                const data = await response.json();
                
                if (response.ok) {
                    currentQRId = data.rawData; // Use rawData as the token for testing
                    qrCodeDiv.innerHTML = '<img src=""data:image/png;base64,' + data.qrCode + '"" alt=""QR Code"" style=""max-width: 100%; height: auto;"">';
                    resultDiv.innerHTML = '<div class=""result success""><span class=""status-indicator status-waiting""></span>QR Code generated for user: ' + username + '. You can test login by clicking ""Test QR Login"" below.</div>';
                    
                    // Add a test button for direct QR login
                    resultDiv.innerHTML += '<br><button onclick=""testDirectQRLogin()"" style=""background: #007bff; color: white; border: none; padding: 10px 20px; border-radius: 5px; cursor: pointer; margin-top: 10px;"">Test QR Login</button>';
                } else {
                    resultDiv.innerHTML = '<div class=""result error"">Failed to generate QR code: ' + (data.message || 'Unknown error') + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Network error: ' + error.message + '</div>';
            }
        }

        async function testDirectQRLogin() {
            if (!currentQRId) {
                document.getElementById('result').innerHTML = '<div class=""result error"">No QR code generated yet</div>';
                return;
            }

            const resultDiv = document.getElementById('result');
            const username = document.getElementById('qrUsername').value;
            
            try {
                resultDiv.innerHTML = '<div class=""result info"">Simulating QR code scan and login...</div>';
                
                // The DirectQRLogin endpoint doesn't need DeviceId in the request body
                // The device ID is extracted from the token itself by the service
                // Use 'desktop' to match the device type used during QR generation
                const response = await fetch('/api/v1/auth/qr/direct/login', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        Token: currentQRId,
                        DeviceType: 'desktop',
                        IsDesktopLogin: true
                    })
                });
                
                const data = await response.json();
                
                if (response.ok) {
                    // Decode the JWT token to show user info
                    let userInfo = '';
                    if (data.token) {
                        try {
                            const tokenParts = data.token.split('.');
                            const payload = JSON.parse(atob(tokenParts[1]));
                            const role = payload['role'] || '0';
                            const isAdmin = role === '1';
                            userInfo = '<br><strong>User:</strong> ' + (payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || payload.name || username) + 
                                      '<br><strong>Role:</strong> ' + (isAdmin ? 'Administrator' : 'Regular User') +
                                      '<br><strong>Device ID:</strong> ' + (data.deviceId || deviceId) +
                                      '<br><strong>Token expires:</strong> ' + new Date(payload.exp * 1000).toLocaleString();
                        } catch (e) {
                            userInfo = '<br><strong>Token:</strong> ' + data.token.substring(0, 50) + '...' +
                                      '<br><strong>Device ID:</strong> ' + (data.deviceId || deviceId);
                        }
                    }
                    
                    resultDiv.innerHTML = '<div class=""result success""><span class=""status-indicator status-success""></span>' +
                        '<strong>QR Authentication Successful!</strong>' + userInfo + 
                        '<br><br><em>This simulates what would happen when scanning the QR code with a mobile device.</em></div>';
                } else {
                    let errorDetails = '';
                    if (response.status === 400) {
                        errorDetails = '<br><small>This might be due to token format or validation issues. Check server logs for details.</small>';
                    }
                    resultDiv.innerHTML = '<div class=""result error""><span class=""status-indicator status-error""></span>QR login failed: ' + (data.message || 'Unknown error') + errorDetails + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Network error: ' + error.message + '</div>';
            }
        }

        async function checkStatus() {
            // This function is kept for compatibility but the direct login approach doesn't need polling
            const resultDiv = document.getElementById('result');
            resultDiv.innerHTML = '<div class=""result info""><span class=""status-indicator status-waiting""></span>Use ""Test QR Login"" button to simulate scanning the QR code</div>';
        }

        window.addEventListener('beforeunload', function() {
            if (statusCheckInterval) {
                clearInterval(statusCheckInterval);
            }
        });
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }
        [HttpGet("webauthn")]
        [AllowAnonymous]
        public IActionResult WebAuthn()
        {
            var devCheck = CheckDevelopmentEnvironment();
            if (devCheck != null) return devCheck;

            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>WebAuthn - BRU Avtopark Development</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; padding: 40px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; }
        .container { max-width: 700px; margin: 0 auto; background: white; padding: 40px; border-radius: 10px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); }
        h2 { text-align: center; color: #333; margin-bottom: 30px; }
        .section { margin: 30px 0; padding: 20px; border: 1px solid #ddd; border-radius: 8px; }
        .section h3 { margin-top: 0; color: #667eea; }
        .form-group { margin-bottom: 20px; }
        label { display: block; margin-bottom: 5px; color: #555; font-weight: bold; }
        input[type=""text""] { width: 100%; padding: 12px; border: 2px solid #ddd; border-radius: 6px; font-size: 16px; box-sizing: border-box; }
        input[type=""text""]:focus { border-color: #667eea; outline: none; }
        .btn { padding: 12px 24px; background: #667eea; color: white; border: none; border-radius: 6px; font-size: 16px; cursor: pointer; margin: 10px 5px; }
        .btn:hover { background: #5a6fd8; }
        .btn-success { background: #28a745; }
        .btn-success:hover { background: #218838; }
        .btn-danger { background: #dc3545; }
        .btn-danger:hover { background: #c82333; }
        .result { margin-top: 20px; padding: 15px; border-radius: 6px; }
        .success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
        .info { background: #d1ecf1; color: #0c5460; border: 1px solid #bee5eb; }
        .back-link { text-align: center; margin-top: 20px; }
        .back-link a { color: #667eea; text-decoration: none; }
        .credential-list { margin-top: 15px; }
        .credential-item { background: #f8f9fa; padding: 10px; margin: 5px 0; border-radius: 4px; display: flex; justify-content: space-between; align-items: center; }
    </style>
</head>
<body>
    <div class=""container"">
        <h2>🔐 WebAuthn (FIDO2) Authentication</h2>
        
        <div class=""section"">
            <h3>Register New Credential</h3>
            <div class=""form-group"">
                <label for=""registerUsername"">Username:</label>
                <input type=""text"" id=""registerUsername"" placeholder=""Enter username"" required>
            </div>
            <button class=""btn"" onclick=""registerCredential()"">Register WebAuthn Credential</button>
            <div id=""registerResult""></div>
        </div>

        <div class=""section"">
            <h3>Authenticate</h3>
            <div class=""form-group"">
                <label for=""authUsername"">Username:</label>
                <input type=""text"" id=""authUsername"" placeholder=""Enter username"" required>
            </div>
            <button class=""btn btn-success"" onclick=""authenticate()"">Authenticate with WebAuthn</button>
            <div id=""authResult""></div>
        </div>

        <div class=""section"">
            <h3>Credential Management</h3>
            <button class=""btn"" onclick=""listCredentials()"">List My Credentials</button>
            <div id=""credentialsList""></div>
        </div>

        <div class=""result info"">
            <strong>WebAuthn Requirements:</strong><br>
            • HTTPS connection (or localhost)<br>
            • Compatible browser (Chrome, Firefox, Safari, Edge)<br>
            • Authenticator device (fingerprint, face, security key, etc.)
        </div>

        <div class=""back-link"">
            <a href=""/api/dev/auth"">← Back to Auth Hub</a>
        </div>
    </div>

    <script>
        if (!window.PublicKeyCredential) {
            document.body.innerHTML = '<div style=""text-align: center; padding: 50px; color: red;""><h2>WebAuthn Not Supported</h2><p>Your browser does not support WebAuthn. Please use a modern browser.</p></div>';
        }

        async function registerCredential() {
            const username = document.getElementById('registerUsername').value;
            const resultDiv = document.getElementById('registerResult');
            
            if (!username) {
                resultDiv.innerHTML = '<div class=""result error"">Please enter a username</div>';
                return;
            }

            const adminToken = localStorage.getItem('adminToken');
            if (!adminToken) {
                resultDiv.innerHTML = '<div class=""result error"">No admin token found. Please login first using the <a href=""/dev/auth/login"">Login page</a> to get an admin token.</div>';
                return;
            }

            try {
                const optionsResponse = await fetch('/api/v1/auth/webauthn/register/begin', {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Authorization': 'Bearer ' + (localStorage.getItem('adminToken') || '')
                    },
                    body: JSON.stringify({ displayName: username })
                });

                if (!optionsResponse.ok) {
                    const errorData = await optionsResponse.json().catch(() => ({ error: 'Unknown error' }));
                    throw new Error(`Failed to get registration options: ${errorData.error || errorData.message || 'Unknown error'} (Status: ${optionsResponse.status})`);
                }

                const options = await optionsResponse.json();
                
                options.challenge = base64ToArrayBuffer(options.challenge);
                options.user.id = base64ToArrayBuffer(options.user.id);

                const credential = await navigator.credentials.create({
                    publicKey: options
                });

                const registerResponse = await fetch('/api/v1/auth/webauthn/register/complete', {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Authorization': 'Bearer ' + (localStorage.getItem('adminToken') || '')
                    },
                    body: JSON.stringify({
                        response: JSON.stringify({
                            id: credential.id,
                            rawId: arrayBufferToBase64(credential.rawId),
                            response: {
                                attestationObject: arrayBufferToBase64(credential.response.attestationObject),
                                clientDataJSON: arrayBufferToBase64(credential.response.clientDataJSON)
                            },
                            type: credential.type
                        }),
                        friendlyName: username + ' - ' + new Date().toLocaleDateString()
                    })
                });

                const result = await registerResponse.json();
                
                if (registerResponse.ok) {
                    resultDiv.innerHTML = '<div class=""result success"">WebAuthn credential registered successfully!</div>';
                } else {
                    resultDiv.innerHTML = '<div class=""result error"">Registration failed: ' + (result.message || 'Unknown error') + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Registration error: ' + error.message + '</div>';
            }
        }

        async function authenticate() {
            const username = document.getElementById('authUsername').value;
            const resultDiv = document.getElementById('authResult');
            
            if (!username) {
                resultDiv.innerHTML = '<div class=""result error"">Please enter a username</div>';
                return;
            }

            try {
                const optionsResponse = await fetch('/api/v1/auth/webauthn/login/begin', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ username })
                });

                if (!optionsResponse.ok) {
                    throw new Error('Failed to get authentication options');
                }

                const options = await optionsResponse.json();
                
                options.challenge = base64ToArrayBuffer(options.challenge);
                if (options.allowCredentials) {
                    options.allowCredentials = options.allowCredentials.map(cred => ({
                        ...cred,
                        id: base64ToArrayBuffer(cred.id)
                    }));
                }

                const assertion = await navigator.credentials.get({
                    publicKey: options
                });

                const authResponse = await fetch('/api/v1/auth/webauthn/login/complete', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        response: JSON.stringify({
                            id: assertion.id,
                            rawId: arrayBufferToBase64(assertion.rawId),
                            response: {
                                authenticatorData: arrayBufferToBase64(assertion.response.authenticatorData),
                                clientDataJSON: arrayBufferToBase64(assertion.response.clientDataJSON),
                                signature: arrayBufferToBase64(assertion.response.signature),
                                userHandle: assertion.response.userHandle ? arrayBufferToBase64(assertion.response.userHandle) : null
                            },
                            type: assertion.type
                        })
                    })
                });

                const result = await authResponse.json();
                
                if (authResponse.ok) {
                    resultDiv.innerHTML = '<div class=""result success"">Authentication successful! Token: ' + (result.token || 'Generated') + '</div>';
                } else {
                    resultDiv.innerHTML = '<div class=""result error"">Authentication failed: ' + (result.message || 'Unknown error') + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Authentication error: ' + error.message + '</div>';
            }
        }

        async function listCredentials() {
            const resultDiv = document.getElementById('credentialsList');
            
            try {
                const response = await fetch('/api/v1/auth/webauthn/credentials', {
                    headers: {
                        'Authorization': 'Bearer ' + (localStorage.getItem('adminToken') || '')
                    }
                });
                const data = await response.json();
                
                if (response.ok && data.credentials) {
                    let html = '<div class=""credential-list"">';
                    data.credentials.forEach(cred => {
                        html += '<div class=""credential-item"">';
                        html += '<span>' + cred.name + ' (Created: ' + new Date(cred.createdAt).toLocaleDateString() + ')</span>';
                        html += '<button class=""btn btn-danger"" onclick=""deleteCredential(\'' + cred.id + '\')"">Delete</button>';
                        html += '</div>';
                    });
                    html += '</div>';
                    resultDiv.innerHTML = html;
                } else {
                    resultDiv.innerHTML = '<div class=""result info"">No credentials found</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Failed to load credentials: ' + error.message + '</div>';
            }
        }

        async function deleteCredential(credentialId) {
            try {
                const response = await fetch('/api/v1/auth/webauthn/credentials/' + credentialId, {
                    method: 'DELETE',
                    headers: {
                        'Authorization': 'Bearer ' + (localStorage.getItem('adminToken') || '')
                    }
                });
                
                if (response.ok) {
                    listCredentials();
                } else {
                    alert('Failed to delete credential');
                }
            } catch (error) {
                alert('Error deleting credential: ' + error.message);
            }
        }

        function base64ToArrayBuffer(base64) {
            const binaryString = window.atob(base64);
            const bytes = new Uint8Array(binaryString.length);
            for (let i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            return bytes.buffer;
        }

        function arrayBufferToBase64(buffer) {
            const bytes = new Uint8Array(buffer);
            let binary = '';
            for (let i = 0; i < bytes.byteLength; i++) {
                binary += String.fromCharCode(bytes[i]);
            }
            return window.btoa(binary);
        }
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }

        [HttpGet("windows")]
        [AllowAnonymous]
        public IActionResult WindowsAuth()
        {
            var devCheck = CheckDevelopmentEnvironment();
            if (devCheck != null) return devCheck;

            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Windows Authentication - BRU Avtopark Development</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 0; padding: 40px; background: linear-gradient(135deg, #0f4c75 0%, #3282b8 100%); min-height: 100vh; }
        .container { max-width: 700px; margin: 0 auto; background: white; padding: 40px; border-radius: 10px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); }
        h2 { text-align: center; color: #333; margin-bottom: 30px; }
        .section { margin: 30px 0; padding: 20px; border: 1px solid #ddd; border-radius: 8px; }
        .section h3 { margin-top: 0; color: #0f4c75; }
        .form-group { margin-bottom: 20px; }
        label { display: block; margin-bottom: 5px; color: #555; font-weight: bold; }
        input[type=""text""], input[type=""password""] { width: 100%; padding: 12px; border: 2px solid #ddd; border-radius: 6px; font-size: 16px; box-sizing: border-box; }
        input[type=""text""]:focus, input[type=""password""]:focus { border-color: #3282b8; outline: none; }
        .btn { padding: 12px 24px; background: #0f4c75; color: white; border: none; border-radius: 6px; font-size: 16px; cursor: pointer; margin: 10px 5px; }
        .btn:hover { background: #0a3a5c; }
        .btn-success { background: #28a745; }
        .btn-success:hover { background: #218838; }
        .btn-info { background: #17a2b8; }
        .btn-info:hover { background: #138496; }
        .btn-warning { background: #ffc107; color: #212529; }
        .btn-warning:hover { background: #e0a800; }
        .result { margin-top: 20px; padding: 15px; border-radius: 6px; }
        .success { background: #d4edda; color: #155724; border: 1px solid #c3e6cb; }
        .error { background: #f8d7da; color: #721c24; border: 1px solid #f5c6cb; }
        .info { background: #d1ecf1; color: #0c5460; border: 1px solid #bee5eb; }
        .warning { background: #fff3cd; color: #856404; border: 1px solid #ffeaa7; }
        .back-link { text-align: center; margin-top: 20px; }
        .back-link a { color: #0f4c75; text-decoration: none; }
        .user-info { background: #f8f9fa; padding: 15px; border-radius: 6px; margin: 15px 0; }
        .debug-info { background: #f8f9fa; padding: 15px; border: 1px solid #ddd; margin-top: 20px; border-radius: 4px; }
        .json-view { background: #2d2d2d; color: #fff; padding: 15px; border-radius: 4px; margin-top: 10px; overflow-x: auto; font-size: 14px; font-family: monospace; }
        textarea { width: 100%; max-width: 100%; min-height: 60px; margin-top: 5px; padding: 8px; border: 1px solid #ddd; border-radius: 4px; font-family: monospace; font-size: 14px; }
    </style>
</head>
<body>
    <div class=""container"">
        <h2>🖥️ Windows Authentication</h2>
        
        <div class=""section"">
            <h3>Browser Windows User Detection</h3>
            <p>This section detects Windows user information from the browser environment.</p>
            <button class=""btn btn-info"" onclick=""getCurrentUser()"">Detect Current Windows User</button>
            <div id=""currentUserResult""></div>
        </div>

        <div class=""section"">
            <h3>Windows Authentication Login</h3>
            <p>Test Windows authentication using the actual Windows login endpoint.</p>
            <button class=""btn btn-success"" onclick=""testWindowsLogin()"">Test Windows Login</button>
            <div id=""windowsLoginResult""></div>
        </div>

        <div class=""section"">
            <h3>Account Linking</h3>
            <div class=""form-group"">
                <label for=""linkUsername"">Application Username:</label>
                <input type=""text"" id=""linkUsername"" placeholder=""app username"" required>
            </div>
            <div class=""form-group"">
                <label for=""windowsAccount"">Windows Account:</label>
                <input type=""text"" id=""windowsAccount"" placeholder=""DOMAIN\\username"" required>
            </div>
            <button class=""btn btn-success"" onclick=""linkAccount()"">Link Windows Account</button>
            <div id=""linkResult""></div>
        </div>

        <div class=""section"">
            <h3>Check Link Status</h3>
            <p>Check if your current account has Windows authentication linked.</p>
            <button class=""btn btn-info"" onclick=""checkLinkStatus()"">Check Link Status</button>
            <div id=""linkStatusResult""></div>
        </div>

        <div class=""section"">
            <h3>Complete Account Linking</h3>
            <div class=""form-group"">
                <label for=""completeUsername"">Username:</label>
                <input type=""text"" id=""completeUsername"" placeholder=""username"" required>
            </div>
            <div class=""form-group"">
                <label for=""linkToken"">Verification Token:</label>
                <input type=""text"" id=""linkToken"" placeholder=""verification token"" required>
            </div>
            <button class=""btn btn-warning"" onclick=""completeLinking()"">Complete Linking</button>
            <div id=""completeLinkResult""></div>
        </div>

        <div class=""section"">
            <h3>Unlink Account</h3>
            <p>Remove Windows authentication linking from your account.</p>
            <button class=""btn btn-warning"" onclick=""unlinkAccount()"">Unlink Windows Account</button>
            <div id=""unlinkResult""></div>
        </div>

        <div id=""debug-info"" class=""debug-info"" style=""display: none;"">
            <h3>Debug Information</h3>
            <div id=""request-info"">
                <h4>Last Request</h4>
                <div id=""request-json"" class=""json-view""></div>
            </div>
            <div id=""response-info"">
                <h4>Last Response</h4>
                <div id=""response-json"" class=""json-view""></div>
            </div>
        </div>

        <div class=""result warning"">
            <strong>Windows Authentication Notes:</strong><br>
            • Requires Windows environment or domain setup<br>
            • May need IIS with Windows Authentication enabled<br>
            • NTLM/Kerberos protocols for domain authentication<br>
            • Account linking allows mapping Windows users to app users<br>
            • Some features require authentication tokens
        </div>

        <div class=""back-link"">
            <a href=""/api/dev/auth"">← Back to Auth Hub</a>
        </div>
    </div>

    <script>
        function formatJson(obj) {
            return JSON.stringify(obj, null, 2)
                .replace(/""([^""]+)""/g, '<span style=""color: #9cdcfe;"">""$1""</span>')
                .replace(/"": ""([^""]+)""/g, '"": <span style=""color: #ce9178;"">""$1""</span>')
                .replace(/"": (\d+)/g, '"": <span style=""color: #b5cea8;"">$1</span>')
                .replace(/"": (true|false)/g, '"": <span style=""color: #569cd6;"">$1</span>');
        }

        function updateDebugInfo(requestData, responseData) {
            const debugInfo = document.getElementById('debug-info');
            const requestJson = document.getElementById('request-json');
            const responseJson = document.getElementById('response-json');
            
            if (requestData) {
                requestJson.innerHTML = formatJson(requestData);
            }
            if (responseData) {
                responseJson.innerHTML = formatJson(responseData);
            }
            debugInfo.style.display = 'block';
        }

        async function getCurrentUser() {
            const resultDiv = document.getElementById('currentUserResult');
            
            try {
                // Use browser APIs to detect Windows user information
                let userInfo = {
                    platform: navigator.platform,
                    userAgent: navigator.userAgent,
                    language: navigator.language,
                    cookieEnabled: navigator.cookieEnabled,
                    onLine: navigator.onLine
                };

                // Try to get more specific Windows information
                if (navigator.platform.includes('Win')) {
                    userInfo.isWindows = true;
                    userInfo.windowsVersion = 'Detected';
                    
                    // Try to get domain information from user agent or other sources
                    if (navigator.userAgent.includes('Windows NT')) {
                        const match = navigator.userAgent.match(/Windows NT ([0-9.]+)/);
                        if (match) {
                            userInfo.windowsVersion = 'Windows NT ' + match[1];
                        }
                    }
                } else {
                    userInfo.isWindows = false;
                }

                updateDebugInfo(null, userInfo);

                let html = '<div class=""user-info"">';
                html += '<strong>Platform:</strong> ' + userInfo.platform + '<br>';
                html += '<strong>Is Windows:</strong> ' + (userInfo.isWindows ? 'Yes' : 'No') + '<br>';
                if (userInfo.isWindows) {
                    html += '<strong>Windows Version:</strong> ' + userInfo.windowsVersion + '<br>';
                }
                html += '<strong>Language:</strong> ' + userInfo.language + '<br>';
                html += '<strong>Online:</strong> ' + (userInfo.onLine ? 'Yes' : 'No') + '<br>';
                html += '<em>Note: Browser security limits prevent direct Windows user detection. Use Windows Login for authentication.</em>';
                html += '</div>';
                
                resultDiv.innerHTML = html;
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Error detecting user: ' + error.message + '</div>';
                updateDebugInfo(null, { error: error.message });
            }
        }

        async function testWindowsLogin() {
            const resultDiv = document.getElementById('windowsLoginResult');
            
            try {
                resultDiv.innerHTML = '<div class=""result info"">Attempting Windows authentication...</div>';
                
                // Try the windows-login endpoint
                const response = await fetch('/api/v1/auth/windows/windows-login', {
                    method: 'GET',
                    credentials: 'include' // Include credentials for Windows auth
                });
                
                const data = await response.json();
                updateDebugInfo({ endpoint: '/api/v1/auth/windows/windows-login', method: 'GET' }, data);
                
                if (response.ok) {
                    let html = '<div class=""result success"">';
                    html += '<p>Windows authentication successful!</p>';
                    if (data.user) {
                        html += '<p><strong>User ID:</strong> ' + data.user.userId + '</p>';
                        html += '<p><strong>Login:</strong> ' + data.user.login + '</p>';
                        html += '<p><strong>Role:</strong> ' + (data.user.role === 1 ? 'Administrator' : 'User') + '</p>';
                        html += '<p><strong>Needs Linking:</strong> ' + (data.user.doesWindowsAccountNeedLinking ? 'Yes' : 'No') + '</p>';
                    }
                    if (data.token) {
                        html += '<p><strong>Token:</strong></p>';
                        html += '<textarea rows=""3"">' + data.token + '</textarea>';
                    }
                    html += '</div>';
                    resultDiv.innerHTML = html;
                } else {
                    resultDiv.innerHTML = '<div class=""result error"">Windows authentication failed: ' + (data.message || 'Unknown error') + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Network error: ' + error.message + '</div>';
                updateDebugInfo({ endpoint: '/api/v1/auth/windows/windows-login', method: 'GET' }, { error: error.message });
            }
        }

        async function linkAccount() {
            const appUsername = document.getElementById('linkUsername').value;
            const windowsAccount = document.getElementById('windowsAccount').value;
            const resultDiv = document.getElementById('linkResult');
            
            if (!appUsername || !windowsAccount) {
                resultDiv.innerHTML = '<div class=""result error"">Please enter both application username and Windows account</div>';
                return;
            }

            try {
                const requestData = { 
                    username: appUsername, 
                    windowsUsername: windowsAccount 
                };

                const response = await fetch('/api/v1/auth/windows/link-windows-account', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(requestData)
                });
                
                const data = await response.json();
                updateDebugInfo(requestData, data);
                
                if (response.ok) {
                    let html = '<div class=""result success"">';
                    html += '<p>Account linking initiated successfully!</p>';
                    if (data.verificationToken) {
                        html += '<p><strong>Verification Token:</strong></p>';
                        html += '<textarea rows=""2"">' + data.verificationToken + '</textarea>';
                        html += '<p><em>Use this token in the ""Complete Account Linking"" section above.</em></p>';
                    }
                    html += '<p>' + (data.message || 'Please complete the linking process.') + '</p>';
                    html += '</div>';
                    resultDiv.innerHTML = html;
                } else {
                    resultDiv.innerHTML = '<div class=""result error"">Account linking failed: ' + (data.message || 'Unknown error') + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Network error: ' + error.message + '</div>';
                updateDebugInfo({ username: appUsername, windowsUsername: windowsAccount }, { error: error.message });
            }
        }

        async function checkLinkStatus() {
            const resultDiv = document.getElementById('linkStatusResult');
            
            try {
                const response = await fetch('/api/v1/auth/windows/check-windows-link-status', {
                    method: 'GET',
                    headers: {
                        'Authorization': 'Bearer ' + (localStorage.getItem('authToken') || '')
                    }
                });
                
                const data = await response.json();
                updateDebugInfo({ endpoint: '/api/v1/auth/windows/check-windows-link-status' }, data);
                
                if (response.ok) {
                    let html = '<div class=""user-info"">';
                    html += '<strong>Is Linked:</strong> ' + (data.isLinked ? 'Yes' : 'No') + '<br>';
                    if (data.windowsIdentity) {
                        html += '<strong>Windows Identity:</strong> ' + data.windowsIdentity + '<br>';
                    }
                    html += '<strong>Needs Linking:</strong> ' + (data.needsLinking ? 'Yes' : 'No') + '<br>';
                    html += '</div>';
                    resultDiv.innerHTML = html;
                } else {
                    resultDiv.innerHTML = '<div class=""result error"">Failed to check link status: ' + (data.message || 'Unknown error') + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Network error: ' + error.message + '</div>';
                updateDebugInfo({ endpoint: '/api/v1/auth/windows/check-windows-link-status' }, { error: error.message });
            }
        }

        async function completeLinking() {
            const username = document.getElementById('completeUsername').value;
            const token = document.getElementById('linkToken').value;
            const resultDiv = document.getElementById('completeLinkResult');
            
            if (!username || !token) {
                resultDiv.innerHTML = '<div class=""result error"">Please enter both username and verification token</div>';
                return;
            }

            try {
                const requestData = { 
                    username: username, 
                    token: token 
                };

                const response = await fetch('/api/v1/auth/windows/complete-windows-link', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(requestData)
                });
                
                const data = await response.json();
                updateDebugInfo(requestData, data);
                
                if (response.ok) {
                    let html = '<div class=""result success"">';
                    html += '<p>Account linking completed successfully!</p>';
                    if (data.username) {
                        html += '<p><strong>Username:</strong> ' + data.username + '</p>';
                    }
                    if (data.windowsIdentity) {
                        html += '<p><strong>Windows Identity:</strong> ' + data.windowsIdentity + '</p>';
                    }
                    html += '</div>';
                    resultDiv.innerHTML = html;
                } else {
                    resultDiv.innerHTML = '<div class=""result error"">Linking completion failed: ' + (data.message || 'Unknown error') + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Network error: ' + error.message + '</div>';
                updateDebugInfo({ username: username, token: token }, { error: error.message });
            }
        }

        async function unlinkAccount() {
            const resultDiv = document.getElementById('unlinkResult');
            
            try {
                const response = await fetch('/api/v1/auth/windows/unlink-windows-account', {
                    method: 'POST',
                    headers: {
                        'Authorization': 'Bearer ' + (localStorage.getItem('authToken') || ''),
                        'Content-Type': 'application/json'
                    }
                });
                
                const data = await response.json();
                updateDebugInfo({ endpoint: '/api/v1/auth/windows/unlink-windows-account' }, data);
                
                if (response.ok) {
                    resultDiv.innerHTML = '<div class=""result success"">Windows account unlinked successfully!</div>';
                } else {
                    resultDiv.innerHTML = '<div class=""result error"">Unlinking failed: ' + (data.message || 'Unknown error') + '</div>';
                }
            } catch (error) {
                resultDiv.innerHTML = '<div class=""result error"">Network error: ' + error.message + '</div>';
                updateDebugInfo({ endpoint: '/api/v1/auth/windows/unlink-windows-account' }, { error: error.message });
            }
        }

        // Auto-detect Windows user on page load
        window.addEventListener('load', function() {
            getCurrentUser();
        });
    </script>
</body>
</html>";

            return Content(html, "text/html");
        }

        [HttpGet("status")]
        [AllowAnonymous]
        public async Task<IActionResult> Status()
        {
            var devCheck = CheckDevelopmentEnvironment();
            if (devCheck != null) return devCheck;

            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var userCount = canConnect ? await _context.Users.CountAsync() : 0;

                var status = new
                {
                    Environment = _environment.EnvironmentName,
                    IsDevelopment = _environment.IsDevelopment(),
                    DatabaseConnected = canConnect,
                    UserCount = userCount,
                    ServerTime = DateTime.Now,
                    Host = Request.Host.ToString(),
                    Scheme = Request.Scheme,
                    AvailableEndpoints = new[]
                    {
                        "/api/dev/auth - Development Hub",
                        "/api/dev/auth/login - Login Test Page",
                        "/api/dev/auth/register - Registration Test Page", 
                        "/api/dev/auth/qr - QR Authentication Test",
                        "/api/dev/auth/webauthn - WebAuthn Test",
                        "/api/dev/auth/windows - Windows Auth Test",
                        "/api/dev/auth/status - This status endpoint"
                    }
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking development status");
                return StatusCode(500, new { error = "Failed to check status", message = ex.Message });
            }
        }
    }
}