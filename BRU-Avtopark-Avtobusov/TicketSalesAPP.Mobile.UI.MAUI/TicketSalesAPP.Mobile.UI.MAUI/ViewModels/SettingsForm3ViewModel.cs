using CommunityToolkit.Mvvm.ComponentModel;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class SettingsForm3ViewModel : ObservableObject
    {

        [ObservableProperty]
        private bool darkMode = Application.Current?.RequestedTheme == AppTheme.Dark;

        [ObservableProperty]
        private string language = "English";

        [ObservableProperty]
        private IEnumerable<string> languages = new List<string> {
            "English", "German", "French", "Spanish", "Italian"
        };

        partial void OnDarkModeChanged(bool value)
        {
            if (Application.Current == null)
                return;

            Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }
    }
}