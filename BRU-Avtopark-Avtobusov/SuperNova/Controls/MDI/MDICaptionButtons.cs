using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using SuperNova.Utils;

namespace SuperNova.Controls;

public class MDICaptionButtons : CaptionButtons
{
    protected override Type StyleKeyOverride => typeof(CaptionButtons);

    private IDisposable? disposables;

    private Button? closeButton;

    private void Attach(MDIWindow hostWindow)
    {
        if (disposables == null)
        {
            disposables = hostWindow.GetObservable(MDIHostPanel.WindowStateProperty)
                .Subscribe(new ActionObserver<WindowState>(x =>
                {
                    PseudoClasses.Set(":minimized", x == WindowState.Minimized);
                    PseudoClasses.Set(":normal", x == WindowState.Normal);
                    PseudoClasses.Set(":maximized", x == WindowState.Maximized);
                    PseudoClasses.Set(":fullscreen", x == WindowState.FullScreen);
                }));
            if (closeButton != null)
                closeButton.IsEnabled = hostWindow.CanClose;
        }
    }

    private new void Detach()
    {
        if (disposables != null)
        {
            disposables.Dispose();
            disposables = null;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (FindHost() is { } host)
            Attach(host);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Detach();
    }

    protected override void OnClose()
    {
        base.OnClose();
        if (FindHost() is { } window)
            window.CloseCommand?.Execute(window.CloseCommandParameter);
    }

    protected override void OnMinimize()
    {
        base.OnMinimize();
        if (FindHost() is { } window)
        {
            if (MDIHostPanel.GetWindowState(window) == WindowState.Minimized)
            {
                MDIHostPanel.SetOldMinimizedWindowLocation(window, MDIHostPanel.GetWindowLocation(window));
                MDIHostPanel.SetWindowLocation(window, MDIHostPanel.GetOldWindowLocation(window));
                MDIHostPanel.SetWindowState(window,  WindowState.Normal);
            }
            else
            {
                MDIHostPanel.SetOldWindowLocation(window, MDIHostPanel.GetWindowLocation(window));
                MDIHostPanel.SetWindowState(window,  WindowState.Minimized);
                if (MDIHostPanel.GetOldMinimizedWindowLocation(window) is { } oldMinimizedWindowLocation)
                    MDIHostPanel.SetWindowLocation(window, oldMinimizedWindowLocation);
                else if (this.FindAncestorOfType<MDIHostPanel>() is { } hostPanel)
                    MDIHostPanel.SetWindowLocation(window, new Point(0, hostPanel.Bounds.Height - window.MinHeight));
            }
        }
    }

    protected override void OnRestore()
    {
        base.OnRestore();
        if (FindHost() is { } window)
        {
            if (MDIHostPanel.GetWindowState(window) == WindowState.Maximized)
            {
                MDIHostPanel.SetWindowState(window,  WindowState.Normal);
            }
            else
            {
                MDIHostPanel.SetWindowState(window,  WindowState.Maximized);
            }
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        closeButton = e.NameScope.Get<Button>("PART_CloseButton");
    }

    private MDIWindow? FindHost()
    {
        return this.FindAncestorOfType<MDIWindow>();
    }
}