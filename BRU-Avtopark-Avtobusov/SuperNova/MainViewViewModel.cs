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
// Removed VB interpreter dependencies to prevent stack overflow:
// using SuperNova.Runtime;
// using SuperNova.Runtime.Components;
// using SuperNova.Runtime.Interpreter;
using SuperNova.Tools.Navigation;
using SuperNova.Tools.Reports;
using Serilog;
using System.Linq;

namespace SuperNova;

public partial class MainViewViewModel : ObservableObject, IMainViewViewModel
{
    private readonly IWindowManager windowManager;
    private readonly IProjectService projectService;
    private readonly DockFactory dockFactory;
    private readonly IProjectRunnerService projectRunnerService;
    private readonly IEventBus eventBus;

    public IMdiWindowManager MdiWindowManager { get; }

    public IWindowManager WindowManager => windowManager;

    public ToolBoxToolViewModel ToolBox { get; }

    public PropertiesToolViewModel Properties { get; }
    public ImmediateToolViewModel Immediate { get; }
    public FormLayoutToolViewModel FormLayout { get; }
    public LocalsToolViewModel Locals { get; }
    public WatchesToolViewModel Watches { get; }
    public ProjectToolViewModel ProjectExplorer { get; }
    public ColorPaletteToolViewModel ColorPalette { get; }
    public NavigationToolViewModel Navigation { get; }
    public ReportsToolViewModel Reports { get; }
    public required IFocusedProjectUtil FocusedProjectUtil { get; init; }

    public IRootDock Layout { get; }

    public DelegateCommand StartDefaultProjectCommand { get; }

    public DelegateCommand StartDefaultProjectWithFullCompileCommand { get; }

    public DelegateCommand BreakProjectCommand { get; }

    public DelegateCommand EndProjectCommand { get; }

    public DelegateCommand RestartProjectCommand { get; }

    public DelegateCommand ProjectReferencesCommand { get; }

    public DelegateCommand ProjectComponentsCommand { get; }

    public DelegateCommand ProjectPropertiesCommand { get; }

    public DelegateCommand MakeProjectCommand { get; }

    public DelegateCommand RemoveProjectCommand { get; }

    public string Title => FocusedProjectUtil.FocusedOrStartupProject?.Name is string projectName
        ? $"{projectName} - Avalonia Visual Basic {(projectRunnerService.IsRunning ? "[run]" : "[design]")}"
        : "Avalonia Visual Basic [design]";

    public string FocusedProjectName => FocusedProjectUtil.FocusedOrStartupProject?.Name ?? "Project";
    public string FocusedFormName => FocusedProjectUtil.FocusedForm?.Name ?? "Form";

    public bool IsStandardToolbarVisible { get; set; } = true;

    public class DockFactory : Factory
    {
        private readonly IMdiWindowManager mdiWindowManager;
        private readonly ToolBoxToolViewModel toolBox;
        private readonly ProjectToolViewModel project;
        private readonly PropertiesToolViewModel properties;
        private readonly FormLayoutToolViewModel formLayout;
        private readonly MDIControllerViewModel mdiController;
        private readonly NavigationToolViewModel navigation;
        private readonly ReportsToolViewModel reports;

        public ProportionalDock? LeftDock;
        public ProportionalDock? RightDock;
        public ProportionalDock? MiddleDock;

        public DockFactory(IMdiWindowManager mdiWindowManager,
            ToolBoxToolViewModel toolBox,
            ProjectToolViewModel project,
            PropertiesToolViewModel properties,
            FormLayoutToolViewModel formLayout,
            MDIControllerViewModel mdiController,
            NavigationToolViewModel navigation,
            ReportsToolViewModel reports)
        {
            this.mdiWindowManager = mdiWindowManager;
            this.toolBox = toolBox;
            this.project = project;
            this.properties = properties;
            this.formLayout = formLayout;
            this.mdiController = mdiController;
            this.navigation = navigation;
            this.reports = reports;
        }

        public override IToolDock CreateToolDock()
        {
            var toolDock = base.CreateToolDock();
            toolDock.CanFloat = false;
            return toolDock;
        }

        // 3. Add this method to DockFactory to handle tool visibility
        public void ShowTool(string toolId)
        {
            if (LeftDock?.VisibleDockables?.FirstOrDefault(d => d.Id == toolId) is IDockable tool)
            {
                if (tool is IToolDock toolDock)
                {
                    toolDock.IsActive = true;
                }
            }
        }

