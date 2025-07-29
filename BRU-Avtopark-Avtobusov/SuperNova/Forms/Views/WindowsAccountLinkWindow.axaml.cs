using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ReactiveUI;
using SuperNova.Forms.ViewModels;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace SuperNova.Forms.Views
{
    public partial class WindowsAccountLinkWindow : ReactiveWindow<WindowsAccountLinkViewModel>
    {
        public WindowsAccountLinkWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            
            // Create the ViewModel
            ViewModel = new WindowsAccountLinkViewModel();
            
            // Handle window close request from ViewModel
            this.WhenActivated(disposables =>
            {
                if (ViewModel != null)
                {
                    ViewModel.CloseWindow
                        .RegisterHandler(async interaction =>
                        {
                            Close();
                            interaction.SetOutput(Unit.Default);
                        })
                        .DisposeWith(disposables);
                }
            });
            
            // Set window properties
            CanResize = false;
            SizeToContent = Avalonia.Controls.SizeToContent.Height;
            MinHeight = 500;
            MaxHeight = 600;
            Width = 450;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
