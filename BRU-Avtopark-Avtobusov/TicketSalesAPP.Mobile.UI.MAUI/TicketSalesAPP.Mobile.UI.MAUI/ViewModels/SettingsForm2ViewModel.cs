using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class SettingsForm2ViewModel : ObservableObject
    {

        [ObservableProperty]
        private string fullName = "Alfred Newman";

        [ObservableProperty]
        private string company = "Newman Systems";

        public string NameInitials => string.Concat(FullName.Split(' ').Select(s => s[0]));

        [RelayCommand]
        async Task HandleActionAsync()
        {
            await Shell.Current.DisplayAlert("Action", "Action executed", "OK");
        }
    }
}