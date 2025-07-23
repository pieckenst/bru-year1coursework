using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using SuperNova.Utils;
using Classic.Avalonia.Theme;
using SuperNova.IDE;

namespace SuperNova.Controls;

public class MDIWindow : ContentControl, IMdiWindow
{
    private ClassicBorderDecorator? border;
    private Border? titleBar;

    public static readonly StyledProperty<IImage?> IconProperty = AvaloniaProperty.Register<MDIWindow, IImage?>("Icon");
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<MDIWindow, string>("Title");
    public static readonly StyledProperty<bool> CanCloseProperty = AvaloniaProperty.Register<MDIWindow, bool>("CanClose", true);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IImage? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    // Explicit interface implementation to handle nullability
    IImage IMdiWindow.Icon => Icon ?? new Avalonia.Media.Imaging.Bitmap(new System.IO.MemoryStream());

    // CloseRequest event for IMdiWindow interface
    public event Action<IMdiWindow>? CloseRequest;

    public ICommand CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public object? CloseCommandParameter
    {
        get => GetValue(CloseCommandParameterProperty);
        set => SetValue(CloseCommandParameterProperty, value);
    }

    public bool CanClose
    {
        get => GetValue(CanCloseProperty);
        set => SetValue(CanCloseProperty, value);
    }

    static MDIWindow()
    {
        FocusableProperty.OverrideDefaultValue<MDIWindow>(true);
    }

    public MDIWindow()
    {
        AddHandler(PointerPressedEvent, OnWindowPressed, RoutingStrategies.Tunnel);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        titleBar = e.NameScope.Get<Border>("PART_TitleBar");
        border = e.NameScope.Get<ClassicBorderDecorator>("PART_Border");

        titleBar.AddHandler(PointerPressedEvent, OnTitleBarDown);
        titleBar.AddHandler(PointerMovedEvent, OnTitleBarMoved);
        border.AddHandler(PointerPressedEvent, OnBorderDown);
        border.AddHandler(PointerReleasedEvent, OnBorderReleased);
        border.AddHandler(PointerEnteredEvent, OnBorderEnter);
        border.AddHandler(PointerMovedEvent, OnBorderMoved);

        var contentPresenter = e.NameScope.Get<ContentPresenter>("PART_ContentPresenter");
        contentPresenter.GetObservable(ContentPresenter.ChildProperty)
            .Subscribe(new ActionObserver<Control?>(control =>
            {
                var child = contentPresenter.Child;
                if (child != null)
                {
                    child.GetObservable(CommandManager.CommandBindingsProperty)
                        .Subscribe(new ActionObserver<IList<CommandBinding>?>(bindings =>
                        {
                            if (bindings != null)
                                CommandManager.SetCommandBindings(this, bindings);
                        }));
                }
            }));
    }

    private void OnBorderEnter(object? sender, PointerEventArgs e)
    {
        SetResizeCursor(e);
    }

    private void SetResizeCursor(PointerEventArgs e)
    {
        var position = e.GetPosition(this);
        var isLeft = position.X <= 2;
        var isRight = position.X >= Bounds.Width - 2;
        var isTop = position.Y <= 2;
        var isBottom = position.Y >= Bounds.Height - 2;

        var cursor = StandardCursorType.Arrow;

        if (MDIHostPanel.GetWindowState(this) == WindowState.Normal)
        {
            if (isLeft)
            {
                if (isTop)
                    cursor = StandardCursorType.SizeAll;
                else if (isBottom)
                    cursor = StandardCursorType.SizeAll;
                else
                    cursor = StandardCursorType.SizeWestEast;
            }
            else if (isRight)
            {
                if (isTop)
                    cursor = StandardCursorType.SizeAll;
                else if (isBottom)
                    cursor = StandardCursorType.SizeAll;
                else
                    cursor = StandardCursorType.SizeWestEast;
            }
            else if (isTop || isBottom)
            {
                cursor = StandardCursorType.SizeNorthSouth;
            }
        }

        if (border != null)
            border.Cursor = new Cursor(cursor);
    }

    private void OnBorderReleased(object? sender, PointerReleasedEventArgs e)
    {
        rightResize = false;
        bottomResize = false;
        leftResize = false;
        topResize = false;
    }

    private bool rightResize;
    private bool bottomResize;
    private bool leftResize;
    private bool topResize;
    private Size initialSize;

