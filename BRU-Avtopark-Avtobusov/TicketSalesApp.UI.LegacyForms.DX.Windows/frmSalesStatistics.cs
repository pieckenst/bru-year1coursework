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
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web; // For HttpUtility
using DevExpress.XtraCharts; // Added for charts
using Serilog; // Added for Serilog
using System.Text.Json.Serialization; // Added for ReferenceHandler

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    // ViewModel for route statistics
    public class RouteStatistic
    {
        public string RouteName { get; set; } = string.Empty;
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public double SalesPercentage { get; set; }
    }

    // ViewModel for daily statistics
    public class DailyStatistic
    {
        public DateTime Date { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public double GrowthRate { get; set; }
    }

    // ViewModel for data from /api/statistics/sales
    public class SalesStatisticsViewModel
    {
        public int TotalSalesCount { get; set; }
        public double TotalIncome { get; set; }
        public List<SalesByRouteStatistic> SalesByRoute { get; set; } = new List<SalesByRouteStatistic>();
        public List<RouteStatistic> RouteStatistics { get; set; } = new List<RouteStatistic>();
        public List<DailyStatistic> DailyStatistics { get; set; } = new List<DailyStatistic>();
        public double AverageGrowthRate { get; set; }
    }

    public class SalesByRouteStatistic
    {
        public string RouteName { get; set; } = string.Empty;
        public int SalesCount { get; set; }
        public double TotalIncome { get; set; }
    }

    public partial class frmSalesStatistics : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClientService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
             PropertyNameCaseInsensitive = true,
             ReferenceHandler = ReferenceHandler.Preserve
        };
        private readonly string _baseUrl = "http://localhost:5000"; // Base API URL
        private ObservableCollection<RouteStatistic> _routeStatistics = new ObservableCollection<RouteStatistic>();
        private ObservableCollection<DailyStatistic> _dailyStatistics = new ObservableCollection<DailyStatistic>();
        private bool _isBusy;
        private string _errorMessage = string.Empty;
        private bool _hasError;

        public frmSalesStatistics()
        {
            InitializeComponent();

            // Get instance directly
            _apiClientService = ApiClientService.Instance;

            // Set default dates (current month and previous month)
            dateFromFilter.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            dateToFilter.DateTime = DateTime.Now.Date;

            // Initialize Chart
            ConfigureChart();

            // Load data on load
            this.Load += async (s, e) => await LoadStatisticsAsync();

            // Handle auth token changes
            _apiClientService.OnAuthTokenChanged += async (s, token) => {
                await LoadStatisticsAsync();
            }; 
        }

        private void ConfigureChart()
        {
            // Basic configuration (can be done in designer too)
            chartControlSalesByRoute.Series.Clear();
            var series = new Series("Продажи по маршрутам", ViewType.Pie);
            chartControlSalesByRoute.Series.Add(series);

            // Configure series labels
            series.Label.TextPattern = "{A}: {V:N0} ({VP:p0})"; // Argument: Value (Count) (Percentage)
            series.LegendTextPattern = "{A}"; // Show route name in legend
            
            // ArgumentDataMember should match the property name for the label (RouteName)
            series.ArgumentDataMember = nameof(RouteStatistic.RouteName); 
            // ValueDataMembers should match the property for the value (TotalSales)
            series.ValueDataMembers.AddRange(new string[] { nameof(RouteStatistic.TotalSales) }); 

            // Tooltip configuration
            PieSeriesView view = series.View as PieSeriesView;
            if (view != null)
            {
                view.Titles.Add(new SeriesTitle { Text = "Количество продаж"}); // Optional title
                view.ExplodeMode = PieExplodeMode.UseFilters;
                view.ExplodedDistancePercentage = 30;
            }
            
            chartControlSalesByRoute.Legend.Visibility = DevExpress.Utils.DefaultBoolean.True;
        }

        private async Task LoadStatisticsAsync()
        {
            Log.Information("Loading sales statistics...");
            try
            {
                _isBusy = true;
                Cursor = Cursors.WaitCursor;
                _hasError = false;
                _errorMessage = string.Empty;
                
                // Clear previous data
                lblTotalSalesValue.Text = "-";
                lblTotalIncomeValue.Text = "-";
                chartControlSalesByRoute.DataSource = null;

                DateTime fromDate = dateFromFilter.DateTime.Date;
                DateTime toDate = dateToFilter.DateTime.Date.AddDays(1).AddTicks(-1); // Include whole end day

                var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
                queryBuilder["fromDate"] = fromDate.ToString("o"); // ISO 8601
                queryBuilder["toDate"] = toDate.ToString("o");

                var client = _apiClientService.CreateClient();
                var apiUrl = $"{_baseUrl}/api/statistics/sales?{queryBuilder.ToString()}";
                Log.Information("Fetching sales statistics from: {ApiUrl}", apiUrl);

                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var stats = await response.Content.ReadFromJsonAsync<SalesStatisticsViewModel>(_jsonOptions);
                    if (stats != null)
                    {
                        Log.Information("Successfully loaded sales statistics.");
                        
                        // Update UI elements
                        lblTotalSalesValue.Text = stats.TotalSalesCount.ToString("N0");
                        lblTotalIncomeValue.Text = stats.TotalIncome.ToString("C");

                        // Process route statistics
                        var routeStats = stats.SalesByRoute
                            .Select(r => new RouteStatistic {
                                RouteName = r.RouteName,
                                TotalSales = r.SalesCount,
                                TotalRevenue = (decimal)r.TotalIncome,
                                SalesPercentage = (double)r.SalesCount / stats.TotalSalesCount * 100
                            })
                            .OrderByDescending(r => r.TotalSales)
                            .ToList();
                        
                        _routeStatistics = new ObservableCollection<RouteStatistic>(routeStats);

                        // Bind data to chart
                        if (routeStats.Any())
                        {
                            chartControlSalesByRoute.DataSource = routeStats.Take(5).ToList(); // Show top 5 routes
                            chartControlSalesByRoute.RefreshData();
                        }
                        else
                        {
                            Log.Information("No 'Sales by Route' data to display in chart.");
                            // Optionally display a message on the chart or hide it
                        }
                    }
                    else
                    {
                        Log.Warning("Sales statistics API returned success but data was null or empty.");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load sales statistics. Status: {StatusCode}, Content: {ErrorContent}",
                                    response.StatusCode, errorContent);
                    _hasError = true;
                    _errorMessage = $"Ошибка загрузки статистики: {response.ReasonPhrase}\n{errorContent}";
                    XtraMessageBox.Show(_errorMessage, "Ошибка API",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (HttpRequestException httpEx)
            {
                Log.Error(httpEx, "Network error loading sales statistics.");
                _hasError = true;
                _errorMessage = $"Сетевая ошибка при загрузке статистики: {httpEx.Message}";
                XtraMessageBox.Show(_errorMessage, "Ошибка сети",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (JsonException jsonEx)
            {
                Log.Error(jsonEx, "Error deserializing sales statistics.");
                _hasError = true;
                _errorMessage = $"Ошибка обработки данных статистики: {jsonEx.Message}";
                XtraMessageBox.Show(_errorMessage, "Ошибка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Generic error loading sales statistics.");
                _hasError = true;
                _errorMessage = $"Произошла ошибка при загрузке статистики: {ex.Message}";
                XtraMessageBox.Show(_errorMessage, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
                Cursor = Cursors.Default;
            }
        }

        private async void btnApplyFilter_Click(object sender, EventArgs e)
        {
            Log.Information("Apply Filter button clicked for statistics.");
            if (dateFromFilter.DateTime > dateToFilter.DateTime)
            {
                XtraMessageBox.Show("Дата 'С' не может быть позже даты 'По'.", "Неверный диапазон дат",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await LoadStatisticsAsync();
        }
    }
} 