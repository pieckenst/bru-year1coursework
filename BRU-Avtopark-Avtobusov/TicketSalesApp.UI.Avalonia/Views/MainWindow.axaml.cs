using Avalonia;
using Avalonia.Controls;
using PleasantUI.Controls;

using TicketSalesApp.UI.Avalonia.ViewModels;
using TicketSalesApp.UI.Avalonia.Views.Pages;

namespace TicketSalesApp.UI.Avalonia.Views
{
    public partial class MainWindow : PleasantWindow
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly SearchResultsView _searchResultsView;
        private readonly NavigationView _navigationView;
        private NavigationViewItem? _searchNavigationItem;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainWindowViewModel();
            DataContext = _viewModel;

            // Subscribe to search cleared event
            _viewModel.SearchCleared += OnSearchCleared;

            // Get the NavigationView control
            _navigationView = this.FindControl<NavigationView>("MainNavigationView");

            // Create pages with proper DataContext
            var ticketSearchView = new TicketSearchView { DataContext = _viewModel.TicketSearchViewModel };
            var myTicketsView = new MyTicketsView { DataContext = _viewModel.MyTicketsViewModel };
            var scheduleView = new ScheduleView { DataContext = _viewModel.ScheduleViewModel };
            var helpView = new HelpView { DataContext = _viewModel.HelpViewModel };
            var aboutView = new AboutView { DataContext = _viewModel.AboutViewModel };
            _searchResultsView = new SearchResultsView { DataContext = _viewModel.SearchResultsViewModel };

            // Create search navigation item
            _searchNavigationItem = new NavigationViewItem
            {
                Header = "Результаты поиска",
                FuncControl = () => _searchResultsView
            };

            // Bind navigation items to their views
            var ticketSearchPage = this.FindControl<NavigationViewItem>("TicketSearchPage");
            var myTicketsPage = this.FindControl<NavigationViewItem>("MyTicketsPage");
            var schedulePage = this.FindControl<NavigationViewItem>("SchedulePage");
            var helpPage = this.FindControl<NavigationViewItem>("HelpPage");
            var aboutPage = this.FindControl<NavigationViewItem>("AboutPage");

            if (ticketSearchPage != null) ticketSearchPage.FuncControl += () => ticketSearchView;
            if (myTicketsPage != null) myTicketsPage.FuncControl += () => myTicketsView;
            if (schedulePage != null) schedulePage.FuncControl += () => scheduleView;
            if (helpPage != null) helpPage.FuncControl += () => helpView;
            if (aboutPage != null) aboutPage.FuncControl += () => aboutView;

            // Initialize search handling
            var searchBox = this.FindControl<TextBox>("SearchBox");
            if (searchBox != null)
            {
                searchBox.TextChanged += async (s, e) =>
                {
                    var text = searchBox.Text;
                    if (!string.IsNullOrWhiteSpace(text) && text.Length >= 3)
                    {
                        await _viewModel.PerformSearchAsync(text);
                        ShowSearchResults();
                    }
                    else if (string.IsNullOrWhiteSpace(text))
                    {
                        RemoveSearchResults();
                    }
                };
            }

            var menuButton = this.FindControl<Button>("MenuButton");
            if (menuButton != null)
            {
                menuButton.Click += (s, e) =>
                {
                    _viewModel.IsNavigationViewOpen = !_viewModel.IsNavigationViewOpen;
                };
            }

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void OnSearchCleared(object? sender, EventArgs e)
        {
            RemoveSearchResults();
        }

        private void RemoveSearchResults()
        {
            if (_navigationView != null && _searchNavigationItem != null)
            {
                // Remove search item from navigation
                if (_navigationView.Items.Contains(_searchNavigationItem))
                {
                    _navigationView.Items.Remove(_searchNavigationItem);
                }

                // Select the first item if search was selected
                if (_navigationView.SelectedItem == _searchNavigationItem)
                {
                    _navigationView.SelectedItem = _navigationView.Items.Cast<NavigationViewItem>().FirstOrDefault();
                }
            }
        }

        private void ShowSearchResults()
        {
            if (_navigationView != null && _searchNavigationItem != null)
            {
                // Add search item to navigation if not already present
                if (!_navigationView.Items.Contains(_searchNavigationItem))
                {
                    _navigationView.Items.Insert(0, _searchNavigationItem);
                }

                // Select the search item to show results
                _navigationView.SelectedItem = _searchNavigationItem;
            }
        }
    }
}