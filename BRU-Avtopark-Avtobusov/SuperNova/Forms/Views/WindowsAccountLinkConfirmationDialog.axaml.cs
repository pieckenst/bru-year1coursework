using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SuperNova.Forms.ViewModels;
using System;
using System.Threading.Tasks;

namespace SuperNova.Forms.Views
{
    public partial class WindowsAccountLinkConfirmationDialog : Window
    {
        public WindowsAccountLinkConfirmationDialog()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public WindowsAccountLinkConfirmationDialog(string windowsUsername, string username)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            
            var viewModel = new WindowsAccountLinkConfirmationViewModel(windowsUsername, username);
            DataContext = viewModel;

            // Handle dialog result
            viewModel.ConfirmCommand.Subscribe(_ => Close(true));
            viewModel.CancelCommand.Subscribe(_ => Close(false));
            
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
