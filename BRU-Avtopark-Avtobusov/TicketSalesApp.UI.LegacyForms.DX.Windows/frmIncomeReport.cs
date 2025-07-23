using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using DevExpress.XtraPrinting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web; // For HttpUtility
using DevExpress.Data;
using DevExpress.XtraGrid.Views.Base; // For SummaryItemType
using Serilog; // Added for Serilog
using System.Text.Json.Serialization; // Added for ReferenceHandler

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    // Model for monthly income data
    public class MonthlyIncome
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal TotalIncome { get; set; }
        public int TicketsSold { get; set; }
        public decimal AverageTicketPrice { get; set; }
    }

    // Model for route income data
    public class RouteIncome
    {
        public string RouteName { get; set; } = string.Empty;
        public decimal TotalIncome { get; set; }
        public int TicketsSold { get; set; }
        public decimal AverageTicketPrice { get; set; }
    }

    // ViewModel for Route Lookup
    public class RouteLookupViewModel
    {
        public int RouteId { get; set; }
        public string DisplayName { get; set; } = string.Empty; // e.g., "Start - End"
    }

    public partial class frmIncomeReport : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClientService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
             PropertyNameCaseInsensitive = true,
             ReferenceHandler = ReferenceHandler.Preserve
        };
        private readonly string _baseUrl = "http://localhost:5000"; // Base API URL
        private List<RouteLookupViewModel> _availableRoutes = new List<RouteLookupViewModel>();
        private List<SaleViewModel> _allSalesData = new List<SaleViewModel>(); // Added to store all sales data
        
        // Collections for income data
        private ObservableCollection<MonthlyIncome> _monthlyIncomes = new();
        private ObservableCollection<RouteIncome> _routeIncomes = new();
        
        // Summary statistics
        private decimal _totalIncome;
        private int _totalTicketsSold;
        private decimal _averageTicketPrice;

        public frmIncomeReport()
        {
            InitializeComponent();

            // Get instance directly
            _apiClientService = ApiClientService.Instance;

            // Set default dates - last 30 days
            dateFromFilter.DateTime = DateTime.Now.Date.AddDays(-30);
            dateToFilter.DateTime = DateTime.Now.Date;

            // Configure GridView
            ConfigureGridView();

            // Bind events
            this.Load += frmIncomeReport_Load;
            gridViewReport.CustomColumnDisplayText += gridViewReport_CustomColumnDisplayText;

            // Handle auth token changes
            _apiClientService.OnAuthTokenChanged += async (s, token) => {
                await LoadReportDataAsync(); // Reload all data on token change
            };
        }

        private void ConfigureGridView()
        {
            gridViewReport.OptionsBehavior.Editable = false;
            gridViewReport.OptionsBehavior.ReadOnly = true;
            gridViewReport.OptionsView.ShowGroupPanel = false;
            gridViewReport.OptionsView.ShowFooter = true; // Enable footer for totals

            // Add summary item for Total Amount
            GridColumnSummaryItem totalIncomeSummary = new GridColumnSummaryItem(
                SummaryItemType.Sum, 
                nameof(SaleViewModel.TotalAmount), 
                "Общий Доход: {0:C}" // Format as currency
            );
            colTotalAmount.Summary.Add(totalIncomeSummary); // Assuming colTotalAmount is the name from the designer
        }

        private async void frmIncomeReport_Load(object sender, EventArgs e)
        {
            Log.Information("Loading routes for Income Report filter...");
            await LoadRoutesAsync(); // Load routes for filter first
            Log.Information("Loading initial income report data...");
            await LoadReportDataAsync(); // Then load report data and apply initial filters
        }

        private async Task LoadRoutesAsync()
        {
            try
            {
                var client = _apiClientService.CreateClient();
                var apiUrl = $"{_baseUrl}/api/routes"; // REMOVED /lookup
                Log.Information("Fetching route lookup data from: {ApiUrl}", apiUrl);
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Assuming the base /api/routes returns a list suitable for lookup (ID + name)
                    // Or that RouteLookupViewModel matches the fields returned by /api/routes
                    _availableRoutes = await response.Content.ReadFromJsonAsync<List<RouteLookupViewModel>>(_jsonOptions)
                                          ?? new List<RouteLookupViewModel>();
                    
                    // Add an "All Routes" option
                    _availableRoutes.Insert(0, new RouteLookupViewModel { RouteId = -1, DisplayName = "[Все маршруты]" });
                    
                    // Bind to LookUpEdit
                    lueRouteFilter.Properties.DataSource = _availableRoutes;
                    lueRouteFilter.Properties.DisplayMember = nameof(RouteLookupViewModel.DisplayName); // Ensure this matches the actual property name in the returned data or ViewModel
                    lueRouteFilter.Properties.ValueMember = nameof(RouteLookupViewModel.RouteId); // Ensure this matches the actual property name
                    lueRouteFilter.EditValue = -1; // Default to "All Routes"

                    Log.Information("Successfully loaded {Count} routes for lookup.", _availableRoutes.Count - 1); // Exclude placeholder
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load route lookup data. Status: {StatusCode}, Content: {ErrorContent}",
                                    response.StatusCode, errorContent);
                    // Clear and disable filter if loading fails
                    _availableRoutes = new List<RouteLookupViewModel>();
                    lueRouteFilter.Properties.DataSource = _availableRoutes;
                    lueRouteFilter.Enabled = false; 
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching route lookup data.");
                _availableRoutes = new List<RouteLookupViewModel>();
                lueRouteFilter.Properties.DataSource = _availableRoutes;
                lueRouteFilter.Enabled = false; 
            }
        }

        private async Task LoadReportDataAsync()
        {
            Log.Information("Loading all income report data...");
            try
            {
                Cursor = Cursors.WaitCursor;
                _allSalesData = new List<SaleViewModel>(); // Clear previous full data
                gridControlReport.DataSource = null; // Clear display

                // Fetch ALL sales data without server-side filtering
                var apiUrl = $"{_baseUrl}/api/sales"; 
                Log.Information("Fetching all sales data from: {ApiUrl}", apiUrl);

                var client = _apiClientService.CreateClient();
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Store all sales data locally
                    _allSalesData = await response.Content.ReadFromJsonAsync<List<SaleViewModel>>(_jsonOptions)
                                    ?? new List<SaleViewModel>();
                    
                    Log.Information("Successfully loaded {Count} total sales items.", _allSalesData.Count);

                    // Apply initial filters and bind
                    ApplyFiltersAndBindData(); 
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load ALL sales data. Status: {StatusCode}, Content: {ErrorContent}",
                                    response.StatusCode, errorContent);
                    XtraMessageBox.Show($"Ошибка загрузки отчета: {response.ReasonPhrase}\n{errorContent}", "Ошибка API",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Clear grid if loading failed
                    gridControlReport.DataSource = null;
                     _monthlyIncomes.Clear();
                    _routeIncomes.Clear();
                    _totalIncome = 0;
                    _totalTicketsSold = 0;
                    _averageTicketPrice = 0;
                    // Potentially update summary labels here if needed
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading ALL sales data.");
                XtraMessageBox.Show($"Произошла ошибка при загрузке данных отчета: {ex.Message}", "Критическая Ошибка",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                gridControlReport.DataSource = null; // Clear grid on error
                _monthlyIncomes.Clear();
                _routeIncomes.Clear();
                _totalIncome = 0;
                _totalTicketsSold = 0;
                _averageTicketPrice = 0;
                // Potentially update summary labels here if needed
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // New method to apply filters and update the grid
        private void ApplyFiltersAndBindData()
        {
             Log.Information("Applying client-side filters...");
             try
             {
                Cursor = Cursors.WaitCursor;
                gridControlReport.DataSource = null; // Clear previous filtered data

                DateTime fromDate = dateFromFilter.DateTime.Date;
                DateTime toDate = dateToFilter.DateTime.Date.AddDays(1).AddTicks(-1); // Include end date
                int? routeId = lueRouteFilter.EditValue as int?;
                if (routeId == -1) routeId = null; // Treat -1 as no filter

                // Filter the local _allSalesData list
                var filteredData = _allSalesData
                    .Where(s => s.SaleDate >= fromDate && s.SaleDate <= toDate)
                    .ToList(); // Start with date filter

                if (routeId.HasValue)
                {
                    // Find the schedule IDs for the selected route
                    // We need to join or filter based on the RouteId which might be nested
                    // Assuming SaleViewModel has a RouteScheduleId or similar property.
                    // If not, this part needs adjustment based on the actual SaleViewModel structure.
                    // Let's assume SaleViewModel has RouteScheduleId and we need to map RouteId -> RouteScheduleId
                    // This mapping isn't available directly here. A simpler approach if SaleViewModel has RouteId:
                    // filteredData = filteredData.Where(s => s.RouteId == routeId.Value).ToList(); 
                    
                    // If SaleViewModel has RouteDescription, we can filter by that if the lookup uses it.
                    // Let's filter by RouteId assuming it's in SaleViewModel for now.
                    // **Assumption:** SaleViewModel has a direct or indirect RouteId property.
                    // Need to verify SaleViewModel structure if this doesn't work.
                    // Example if SaleViewModel has RouteSchedule.RouteId:
                    // filteredData = filteredData.Where(s => s.RouteSchedule?.RouteId == routeId.Value).ToList();
                    
                    // Safest approach given the models: Filter by RouteDescription if possible.
                    var selectedRoute = _availableRoutes.FirstOrDefault(r => r.RouteId == routeId.Value);
                    if (selectedRoute != null)
                    {
                         // Assuming SaleViewModel has RouteDescription matching the lookup DisplayName
                         filteredData = filteredData.Where(s => s.RouteDescription == selectedRoute.DisplayName).ToList();
                    }
                    // If filtering by RouteId is necessary, SaleViewModel needs that property, 
                    // or we need a way to link Sale -> RouteSchedule -> Route
                     Log.Information("Applying filter for Route ID: {RouteId}", routeId.Value);
                }

                // Set filtered data source
                gridControlReport.DataSource = filteredData;
                Log.Information("Client-side filtering applied. Displaying {Count} items.", filteredData.Count);

                // Clear previous summary data
                _monthlyIncomes.Clear();
                _routeIncomes.Clear();
                _totalIncome = 0;
                _totalTicketsSold = 0;
                _averageTicketPrice = 0;

                // Recalculate summaries based on filtered data
                if (filteredData.Any())
                {
                    var monthlyData = filteredData
                        .GroupBy(s => new { Year = s.SaleDate.Year, Month = s.SaleDate.Month })
                        .Select(g => new MonthlyIncome
                        {
                            Year = g.Key.Year,
                            Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM"),
                            TotalIncome = g.Sum(s => (decimal)s.TotalAmount),
                            TicketsSold = g.Count(),
                            AverageTicketPrice = g.Count() > 0 ? g.Sum(s => (decimal)s.TotalAmount) / g.Count() : 0
                        })
                        .OrderBy(m => m.Year)
                        .ThenBy(m => DateTime.ParseExact(m.Month, "MMMM", System.Globalization.CultureInfo.CurrentCulture).Month) // Use current culture for month name parsing
                        .ToList();
                    _monthlyIncomes = new ObservableCollection<MonthlyIncome>(monthlyData);

                    var routeData = filteredData
                        .GroupBy(s => s.RouteDescription) // Assuming RouteDescription exists and is correct
                        .Select(g => new RouteIncome
                        {
                            RouteName = g.Key,
                            TotalIncome = g.Sum(s => (decimal)s.TotalAmount),
                            TicketsSold = g.Count(),
                            AverageTicketPrice = g.Count() > 0 ? g.Sum(s => (decimal)s.TotalAmount) / g.Count() : 0
                        })
                        .OrderByDescending(r => r.TotalIncome)
                        .ToList();
                    _routeIncomes = new ObservableCollection<RouteIncome>(routeData);
                        
                    _totalIncome = filteredData.Sum(s => (decimal)s.TotalAmount);
                    _totalTicketsSold = filteredData.Count;
                    _averageTicketPrice = filteredData.Count > 0 ? _totalIncome / filteredData.Count : 0;
                }
                 // Update summary labels or footer if needed
                // gridViewReport.UpdateSummary(); // Might be needed
             }
             catch (Exception ex)
             {
                Log.Error(ex, "Error applying client-side filters.");
                 XtraMessageBox.Show($"Произошла ошибка при применении фильтров: {ex.Message}", "Ошибка Фильтрации",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
             }
             finally
             {
                 Cursor = Cursors.Default;
             }
        }

        private void gridViewReport_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            // Reuse formatting logic from frmSalesManagement if applicable
            if (e.Column.FieldName == nameof(SaleViewModel.TotalAmount) && e.Value is double amount)
            {
                e.DisplayText = string.Format("{0:C}", amount);
            }
            else if (e.Value is DateTime dt)
            {
                if (e.Column.FieldName == nameof(SaleViewModel.SaleDate))
                {
                    e.DisplayText = dt.ToString("dd.MM.yyyy HH:mm");
                }
                else if (e.Column.FieldName == nameof(SaleViewModel.DepartureTime) || e.Column.FieldName == nameof(SaleViewModel.ArrivalTime))
                {
                    e.DisplayText = dt.ToString("dd.MM HH:mm");
                }
            }
        }

        private void btnApplyFilter_Click(object sender, EventArgs e) // No async void needed
        {
            Log.Information("Apply Filter button clicked for income report.");
            // Directly apply filters to the existing data
            ApplyFiltersAndBindData();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            Log.Information("Export button clicked for income report.");
            try
            {
                if (gridViewReport.RowCount == 0)
                {
                    XtraMessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                    FilterIndex = 1,
                    RestoreDirectory = true,
                    FileName = $"IncomeReport_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    Cursor = Cursors.WaitCursor;
                    string fileExt = System.IO.Path.GetExtension(saveDialog.FileName).ToLowerInvariant();
                    
                    // Use WYSIWYG export options
                    var options = new XlsxExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG };
                    // Footer export is handled via WYSIWYG by default if the grid footer is visible.
                    // There's no separate 'ShowFooter' on XlsxExportOptionsEx.
                    
                    if (fileExt == ".xlsx")
                    {
                        gridControlReport.ExportToXlsx(saveDialog.FileName, options);
                    }
                    else if (fileExt == ".csv")
                    {
                        var csvOptions = new CsvExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG };
                        gridControlReport.ExportToCsv(saveDialog.FileName, csvOptions); 
                    }
                    
                    Log.Information("Income report exported successfully to {FileName}", saveDialog.FileName);
                    XtraMessageBox.Show("Отчет успешно экспортирован.", "Экспорт завершен",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Optional: Open file logic (same as frmSalesManagement)
                    if (XtraMessageBox.Show("Открыть экспортированный файл?", "Открыть файл",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        try
                        {
                            var processStartInfo = new System.Diagnostics.ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true };
                            System.Diagnostics.Process.Start(processStartInfo);
                        }
                        catch (Exception exOpen)
                        {
                            Log.Error(exOpen, "Failed to open exported file {FileName}", saveDialog.FileName);
                            XtraMessageBox.Show($"Не удалось открыть файл: {exOpen.Message}", "Ошибка открытия файла",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting income report.");
                XtraMessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка экспорта",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}