        public override IRootDock CreateLayout()
        {
            var toolBoxTool = CreateToolDock();
            toolBoxTool.ActiveDockable = toolBox;
            toolBoxTool.VisibleDockables = CreateList<IDockable>(toolBox);
            toolBoxTool.Alignment = Alignment.Left;
            toolBoxTool.Proportion = 0.06;

            var projectTool = CreateToolDock();
            projectTool.ActiveDockable = project;
            projectTool.VisibleDockables = CreateList<IDockable>(project);
            projectTool.Alignment = Alignment.Top;

            var propertiesTool = CreateToolDock();
            propertiesTool.ActiveDockable = properties;
            propertiesTool.VisibleDockables = CreateList<IDockable>(properties);
            propertiesTool.Alignment = Alignment.Top;

            var formLayoutTool = CreateToolDock();
            formLayoutTool.ActiveDockable = formLayout;
            formLayoutTool.VisibleDockables = CreateList<IDockable>(formLayout);
            formLayoutTool.Alignment = Alignment.Top;

            var navigationTool = CreateToolDock();
            navigationTool.Id = "NavigationTool";  // Add this line
            navigationTool.ActiveDockable = navigation;
            navigationTool.VisibleDockables = CreateList<IDockable>(navigation);
            navigationTool.Alignment = Alignment.Left;
            navigationTool.Proportion = 0.2;
            navigationTool.Title = "Navigation";  // Ensure title is set
            navigationTool.CanClose = false;  // Make sure it can be closed/opened
            navigationTool.IsCollapsable = true;  // Allow collapsing
            // Add new reports tool
            var reportsTool = CreateToolDock();
            reportsTool.ActiveDockable = reports;
            reportsTool.VisibleDockables = CreateList<IDockable>(reports);
            reportsTool.Alignment = Alignment.Bottom;
            reportsTool.Proportion = 0.3;

            // 2. Update the LeftDock configuration
            LeftDock = new ProportionalDock
            {
                Orientation = Orientation.Vertical,
                Proportion = 0.2,
                CanClose = true,
                CanFloat = true,
                IsCollapsable = true,
                Context = nameof(LeftDock),
                Id = "LeftDock",  // Add ID
                VisibleDockables = CreateList<IDockable>
                (
                    navigationTool // toolbox aint needed for now - i will rework it later 
                    
                )
            };

            RightDock = new ProportionalDock
            {
                Orientation = Orientation.Vertical,
                Proportion = 0.2155,
                CanClose = true,
                CanFloat = true,
                IsCollapsable = true,
                Context = nameof(RightDock),
                VisibleDockables = CreateList<IDockable>
                (
                    

                    propertiesTool //no we dont need form layout tool here anymore -and project tool is out of here for now ,till i rework it

                    
                )
            };

            var documentDock = new DocumentDock()
            {
                ActiveDockable = mdiController,
                CanClose = true,
                CanFloat = true,
                IsCollapsable = true,
                VisibleDockables = CreateList<IDockable>(mdiController),
            };

            // Add reports to bottom of middle dock
            MiddleDock = new ProportionalDock()
            {
                Orientation = Orientation.Vertical,
                CanClose = false,
                IsCollapsable = false,
                CanFloat = false,
                Context = nameof(MiddleDock),
                VisibleDockables = CreateList<IDockable>
                (
                    documentDock,

                    reportsTool
                )
            };

            var mainLayout = new ProportionalDock
            {
                Orientation = Orientation.Horizontal,
                CanFloat = false,
                VisibleDockables = CreateList<IDockable>
                (
                    LeftDock,

                    MiddleDock,

                    RightDock
                )
            };

            var rootDock = CreateRootDock();

            rootDock.IsFocusableRoot = true;
            rootDock.IsCollapsable = false;
            rootDock.ActiveDockable = mainLayout;
            rootDock.DefaultDockable = mainLayout;
            rootDock.VisibleDockables = CreateList<IDockable>(mainLayout);

            return rootDock;
        }
    }

