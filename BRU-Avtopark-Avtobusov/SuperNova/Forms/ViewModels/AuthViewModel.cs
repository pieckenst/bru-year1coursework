using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using Serilog;
using SuperNova;
using SuperNova.Forms.Services;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using TicketSalesApp.Core;


namespace SuperNova.Forms.ViewModels
{
    public partial class AuthViewModel : ReactiveObject
    {
        private readonly HttpClient _httpClient;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private bool _isWindowsAuthInProgress;

        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public bool IsWindowsAuthInProgress
        {
            get => _isWindowsAuthInProgress;
            set => this.RaiseAndSetIfChanged(ref _isWindowsAuthInProgress, value);
        }

        public bool IsAuthenticated { get; private set; }

        public ICommand WindowsLoginCommand { get; }

        public AuthViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            WindowsLoginCommand = new AsyncRelayCommand(WindowsLoginAsync);
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Please enter username and password";
                    return;
                }

                var loginData = new { Login = Username, Password };
                var response = await _httpClient.PostAsJsonAsync("Auth/Login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (result?.Token != null)
                    {
                        // Decode JWT token to check role
                        var handler = new JwtSecurityTokenHandler();
                        var token = handler.ReadJwtToken(result.Token);
                        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == "role");

                        if (roleClaim?.Value != "1") // Not an admin
                        {
                            // Show error message box
                            var messageBox = MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Доступ запрещен",
                                    "Это приложение предназначено для администраторов. Пожалуйста, используйте приложения модуля пользователя вместо интерфейса администратора.",
                                    ButtonEnum.Ok,
                                    Icon.Error);

                            await messageBox.ShowAsync();

                            // Close the application
                            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                            {
                                desktopLifetime.Shutdown();
                            }
                            return;
                        }

                        // Store token in the service
                        ApiClientService.Instance.AuthToken = result.Token;

                        IsAuthenticated = true;
                        Log.Information("Administrator successfully authenticated");

                        // Close the auth window
                        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                        {
                            if (lifetime.Windows.Count > 0 && lifetime.Windows[0] is Window window)
                            {
                                window.Close();
                            }
                        }
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Authentication failed: {error}";
                    Log.Warning("Authentication failed: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred during authentication";
                Log.Error(ex, "Authentication error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task WindowsLoginAsync()
        {
            try
            {
                IsLoading = true;
                IsWindowsAuthInProgress = true;
                ErrorMessage = string.Empty;

                // Prompt Windows Credential Picker explicitly
                var cred = WindowsCredPicker.Prompt("Введите учетные данные Windows", "Пожалуйста, введите ваши учетные данные для входа");

                if (cred == null)
                {
                    ErrorMessage = "Аутентификация отменена пользователем.";
                    return;
                }

                var (domain, username, password) = cred.Value;

                // Compose full username with domain if available
                string fullUsername = string.IsNullOrEmpty(domain) ? username : $"{domain}\\{username}";

                // Create HttpClientHandler with provided credentials
                var handler = new HttpClientHandler
                {
                    UseDefaultCredentials = false,
                    PreAuthenticate = true,
                    Credentials = new System.Net.NetworkCredential(username, password, domain)
                };

                using var httpClient = new HttpClient(handler);
                httpClient.BaseAddress = _httpClient.BaseAddress;

                // Call Windows authentication endpoint
                var response = await httpClient.GetAsync("Auth/windows-login");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (result?.Token != null)
                    {
                        var tokenHandler = new JwtSecurityTokenHandler();
                        var token = tokenHandler.ReadJwtToken(result.Token);
                        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == "role");

                        if (roleClaim?.Value != "1") // Not an admin
                        {
                            var messageBox = MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Доступ запрещен",
                                    "Это приложение предназначено для администраторов. Пожалуйста, используйте приложения модуля пользователя вместо интерфейса администратора.",
                                    ButtonEnum.Ok,
                                    Icon.Error);

                            await messageBox.ShowAsync();

                            if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                            {
                                desktopLifetime.Shutdown();
                            }
                            return;
                        }

                        ApiClientService.Instance.AuthToken = result.Token;
                        IsAuthenticated = true;
                        Log.Information("Administrator successfully authenticated via Windows");

                        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                        {
                            if (lifetime.Windows.Count > 0 && lifetime.Windows[0] is Window window)
                            {
                                window.Close();
                            }
                        }
                    }
                }
                else if (response.StatusCode == (System.Net.HttpStatusCode)418 || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    var errorMessage = errorResponse?.Message ?? "Ваша учетная запись Windows не имеет пароля. Пожалуйста, установите пароль в настройках Windows и повторите попытку.";

                    var messageBox = MessageBoxManager
                        .GetMessageBoxStandard(
                            "Небезопасная учетная запись",
                            errorMessage,
                            ButtonEnum.Ok,
                            Icon.Warning);

                    await messageBox.ShowAsync();
                    ErrorMessage = "Вход не выполнен: небезопасная учетная запись Windows";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Ошибка Windows-аутентификации: {error}";
                    Log.Warning("Windows authentication failed with status {StatusCode}: {Error}", response.StatusCode, error);
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Ошибка подключения к серверу. Проверьте сетевое подключение и повторите попытку.";
                Log.Error(ex, "Network error during Windows authentication");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Ошибка при попытке входа с Windows";
                Log.Error(ex, "Windows authentication error");
            }
            finally
            {
                IsLoading = false;
                IsWindowsAuthInProgress = false;
            }
        }




        private class AuthResponse
        {
            public string? Token { get; set; }
            public UserDto? User { get; set; }
        }

        private class UserDto
        {
            public int UserId { get; set; }
            public string? Login { get; set; }
            public string? Email { get; set; }
            public int Role { get; set; }
        }

        private class ErrorResponse
        {
            public string? Message { get; set; }
        }

        private class CredentialPromptResult
        {
            public bool IsSuccess { get; private set; }
            public string? Credential { get; private set; }
            public string? ErrorMessage { get; private set; }

            private CredentialPromptResult() { }

            public static CredentialPromptResult Success(string credential) => new()
            {
                IsSuccess = true,
                Credential = credential
            };

            public static CredentialPromptResult Failure(string errorMessage) => new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}