using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace TicketSalesApp.UI.Administration.Avalonia.Services
{
    public class ApiClientService
    {
        private static ApiClientService? _instance;
        private static readonly object _lock = new();
        private string? _authToken;

        public static ApiClientService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ApiClientService();
                    }
                }
                return _instance;
            }
        }

        private ApiClientService()
        {
        }

        public string? AuthToken
        {
            get => _authToken;
            set
            {
                _authToken = value;
                OnAuthTokenChanged?.Invoke(this, value);
            }
        }

        public event EventHandler<string?> OnAuthTokenChanged;

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

            return client;
        }
    }
} 