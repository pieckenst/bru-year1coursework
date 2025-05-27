using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevExpress.Maui.Core;
using System.Collections.ObjectModel;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Services;
using TicketSalesAPP.Mobile.UI.MAUI.Infrastructure.Repositories;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class WebApiViewModel : ObservableObject
    {
        readonly OrdersRepository ordersRepository;

        [ObservableProperty]
        ObservableCollection<Order>? orders;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(InitializeCommand))]
        bool isInitialized;

        public WebApiViewModel(ICacheService cacheService, IWebApiService webApiService)
        {
            ordersRepository = new OrdersRepository(cacheService, webApiService);
        }

        [RelayCommand(CanExecute = nameof(CanInitialize))]
        async Task InitializeAsync()
        {
            var data = await GetItems();
            Orders = new ObservableCollection<Order>(data);
            IsInitialized = true;
        }
        [RelayCommand]
        async Task DeleteItemAsync(Order item)
        {
            var isSuccessful = await ordersRepository.DeleteAsync(item);
            if (!isSuccessful)
            {
                await Shell.Current.DisplayAlert("Error", "The item could not be deleted.", "OK");
                return;
            }
            Orders?.Remove(item);
        }
        [RelayCommand]
        void ValidateAndSave(ValidateItemEventArgs args)
        {
            args.AutoUpdateItemsSource = false;
            if (args.Item is not Order item)
                return;

            args.IsValidAsync = Task.Run(async () =>
            {
                try
                {
                    ArgumentNullException.ThrowIfNull(Orders);

                    if (args.DataChangeType == DataChangeType.Add)
                    {
                        var newItem = await ordersRepository.AddAsync(item);
                        if (newItem is null)
                            throw new InvalidOperationException("The item could not be added.");
                        Orders.Add(newItem);
                    }
                    if (args.DataChangeType == DataChangeType.Edit)
                    {
                        if (!await ordersRepository.UpdateAsync(item))
                            throw new InvalidOperationException("The item could not be updated.");
                        Orders[args.SourceIndex] = item;
                    }
                    if (args.DataChangeType == DataChangeType.Delete)
                    {
                        if (!await ordersRepository.DeleteAsync(item))
                            throw new InvalidOperationException("The item could not be deleted.");
                        Orders.Remove(item);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    _ = Shell.Current.DisplayAlert("Error", ex.Message, "OK");
                    return false;
                }
            });
        }
        [RelayCommand]
        void CreateDetailFormViewModel(CreateDetailFormViewModelEventArgs args)
        {
            if (args.DetailFormType != DetailFormType.Edit)
                return;

            var item = new Order();
            Order.Copy((Order)args.Item!, item);
            args.Result = new DetailEditFormViewModel(item, isNew: false);
        }

        bool CanInitialize() => !IsInitialized;

        async Task<IEnumerable<Order>> GetItems()
        {
            var data = await ordersRepository.GetAsync();
            return data ?? Enumerable.Empty<Order>();
        }

        [RelayCommand]
        private void Refresh()
        {
            _ = InitializeAsync();
        }
    }
}