    private void OnBorderMoved(object? sender, PointerEventArgs e)
    {
        SetResizeCursor(e);
        if (FindParentMDIHost() is not { } canvas)
            return;

        var position = e.GetPosition(canvas);

        if (rightResize)
        {
            MDIHostPanel.SetWindowSize(this, MDIHostPanel.GetWindowSize(this).WithWidth(Math.Max(MinWidth, initialSize.Width + (position.X - initialPress.X))));
        }
        if (bottomResize)
        {
            MDIHostPanel.SetWindowSize(this, MDIHostPanel.GetWindowSize(this).WithHeight(Math.Max(MinHeight, initialSize.Height + (position.Y - initialPress.Y))));
        }
        if (leftResize)
        {
            var destinationRight = initialPosition.X + initialSize.Width;
            MDIHostPanel.SetWindowLocation(this, MDIHostPanel.GetWindowLocation(this).WithX(Math.Min(initialPosition.X + (position.X - initialPress.X), destinationRight - MinWidth)));
            MDIHostPanel.SetWindowSize(this, MDIHostPanel.GetWindowSize(this).WithWidth(Math.Max(MinWidth, destinationRight - MDIHostPanel.GetWindowLocation(this).X)));
        }
        if (topResize)
        {
            var destinationBottom = initialPosition.Y + initialSize.Height;
            MDIHostPanel.SetWindowLocation(this, MDIHostPanel.GetWindowLocation(this).WithY(Math.Min(initialPosition.Y + (position.Y - initialPress.Y), destinationBottom - MinHeight)));
            MDIHostPanel.SetWindowSize(this, MDIHostPanel.GetWindowSize(this).WithHeight(Math.Max(MinHeight, destinationBottom - MDIHostPanel.GetWindowLocation(this).Y)));
        }
    }

    private void OnBorderDown(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
            return;

        if (!GatherInitialMeasure(e))
            return;

        if (MDIHostPanel.GetWindowState(this) != WindowState.Normal)
            return;

        var position = e.GetPosition(this);
        leftResize = position.X <= 2;
        rightResize = position.X >= Bounds.Width - 2;
        topResize = position.Y <= 2;
        bottomResize = position.Y >= Bounds.Height - 2;
    }

    private Point initialPosition;
    private Point initialPress;
    public static readonly StyledProperty<object?> CloseCommandParameterProperty = AvaloniaProperty.Register<MDIWindow, object?>("CloseCommandParameter");
    public static readonly StyledProperty<ICommand> CloseCommandProperty = AvaloniaProperty.Register<MDIWindow, ICommand>("CloseCommand");

    private MDIHostPanel? FindParentMDIHost() => this.FindAncestorOfType<MDIHostPanel>();

    private void OnTitleBarMoved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(e.Source, titleBar))
            return;
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed)
        {
            if (FindParentMDIHost() is { } mdiHost)
            {
                var curPosition = e.GetPosition(mdiHost);
                var diff = curPosition - initialPress;
                MDIHostPanel.SetWindowLocation(this, new Point(
                    Math.Clamp(initialPosition.X + diff.X, -Bounds.Width + 50, mdiHost.Bounds.Width - 50),
                    Math.Clamp(initialPosition.Y + diff.Y, -Bounds.Height + 50, mdiHost.Bounds.Height - 50)
                    ));
            }
        }
    }

    private void OnTitleBarDown(object? sender, PointerPressedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, titleBar))
            return;
        var point = e.GetCurrentPoint(this);
        if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
        {
            GatherInitialMeasure(e);
        }
    }

    private void OnWindowPressed(object? sender, PointerPressedEventArgs e)
    {
        // Add safety check to prevent recursive activation
        if (e.Handled)
            return;

        try
        {
            e.Handled = true;
            this.ActivateMDIForm();
        }
        catch (Exception ex)
        {
            // Log the error but don't let it crash the application
            System.Diagnostics.Debug.WriteLine($"Error in OnWindowPressed: {ex.Message}");
        }
    }

    private bool GatherInitialMeasure(PointerEventArgs e)
    {
        if (FindParentMDIHost() is { } canvas)
        {
            initialPosition = MDIHostPanel.GetWindowLocation(this);
            initialPress = e.GetPosition(canvas);
            initialSize = MDIHostPanel.GetWindowSize(this);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Close the MDI window and trigger CloseRequest event
    /// </summary>
    public void Close()
    {
        CloseRequest?.Invoke(this);
    }
}