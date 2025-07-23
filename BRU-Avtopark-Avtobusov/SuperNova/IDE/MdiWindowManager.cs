using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PropertyChanged.SourceGenerator;
using SuperNova.Controls;

namespace SuperNova.IDE;

public partial class MdiWindowManager : ObservableObject, IMdiWindowManager
{
    [Notify] private IMdiWindow? activeWindow;
    public ObservableCollection<IMdiWindow> Windows { get; } = new();

    public void OpenWindow(IMdiWindow window)
    {
        Windows.Add(window);
        ActiveWindow = window;
        window.CloseRequest += WindowCloseRequest;
    }

    private void WindowCloseRequest(IMdiWindow window) => CloseWindow(window);

    public void CloseWindow(IMdiWindow window)
    {
        window.CloseRequest -= WindowCloseRequest;
        Windows.Remove(window);
        if (window is IDisposable disposable)
            disposable.Dispose();
    }

    /// <summary>
    /// Safe factory method to create MDI windows without VB interpreter dependencies
    /// </summary>
    public MDIWindow CreateSafeWindow(string title, object? content = null)
    {
        var window = new MDIWindow
        {
            Title = title,
            CanClose = true
        };

        if (content != null)
        {
            window.Content = content;
        }
        return window;
    }

    /// <summary>
    /// Safe method to open windows without VB interpreter dependencies
    /// </summary>
    public void OpenSafeWindow(string title, object? content = null)
    {
        var window = CreateSafeWindow(title, content);
        OpenWindow(window);
    }
}

