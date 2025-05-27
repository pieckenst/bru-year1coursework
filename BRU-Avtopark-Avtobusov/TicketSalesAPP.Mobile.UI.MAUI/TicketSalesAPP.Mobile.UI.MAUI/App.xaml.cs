using Microsoft.Maui.Controls;
using TicketSalesAPP.Mobile.UI.MAUI.Views;

namespace TicketSalesAPP.Mobile.UI.MAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AuthPage();
        }

        protected override async void OnStart()
        {
            base.OnStart();

            // Check if user is already authenticated
            var token = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrEmpty(token))
            {
                // Validate token and switch to main app if valid
                var isValid = await ValidateTokenAsync(token);
                if (isValid)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MainPage = new AppShell();
                    });
                }
            }
        }

        private async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var response = await client.GetAsync("http://localhost:5000/api/Users/current");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}