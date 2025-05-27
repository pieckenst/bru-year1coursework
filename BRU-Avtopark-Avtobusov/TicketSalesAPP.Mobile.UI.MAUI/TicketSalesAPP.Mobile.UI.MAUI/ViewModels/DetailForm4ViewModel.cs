using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class DetailForm4ViewModel : ObservableObject
    {

        [ObservableProperty]
        private int price = 780000;

        [ObservableProperty]
        private string description = "Conveniently located with access to major freeways, 4 bedrooms, 3 bathrooms, 2,170 sqft.";

        [ObservableProperty]
        private string address = "695 Santa Fe Ave, Los Angeles, CA 90021 ";

        [ObservableProperty]
        private int yearBuilt = 2019;

        [ObservableProperty]
        private string heating = "Natural gas";

        [ObservableProperty]
        private string cooling = "Air conditioning";

        [ObservableProperty]
        private int levels = 2;

        [ObservableProperty]
        private int parkingSpaces = 2;

        [ObservableProperty]
        private string lotSize = "0.73 Acres";

        [RelayCommand]
        private async Task HandleActionAsync()
        {
            await Shell.Current.DisplayAlert("Action", "Action executed", "OK");
        }
    }
}