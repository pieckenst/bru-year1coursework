using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Services;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class MvvmViewModel : ObservableObject
    {

        [ObservableProperty]
        ObservableCollection<Customer>? customers;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InitializeCommand))]
        bool isInitialized;

        readonly IDataService dataService;
        public MvvmViewModel(IDataService dataService)
        {
            this.dataService = dataService;
        }

        [RelayCommand(CanExecute = nameof(CanInitialize))]
        async Task InitializeAsync()
        {
            Customers = new ObservableCollection<Customer>(await dataService.GetCustomersAsync());
            IsInitialized = true;
        }

        bool CanInitialize() => !IsInitialized;
    }
}
