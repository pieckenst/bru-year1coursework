using System;
using System.Reactive;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;

namespace SuperNova.Forms.ViewModels
{
    public class WindowsAccountLinkConfirmationViewModel : ReactiveObject
    {
        private readonly System.Reactive.Disposables.CompositeDisposable _disposables = new();

        public string WindowsUsername { get; }
        public string Username { get; }
        
        public IRelayCommand ConfirmCommand { get; }
        public IRelayCommand CancelCommand { get; }
        
        public event EventHandler<bool>? DialogResult;

        public WindowsAccountLinkConfirmationViewModel(string windowsUsername, string username)
        {
            WindowsUsername = windowsUsername;
            Username = username;
            
            ConfirmCommand = new RelayCommand(OnConfirm);
            CancelCommand = new RelayCommand(OnCancel);
        }

        private void OnConfirm()
        {
            DialogResult?.Invoke(this, true);
        }
        
        private void OnCancel()
        {
            DialogResult?.Invoke(this, false);
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}