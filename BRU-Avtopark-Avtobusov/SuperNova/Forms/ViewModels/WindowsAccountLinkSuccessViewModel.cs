using System;
using System.Reactive;
using ReactiveUI;

namespace SuperNova.Forms.ViewModels
{
    public class WindowsAccountLinkSuccessViewModel : ReactiveObject
    {
        public string Username { get; }
        public string SuccessMessage { get; } = "Your Windows account has been successfully linked to your application account.";
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        public WindowsAccountLinkSuccessViewModel(string username, Action closeAction)
        {
            Username = username;
            CloseCommand = ReactiveCommand.Create(closeAction);
        }
    }
}
