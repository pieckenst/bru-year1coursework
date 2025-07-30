using System;
using Avalonia;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Avalonia.Input;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using System.Linq;

namespace SuperNova.Forms.ViewModels
{
    public class WindowsAccountLinkConfirmationViewModel : ReactiveObject
    {
        private readonly System.Reactive.Disposables.CompositeDisposable _disposables = new();

        public string WindowsUsername { get; }
        public string Username { get; }
        public string Token { get; }
        
        public IRelayCommand ConfirmCommand { get; }
        public IRelayCommand CancelCommand { get; }
        public ICommand CopyTokenCommand { get; }
        
        public event EventHandler<bool>? DialogResult;

        public WindowsAccountLinkConfirmationViewModel(string windowsUsername, string username, string token)
        {
            WindowsUsername = windowsUsername;
            Username = username;
            Token = token;
            
            ConfirmCommand = new RelayCommand(OnConfirm);
            CancelCommand = new RelayCommand(OnCancel);
            CopyTokenCommand = new RelayCommand(OnCopyToken);
        }

        private void OnConfirm()
        {
            DialogResult?.Invoke(this, true);
        }
        
        private void OnCancel()
        {
            DialogResult?.Invoke(this, false);
        }
        
        private async void OnCopyToken()
        {
            try
            {
                var window = GetActiveWindow();
                if (window == null) return;
                
                await window.Clipboard.SetTextAsync(Token);
                var box = MessageBoxManager.GetMessageBoxStandard("Success", "Token copied to clipboard!", ButtonEnum.Ok, Icon.Success);
                await box.ShowWindowDialogAsync(window);
            }
            catch (Exception ex)
            {
                var window = GetActiveWindow();
                if (window == null) return;
                
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Failed to copy token: {ex.Message}", ButtonEnum.Ok, Icon.Error);
                await box.ShowWindowDialogAsync(window);
            }
        }
        
        private static Window? GetActiveWindow()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow;
            }
            return null;
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}