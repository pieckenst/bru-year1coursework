using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views
{
    public partial class RouteManagementToolWindow : UserControl
    {
        public RouteManagementToolWindow()
        {
            InitializeComponent();
            DataContext = new RouteManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}