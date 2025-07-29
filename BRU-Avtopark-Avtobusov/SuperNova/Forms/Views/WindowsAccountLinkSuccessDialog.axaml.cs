using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PleasantUI.Controls;
using SuperNova.Forms.ViewModels;
using System;
using System.Threading.Tasks;

namespace SuperNova.Forms.Views
{
    public partial class WindowsAccountLinkSuccessDialog : PleasantWindow
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