    public MainViewViewModel(IWindowManager windowManager,
        MdiWindowManager mdiWindowManager,
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
        IProjectManager projectManager,
        IFocusedProjectUtil focusedProjectUtil,
        IProjectService projectService,
        DockFactory dockFactory,
        IProjectRunnerService projectRunnerService,
        IEventBus eventBus)
    {
        this.windowManager = windowManager;
        this.projectService = projectService;
        this.dockFactory = dockFactory;
        this.projectRunnerService = projectRunnerService;
        this.eventBus = eventBus;
        MdiWindowManager = mdiWindowManager;
        ToolBox = toolBox;
        Properties = properties;
        Immediate = immediate;
        FormLayout = formLayout;
        Locals = locals;
        Watches = watches;
        ProjectExplorer = projectExplorer;
        ColorPalette = colorPalette;
        Navigation = navigation;
        Reports = reports;
        FocusedProjectUtil = focusedProjectUtil;

        Layout = dockFactory.CreateLayout();
        dockFactory.InitLayout(Layout);



        StartDefaultProjectCommand = new DelegateCommand(projectRunnerService.RunStartupProject,
            () => projectRunnerService.CanStartDefaultProject);
        StartDefaultProjectWithFullCompileCommand = new DelegateCommand(projectRunnerService.RunStartupProject,
            () => projectRunnerService.CanStartDefaultProjectWithFullCompile);
        BreakProjectCommand = new DelegateCommand(projectRunnerService.BreakCurrentProject,
            () => projectRunnerService.CanBreakProject);
        EndProjectCommand = new DelegateCommand(projectRunnerService.EndProject,
            () => projectRunnerService.CanEndProject);
        RestartProjectCommand = new DelegateCommand(projectRunnerService.RestartProject,
            () => projectRunnerService.CanRestartProject);
        ProjectReferencesCommand = new DelegateCommand(() => projectService.EditProjectReferences(FocusedProjectUtil.FocusedOrStartupProject!),
            () => FocusedProjectUtil.FocusedOrStartupProject != null);
        ProjectComponentsCommand = new DelegateCommand(() => projectService.EditProjectComponents(FocusedProjectUtil.FocusedOrStartupProject!),
            () => FocusedProjectUtil.FocusedOrStartupProject != null);
        ProjectPropertiesCommand = new DelegateCommand(() => projectService.EditProjectProperties(FocusedProjectUtil.FocusedOrStartupProject!),
            () => FocusedProjectUtil.FocusedOrStartupProject != null);
        MakeProjectCommand = new DelegateCommand(() => projectService.MakeProject(FocusedProjectUtil.FocusedOrStartupProject!).ListenErrors(),
            () => FocusedProjectUtil.FocusedOrStartupProject != null);
        RemoveProjectCommand = new DelegateCommand(() => projectService.UnloadProject(FocusedProjectUtil.FocusedOrStartupProject!).ListenErrors(),
            () => FocusedProjectUtil.FocusedOrStartupProject != null);

        FocusedProjectUtil.ObservePropertyChanged(x => x.FocusedOrStartupProject)
            .Subscribe(_ =>
            {
                ProjectReferencesCommand.RaiseCanExecutedChanged();
                ProjectComponentsCommand.RaiseCanExecutedChanged();
                ProjectPropertiesCommand.RaiseCanExecutedChanged();
                MakeProjectCommand.RaiseCanExecutedChanged();
                RemoveProjectCommand.RaiseCanExecutedChanged();
                OnPropertyChanged(nameof(Title));
            });

        projectRunnerService.ObservePropertyChanged(x => x.IsRunning)
            .Subscribe(_ => OnPropertyChanged(nameof(Title)));
    }

    public void OnInitialized()
    {
        //projectService.CreateNewProject().ListenErrors();
    }

    public void NYI()
    {
        windowManager.MessageBox("Данная функция ещё не реализована", "Погодьте", MessageBoxButtons.Ok, MessageBoxIcon.Information);
    }

    public void SaveProject() => projectService.SaveAllProjects(false).ListenErrors();

    public void SaveProjectAs() => projectService.SaveAllProjects(true).ListenErrors();

    public void OpenProject() => projectService.OpenProject().ListenErrors();

    public void MakeProject() => projectService.MakeProject().ListenErrors();

    public void OpenAddInManager() => windowManager.ShowDialog(new AddInManagerViewModel());

    public void OpenOptions() => windowManager.ShowDialog(new OptionsViewModel());



    public void OpenNavigationTool()
    {
        if (dockFactory is DockFactory factory)
        {
            factory.ShowTool("NavigationTool");
        }
    }

