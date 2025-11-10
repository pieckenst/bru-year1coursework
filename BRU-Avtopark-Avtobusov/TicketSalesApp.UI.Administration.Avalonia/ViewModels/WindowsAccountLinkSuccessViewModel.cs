using System;
using System.Reactive;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using System.Reactive.Disposables;

namespace TicketSalesApp.UI.Administration.Avalonia.ViewModels
{
    public class WindowsAccountLinkSuccessViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        
        public string Username { get; }
        public string SuccessMessage { get; } = "Your Windows account has been successfully linked to your application account.";
        public ICommand CloseCommand { get; }

        public WindowsAccountLinkSuccessViewModel(string username, Action closeAction)
        {
            Username = username;
            CloseCommand = new RelayCommand(closeAction);
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
