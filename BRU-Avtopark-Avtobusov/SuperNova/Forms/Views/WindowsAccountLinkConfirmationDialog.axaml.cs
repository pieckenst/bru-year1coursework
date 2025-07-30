using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PleasantUI.Controls;
using SuperNova.Forms.ViewModels;
using System;

namespace SuperNova.Forms.Views
{
    public partial class WindowsAccountLinkConfirmationDialog : PleasantWindow
    {
        private readonly WindowsAccountLinkConfirmationViewModel _viewModel;

        public WindowsAccountLinkConfirmationDialog()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public WindowsAccountLinkConfirmationDialog(string windowsUsername, string username, string token)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            _viewModel = new WindowsAccountLinkConfirmationViewModel(windowsUsername, username, token);
            DataContext = _viewModel;

            // Handle dialog result
            _viewModel.DialogResult += (_, result) => Close(result);

            // Set window properties
            CanResize = false;
            SizeToContent = Avalonia.Controls.SizeToContent.Height;
            MinHeight = 450;
            MaxHeight = 600;
            Width = 450;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}