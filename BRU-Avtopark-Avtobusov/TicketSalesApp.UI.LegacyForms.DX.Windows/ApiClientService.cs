using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Nodes;
using Serilog;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public class ApiClientService
    {
        private static ApiClientService _instance;
        private static readonly object _lock = new object();
        private static readonly ILogger Log = Serilog.Log.ForContext<ApiClientService>();

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
                var parts = token.Split('.');
                if (parts.Length < 2)
                {
                    Log.Warning("JWT token does not contain enough parts.");
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

                Log.Debug("Decoded JWT Payload: {Payload}", decodedPayload);

                JsonNode jsonPayload = JsonNode.Parse(decodedPayload);

                if (jsonPayload?["role"] != null)
                {
                    try
                    {
                        _userRole = jsonPayload["role"].GetValue<int>();
                        Log.Information("User role parsed from token: {UserRole}", _userRole);
                    }
                    catch (Exception exConv)
                    {
                         string roleValueStr = jsonPayload["role"].ToString();
                         Log.Error(exConv, "Failed to convert role claim '{RoleValue}' to integer", roleValueStr);
                        _userRole = null;
                    }
                }
                else
                {
                    Log.Warning("JWT payload does not contain 'role' claim.");
                    _userRole = null;
                }

                if (jsonPayload?["unique_name"] != null)
                {
                     _userName = jsonPayload["unique_name"].GetValue<string>();
                     Log.Information("Username parsed from token: {UserName}", _userName);
                }
                else if (jsonPayload?["name"] != null)
                {
                     _userName = jsonPayload["name"].GetValue<string>();
                     Log.Information("Username parsed from token: {UserName}", _userName);
                }
                else
                {
                     Log.Warning("JWT payload does not contain standard username claim ('unique_name' or 'name').");
                     _userName = null;
                }

            }
            catch (FormatException exFormat)
            {
                Log.Error(exFormat, "Failed to decode Base64 payload");
            }
            catch (JsonException exJson)
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