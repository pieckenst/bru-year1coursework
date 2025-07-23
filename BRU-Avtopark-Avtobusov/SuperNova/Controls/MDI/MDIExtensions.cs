using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SuperNova.Controls;

/// <summary>
/// Global guard to prevent MDI stack overflow issues
/// </summary>
public static class MDIStackGuard
{
    private static readonly ThreadLocal<int> _recursionDepth = new(() => 0);
    private const int MAX_RECURSION_DEPTH = 10;

    public static bool CanProceed()
    {
        return _recursionDepth.Value < MAX_RECURSION_DEPTH;
    }

    public static IDisposable EnterGuard()
    {
        _recursionDepth.Value++;
        return new GuardDisposable();
    }

    private class GuardDisposable : IDisposable
    {
        public void Dispose()
        {
            if (_recursionDepth.Value > 0)
                _recursionDepth.Value--;
        }
    }
}

public static class MDIExtensions
{
    private static readonly HashSet<Control> _activatingControls = new();

    public static void ActivateMDIForm(this Control control)
    {
        var controlId = control.GetHashCode().ToString();
        var operationName = $"ActivateMDIForm_{controlId}";

        MDIStackOverflowPrevention.SafeExecute(operationName, () =>
        {
            // Prevent stack overflow by checking if we're already activating this control
            if (_activatingControls.Contains(control))
                return;

            try
            {
                _activatingControls.Add(control);

                // Only activate if the control is attached to visual tree
                if (control.IsAttachedToVisualTree())
                {
                    control.RaiseEvent(new RoutedEventArgs()
                    {
                        Source = control,
                        RoutedEvent = MDIHost.ActivateWindowEvent
                    });
                }
            }
            finally
            {
                _activatingControls.Remove(control);
            }
        });
    }
}