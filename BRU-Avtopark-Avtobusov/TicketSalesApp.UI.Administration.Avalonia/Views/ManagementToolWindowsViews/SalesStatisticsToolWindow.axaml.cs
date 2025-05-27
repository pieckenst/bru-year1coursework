using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views.ManagementToolWindowsViews
{
    public partial class SalesStatisticsToolWindow : UserControl
    {
        public SalesStatisticsToolWindow()
        {
            InitializeComponent();
            DataContext = new SalesStatisticsViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}