using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Serilog;
using NLog;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public class ApiClientService
    {
        private static ApiClientService _instance;
        private static readonly object _lock = new object();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string _authToken;
        private int? _userRole;
        private string _userName;

        private Timer _refreshTimer;
        private const int RefreshIntervalMinutes = 25; // Token refresh interval

        public static ApiClientService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null) 
                        {
                           _instance = new ApiClientService();
                        }
                    }
                }
                return _instance;
            }
        }

        private ApiClientService()
        {
        }

        public string AuthToken
        {
            get { return _authToken; }
            set
            {
                _authToken = value;
                ParseTokenAndStoreInfo(value);
                var handler = OnAuthTokenChanged;
                if (handler != null)
                {
                    handler(this, value);
                }
            }
        }

        public event Action<object, string> OnAuthTokenChanged;

        public HttpClient CreateClient()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5000/api/")
            };

            if (!string.IsNullOrEmpty(_authToken))
            {
                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _authToken);
            }

            // Set the default request headers to accept UTF-8 encoded responses
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json", 1.0));
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));

            return client;
        }

        public int? UserRole
        {
            get { return _userRole; }
        }

        public string UserName
        {
            get { return _userName; }
        }

        private void ParseTokenAndStoreInfo(string token)
        {
            _userRole = null;
            _userName = null;

            if (string.IsNullOrEmpty(token))
            {
                Log.Debug("Token is null or empty, clearing role and username.");
                return;
            }

            try
            {
                Log.Debug("Starting JWT token parsing...");
                var parts = token.Split('.');
                if (parts.Length < 2)
                {
                    Log.Warn("JWT token does not contain enough parts. Token has {0} parts, expected at least 2.", parts.Length);
                    return;
                }

                string payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                byte[] data = Convert.FromBase64String(payload);
                string decodedPayload = Encoding.UTF8.GetString(data);

                Log.Debug("Decoded JWT Payload: {0}", decodedPayload);

                JObject jsonPayload = JObject.Parse(decodedPayload);
                Log.Debug("JWT payload parsed successfully. Claims count: {0}", jsonPayload.Count);

                JToken roleToken;
                if (jsonPayload.TryGetValue("role", StringComparison.OrdinalIgnoreCase, out roleToken))
                {
                    Log.Debug("Found 'role' claim in token. Raw value: {0}, Type: {1}", roleToken.ToString(), roleToken.Type);
                    try
                    {
                        // Handle both single value and array (when server adds role claim multiple times)
                        if (roleToken.Type == JTokenType.Array)
                        {
                            Log.Warn("Role claim is an array (duplicate claims detected). Taking first value.");
                            var roleArray = (JArray)roleToken;
                            if (roleArray.Count > 0)
                            {
                                _userRole = roleArray[0].Value<int>();
                            }
                            else
                            {
                                Log.Error("Role array is empty!");
                                _userRole = null;
                            }
                        }
                        else
                        {
                            _userRole = roleToken.Value<int>();
                        }
                        
                        Log.Info("*** User role successfully parsed from token: {0} ***", _userRole);
                        Log.Info("*** UserRole is now: {0} (0=User, 1=Admin) ***", _userRole.Value == 1 ? "ADMIN" : "USER");
                    }
                    catch (Exception exConv)
                    {
                         string roleValueStr = roleToken.ToString();
                         string errorMsg = string.Format("Failed to convert role claim '{0}' to integer. Exception: {1}", roleValueStr, exConv.ToString());
                         Log.Error(errorMsg);
                        _userRole = null;
                    }
                }
                else
                {
                    Log.Warn("JWT payload does not contain 'role' claim. Available claims: {0}", 
                        string.Join(", ", jsonPayload.Properties().Select(p => p.Name)));
                    _userRole = null;
                }

                JToken nameToken;
                if (jsonPayload.TryGetValue("unique_name", StringComparison.OrdinalIgnoreCase, out nameToken) ||
                    jsonPayload.TryGetValue("name", StringComparison.OrdinalIgnoreCase, out nameToken))
                {
                     _userName = nameToken.Value<string>();
                     Log.Info("Username parsed from token: {0}", _userName);
                }
                else
                {
                     Log.Warn("JWT payload does not contain standard username claim ('unique_name' or 'name').");
                     _userName = null;
                }

            }
            catch (FormatException exFormat)
            {
                string errorMsg = string.Format("Failed to decode Base64 payload. Exception: {0}", exFormat.ToString());
                Log.Error(errorMsg);
            }
            catch (JsonReaderException exJson)
            {
                 string errorMsg = string.Format("Failed to parse JSON payload. Exception: {0}", exJson.ToString());
                 Log.Error(errorMsg);
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("An unexpected error occurred while parsing JWT token. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
            }
        }
    }
} 