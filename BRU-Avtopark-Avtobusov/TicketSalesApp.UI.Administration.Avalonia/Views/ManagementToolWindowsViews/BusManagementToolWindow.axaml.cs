using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views
{
    public partial class BusManagementToolWindow : UserControl
    {
        public BusManagementToolWindow()
        {
            InitializeComponent();
            DataContext = new BusManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}