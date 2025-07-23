using System;
using Avalonia.Controls;
using Avalonia.Labs.Input;

namespace SuperNova;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void AvaloniaOnWeb(object? sender, ExecutedRoutedEventArgs e)
    {
        TopLevel.GetTopLevel(this)!.Launcher.LaunchUriAsync(new Uri("https://avaloniaui.net"));
    }

    public MainView WindowInitialized()
    {
        if (DataContext is MainViewViewModel vm)
        {
            vm.OnInitialized();
        }
        return this;
    }
}