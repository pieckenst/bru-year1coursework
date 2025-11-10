using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using TicketSalesApp.Core.Models;
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

        private void Employee_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is Employee employee)
            {
                if (DataContext is EmployeeManagementViewModel viewModel)
                {
                    viewModel.SelectedEmployee = employee;
                    viewModel.ShowDetailCommand.Execute(null);
                }
            }
        }
    }
}