    public void Help()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var helpWindow = new HelpWindow();
            helpWindow.ShowDialog(desktop.MainWindow);
        }
    }

    /// <summary>
    /// Safe method to open business forms in MDI without VB interpreter dependencies
    /// </summary>
    public void OpenBusinessForm(string title, object content)
    {
        try
        {
            var businessWindow = new BusinessMDIWindow(title, content);
            MdiWindowManager.OpenWindow(businessWindow);
        }
        catch (Exception ex)
        {
            // Log error without causing stack overflow
            System.Diagnostics.Debug.WriteLine($"Error opening business form: {ex.Message}");
        }
    }

    public void About()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog(desktop.MainWindow);
        }
    }

    public async Task NewProject()
    {
        await projectService.UnloadAllProjects();
        await projectService.CreateNewProject();
    }

    public async Task AddProject(IProjectTemplate? projectTemplate)
    {
        if (projectTemplate == null)
            await projectService.CreateNewProject();
        else
            await projectService.CreateNewProject(projectTemplate);
    }

    private void DebuggingNotImplementedYet() =>
        windowManager.MessageBox("Debugging is not yet implemented. Maybe one day?", icon: MessageBoxIcon.Information).ListenErrors();

    public void StepInto() => DebuggingNotImplementedYet();
    public void StepOver() => DebuggingNotImplementedYet();
    public void StepOut() => DebuggingNotImplementedYet();
    public void RunToCursor() => DebuggingNotImplementedYet();
    public void AddWatch() => DebuggingNotImplementedYet();
    public void EditWatch() => DebuggingNotImplementedYet();
    public void QuickWatch() => DebuggingNotImplementedYet();
    public void ToggleBreakpoint() => DebuggingNotImplementedYet();
    public void ClearAllBreakpoints() => DebuggingNotImplementedYet();
    public void SetNextStatement() => DebuggingNotImplementedYet();
    public void ShowNextStatement() => DebuggingNotImplementedYet();

    public async Task OpenGithubRepo()
    {
        if (await windowManager.MessageBox(
                "This will open a new tab with this project github repo, but due to a bug in .NET/Avalonia it will also freeze this tab (just refresh the tab).",
                buttons: MessageBoxButtons.YesNo) == MessageBoxResult.No)
            return;

        TopLevel.GetTopLevel(Static.MainView)!.Launcher.LaunchUriAsync(new Uri("https:github.com/BAndysc/AvaloniaVisualBasic6")).ListenErrors();
    }

    public void TileHorizontally() => eventBus.Publish(new RearrangeMDIEvent(MDIRearrangeKind.TileHorizontally));

    public void TileVertically() => eventBus.Publish(new RearrangeMDIEvent(MDIRearrangeKind.TileVertically));

    public void Cascade() => eventBus.Publish(new RearrangeMDIEvent(MDIRearrangeKind.Cascade));

    private T? FindDock<T>(Func<T, bool> action) where T : class, IDockable => FindDock<T>(Layout, action);

    private T? FindDock<T>(IDockable dockable, Func<T, bool> predicate) where T : class, IDockable
    {
        if (dockable is T t && predicate(t))
            return t;
        if (dockable is IDock dock && dock.VisibleDockables != null)
        {
            foreach (var visible in dock.VisibleDockables)
                if (FindDock<T>(visible, predicate) is { } ret)
                    return ret;
        }

        return null;
    }

    private void OpenOrActivateTool(Tool tool, bool right)
    {
        var opened = FindDock<IDockable>(x => ReferenceEquals(x, tool));
        if (opened != null)
        {
            dockFactory.SetFocusedDockable((IDock)opened.Owner!, opened);
            return;
        }

        var middle = FindDock<IDock>(x => x.Context?.Equals(right ? nameof(DockFactory.RightDock) : nameof(DockFactory.MiddleDock)) ?? false)!;
        var toolDock = dockFactory.CreateToolDock();
        toolDock.ActiveDockable = tool;
        toolDock.Factory = dockFactory;
        toolDock.Proportion = 0.3;
        toolDock.VisibleDockables = dockFactory.CreateList<IDockable>(tool);
        toolDock.Alignment = right ? Alignment.Right : Alignment.Bottom;
        middle.VisibleDockables ??= dockFactory.CreateList<IDockable>();
        middle.VisibleDockables.Add(new ProportionalDockSplitter());
        middle.VisibleDockables.Add(toolDock);
        dockFactory.InitDockable(toolDock, middle);
        dockFactory.SetFocusedDockable(toolDock, opened);
    }

    public void OpenImmediateTool() => OpenOrActivateTool(Immediate, false);
    public void OpenLocalsTool() => OpenOrActivateTool(Locals, false);
    public void OpenWatchesTool() => OpenOrActivateTool(Watches, false);
    public void OpenColorPaletteTool() => OpenOrActivateTool(ColorPalette, false);
    public void OpenProjectExplorerTool() => OpenOrActivateTool(ProjectExplorer, true);
    public void OpenPropertiesTool() => OpenOrActivateTool(Properties, true);
    public void OpenFormLayoutTool() => OpenOrActivateTool(FormLayout, true);
    public void OpenToolBox() => OpenOrActivateTool(ToolBox, true);

    public async void OpenUserManagement()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new UserManagementWindow
        {
            DataContext = new UserManagementViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing UserManagementWindow");
        }
    }

    public async void OpenEmployeeManagement()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new EmployeeManagementWindow
        {
            DataContext = new EmployeeManagementViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing EmployeeManagementWindow");
        }
    }

    public async void OpenJobManagement()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new JobManagementWindow
        {
            DataContext = new JobManagementViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing JobManagementWindow");
        }
    }

    public async void OpenBusManagement()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new BusManagementWindow
        {
            DataContext = new BusManagementViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing BusManagementWindow");
        }
    }

    public async void OpenRouteManagement()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new RouteManagementWindow
        {
            DataContext = new RouteManagementViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing RouteManagementWindow");
        }
    }

    public async void OpenMaintenanceManagement()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new MaintenanceManagementWindow
        {
            DataContext = new MaintenanceManagementViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing MaintenanceManagementWindow");
        }
    }

    public async void OpenTicketManagement()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new TicketManagementWindow
        {
            DataContext = new TicketManagementViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing TicketManagementWindow");
        }
    }

    public async void OpenSalesManagement()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new SalesManagementWindow
        {
            DataContext = new SalesManagementViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing SalesManagementWindow");
        }
    }

    public async void OpenSalesStatistics()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new SalesStatisticsWindow
        {
            DataContext = new SalesStatisticsViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing SalesStatisticsWindow");
        }
    }

    public async void OpenIncomeReport()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var window = new IncomeReportWindow
        {
            DataContext = new IncomeReportViewModel()
        };

        try
        {
            await window.ShowDialog(desktop.MainWindow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error showing IncomeReportWindow");
        }
    }

    public void OpenRouteStatistics()
    {
        windowManager.MessageBox(
            "Функция статистики маршрутов будет доступна в следующей версии.",
            "В разработке",
            MessageBoxButtons.Ok).ListenErrors();
    }

    public void ExportReport()
    {
        windowManager.MessageBox(
            "Функция экспорта отчетов будет доступна в следующей версии.",
            "В разработке",
            MessageBoxButtons.Ok).ListenErrors();
    }

    public void OpenCalculator()
    {
        windowManager.MessageBox(
            "Функция калькулятора будет доступна в следующей версии.",
            "В разработке",
            MessageBoxButtons.Ok).ListenErrors();
    }

    public void OpenCalendar()
    {
        windowManager.MessageBox(
            "Функция календаря будет доступна в следующей версии.",
            "В разработке",
            MessageBoxButtons.Ok).ListenErrors();
    }

    public void AdvancedSearch()
    {
        windowManager.MessageBox(
            "Функция расширенного поиска будет доступна в следующей версии.",
            "В разработке",
            MessageBoxButtons.Ok).ListenErrors();
    }

    public void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}

