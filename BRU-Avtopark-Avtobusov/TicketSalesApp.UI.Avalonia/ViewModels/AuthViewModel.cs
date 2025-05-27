// UI/Avalonia/ViewModels/AuthViewModel.cs
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Avalonia.Media.Imaging;
using System.IO;
using Avalonia.Platform;
using System.Timers;
using Timer = System.Timers.Timer;
using Serilog;
using TicketSalesApp.UI.Avalonia.Services;
//using TicketSalesApp.Services.Interfaces;

namespace TicketSalesApp.UI.Avalonia.ViewModels
{
    public class AuthViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient;
        private readonly Timer _qrCheckTimer;
        private string? _deviceId;
        private const string BaseUrl = "http://localhost:5000/api/auth";

        public AuthViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            _qrCheckTimer = new Timer(2000); // Check every 2 seconds
            _qrCheckTimer.Elapsed += async (s, e) => await CheckQRLoginStatus();

            // Commands
            LoginCommand = ReactiveCommand.CreateFromTask(LoginAsync);
            ContinueWithUsernameCommand = ReactiveCommand.CreateFromTask(ContinueWithUsernameAsync);
            RefreshQRCodeCommand = ReactiveCommand.CreateFromTask(RefreshQRCodeAsync);
            SwitchToPasswordLoginCommand = ReactiveCommand.Create(SwitchToPasswordLogin);
            SwitchToQRLoginCommand = ReactiveCommand.Create(SwitchToQRLogin);

            // Start with username entry
            IsUsernameEntryVisible = true;
            IsQRLoginVisible = false;
            IsPasswordLoginVisible = false;
        }

        #region Properties

        private bool _isUsernameEntryVisible;
        public bool IsUsernameEntryVisible
        {
            get => _isUsernameEntryVisible;
            set => this.RaiseAndSetIfChanged(ref _isUsernameEntryVisible, value);
        }

        private bool _isQRLoginVisible;
        public bool IsQRLoginVisible
        {
            get => _isQRLoginVisible;
            set => this.RaiseAndSetIfChanged(ref _isQRLoginVisible, value);
        }

        private bool _isPasswordLoginVisible;
        public bool IsPasswordLoginVisible
        {
            get => _isPasswordLoginVisible;
            set => this.RaiseAndSetIfChanged(ref _isPasswordLoginVisible, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        private string? _login;
        public string? Login
        {
            get => _login;
            set => this.RaiseAndSetIfChanged(ref _login, value);
        }

        private string? _password;
        public string? Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        private bool _hasExistingUsername;
        public bool HasExistingUsername
        {
            get => _hasExistingUsername;
            set => this.RaiseAndSetIfChanged(ref _hasExistingUsername, value);
        }

        private Bitmap? _qrCodeImage;
        public Bitmap? QRCodeImage
        {
            get => _qrCodeImage;
            set => this.RaiseAndSetIfChanged(ref _qrCodeImage, value);
        }

        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set => this.RaiseAndSetIfChanged(ref _isAuthenticated, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        #endregion

        #region Commands

        public ReactiveCommand<Unit, Unit> LoginCommand { get; }
        public ReactiveCommand<Unit, Unit> ContinueWithUsernameCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshQRCodeCommand { get; }
        public ReactiveCommand<Unit, Unit> SwitchToPasswordLoginCommand { get; }
        public ReactiveCommand<Unit, Unit> SwitchToQRLoginCommand { get; }

        #endregion

        #region Methods

        private async Task ContinueWithUsernameAsync()
        {
            if (string.IsNullOrWhiteSpace(Login))
            {
                // Show error
                return;
            }

            IsLoading = true;
            try
            {
                // Generate QR code for the username
                var response = await _httpClient.GetAsync($"{BaseUrl}/qr/direct/generate?username={Login}&deviceType=desktop");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<QRCodeResponse>();
                    if (result != null)
                    {
                        // Convert base64 to bitmap
                        var bytes = Convert.FromBase64String(result.qrCode);
                        using var ms = new MemoryStream(bytes);
                        QRCodeImage = new Bitmap(ms);
                        _deviceId = result.deviceId;

                        // Switch to QR view
                        IsUsernameEntryVisible = false;
                        IsQRLoginVisible = true;
                        HasExistingUsername = true;

                        // Start polling for login status
                        _qrCheckTimer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error
                System.Diagnostics.Debug.WriteLine($"Error generating QR code: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshQRCodeAsync()
        {
            _qrCheckTimer.Stop();
            await ContinueWithUsernameAsync();
        }

        private void SwitchToPasswordLogin()
        {
            IsQRLoginVisible = false;
            IsPasswordLoginVisible = true;
            _qrCheckTimer.Stop();
        }

        private void SwitchToQRLogin()
        {
            IsPasswordLoginVisible = false;
            IsQRLoginVisible = true;
            RefreshQRCodeAsync().ConfigureAwait(false);
        }

        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Пожалуйста, введите логин и пароль";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var loginData = new { Login, Password };
                var response = await _httpClient.PostAsJsonAsync("auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (result?.Token != null)
                    {
                        // Store token in the service
                        ApiClientService.Instance.AuthToken = result.Token;
                        IsAuthenticated = true;
                        Log.Information("User successfully authenticated");
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Ошибка авторизации: {error}";
                    Log.Warning("Authentication failed: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Произошла ошибка при авторизации";
                Log.Error(ex, "Authentication error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CheckQRLoginStatus()
        {
            if (string.IsNullOrEmpty(_deviceId)) return;

            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/qr/direct/check?deviceId={_deviceId}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<QRLoginStatusResponse>();
                    if (result?.success == true)
                    {
                        _qrCheckTimer.Stop();
                        // Handle successful login - store token, etc.
                        IsAuthenticated = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking QR login status: {ex}");
            }
        }

        #endregion

        private class LoginResponse
        {
            public string token { get; set; } = "";
        }

        private class QRCodeResponse
        {
            public string qrCode { get; set; } = "";
            public string deviceId { get; set; } = "";
            public string? rawData { get; set; }
        }

        private class QRLoginStatusResponse
        {
            public bool success { get; set; }
            public string? token { get; set; }
        }

        private class AuthResponse
        {
            public string? Token { get; set; }
        }
    }
}