using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using TicketSalesApp.UI.Administration.Avalonia.Services;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace TicketSalesApp.UI.Administration.Avalonia.ViewModels
{
    public partial class AuthViewModel : ReactiveObject
    {
        private readonly HttpClient _httpClient;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;

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

        public bool IsAuthenticated { get; private set; }

        public AuthViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
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

        private class AuthResponse
        {
            public string? Token { get; set; }
        }
    }
} 