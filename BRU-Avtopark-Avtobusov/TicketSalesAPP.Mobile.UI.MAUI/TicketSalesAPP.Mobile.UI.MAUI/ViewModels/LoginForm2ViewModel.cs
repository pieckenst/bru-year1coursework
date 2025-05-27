using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class LoginForm2ViewModel : ObservableObject
    {
        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        string userName = string.Empty;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        string password = string.Empty;

        [ObservableProperty, NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        bool isBusy;

        [ObservableProperty]
        bool hasError;

        bool CanLogin => !IsBusy && !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password);

        [RelayCommand(CanExecute = nameof(CanLogin))]
        async Task Login()
        {
            IsBusy = true;
            HasError = false;
            await Task.Delay(3000);
            IsBusy = false;
            if (UserName != "Admin" || Password != "123456")
            {
                HasError = true;
                return;
            }
            await Shell.Current.DisplayAlert("Login", "Login successful", "OK");
        }
    }
}