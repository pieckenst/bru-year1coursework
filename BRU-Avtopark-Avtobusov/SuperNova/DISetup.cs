using System;
using Pure.DI;
using System.Diagnostics;
using SuperNova.IDE;
using SuperNova.Projects;
using SuperNova.Tools;
using SuperNova.VisualDesigner;
using static Pure.DI.Lifetime;
using MdiWindowManager = SuperNova.IDE.MdiWindowManager;
using SuperNova.Tools.Reports;
using SuperNova.Tools.Navigation;
using SuperNova.Forms.ViewModels;

namespace SuperNova;

public partial class DISetup
{
    [Conditional("DI")]
    static void Setup() =>
        DI.Setup()
            // Common tools/viewmodels as singletons
            .Bind().As(Singleton).To<ToolBoxToolViewModel>()
            .Bind().As(Singleton).To<PropertiesToolViewModel>()
            .Bind().As(Singleton).To<ProjectToolViewModel>()
            .Bind().As(Singleton).To<FormLayoutToolViewModel>()
            .Bind().As(Singleton).To<ImmediateToolViewModel>()
            .Bind().As(Singleton).To<LocalsToolViewModel>()
            .Bind().As(Singleton).To<WatchesToolViewModel>()
            .Bind().As(Singleton).To<ColorPaletteToolViewModel>()
            .Bind().As(Singleton).To<NavigationToolViewModel>()
            .Bind().As(Singleton).To<ReportsToolViewModel>()
            .Bind().As(Singleton).To<MDIControllerViewModel>()

            // Infrastructure and services
            .Bind().As(Singleton).To<MdiWindowManager>()
            .Bind().As(Singleton).To<WindowManager>()
            .Bind().As(Singleton).To<ProjectManager>()
            .Bind().As(Singleton).To<EditorService>()
            .Bind().As(Singleton).To<MainViewViewModel.DockFactory>()
            .Bind().As(Singleton).To<EventBus>()
            .Bind().As(Singleton).To<ProjectRunnerService>()
            .Bind().As(Singleton).To<ProjectService>()
            .Bind().As(Singleton).To<FocusedProjectUtil>()

            // ViewModel Roots
            .Bind().As(Singleton).To<MainWindowViewModel>()
            .Bind().As(Singleton).To<MainViewViewModel>()

            // Declare multiple roots
            .Root<MainWindowViewModel>("Root")
            .Root<MainViewViewModel>("MainViewRoot");


    // Design-time fallback only for MainWindowViewModel
    public static MainWindowViewModel DesignTimeRootViewModel => new DISetup().Root;

    // Optional helpers to get other roots
    public static MainViewViewModel DesignTimeMainViewRoot => new DISetup().MainViewRoot;
    
}
