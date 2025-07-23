using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using SuperNova.Controls;
using SuperNova.Utils;
using Classic.CommonControls.Dialogs;

namespace SuperNova.IDE;

public class WindowManager : IWindowManager
{
    private Window GetTopWindow(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        foreach (var window in lifetime.Windows)
        {
            if (window.IsActive)
                return window;
        }

        return lifetime.MainWindow ?? throw new Exception("No main window and trying to open a window, fatal error");
    }

    public Task ShowManagedWindow(MDIWindow window)
    {
        var tcs = new TaskCompletionSource<bool>();
        window.CloseCommand = new DelegateCommand(() => tcs.SetResult(true), () => true);
        var blocker = new MDIBlockerControl();
        var mainView = Static.MainView;
        mainView.PART_DialogHost.Children.Add(blocker);
        mainView.PART_DialogHost.Children.Add(window);

        async Task WindowTask()
        {
            await tcs.Task;
            mainView.PART_DialogHost.Children.Remove(window);
            mainView.PART_DialogHost.Children.Remove(blocker);
        }

        return WindowTask();
    }

    private Task ShowManagedWindow(out MDIWindow window, string? title = null, object? content = null, bool canClose = true)
    {
        window = new MDIWindow()
        {
            Title = title ?? "",
            CanClose = canClose,
            Content = new ContentControl()
            {
                Content = content,
                DataContext = content
            },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        return ShowManagedWindow(window);
    }

    public async Task ShowWindow(IDialog dialog)
    {
        if (Static.SingleView)
        {
            var task = ShowManagedWindow(out var window, dialog.Title, dialog);

            dialog.CloseRequested += DialogOnCloseRequested;

            void DialogOnCloseRequested(bool result) => window.CloseCommand.Execute(window.CloseCommandParameter);

            await task;

            dialog.CloseRequested -= DialogOnCloseRequested;
        }
        else if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new DialogWindow()
            {
                Content = dialog,
                DataContext = dialog,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowActivated = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var lifetime = new TaskCompletionSource();
            void OnWindowClosed(object? sender, EventArgs e) => lifetime.SetResult();
            window.Closed += OnWindowClosed;
            window.Show();
            await lifetime.Task;
            window.Closed -= OnWindowClosed;
        }
        else
            throw new NotImplementedException();
    }

    public async Task<string?> InputBox(string prompt, string? caption, string defaultText)
    {
        caption ??= "Avalonia Visual Basic";
        if (Static.SingleView)
        {
            var inputBox = new Classic.CommonControls.Dialogs.MessageBox
            {

                Text = defaultText
            };
            var task = ShowManagedWindow(out var window, caption, inputBox, false);

            string?[] ret = new string?[1];
            await task;



            return ret[0];
        }
        else if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var inputBox = new Classic.CommonControls.Dialogs.MessageBox
            {

                Text = defaultText
            };
            var task = ShowManagedWindow(out var window, caption, inputBox, false);

            string?[] ret = new string?[1];
            await task;



            return ret[0];
        }
        else
            throw new NotImplementedException();
    }
    public async Task<MessageBoxResult> MessageBox(string text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        caption ??= "Avalonia Visual Basic";
        if (Static.SingleView)
        {
            var messageBox = new MessageBox()
            {
                Text = text,
                Buttons = buttons,
                Icon = icon
            };
            var task = ShowManagedWindow(out var window, caption, messageBox, false);

            MessageBoxResult[] ret = new MessageBoxResult[1];

            messageBox.AcceptRequest += MessageBoxOnCloseRequested;

            void MessageBoxOnCloseRequested(MessageBoxResult result)
            {
                ret[0] = result;
                window.CloseCommand.Execute(window.CloseCommandParameter);
            }

            await task;

            messageBox.AcceptRequest -= MessageBoxOnCloseRequested;

            return ret[0];
        }
        else if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return await Classic.CommonControls.Dialogs.MessageBox.ShowDialog(GetTopWindow(desktop), text, caption, buttons, icon);
        }
        else
            throw new NotImplementedException();
    }

    public async Task ShowAbout(AboutDialogOptions options)
    {
        if (Static.SingleView)
        {
            AboutDialogViewModel aboutDialogViewModel = new AboutDialogViewModel(options);
            var aboutDialog = new AboutDialog()
            {
                DataContext = aboutDialogViewModel
            };
            var task = ShowManagedWindow(out var window, "About " + options.Title, aboutDialog, false);

            aboutDialogViewModel.CloseRequested += MessageBoxOnCloseRequested;

            void MessageBoxOnCloseRequested()
            {
                window.CloseCommand.Execute(window.CloseCommandParameter);
            }

            await task;

            aboutDialogViewModel.CloseRequested -= MessageBoxOnCloseRequested;
        }
        else if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await AboutDialog.ShowDialog(GetTopWindow(desktop), options);
        }
        else
            throw new NotImplementedException();
    }

    public async Task<FontDialogResult?> ShowFontDialog(FontDialogResult? initial = null)
    {
        if (Static.SingleView)
        {
            FontDialogViewModel fontDialogViewModel = new FontDialogViewModel();

            var messageBox = new FontDialog()
            {
                DataContext = fontDialogViewModel
            };
            var task = ShowManagedWindow(out var window, "Font", messageBox, false);

            FontDialogResult?[] ret = new FontDialogResult?[1];

            fontDialogViewModel.AcceptRequested += Accept;
            fontDialogViewModel.CancelRequested += Cancel;

            void Accept(FontDialogResult? result)
            {
                ret[0] = result;
                window.CloseCommand.Execute(window.CloseCommandParameter);
            }

            void Cancel()
            {
                ret[0] = null;
                window.CloseCommand.Execute(window.CloseCommandParameter);
            }

            await task;

            fontDialogViewModel.AcceptRequested -= Accept;
            fontDialogViewModel.CancelRequested -= Cancel;

            return ret[0];
        }
        else if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return await FontDialog.ShowDialog(GetTopWindow(desktop));
        }
        else
            throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<string>?> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime classic)
            throw new Exception("This is supported only for desktop apps");

        var topLevel = TopLevel.GetTopLevel(GetTopWindow(classic))!;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

        return files.Select(f => f.TryGetLocalPath())
            .Where(x => x != null)
            .Cast<string>()
            .ToList();
    }

    public async Task<string?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        if (Application.Current!.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime classic)
            throw new Exception("This is supported only for desktop apps");

        var topLevel = TopLevel.GetTopLevel(GetTopWindow(classic))!;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(options);

        return file?.TryGetLocalPath();
    }

    public async Task<bool> ShowDialog(IDialog dialog)
    {
        if (Static.SingleView)
        {
            var lifetime = ShowManagedWindow(out var window, dialog.Title, dialog);

            bool[] ret = new bool[1];
            
            dialog.CloseRequested += DialogOnCloseRequested;

            void DialogOnCloseRequested(bool result)
            {
                ret[0] = result;
                window.CloseCommand?.Execute(window.CloseCommandParameter);
            }

            await lifetime;

            dialog.CloseRequested -= DialogOnCloseRequested;

            return ret[0];
        }
        else if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new DialogWindow()
            {
                Content = dialog,
                DataContext = dialog,
                SizeToContent = SizeToContent.WidthAndHeight,
                ShowActivated = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            return await window.ShowDialog<bool>(GetTopWindow(desktop));
        }
        throw new NotImplementedException();
    }
}