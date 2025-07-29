using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SuperNova.Forms.ViewModels;

namespace SuperNova.Forms.Views
{
    public partial class WindowsAccountLinkSuccessDialog : Window
    {
        public WindowsAccountLinkSuccessDialog()
        {
            InitializeComponent();
        }

        public WindowsAccountLinkSuccessDialog(string username)
        {
            InitializeComponent();
            var viewModel = new WindowsAccountLinkSuccessViewModel(username, () => Close(true));
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
