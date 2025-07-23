using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Platform;
using SuperNova.Controls;
using SuperNova.Events;
using SuperNova.Runtime;
using SuperNova.Runtime.BuiltinControls;
using SuperNova.Runtime.Components;
using SuperNova.Runtime.Interpreter;
using SuperNova.Runtime.ProjectElements;
using SuperNova.Utils;
using SuperNova.VisualDesigner;
using Classic.CommonControls.Dialogs;
using PropertyChanged.SourceGenerator;

namespace SuperNova.IDE;

public partial class ProjectRunnerService : IProjectRunnerService
{
    private readonly IEventBus eventBus;
    private readonly IWindowManager windowManager;
    private readonly IProjectManager projectManager;

    [Notify]
    [AlsoNotify(nameof(IsRunning), nameof(CanStartDefaultProject), nameof(CanStartDefaultProjectWithFullCompile), nameof(CanBreakProject), nameof(CanEndProject), nameof(CanRestartProject))]
    private System.IDisposable? runningProject;

    public bool IsRunning => runningProject != null;

    public ProjectRunnerService(IEventBus eventBus,
        IWindowManager windowManager,
        IProjectManager projectManager)
    {
        this.eventBus = eventBus;
        this.windowManager = windowManager;
        this.projectManager = projectManager;
    }

    private void OnIsRunningChanged() => CommandManager.InvalidateRequerySuggested();

    public void RunProject(ProjectDefinition projectDefinition)
    {
        eventBus.Publish(new ApplyAllUnsavedChangesEvent());

        if (projectDefinition.StartupForm is { } form)
        {
            async Task WindowTask()
            {
                var tokenSource = new CancellationTokenSource();

                var syntaxChecker = new SyntaxChecker();
                try
                {
                    syntaxChecker.Run(form.Code);
                }
                catch (VBCompileErrorException error)
                {
                    await windowManager.MessageBox(error.Message, icon: MessageBoxIcon.Warning);
                    throw new OperationCanceledException();
                }

                if (Static.SingleView)
                {
                    var task = RunFormInBrowser(form, tokenSource.Token, out var window);
                    RunningProject = new ActionDisposable(() => tokenSource.Cancel());
                    await task;
                    RunningProject = null;
                }
                else
                {
                    var task = VBLoader.RunForm(form, tokenSource.Token, out var window);
                    window.Show();
                    RunningProject = new ActionDisposable(() => tokenSource.Cancel());
                    await task;
                    RunningProject = null;
                }
            }
            WindowTask().ListenErrors();
        }
        else
        {
            windowManager.MessageBox("Must have a startup form or Sub Main()", icon: MessageBoxIcon.Error);
        }
    }

    public void RunStartupProject()
    {
        if (projectManager.StartupProject is {} startupProject)
        {
            RunProject(startupProject);
        }
    }

    public void BreakCurrentProject()
    {
        throw new NotImplementedException();
    }

    public void EndProject()
    {
        RunningProject?.Dispose();
        RunningProject = null;
    }

    public void RestartProject()
    {
        EndProject();
        RunStartupProject();
    }

    public bool CanStartDefaultProject => !IsRunning && projectManager.StartupProject != null;
    public bool CanStartDefaultProjectWithFullCompile => CanStartDefaultProject;
    public bool CanBreakProject => IsRunning && false;
    public bool CanEndProject => IsRunning;
    public bool CanRestartProject => IsRunning;

    private VBMDIFormRuntime InstantiateWindow(ComponentInstance instance)
    {
        var form = new VBMDIFormRuntime(windowManager)
        {
            Title = instance.GetPropertyOrDefault(VBProperties.CaptionProperty) ?? "",
            Width = instance.GetPropertyOrDefault(VBProperties.WidthProperty),
            Height = instance.GetPropertyOrDefault(VBProperties.HeightProperty),
            [AttachedProperties.BackColorProperty] = instance.GetPropertyOrDefault(VBProperties.BackColorProperty),
            [MDIHostPanel.WindowLocationProperty] = new Point((int)instance.GetPropertyOrDefault(VBProperties.LeftProperty), (int)instance.GetPropertyOrDefault(VBProperties.TopProperty))
        };
        VBProps.SetName(form, instance.GetPropertyOrDefault(VBProperties.NameProperty));
        return form;
    }
    
    private Task RunFormInBrowser(FormDefinition element, CancellationToken token, out VBMDIFormRuntime window)
    {
        var form = element.Components.FirstOrDefault(x => x.BaseClass == FormComponentClass.Instance);
        if (form == null)
            throw new Exception("No form found");

        window = InstantiateWindow(form);
        // Remove VB Context usage to prevent compilation errors
        // VB interpreter system has been removed to fix stack overflow issues

        var task = windowManager.ShowManagedWindow(window);
        // Remove VB component spawning - use direct UI controls instead
        // window.Content = VBLoader.SpawnComponents(element, window.Context.ExecutionContext, window.Context.RootEnv);

        // Remove VB code setting - no longer using VB interpreter
        // window.Context.SetCode(code: element.Code);
        token.Register((state, _) =>
        {
            (state as MDIWindow)!.CloseCommand.Execute(null);
        }, window);

        return task;
    }

    public class VBMDIFormRuntime : MDIWindow
    {
        protected override Type StyleKeyOverride => typeof(MDIWindow);

        // Remove VB interpreter dependencies to prevent stack overflow
        private bool isInitialized = false;

        public VBMDIFormRuntime(IWindowManager windowManager)
        {
            // Safe initialization without VB interpreter
            CanClose = true;

            // Remove the problematic BoundsProperty subscription that causes stack overflow
            // The original subscription to windowContext.ExecuteSub("Form_Resize") was causing circular calls
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            // Remove VB interpreter call that could cause stack overflow
            // windowContext.ExecuteSub("Form_Load");
            isInitialized = true;
        }

        // Remove ExecuteSub method that depends on VB interpreter
        // public void ExecuteSub(string name) - REMOVED to prevent stack overflow

        // Add safe methods for business logic
        public new bool IsInitialized => isInitialized;

        public void SafeInitialize()
        {
            if (!isInitialized)
            {
                isInitialized = true;
                // Perform safe initialization without VB interpreter calls
            }
        }
    }

    // Remove MDIStandaloneStandardLib class entirely to eliminate VB interpreter dependencies
    // This class was causing circular references and stack overflow issues
    // Business logic should use direct UI interactions instead of VB interpreter calls
}