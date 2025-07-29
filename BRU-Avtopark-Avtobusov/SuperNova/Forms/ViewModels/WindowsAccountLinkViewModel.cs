using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
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
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
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

        public WindowsAccountLinkViewModel(Window? ownerWindow = null)
        {
            _ownerWindow = ownerWindow;
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

            try
            {
                IsBusy = true;
                StatusMessage = "Verifying credentials...";

                // Show confirmation dialog
                var confirmDialog = new WindowsAccountLinkConfirmationDialog(WindowsUsername, Username);
                var confirmed = await confirmDialog.ShowDialog<bool>(_ownerWindow);
                
                if (!confirmed)
                {
                    StatusMessage = "Account linking cancelled";
                    return;
                }

                // Simulate API call
                await Task.Delay(1500);
                
                // Show success state
                ShowSuccess = true;
                StatusMessage = "Your Windows account has been successfully linked!";
                
                // Show success dialog
                var successDialog = new WindowsAccountLinkSuccessDialog(WindowsUsername);
                await successDialog.ShowDialog(_ownerWindow);
                
                // Close the window
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