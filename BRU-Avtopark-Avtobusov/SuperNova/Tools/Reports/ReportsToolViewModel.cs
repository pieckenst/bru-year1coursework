using System;
using System.Collections.ObjectModel;
using Dock.Model.Mvvm.Controls;
using SuperNova.IDE;
using PropertyChanged.SourceGenerator;
using Avalonia.Controls;

namespace SuperNova.Tools.Reports;

public partial class ReportsToolViewModel : Tool
{
    private readonly IMdiWindowManager mdiWindowManager;
    private readonly IWindowManager windowManager;

    [Notify] private TreeViewItem? selectedReport;
    [Notify] private bool canEditReport;
    [Notify] private bool canDeleteReport;
    [Notify] private bool canRunReport;
    [Notify] private bool canExportReport;

    public ObservableCollection<TreeViewItem> ReportItems { get; } = new();

    public ReportsToolViewModel(IMdiWindowManager mdiWindowManager, IWindowManager windowManager)
    {
        this.mdiWindowManager = mdiWindowManager;
        this.windowManager = windowManager;
        
        Title = "Отчеты";
        CanPin = true;
        CanClose = true;
        CanFloat = true;

        InitializeReportItems();
    }

    private void InitializeReportItems()
    {
        var financialNode = new TreeViewItem { Header = "Финансовые отчеты", IsExpanded = true };
        financialNode.Items.Add(new TreeViewItem { Header = "Доходы по месяцам" });
        financialNode.Items.Add(new TreeViewItem { Header = "Расходы по категориям" });
        financialNode.Items.Add(new TreeViewItem { Header = "Прибыль и убытки" });
        ReportItems.Add(financialNode);

        var salesNode = new TreeViewItem { Header = "Отчеты по продажам", IsExpanded = true };
        salesNode.Items.Add(new TreeViewItem { Header = "Продажи по маршрутам" });
        salesNode.Items.Add(new TreeViewItem { Header = "Продажи по периодам" });
        salesNode.Items.Add(new TreeViewItem { Header = "Возвраты билетов" });
        ReportItems.Add(salesNode);

        var busNode = new TreeViewItem { Header = "Отчеты по автопарку", IsExpanded = true };
        busNode.Items.Add(new TreeViewItem { Header = "Состояние автобусов" });
        busNode.Items.Add(new TreeViewItem { Header = "График обслуживания" });
        busNode.Items.Add(new TreeViewItem { Header = "Расход топлива" });
        ReportItems.Add(busNode);

        var staffNode = new TreeViewItem { Header = "Отчеты по персоналу", IsExpanded = true };
        staffNode.Items.Add(new TreeViewItem { Header = "Рабочее время" });
        staffNode.Items.Add(new TreeViewItem { Header = "Эффективность работы" });
        staffNode.Items.Add(new TreeViewItem { Header = "Премии и штрафы" });
        ReportItems.Add(staffNode);

        var analyticsNode = new TreeViewItem { Header = "Аналитические отчеты", IsExpanded = true };
        analyticsNode.Items.Add(new TreeViewItem { Header = "Загруженность маршрутов" });
        analyticsNode.Items.Add(new TreeViewItem { Header = "Сезонность продаж" });
        analyticsNode.Items.Add(new TreeViewItem { Header = "Прогноз доходов" });
        ReportItems.Add(analyticsNode);
    }

    public void SelectReport(string reportName)
    {
        // Enable/disable buttons based on selection
        canEditReport = !string.IsNullOrEmpty(reportName);
        canDeleteReport = !string.IsNullOrEmpty(reportName);
        canRunReport = !string.IsNullOrEmpty(reportName);
        canExportReport = !string.IsNullOrEmpty(reportName);
    }

    public void CreateNewReport()
    {
        windowManager.MessageBox(
            "Функция создания отчетов будет доступна в следующей версии.", 
            "В разработке");
    }

    public void EditSelectedReport()
    {
        if (selectedReport?.Header is not string reportName)
            return;

        windowManager.MessageBox(
            $"Функция редактирования отчета '{reportName}' будет доступна в следующей версии.", 
            "В разработке");
    }

    public void DeleteSelectedReport()
    {
        if (selectedReport?.Header is not string reportName)
            return;

        windowManager.MessageBox(
            $"Функция удаления отчета '{reportName}' будет доступна в следующей версии.", 
            "В разработке");
    }

    public void RunSelectedReport()
    {
        if (selectedReport?.Header is not string reportName)
            return;

        windowManager.MessageBox(
            $"Функция запуска отчета '{reportName}' будет доступна в следующей версии.", 
            "В разработке");
    }

    public void ExportSelectedReport()
    {
        if (selectedReport?.Header is not string reportName)
            return;

        windowManager.MessageBox(
            $"Функция экспорта отчета '{reportName}' будет доступна в следующей версии.", 
            "В разработке");
    }
} 