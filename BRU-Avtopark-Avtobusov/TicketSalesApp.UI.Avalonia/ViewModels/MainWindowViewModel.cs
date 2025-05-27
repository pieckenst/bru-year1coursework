// UI/Avalonia/ViewModels/MainViewModel.cs
using Avalonia.Controls;
using Avalonia.Layout;
using Dock.Model.Controls;
using PleasantUI;
using PleasantUI.Controls;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;
using TicketSalesApp.UI.Avalonia.Views;
using System.Linq;
using TicketSalesApp.UI.Avalonia.Views.Pages;
using System.Threading.Tasks;

namespace TicketSalesApp.UI.Avalonia.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _searchText = string.Empty;
        private bool _isNavigationViewOpen = true;
        private bool _isSearching;

        public event EventHandler? SearchCleared;

        public TicketSearchViewModel TicketSearchViewModel { get; }
        public MyTicketsViewModel MyTicketsViewModel { get; }
        public ScheduleViewModel ScheduleViewModel { get; }
        public HelpViewModel HelpViewModel { get; }
        public AboutViewModel AboutViewModel { get; }
        public SearchResultsViewModel SearchResultsViewModel { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 3 && !_isSearching)
                {
                    _ = PerformSearchAsync(value);
                }
            }
        }

        public bool IsNavigationViewOpen
        {
            get => _isNavigationViewOpen;
            set => this.RaiseAndSetIfChanged(ref _isNavigationViewOpen, value);
        }

        public async Task PerformSearchAsync(string query)
        {
            if (_isSearching) return;
            _isSearching = true;

            try
            {
                // Update search results
                await SearchResultsViewModel.UpdateResults(query);
            }
            finally
            {
                _isSearching = false;
            }
        }

        public MainWindowViewModel()
        {
            // Initialize ViewModels
            TicketSearchViewModel = new TicketSearchViewModel();
            MyTicketsViewModel = new MyTicketsViewModel();
            ScheduleViewModel = new ScheduleViewModel();
            HelpViewModel = new HelpViewModel();
            AboutViewModel = new AboutViewModel();
            SearchResultsViewModel = new SearchResultsViewModel();

            // Initialize commands
            ClearSearchCommand = ReactiveCommand.Create(ClearSearch);
        }

        public IReactiveCommand ClearSearchCommand { get; }

        private void ClearSearch()
        {
            SearchText = string.Empty;
            SearchResultsViewModel.ClearResults();
            SearchCleared?.Invoke(this, EventArgs.Empty);
        }
    }
}
