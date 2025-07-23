using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PleasantUI.Controls;

namespace SuperNova.Forms.AdministratorUi.Views
{
    public partial class IncomeReportWindow : PleasantWindow
    {
        public IncomeReportWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 