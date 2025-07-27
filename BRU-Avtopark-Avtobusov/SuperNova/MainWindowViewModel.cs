// UI/Avalonia/ViewModels/MainViewModel.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SuperNova.Events;
using SuperNova.Forms.ViewModels;
using SuperNova.Forms.Views;
using SuperNova.Forms.AdministratorUi.Views;
using SuperNova.Forms.AdministratorUi.ViewModels;
using SuperNova.IDE;
using SuperNova.Projects;
using Classic.CommonControls.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using PropertyChanged.SourceGenerator;
using R3;
using MdiWindowManager = SuperNova.IDE.MdiWindowManager;
using SuperNova.VisualDesigner;
using SuperNova.Tools;
using SuperNova.Utils;
using SuperNova.Controls;

using PleasantUI;
using PleasantUI.Controls;
using ReactiveUI;
// Removed VB interpreter dependencies to prevent stack overflow:
// using SuperNova.Runtime;
// using SuperNova.Runtime.Components;
// using SuperNova.Runtime.Interpreter;
using SuperNova.Tools.Navigation;
using SuperNova.Tools.Reports;
using Serilog;
using System.Linq;

namespace SuperNova.Forms.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string _searchText = string.Empty;
        private bool _isNavigationViewOpen = true;
        private bool _isSearching;
        private readonly IWindowManager _windowManager;
        private readonly IProjectService _projectService;
        private readonly IEventBus _eventBus;
        private readonly IProjectRunnerService _projectRunnerService;
        private readonly IProjectManager _projectManager;
        private readonly IFocusedProjectUtil _focusedProjectUtil;

        public  readonly MainViewViewModel MainViewViewModelref;

        public event EventHandler? SearchCleared;
/// <summary>
/// leave all variables as placeholders
/// </summary>
        public MainViewViewModel MainViewViewModel { get; } 
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

        public MainWindowViewModel(
            IWindowManager windowManager,
            IMdiWindowManager mdiWindowManager,
            IProjectService projectService,
            IProjectRunnerService projectRunnerService,
            IEventBus eventBus,
            IProjectManager projectManager,
            IFocusedProjectUtil focusedProjectUtil,
            ToolBoxToolViewModel toolBox,
            PropertiesToolViewModel properties,
            ImmediateToolViewModel immediate,
            FormLayoutToolViewModel formLayout,
            LocalsToolViewModel locals,
            WatchesToolViewModel watches,
            ProjectToolViewModel projectExplorer,
            ColorPaletteToolViewModel colorPalette,
            NavigationToolViewModel navigation,
            ReportsToolViewModel reports,
            MainViewViewModel.DockFactory dockfactory,
            MainViewViewModel mainViewViewModel)
        {
            _windowManager = windowManager;
            _projectService = projectService;
            _eventBus = eventBus;
            _projectRunnerService = projectRunnerService;
            _projectManager = projectManager;
            _focusedProjectUtil = focusedProjectUtil;
            
            // Set the MainViewViewModel from DI
            MainViewViewModel = new MainViewViewModel(
                windowManager,
                (MdiWindowManager)mdiWindowManager,
                toolBox,
                properties,
                immediate,
                formLayout,
                locals,
                watches,
                projectExplorer,
                colorPalette,
                navigation,
                reports,
                projectManager,
                focusedProjectUtil,
                projectService,
                dockfactory,
                projectRunnerService,
                eventBus
            );
            
            SearchResultsViewModel = new SearchResultsViewModel();

            // Initialize commands
            ClearSearchCommand = ReactiveUI.ReactiveCommand.CreateFromTask<System.Reactive.Unit, System.Reactive.Unit>(_ => 
            {
                ClearSearch();
                return System.Threading.Tasks.Task.FromResult(System.Reactive.Unit.Default);
            });
            
           
        }

        public ReactiveUI.ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ClearSearchCommand { get; }

        private void ClearSearch()
        {
            SearchText = string.Empty;
            SearchResultsViewModel.ClearResults();
            SearchCleared?.Invoke(this, EventArgs.Empty);
        }
    }
}
