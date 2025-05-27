using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;

namespace TicketSalesApp.UI.Administration.Avalonia.Views.ManagementToolWindowsViews
{
    public partial class EmployeeManagementToolWindow : UserControl
    {
        public EmployeeManagementToolWindow()
        {
            InitializeComponent();
            DataContext = new EmployeeManagementViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}