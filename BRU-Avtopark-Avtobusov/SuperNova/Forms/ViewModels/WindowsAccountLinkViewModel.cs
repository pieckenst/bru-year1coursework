using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using SuperNova.Forms.Views;

namespace SuperNova.Forms.ViewModels
{
    public class WindowsAccountLinkViewModel : ReactiveObject, IDisposable
    {
        private readonly System.Reactive.Disposables.CompositeDisposable _disposables = new();
        private readonly HttpClient _httpClient;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _windowsUsername = string.Empty;
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private bool _showSuccess;
        private Window? _ownerWindow;

        public string Username
        {
            get => _username;
            set 
            {
                if (_username != value)
                {
                    _username = value;
                    this.RaisePropertyChanged();
                    (LinkAccountCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set 
            {
                if (_password != value)
                {
                    _password = value;
                    this.RaisePropertyChanged();
                    (LinkAccountCommand as RelayCommand)?.NotifyCanExecuteChanged();
                }
            }
        }

        public string WindowsUsername
        {
            get => _windowsUsername;
            private set => this.RaiseAndSetIfChanged(ref _windowsUsername, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool ShowSuccess
        {
            get => _showSuccess;
            private set => this.RaiseAndSetIfChanged(ref _showSuccess, value);
        }

        public ICommand LinkAccountCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler? RequestClose;

        public WindowsAccountLinkViewModel(Window? ownerWindow = null, HttpClient? httpClient = null)
        {
            _ownerWindow = ownerWindow;
            _httpClient = httpClient ?? new HttpClient { BaseAddress = new Uri("https://localhost:5001/api/") };
            WindowsUsername = $"{Environment.UserDomainName}\\{Environment.UserName}";

            LinkAccountCommand = new RelayCommand(
                execute: async () => await LinkAccountAsync(),
                canExecute: () => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password));

            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));
        }

        public void SetOwnerWindow(Window ownerWindow)
        {
            _ownerWindow = ownerWindow;
        }

        private async Task LinkAccountAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Please enter both username and password";
                return;
            }

            if (_ownerWindow == null)
            {
                // Try to find the owner window if not set
                _ownerWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow
                    : null;

                if (_ownerWindow == null)
                {
                    StatusMessage = "Error: Could not determine parent window";
                    return;
                }
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Initiating account linking...";

                // Step 1: Get the linking token from the server
                var linkResponse = await _httpClient.PostAsJsonAsync("auth/link-windows-account", new 
                {
                    WindowsUsername = WindowsUsername,
                    Username = Username
                });
                
                if (!linkResponse.IsSuccessStatusCode)
                {
                    var error = await linkResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to initiate account linking: {error}");
                }

                var linkResult = await linkResponse.Content.ReadFromJsonAsync<JsonElement>();
                var token = linkResult.GetProperty("verificationToken").GetString() ?? throw new Exception("No token received from server");

                // Step 2: Show confirmation dialog with the token
                var confirmDialog = new WindowsAccountLinkConfirmationDialog(WindowsUsername, Username, token);
                var confirmed = await confirmDialog.ShowDialog<bool>(_ownerWindow);
                
                if (!confirmed)
                {
                    StatusMessage = "Account linking was cancelled.";
                    return;
                }

                // Step 3: Complete the linking process
                StatusMessage = "Completing account linking...";
                
                var completeResponse = await _httpClient.PostAsJsonAsync("auth/complete-windows-link", new 
                {
                    WindowsUsername = WindowsUsername,
                    Username = Username,
                    Token = token
                });

                if (!completeResponse.IsSuccessStatusCode)
                {
                    var error = await completeResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to complete account linking: {error}");
                }

                // Show success dialog
                var successDialog = new WindowsAccountLinkSuccessDialog(Username);
                await successDialog.ShowDialog(_ownerWindow);
                
                // Update UI to show success
                ShowSuccess = true;
                StatusMessage = "Your Windows account has been successfully linked!";
                
                // Clear the form
                Username = string.Empty;
                Password = string.Empty;
                
                // Close the window after a short delay
                await Task.Delay(2000);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                StatusMessage = $"An error occurred: {ex.Message}";
                var errorDialog = MessageBoxManager.GetMessageBoxStandard(
                    "Error",
                    $"Failed to link accounts: {ex.Message}",
                    ButtonEnum.Ok,
                    Icon.Error);
                
                if (_ownerWindow != null)
                {
                    await errorDialog.ShowWindowDialogAsync(_ownerWindow);
                }
                else
                {
                    await errorDialog.ShowAsync();
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}