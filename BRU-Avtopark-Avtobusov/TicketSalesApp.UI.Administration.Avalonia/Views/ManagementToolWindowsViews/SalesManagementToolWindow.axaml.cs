using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views
{
    public partial class SalesManagementToolWindow : UserControl
    {
        public SalesManagementToolWindow()
        {
            InitializeComponent();
            DataContext = new SalesManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}