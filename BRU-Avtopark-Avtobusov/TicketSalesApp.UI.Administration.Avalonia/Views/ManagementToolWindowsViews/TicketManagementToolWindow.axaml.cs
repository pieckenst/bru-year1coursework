using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views
{
    public partial class TicketManagementToolWindow : UserControl
    {
        public TicketManagementToolWindow()
        {
            InitializeComponent();
            DataContext = new TicketManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}