using System;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;

namespace SuperNova.Forms.ViewModels
{
    public class WindowsAccountLinkConfirmationViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        
        public string WindowsUsername { get; }
        public string Username { get; }
        
        public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        
        public Interaction<Unit, Unit> CloseWindow { get; } = new();

        public WindowsAccountLinkConfirmationViewModel(string windowsUsername, string username)
        {
            WindowsUsername = windowsUsername;
            Username = username;
            
            ConfirmCommand = ReactiveCommand.CreateFromTask(ConfirmAsync);
            CancelCommand = ReactiveCommand.CreateFromTask(CancelAsync);
            
            this.WhenActivated(disposables =>
            {
                // Handle any additional activation logic here
                Disposable
                    .Create(() =>
                    {
                        // Cleanup code when deactivated
                    })
                    .DisposeWith(disposables);
            });
        }
        
        private async Task ConfirmAsync()
        {
            await CloseWindow.Handle(Unit.Default);
        }
        
        private async Task CancelAsync()
        {
            await CloseWindow.Handle(Unit.Default);
        }
        
        public void Dispose()
        {
            _disposables.Dispose();
            ConfirmCommand?.Dispose();
            CancelCommand?.Dispose();
        }
}
