using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace SuperNova.Tools.Reports;

public partial class ReportsToolView : UserControl
{
    private Button? newReportButton;
    private Button? editReportButton;
    private Button? deleteReportButton;
    private Button? runReportButton;
    private Button? exportReportButton;
    private TreeView? reportsTree;

    public ReportsToolView()
    {
        InitializeComponent();
        InitializeControls();
        SetupEventHandlers();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeControls()
    {
        newReportButton = this.FindControl<Button>("NewReportButton");
        editReportButton = this.FindControl<Button>("EditReportButton");
        deleteReportButton = this.FindControl<Button>("DeleteReportButton");
        runReportButton = this.FindControl<Button>("RunReportButton");
        exportReportButton = this.FindControl<Button>("ExportReportButton");
        reportsTree = this.FindControl<TreeView>("ReportsTree");
    }

    private void SetupEventHandlers()
    {
        if (newReportButton != null)
            newReportButton.Click += NewReportButton_Click;
        
        if (editReportButton != null)
            editReportButton.Click += EditReportButton_Click;
        
        if (deleteReportButton != null)
            deleteReportButton.Click += DeleteReportButton_Click;
        
        if (runReportButton != null)
            runReportButton.Click += RunReportButton_Click;
        
        if (exportReportButton != null)
            exportReportButton.Click += ExportReportButton_Click;
        
        if (reportsTree != null)
            reportsTree.SelectionChanged += ReportsTree_SelectionChanged;
    }

    private void NewReportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsToolViewModel vm)
            vm.CreateNewReport();
    }

    private void EditReportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsToolViewModel vm)
            vm.EditSelectedReport();
    }

    private void DeleteReportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsToolViewModel vm)
            vm.DeleteSelectedReport();
    }

    private void RunReportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsToolViewModel vm)
            vm.RunSelectedReport();
    }

    private void ExportReportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsToolViewModel vm)
            vm.ExportSelectedReport();
    }

    private void ReportsTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ReportsToolViewModel vm && 
            e.AddedItems.Count > 0 && 
            e.AddedItems[0] is TreeViewItem selectedItem)
        {
            vm.SelectReport(selectedItem.Header?.ToString() ?? string.Empty);
        }
    }
} 