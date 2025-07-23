using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Input;
using Classic.Avalonia.Theme;
using PleasantUI.Controls;
using System;

namespace SuperNova;

public partial class MainWindow : PleasantWindow
{
    private bool once;
    private MainView? _mainView;
    
    // Renamed property to avoid ambiguity
    public MainView? MainViewControl => _mainView ??= this.FindControl<MainView>("MainView")!;
    
    public MainWindow()
    {
        InitializeComponent();

        // Ensure MainViewControl is found before setting up commands
        var mainView = MainViewControl;
        if (mainView != null)
        {
            // this is required to make commands work without focusing the MainView first
            CommandManager.SetCommandBindings(this, CommandManager.GetCommandBindings(mainView));
            CommandManager.InvalidateRequerySuggested();
        }

#if DEBUG
        this.AttachDevTools();
#endif

        Activated += OnActivated;
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        CommandManager.InvalidateRequerySuggested();
        if (once)
            return;
        once = true;
        
        var mainView = MainViewControl;
        if (mainView != null)
        {
            mainView.WindowInitialized();
        }
    }
}