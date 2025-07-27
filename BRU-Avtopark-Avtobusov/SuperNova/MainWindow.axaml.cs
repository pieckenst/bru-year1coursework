using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Input;
using PleasantUI.Controls;
using System;
using System.Linq;
using SuperNova.Forms.ViewModels;
using SuperNova.Forms.Views;
using SuperNova;
using SuperNova.IDE;
using SuperNova.Projects;
using SuperNova.Tools;
using SuperNova.VisualDesigner;
using SuperNova.Utils;

namespace SuperNova
{
    public partial class MainWindow : PleasantWindow
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly NavigationView _navigationView;
        private NavigationViewItem? _searchNavigationItem;
        private bool _isInitialized;
        private MainView? _mainView;

        public MainView? MainViewControl => _mainView ??= new MainView();

        public MainWindow()
        {
            InitializeComponent();

            // Set the window's data context to MainWindowViewModel
            _viewModel = Static.RootViewModel;
            DataContext = _viewModel;

            // Get the NavigationView control
            _navigationView = this.FindControl<NavigationView>("MainNavigationView");
            
            // Set up the main view page
            var mainViewPage = this.FindControl<NavigationViewItem>("MainViewViewPage");
            if (mainViewPage != null && !_isInitialized)
            {
                var mainView = MainViewControl;
                if (mainView != null)
                {
                    // Set the MainView's DataContext to MainViewViewModel
                    if (mainView.DataContext == null)
                    {
                        mainView.DataContext = Static.MainViewViewModel;
                    }

                    // Set up MainView command handling
                    CommandManager.SetCommandBindings(this, CommandManager.GetCommandBindings(mainView));
                    CommandManager.InvalidateRequerySuggested();
                    
                    // Set the main view content with proper DataContext
                    mainViewPage.FuncControl = () => {
                        if (mainView.DataContext == null)
                        {
                            mainView.DataContext = Static.MainViewViewModel;
                        }
                        return mainView;
                    };
                    
                    // Select the main view by default
                    if (_navigationView != null)
                    {
                        _navigationView.SelectedItem = mainViewPage;
                    }
                    
                    _isInitialized = true;
                }
            }
            
            // Initialize search cleared event
            _viewModel.SearchCleared += OnSearchCleared;

            // Navigation view and pages
            _navigationView = this.FindControl<NavigationView>("MainNavigationView");

            var MainViewViewPage = new MainView { DataContext = _viewModel.MainViewViewModel };
            //var myTicketsView = new MyTicketsView { DataContext = _viewModel.MyTicketsViewModel };
            //var scheduleView = new ScheduleView { DataContext = _viewModel.ScheduleViewModel };
            //var helpView = new HelpView { DataContext = _viewModel.HelpViewModel };
            //var aboutView = new AboutView { DataContext = _viewModel.AboutViewModel };
            

           

            // Bind navigation items to views
            var MainViewViewPageref = this.FindControl<NavigationViewItem>("MainViewViewModel");
            //var myTicketsPage = this.FindControl<NavigationViewItem>("MyTicketsPage");
            //var schedulePage = this.FindControl<NavigationViewItem>("SchedulePage");
            //var helpPage = this.FindControl<NavigationViewItem>("HelpPage");
            //var aboutPage = this.FindControl<NavigationViewItem>("AboutPage");

             //if (myTicketsPage != null) myTicketsPage.FuncControl += () => myTicketsView;
            //if (schedulePage != null) schedulePage.FuncControl += () => scheduleView;
            //if (helpPage != null) helpPage.FuncControl += () => helpView;
            //if (aboutPage != null) aboutPage.FuncControl += () => aboutView;

            // Search box logic
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
            Activated += OnActivated;
        }

        private void OnActivated(object? sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
            
            // Ensure MainView has the correct DataContext
            var mainView = MainViewControl;
            if (mainView != null && mainView.DataContext == null)
            {
                mainView.DataContext = Static.MainViewViewModel;
                
                // Re-apply command bindings to ensure they're using the correct DataContext
                CommandManager.SetCommandBindings(this, CommandManager.GetCommandBindings(mainView));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void OnSearchCleared(object? sender, EventArgs e)
        {
            RemoveSearchResults();
        }

        private void RemoveSearchResults()
        {
            if (_navigationView != null && _searchNavigationItem != null)
            {
                if (_navigationView.Items.Contains(_searchNavigationItem))
                {
                    _navigationView.Items.Remove(_searchNavigationItem);
                }

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
                if (!_navigationView.Items.Contains(_searchNavigationItem))
                {
                    _navigationView.Items.Insert(0, _searchNavigationItem);
                }

                _navigationView.SelectedItem = _searchNavigationItem;
            }
        }
    }
}