public class VBMDIWindow : MDIWindow
{
    protected override Type StyleKeyOverride => typeof(MDIWindow);

    // Remove VB interpreter dependencies completely
    // Keep only basic MDI window functionality

    public VBMDIWindow()
    {
        // Initialize without VB interpreter
    }

    // Remove all VB interpreter related methods and properties
    // The window now functions as a pure MDI window without VB execution capabilities
}

/// <summary>
/// Business MDI Window for administrative forms without VB interpreter dependencies
/// </summary>
public class BusinessMDIWindow : MDIWindow, ISafeMdiWindow
{
    protected override Type StyleKeyOverride => typeof(MDIWindow);

    // Implement the CloseRequest event from IMdiWindow interface
    public new event Action<IMdiWindow>? CloseRequest;

    public BusinessMDIWindow()
    {
        // Safe initialization without circular dependencies
        CanClose = true;

        // Wire up the base CloseRequest to our new event
        base.CloseRequest += (window) => CloseRequest?.Invoke(this);
    }

    public BusinessMDIWindow(string title) : this()
    {
        Title = title;
    }

    public BusinessMDIWindow(string title, object content) : this(title)
    {
        Content = content;
    }

    // Safe property setters without VB interpreter
    public void SetTitle(string title)
    {
        Title = title;
    }

    public void SetContent(object content)
    {
        Content = content;
    }

    public void SafeClose()
    {
        // Safe close without VB interpreter calls
        // MDIWindow doesn't have CloseRequest, so we need to use the proper close mechanism
        Close();
    }
}
