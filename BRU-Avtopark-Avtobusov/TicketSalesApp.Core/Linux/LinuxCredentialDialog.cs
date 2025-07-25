#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using Gio;
using GObject;
using Gtk;
using Adw;

namespace TicketSalesApp.Core.Linux
{
    [SupportedOSPlatform("linux")]
    public class LinuxCredentialDialog : IDisposable
    {
        private readonly TaskCompletionSource<(string username, string password)> _tcs = new();
        private Adw.Application? _app;
        private LoginWindow? _window;
        private readonly string _title;

        public LinuxCredentialDialog(string title = "Login Required")
        {
            _title = title;
        }

        public async Task<(string username, string password)> ShowDialogAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new PlatformNotSupportedException("This dialog is only supported on Linux");

            var tcs = new TaskCompletionSource<(string, string)>();
            var app = Adw.Application.New("com.ticketsalesapp.credentialdialog", Gio.ApplicationFlags.FlagsNone);

            app.OnActivate += (sender, args) =>
            {
                var window = new LoginWindow(app, _title, tcs);
                window.Show();
            };

            // Run the application
            var result = await System.Threading.Tasks.Task.Run(async () =>
            {
                var exitCode = app.RunWithSynchronizationContext(null);
                if (exitCode != 0)
                    tcs.TrySetException(new Exception($"Application exited with code {exitCode}"));
                return await tcs.Task;
            });

            return result;
        }

        public void Dispose()
        {
            _window?.Dispose();
            _app?.Dispose();
            GC.SuppressFinalize(this);
        }

        ~LinuxCredentialDialog()
        {
            Dispose();
        }

        private class LoginWindow : Adw.ApplicationWindow
        {
            private readonly Entry _username;
            private readonly Entry _password;
            private readonly TaskCompletionSource<(string, string)> _tcs;

            public LoginWindow(Adw.Application app, string title, TaskCompletionSource<(string, string)> tcs)

            {
                _tcs = tcs;

                // Set window properties
                Title = title;
                SetDefaultSize(400, 240);
                SetModal(true);
                SetResizable(false);

                // Create header bar
                var header = Adw.HeaderBar.New();
                var titleLabel = Label.New(title);
                header.SetTitleWidget(titleLabel);
                SetTitlebar(header);

                // Create main container
                var clamp = Adw.Clamp.New();
                clamp.SetMaximumSize(400);
                clamp.SetTighteningThreshold(360);
                clamp.SetHexpand(true);

                var vbox = Box.New(Orientation.Vertical, 16);
                vbox.SetMarginTop(12);
                vbox.SetMarginBottom(12);
                vbox.SetMarginStart(12);
                vbox.SetMarginEnd(12);

                // Username field
                _username = Entry.New();
                _username.SetPlaceholderText("Username");
                _username.SetHexpand(true);

                // Password field
                _password = Entry.New();
                _password.SetPlaceholderText("Password");
                _password.SetVisibility(false);
                _password.SetHexpand(true);

                // Buttons
                var btnBox = Box.New(Orientation.Horizontal, 6);
                btnBox.SetHalign(Align.End);

                var cancelBtn = Button.New();
                cancelBtn.SetLabel("Cancel");

                var loginBtn = Button.New();
                loginBtn.SetLabel("Login");
                loginBtn.AddCssClass("suggested-action");

                btnBox.Append(cancelBtn);
                btnBox.Append(loginBtn);

                // Assemble UI
                vbox.Append(_username);
                vbox.Append(_password);

                var separator = Separator.New(Orientation.Horizontal);
                vbox.Append(separator);
                vbox.Append(btnBox);

                clamp.SetChild(vbox);
                SetChild(clamp);

                // Connect signals
                cancelBtn.OnClicked += (_, _) => OnCancelClicked();
                loginBtn.OnClicked += (_, _) => OnLoginClicked();
                _password.OnActivate += (_, _) => OnLoginClicked();
            }

            private void OnLoginClicked()
            {
                var user = _username.GetText()?.Trim() ?? string.Empty;
                var pass = _password.GetText() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
                {
                    ShowError("Please enter both username and password.");
                    return;
                }

                _tcs.TrySetResult((user, pass));
                Close();
            }

            private void OnCancelClicked()
            {
                _tcs.TrySetCanceled();
                Close();
            }

            private void ShowError(string message)
            {
                var dialog = new Adw.MessageDialog();
                dialog.Heading = "Error";
                dialog.Body = message;
                dialog.AddResponse("ok", "_OK");
                dialog.DefaultResponse = "ok";
                // If user closes without choosing, use "ok"
                dialog.SetCloseResponse("ok");

                // Set dialog modal and transient parent
                dialog.Modal = true;
                dialog.SetTransientFor(this);

                dialog.OnResponse += (sender, args) =>
                {
                    Console.WriteLine($"Response ID: {args.Response}");
                    dialog.Destroy();
                };
                dialog.Present();
            }

        }
    }
}
