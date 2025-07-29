using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using SuperNova.Forms.Services;
using SuperNova.Forms.Views;

namespace SuperNova.Forms.ViewModels
{
    public class WindowsAccountLinkViewModel : ReactiveObject
    {
        private readonly IAuthenticationService _authService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _windowsUsername = string.Empty;
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private bool _showSuccess;

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
            set => this.RaiseAndSetIfChanged(ref _windowsUsername, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool ShowSuccess
        {
            get => _showSuccess;
            set => this.RaiseAndSetIfChanged(ref _showSuccess, value);
        }

        public ReactiveCommand<Unit, Unit> LinkAccountCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        public Interaction<Unit, bool> ShowConfirmation { get; } = new();
        public Interaction<Unit, Unit> CloseWindow { get; } = new();

        public WindowsAccountLinkViewModel(IAuthenticationService authService = null)
        {
            _authService = authService;
            
            LinkAccountCommand = ReactiveCommand.CreateFromTask(LinkAccountAsync);
            CancelCommand = ReactiveCommand.CreateFromTask(CancelAsync);
            
            // Set the Windows username from environment
            WindowsUsername = $"{Environment.UserDomainName}\\{Environment.UserName}";
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
                var dialog = new WindowsAccountLinkConfirmationDialog(WindowsUsername, Username);
                var confirmed = await dialog.ShowDialog<bool>((Window)TopLevel.GetTopLevel((Control)VisualRoot));
                
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
                await successDialog.ShowDialog((Window)TopLevel.GetTopLevel((Control)VisualRoot));
                
                // Close window after success
                await CloseWindow.Handle(Unit.Default);
            }
            catch (Exception ex)
            {
                StatusMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CancelAsync()
        {
            await CloseWindow.Handle(Unit.Default);
        }
    }
}
