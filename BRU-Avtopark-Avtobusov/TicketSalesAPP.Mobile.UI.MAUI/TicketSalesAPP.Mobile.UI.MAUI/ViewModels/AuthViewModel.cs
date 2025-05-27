using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Graphics;
using System.Text.Json;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class AuthViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private Timer? _qrCheckTimer;

        [ObservableProperty]
        private bool _isUsernameEntryVisible = true;

        [ObservableProperty]
        private bool _isQRLoginVisible;

        [ObservableProperty]
        private bool _isPasswordLoginVisible;

        [ObservableProperty]
        private string? _login;

        [ObservableProperty]
        private string? _password;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private ImageSource? _qrCodeImage;

        private string? _deviceId;

        public AuthViewModel()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/api/") };
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [RelayCommand]
        private async Task ContinueWithUsername()
        {
            if (string.IsNullOrWhiteSpace(Login))
            {
                ErrorMessage = "Введите имя пользователя";
                HasError = true;
                return;
            }

            try
            {
                var response = await _httpClient.GetAsync($"auth/qr/direct/generate?username={Login}&deviceType=mobile");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<QRCodeResponse>(_jsonOptions);
                    if (result != null)
                    {
                        _deviceId = result.deviceId;
                        QRCodeImage = ImageSource.FromStream(() => new MemoryStream(Convert.FromBase64String(result.qrCode)));
                        IsUsernameEntryVisible = false;
                        IsQRLoginVisible = true;

                        // Start polling for QR code status
                        _qrCheckTimer = new Timer(CheckQRLoginStatus, null, 0, 2000);
                    }
                }
                else
                {
                    ErrorMessage = "Ошибка генерации QR-кода";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                HasError = true;
            }
        }

        private async void CheckQRLoginStatus(object? state)
        {
            try
            {
                if (string.IsNullOrEmpty(_deviceId)) return;

                var response = await _httpClient.GetAsync($"auth/qr/direct/check?deviceId={_deviceId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<QRLoginStatusResponse>(_jsonOptions);
                    if (result?.success == true && !string.IsNullOrEmpty(result.token))
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            _qrCheckTimer?.Dispose();
                            await HandleSuccessfulLogin(result.token);
                        });
                    }
                }
            }
            catch
            {
                // Ignore polling errors
            }
        }

        [RelayCommand]
        private void SwitchToPasswordLogin()
        {
            IsQRLoginVisible = false;
            IsPasswordLoginVisible = true;
            _qrCheckTimer?.Dispose();
        }

        [RelayCommand]
        private void SwitchToQRLogin()
        {
            IsPasswordLoginVisible = false;
            IsQRLoginVisible = true;
            ContinueWithUsernameCommand.Execute(null);
        }

        [RelayCommand]
        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Введите имя пользователя и пароль";
                HasError = true;
                return;
            }

            try
            {
                var response = await _httpClient.PostAsJsonAsync("auth/login", new { Login, Password });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                    if (result != null)
                    {
                        await HandleSuccessfulLogin(result.token);
                    }
                }
                else
                {
                    ErrorMessage = "Неверное имя пользователя или пароль";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                HasError = true;
            }
        }

        public async Task HandleQRCodeResult(string qrData)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("auth/qr/direct/login", new { Token = qrData, DeviceType = "mobile" });
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>(_jsonOptions);
                    if (result != null)
                    {
                        await HandleSuccessfulLogin(result.token);
                    }
                }
                else
                {
                    ErrorMessage = "Ошибка входа по QR-коду";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                HasError = true;
            }
        }

        private async Task HandleSuccessfulLogin(string token)
        {
            await SecureStorage.SetAsync("auth_token", token);
            Application.Current!.MainPage = new AppShell();
        }

        private class LoginResponse
        {
            public string token { get; set; } = "";
        }

        private class QRCodeResponse
        {
            public string qrCode { get; set; } = "";
            public string deviceId { get; set; } = "";
        }

        private class QRLoginStatusResponse
        {
            public bool success { get; set; }
            public string? token { get; set; }
        }
    }
}