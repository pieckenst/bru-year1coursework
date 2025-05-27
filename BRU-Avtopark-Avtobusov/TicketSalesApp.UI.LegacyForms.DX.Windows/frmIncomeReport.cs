using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using DevExpress.XtraPrinting;
using System.Net.Http;
using System.Web; // For HttpUtility
using DevExpress.Data;
using DevExpress.XtraGrid.Views.Base; // For SummaryItemType
using NLog; // Changed from Serilog to NLog
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TicketSalesApp.Core.Models;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
// Added for synchronous XML processing and threading
using System.Threading;
using System.Globalization;
using System.Xml;
using System.Text.RegularExpressions;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    // Define required ViewModels directly inside the form file
    public class IncomeReport_SaleViewModel // Renamed to avoid ambiguity
    {
        public long SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        public string TicketDescription { get; set; } // Combined ticket info (Example: "Seat 5")
        public string RouteDescription { get; set; } // Combined route info (Example: "Minsk -> Brest")
        public long RouteId { get; set; } // Added for reliable filtering
        public double TotalAmount { get; set; } // Corresponds to Bilet.TicketPrice
        public string PassengerName { get; set; } // Corresponds to Prodazha.TicketSoldToUser
        public string PassengerPhone { get; set; } // Corresponds to Prodazha.TicketSoldToUserPhone

        public IncomeReport_SaleViewModel() // Add default constructor
        {
             TicketDescription = string.Empty;
             RouteDescription = string.Empty;
             PassengerName = string.Empty;
             PassengerPhone = string.Empty;
        }
    }

    // Model for monthly income data
    public class MonthlyIncome
    {
        public string Month { get; set; }
        public int Year { get; set; }
        public decimal TotalIncome { get; set; }
        public int TicketsSold { get; set; }
        public decimal AverageTicketPrice { get; set; }

        public MonthlyIncome()
        {
            Month = string.Empty;
        }
    }

    // Model for route income data
    public class RouteIncome
    {
        public string RouteName { get; set; }
        public decimal TotalIncome { get; set; }
        public int TicketsSold { get; set; }
        public decimal AverageTicketPrice { get; set; }

        public RouteIncome()
        {
            RouteName = string.Empty;
        }
    }

    // ViewModel for Route Lookup
    public class RouteLookupViewModel
    {
        public int RouteId { get; set; }
        public string DisplayName { get; set; }

        public RouteLookupViewModel()
        {
            DisplayName = string.Empty;
        }
    }

    public partial class frmIncomeReport : DevExpress.XtraEditors.XtraForm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger(); // NLog instance
        private static Dictionary<string, JObject> _resolvedRefCache = new Dictionary<string, JObject>(); // Added static cache for cleaned refs
        private static Dictionary<string, JToken> _fullyCleanedCache = new Dictionary<string, JToken>(); // Cache for fully processed results by $id - current instance of cache
        
        private static Dictionary<string, JToken> _fullyCleanedCachebackupfortypeone = new Dictionary<string, JToken>(); //Backup the  Cache for fully processed results by $id - will be sales backup
        private static Dictionary<string, JToken> _fullyCleanedCachebackupfortypetwo = new Dictionary<string, JToken>(); //Backup the  Cache for fully processed results by $id - will be routes backup
        private static string _lastProcessedRootElementName = null; // Track the root element name for cache invalidation
        private readonly ApiClientService _apiClient; // Changed from _apiClientService
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings 
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Add this for safety
            DateFormatHandling = DateFormatHandling.IsoDateFormat, // Match example
            DateTimeZoneHandling = DateTimeZoneHandling.Utc // Match example
        };
        private readonly string _baseUrl = "http://localhost:5000/api"; // Corrected Base API URL
        private List<RouteLookupViewModel> _availableRoutes = new List<RouteLookupViewModel>();
        private List<IncomeReport_SaleViewModel> _allSalesData = new List<IncomeReport_SaleViewModel>(); // Use renamed ViewModel
        
        // Collections for income data
        private ObservableCollection<MonthlyIncome> _monthlyIncomes = new ObservableCollection<MonthlyIncome>();
        private ObservableCollection<RouteIncome> _routeIncomes = new ObservableCollection<RouteIncome>();
        
        // Summary statistics
        private decimal _totalIncome;
        private int _totalTicketsSold;
        private decimal _averageTicketPrice;
        private bool _isBusy = false; // Added loading state flag
        private bool _formLoadComplete = false; // Added

        public frmIncomeReport()
        {
            InitializeComponent();

            // Get instance directly
            _apiClient = ApiClientService.Instance; // Use Instance

            // Set default dates - last 30 days
            dateFromFilter.DateTime = DateTime.Now.Date.AddDays(-30);
            dateToFilter.DateTime = DateTime.Now.Date;

            // Configure GridView
            ConfigureGridView();

            // Bind events
            this.Load += frmIncomeReport_Load;
            gridViewReport.CustomColumnDisplayText += gridViewReport_CustomColumnDisplayText;

            // Handle auth token changes - USE DELEGATE FOR C# 4.0 compatibility
            _apiClient.OnAuthTokenChanged += HandleAuthTokenChanged;
            this.FormClosing += FrmIncomeReport_FormClosing; // Added
        }

        // Added FormClosing event handler
        private void FrmIncomeReport_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Debug("Form closing.");
            _apiClient.OnAuthTokenChanged -= HandleAuthTokenChanged; // Unsubscribe
        }

        // Added Token Changed Handler
        private void HandleAuthTokenChanged(object sender, string token)
        {
            string logMsg = "Auth token changed, triggering synchronous data reload.";
            Log.Debug(logMsg);
            // Use BeginInvoke to ensure UI updates happen on the UI thread safely
            if (this.Visible && this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(delegate {
                    try
                    {
                        // Reload prerequisites (routes) and then main data
                        if (LoadRoutesSynchronously()) // Reload routes first
                        {
                            LoadDataSynchronously(); // Then reload sales data
                        }
                        else
                        {
                            // Handle route load failure (e.g., show message, clear grid)
                            XtraMessageBox.Show("Не удалось обновить список маршрутов после смены токена.", "Ошибка обновления", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            _allSalesData.Clear();
                            _monthlyIncomes.Clear();
                            _routeIncomes.Clear();
                            gridControlReport.DataSource = null;
                            gridControlReport.RefreshDataSource();
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = string.Format("Error during background refresh triggered by token change. Exception: {0}", ex.ToString());
                        Log.Error(errorMsg);
                        // Optionally show a non-blocking message
                    }
                 }));
            } else {
                string skipMsg = "Auth token changed, but form is not visible or handle not created. Skipping reload.";
                Log.Debug(skipMsg);
            }
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
                "TotalAmount",
                "Общий Доход: {0:C}" // Format as currency
            );
            if (colTotalAmount != null) {
                colTotalAmount.Summary.Add(totalIncomeSummary);
            }

            // Add columns for Passenger Name and Phone if they don't exist in designer
            // It's better to add these in the designer, but this is a fallback
             /* // REMOVED - Columns added in Designer instead
            if (gridViewReport.Columns["colPassengerName"] == null)
            {
                var colPassengerName = new GridColumn();
                colPassengerName.Caption = "Пассажир";
                colPassengerName.FieldName = "PassengerName";
                colPassengerName.Name = "colPassengerName";
                colPassengerName.Visible = true;
                colPassengerName.VisibleIndex = 8; // Adjust index as needed
                colPassengerName.Width = 150;
                gridViewReport.Columns.Add(colPassengerName);
            }
            if (gridViewReport.Columns["colPassengerPhone"] == null)
            {
                var colPassengerPhone = new GridColumn();
                colPassengerPhone.Caption = "Телефон";
                colPassengerPhone.FieldName = "PassengerPhone";
                colPassengerPhone.Name = "colPassengerPhone";
                colPassengerPhone.Visible = true;
                colPassengerPhone.VisibleIndex = 9; // Adjust index as needed
                colPassengerPhone.Width = 100;
                gridViewReport.Columns.Add(colPassengerPhone);
            }
            */
        }

        // Modified Load event handler
        private void frmIncomeReport_Load(object sender, EventArgs e)
        {
            if (_formLoadComplete)
            {
                string warnMsg = "frmIncomeReport_Load fired again after initial load completed. Ignoring.";
                Log.Warn(warnMsg);
                return;
            }

            bool initialLoadSuccess = false;
            try
            {
                Log.Debug("frmIncomeReport_Load event triggered (initial run).");
                // Load routes first synchronously
                if (LoadRoutesSynchronously())
                {
                    // If routes load, load the main report data
                    LoadDataSynchronously();
                    initialLoadSuccess = true;
                }
                else
                {
                    // Handle route load failure on initial load
                    Log.Error("Failed to load initial routes. Income report cannot be displayed correctly.");
                    XtraMessageBox.Show("Не удалось загрузить список маршрутов. Отчет о доходах не может быть загружен.", "Ошибка Инициализации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Optionally close the form if routes are critical
                    // this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) this.Close(); }));
                }

                if (initialLoadSuccess) {
                    _formLoadComplete = true;
                    string infoMsg = "Initial form load sequence completed successfully.";
                    Log.Info(infoMsg);
                }
            }
            catch (Exception ex)
            {
                _formLoadComplete = true; // Mark as complete even on error
                string errorMsg = string.Format("Critical error during form load sequence. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                 XtraMessageBox.Show(string.Format("Произошла критическая ошибка при загрузке формы: {0}", ex.Message), "Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 // Optionally close
                 // this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) this.Close(); }));
            }
        }

        // --- New SetLoadingState Method ---
        private void SetLoadingState(bool isLoading)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) SetLoadingState(isLoading); }));
                return;
            }

            if (this.IsDisposed || layoutControl1 == null || layoutControl1.IsDisposed)
            {
                 string warnMsg = "SetLoadingState called but form or controls are disposed.";
                 Log.Warn(warnMsg);
                 return;
            }

            Log.Debug(isLoading ? "Setting UI to loading state." : "Setting UI to normal state.");
            _isBusy = isLoading;
            Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;

            // Disable/Enable controls
            if (gridControlReport != null && !gridControlReport.IsDisposed) gridControlReport.Enabled = !isLoading;
            if (dateFromFilter != null && !dateFromFilter.IsDisposed) dateFromFilter.Enabled = !isLoading;
            if (dateToFilter != null && !dateToFilter.IsDisposed) dateToFilter.Enabled = !isLoading;
            if (lueRouteFilter != null && !lueRouteFilter.IsDisposed) lueRouteFilter.Enabled = !isLoading;
            if (btnApplyFilter != null && !btnApplyFilter.IsDisposed) btnApplyFilter.Enabled = !isLoading;
            if (btnExport != null && !btnExport.IsDisposed) btnExport.Enabled = !isLoading;

            // If enabling controls, update button states based on current context (like data availability)
            if (!isLoading) {
                UpdateButtonStates();
            }
        }

        // --- New UpdateButtonStates Method ---
        private void UpdateButtonStates() {
             if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) UpdateButtonStates(); }));
                return;
            }
             if (this.IsDisposed) return;

            bool isLoading = _isBusy;
            bool hasData = _allSalesData != null && _allSalesData.Any(); // Check if there's any base data

            // Basic example: enable export only if not loading and data exists
            if (btnExport != null && !btnExport.IsDisposed) btnExport.Enabled = !isLoading && hasData;
            if (btnApplyFilter != null && !btnApplyFilter.IsDisposed) btnApplyFilter.Enabled = !isLoading;
            // Add more logic here if needed for other buttons based on selection, etc.
        }

        // --- Modified LoadRoutesAsync to LoadRoutesSynchronously ---
        private bool LoadRoutesSynchronously()
        {
            string logStart = "Loading routes synchronously...";
            Log.Info(logStart);
            HttpClient client = null;
            string routesJsonRaw = null;
            XDocument routesXml = null;
            List<RouteLookupViewModel> loadedRoutes = new List<RouteLookupViewModel>();
            bool success = false;

            SetLoadingState(true); // Indicate loading

            try
            {
                client = _apiClient.CreateClient();
                // Use /api/Routes endpoint based on other examples
                var apiUrl = string.Format("{0}/Routes", _baseUrl);
                Log.Debug("Fetching route lookup data from: {ApiUrl}", apiUrl);
                HttpResponseMessage response = client.GetAsync(apiUrl).Result; // Synchronous call

                if (response.IsSuccessStatusCode)
                {
                    byte[] routesBytes = response.Content.ReadAsByteArrayAsync().Result;
                    routesJsonRaw = Encoding.UTF8.GetString(routesBytes);

                    if (!string.IsNullOrEmpty(routesJsonRaw))
                    {
                        // Process JSON to XML
                        routesXml = ProcessJsonToXml(routesJsonRaw, "Routes"); // Use helper

                        // Parse XML to ViewModel
                        foreach (XElement routeNode in routesXml.Root.Elements("Routes"))
                        {
                            try {
                                if (!routeNode.HasElements) continue; // Skip empty nodes

                                int routeId = 0;
                                string startPoint = string.Empty;
                                string endPoint = string.Empty;

                                XElement idEl = routeNode.Element("routeId");
                                if (idEl != null) int.TryParse(idEl.Value, out routeId); else continue;

                                XElement startEl = routeNode.Element("startPoint");
                                if (startEl != null) startPoint = startEl.Value;
                                XElement endEl = routeNode.Element("endPoint");
                                if (endEl != null) endPoint = endEl.Value;

                                loadedRoutes.Add(new RouteLookupViewModel {
                                    RouteId = routeId,
                                    DisplayName = string.Format("{0} -> {1}", startPoint, endPoint)
                                });
                            } catch (Exception exNode) {
                                string errorMsgNode = string.Format("Error parsing individual Route XML node for lookup: {0}. Node: {1}", exNode.ToString(), routeNode.ToString());
                                Log.Error(errorMsgNode);
                            }
                        }
                        _availableRoutes = loadedRoutes.OrderBy(r => r.DisplayName).ToList();
                        success = true; // Mark as successful
                    }
                    else {
                        string warnMsg = "Route lookup API returned success but content was empty.";
                        Log.Warn(warnMsg);
                        _availableRoutes = new List<RouteLookupViewModel>(); // Clear existing
                    }
                }
                else
                {
                    var errorContent = response.Content.ReadAsStringAsync().Result; // Sync read
                    string errorMsg = string.Format("Failed to load route lookup data. Status: {0}, Content: {1}", response.StatusCode, errorContent);
                    Log.Error(errorMsg);
                    _availableRoutes = new List<RouteLookupViewModel>(); // Clear on error
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Error fetching/processing route lookup data. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                _availableRoutes = new List<RouteLookupViewModel>(); // Clear on exception
                success = false;
            }
            finally
            {
                if (client != null) client.Dispose();

                // Update UI on UI Thread
                Action updateUiAction = delegate() {
                    if (this.IsDisposed) return;
                    // Add "All Routes" option AFTER loading
                    _availableRoutes.Insert(0, new RouteLookupViewModel { RouteId = -1, DisplayName = "[Все маршруты]" });

                    // Bind to LookUpEdit
                    lueRouteFilter.Properties.DataSource = null; // Clear first
                lueRouteFilter.Properties.DataSource = _availableRoutes;
                    lueRouteFilter.Properties.DisplayMember = "DisplayName";
                    lueRouteFilter.Properties.ValueMember = "RouteId";
                    lueRouteFilter.EditValue = -1; // Default to "All Routes"
                    lueRouteFilter.Enabled = success && _availableRoutes.Count > 1; // Enable only if loaded successfully and has more than placeholder

                    SetLoadingState(false); // Reset loading state
                };

                 if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(updateUiAction); }
                 else if (!this.IsDisposed) { updateUiAction(); }
        }
            return success;
        }


        // --- Modified LoadReportDataAsync to LoadDataSynchronously ---
        private void LoadDataSynchronously()
        {
            string logStart = "Loading all income report data synchronously...";
            Log.Info(logStart);
            SetLoadingState(true);

             HttpClient client = null;
            string salesJsonRaw = null;
            XDocument salesXml = null;
            List<IncomeReport_SaleViewModel> loadedSales = new List<IncomeReport_SaleViewModel>();

            try
            {
                // Build Query String (No change needed here, already synchronous)
                var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
                DateTime? fromDate = null, toDate = null;
                int? routeIdFilter = null;
                
                // Get filter values safely on UI thread
                Action getFiltersAction = delegate() {
                if (dateFromFilter.EditValue != null && dateFromFilter.EditValue != DBNull.Value)
                        fromDate = dateFromFilter.DateTime.Date;
                if (dateToFilter.EditValue != null && dateToFilter.EditValue != DBNull.Value)
                        toDate = dateToFilter.DateTime.Date.AddDays(1).AddTicks(-1); // Filtering done client-side now

                    if (lueRouteFilter.EditValue != null && lueRouteFilter.EditValue != DBNull.Value) {
                        int selectedId = Convert.ToInt32(lueRouteFilter.EditValue);
                        if (selectedId > 0) routeIdFilter = selectedId;
                    }
                };
                 if (this.InvokeRequired) { this.Invoke(getFiltersAction); } else { getFiltersAction(); }

                 // Add API query parameters if needed - BUT we load ALL data first now
                 // If API supports filtering, add them here. If not, remove these lines.
                 // Example if API supported it:
                 // if (fromDate.HasValue) queryBuilder["startDate"] = fromDate.Value.ToString("o");
                 // if (toDate.HasValue) queryBuilder["endDate"] = toDate.Value.AddDays(1).AddTicks(-1).ToString("o");
                 // if (routeIdFilter.HasValue) queryBuilder["routeId"] = routeIdFilter.Value.ToString();

                var apiUrl = string.Format("{0}/TicketSales/search?{1}", _baseUrl, queryBuilder.ToString());
                Log.Debug("Fetching ALL income report data from: {ApiUrl}", apiUrl);

                client = _apiClient.CreateClient();
                HttpResponseMessage response = client.GetAsync(apiUrl).Result; // Synchronous call

                if (response.IsSuccessStatusCode)
                {
                    byte[] salesBytes = response.Content.ReadAsByteArrayAsync().Result;
                    salesJsonRaw = Encoding.UTF8.GetString(salesBytes);

                    if (!string.IsNullOrEmpty(salesJsonRaw))
                    {
                        // Process JSON to XML
                        salesXml = ProcessJsonToXml(salesJsonRaw, "Sales"); // Use helper

                        // Parse XML to ViewModel
                        foreach (XElement saleNode in salesXml.Root.Elements("Sales"))
                        {
                             try {
                                if (!saleNode.HasElements) continue;

                                IncomeReport_SaleViewModel vm = new IncomeReport_SaleViewModel();
                                long saleId = 0;
                                DateTime saleDate = DateTime.MinValue;
                                double price = 0;
                                string passengerName = string.Empty;
                                string passengerPhone = string.Empty;
                                string ticketDesc = string.Empty; // e.g., Seat #
                                string routeDesc = string.Empty; // e.g., Start -> End
                                string startPoint = string.Empty;
                                string endPoint = string.Empty;
                                int seat = 0;
                                long routeId = 0; // Added variable to parse RouteId

                                // Common elements directly under Sale
                                XElement idEl = saleNode.Element("saleId");
                                if (idEl != null) long.TryParse(idEl.Value, out saleId); else continue; // Skip if no ID

                                XElement dateEl = saleNode.Element("saleDate");
                                if (dateEl != null) DateTime.TryParse(dateEl.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out saleDate);

                                XElement userEl = saleNode.Element("ticketSoldToUser");
                                if (userEl != null) passengerName = userEl.Value;
                                XElement phoneEl = saleNode.Element("ticketSoldToUserPhone");
                                if (phoneEl != null) passengerPhone = phoneEl.Value;

                                // --- Robust Bilet Node Finding (similar to SalesStatistics) ---
                                XElement biletNode = saleNode.Element("bilet");
                                XElement sourceNodeForBiletData = null; 

                                if (biletNode != null) {
                                    sourceNodeForBiletData = biletNode;
                                    Log.Debug("Found Bilet data within <bilet> tag for Sale ID {0}", saleId);
                                }
                                else {
                                    if (saleNode.Element("ticketPrice") != null && saleNode.Element("marshut") != null) {
                                         sourceNodeForBiletData = saleNode;
                                         Log.Debug("Found Bilet data directly under <Sale> node for Sale ID {0} (likely resolved $ref).", saleId);
                                    }
                                    else {
                                         Log.Warn("Could not locate Bilet data for Sale ID {0}. Price/Route info unavailable. XML: {1}", saleId, saleNode.ToString());
                                         // Continue processing the sale, but price/route might be missing
                                         // Ensure sourceNodeForBiletData is null so parsing is skipped below but vm is still added
                                         sourceNodeForBiletData = null;
                                    }
                                }
                                // --- End Robust Bilet Node Finding ---

                                // Parse Bilet related data using sourceNodeForBiletData if available
                                if (sourceNodeForBiletData != null) 
                                {
                                    XElement priceEl = sourceNodeForBiletData.Element("ticketPrice");
                                    if (priceEl != null) double.TryParse(priceEl.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out price);

                                    XElement seatEl = sourceNodeForBiletData.Element("seatNumber");
                                    if (seatEl != null) int.TryParse(seatEl.Value, out seat);
                                    ticketDesc = "Место " + seat.ToString(); // Construct description

                                    XElement marshutNode = sourceNodeForBiletData.Element("marshut");
                                    if (marshutNode != null) {
                                        XElement startEl = marshutNode.Element("startPoint");
                                        if (startEl != null) startPoint = startEl.Value;
                                        XElement endEl = marshutNode.Element("endPoint");
                                        if (endEl != null) endPoint = endEl.Value;
                                        routeDesc = string.Format("{0} -> {1}", startPoint, endPoint);

                                        // Parse RouteId from Marshut node
                                        XElement routeIdEl = marshutNode.Element("routeId");
                                        if (routeIdEl != null) long.TryParse(routeIdEl.Value, out routeId);
                                    }
                                    else {
                                         Log.Warn("Marshut node missing within Bilet data source for Sale ID {0}. Cannot determine route. Source XML: {1}", saleId, sourceNodeForBiletData.ToString());
                                    }
                                }
                                // If sourceNodeForBiletData was null, warning already logged.

                                vm.SaleId = saleId;
                                vm.SaleDate = saleDate.ToLocalTime(); // Convert to Local Time for display
                                vm.TotalAmount = price;
                                vm.PassengerName = passengerName;
                                vm.PassengerPhone = passengerPhone;
                                vm.TicketDescription = ticketDesc;
                                vm.RouteDescription = routeDesc;
                                vm.RouteId = routeId; // Assign parsed RouteId

                                loadedSales.Add(vm);
                            } catch (Exception exNode) {
                                 // Log error but try to continue with next item if possible
                                 string errorMsgNode = string.Format("Error parsing individual Sale XML node for income report: {0}. Node: {1}", exNode.ToString(), (saleNode != null ? saleNode.ToString() : "[null node]"));
                                 Log.Error(errorMsgNode);
                            }
                        }
                        _allSalesData = loadedSales; // Store all loaded data
                        string infoMsg = string.Format("Successfully loaded {0} total sales items.", _allSalesData.Count);
                        Log.Info(infoMsg);

                        // Apply initial filters and bind (now uses the loaded _allSalesData)
                    ApplyFiltersAndBindData();
                }
                else
                {
                        string warnMsg = "Sales data API returned success but content was empty.";
                        Log.Warn(warnMsg);
                        _allSalesData = new List<IncomeReport_SaleViewModel>(); // Clear existing
                        ApplyFiltersAndBindData(); // Apply filter to empty list (clears grid)
                    }
                }
                else
                {
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    string errorMsg = string.Format("Failed to load sales data. Status: {0}, Content: {1}", response.StatusCode, errorContent);
                    Log.Error(errorMsg);
                    XtraMessageBox.Show(string.Format("Ошибка загрузки отчета: {0}{1}{2}", response.ReasonPhrase, Environment.NewLine, errorContent), "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Clear data and grid on failure
                    _allSalesData = new List<IncomeReport_SaleViewModel>();
                    ApplyFiltersAndBindData();
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Error loading sales data. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                XtraMessageBox.Show(string.Format("Произошла ошибка при загрузке данных отчета: {0}", ex.Message), "Критическая Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 _allSalesData = new List<IncomeReport_SaleViewModel>();
                ApplyFiltersAndBindData();
            }
            finally
            {
                 if (client != null) client.Dispose();
                 SetLoadingState(false); // Reset loading state
            }
        }


        // --- Start JSON Processing Helper Methods (C# 4.0 Compatible - Copied from examples) ---


        /// <summary>
        /// Processes a raw JSON string into an XDocument, attempting to handle $ref issues.
        /// Uses manual cleaning and reconstruction suitable for potentially complex nested structures.
        /// </summary>
        private static void BuildGlobalIdMap(JToken token, Dictionary<string, JObject> idMap)
        {
            if (token == null) return;
            
            JObject obj = token as JObject;
            if (obj != null && obj.Property("$id") != null)
                {
                string idValue = obj.Property("$id").Value.ToString();
                if (!idMap.ContainsKey(idValue))
                    {
                    idMap.Add(idValue, obj);
                    }
                }

            if (token.HasValues)
            {
                foreach (JToken child in token.Children().ToList())
                {
                    BuildGlobalIdMap(child, idMap);
                }
            }
        }

        /// <summary>
        /// Sanitizes a string to be a valid XML element or attribute name.
        /// Replaces invalid characters with underscores.
        /// </summary>
        private static string SanitizeXmlName(string name)
                {
            if (string.IsNullOrEmpty(name)) return "InvalidName";
            // Use System.Xml.XmlConvert for robust encoding if available, otherwise simple replace
            // Since we target .NET 4, XmlConvert is available.
            try { return System.Xml.XmlConvert.EncodeName(name); }
            catch { 
                 // Fallback for very edge cases XmlConvert might not handle
                 return Regex.Replace(name, @"[^\w.-]", "_"); 
            }
        }

        private static XDocument ProcessJsonToXml(string jsonRaw, string rootElementName)
        {
             _resolvedRefCache.Clear(); // Clear cache for this run
             // Conditional Cache Clearing based on root element type
             if (_lastProcessedRootElementName != rootElementName)
             {
                 Log.Debug("Root element name changed from '{0}' to '{1}'. Managing caches.", _lastProcessedRootElementName ?? "[null]", rootElementName);
  
                 // 1. Backup the *current* cache before potentially overwriting/clearing
                   if (_lastProcessedRootElementName == "Sales")
                   {
                     if (_fullyCleanedCache.Count > 0)
                     {
                         Log.Debug("Backing up current fully cleaned cache ({0} items, Sales) to backup slot 1.", _fullyCleanedCache.Count);
                         _fullyCleanedCachebackupfortypeone = new Dictionary<string, JToken>(_fullyCleanedCache);
                     }
                     else if (_fullyCleanedCachebackupfortypeone.Count > 0)
                     {
                         Log.Warn("Current fully cleaned cache (Sales) is empty, but backup slot 1 has {0} items. Not overwriting backup.", _fullyCleanedCachebackupfortypeone.Count);
                     }
                     else
                     {
                         Log.Debug("Current fully cleaned cache (Sales) and backup slot 1 are both empty. Nothing to backup.");
                     }
                   }
                   else if (_lastProcessedRootElementName == "Routes") 
                   {
                     if (_fullyCleanedCache.Count > 0)
                     {
                         Log.Debug("Backing up current fully cleaned cache ({0} items, Routes) to backup slot 2.", _fullyCleanedCache.Count);
                         _fullyCleanedCachebackupfortypetwo = new Dictionary<string, JToken>(_fullyCleanedCache);
                     }
                     else if (_fullyCleanedCachebackupfortypetwo.Count > 0)
                     {
                         Log.Warn("Current fully cleaned cache (Routes) is empty, but backup slot 2 has {0} items. Not overwriting backup.", _fullyCleanedCachebackupfortypetwo.Count);
                     }
                     else
                     {
                         Log.Debug("Current fully cleaned cache (Routes) and backup slot 2 are both empty. Nothing to backup.");
                     }
                   }
                   
                   // Clear the ref cache regardless, as it's less critical to persist across types
                   _resolvedRefCache.Clear();

                   // 2. Restore or Clear the *main* cache based on the *new* root element
                   bool restored = false;
                   if (rootElementName == "Sales" && _fullyCleanedCachebackupfortypeone.Count > 0)
                   {
                       Log.Debug("Restoring fully cleaned cache from Sales backup ({0} items, Slot 1).", _fullyCleanedCachebackupfortypeone.Count);
                       _fullyCleanedCache = new Dictionary<string, JToken>(_fullyCleanedCachebackupfortypeone);
                       restored = true;
                   }
                   else if (rootElementName == "Routes" && _fullyCleanedCachebackupfortypetwo.Count > 0)
                   {
                       Log.Debug("Restoring fully cleaned cache from Routes backup ({0} items, Slot 2).", _fullyCleanedCachebackupfortypetwo.Count);
                       _fullyCleanedCache = new Dictionary<string, JToken>(_fullyCleanedCachebackupfortypetwo);
                       restored = true;
                   }

                   if (!restored)
                   {
                       Log.Debug("No suitable backup found or backup empty. Clearing main fully cleaned cache.");
                       _fullyCleanedCache.Clear(); // Clear if no backup was restored
                   }
 
                   _lastProcessedRootElementName = rootElementName; // Update tracker
               } else {
                   Log.Debug("Processing the same root element type ('{0}') as last time. Reusing existing caches.", rootElementName);
                   // Caches are reused, no clearing needed.
               }

              if (string.IsNullOrWhiteSpace(jsonRaw))

            if (string.IsNullOrWhiteSpace(jsonRaw))
            {
                Log.Warn("ProcessJsonToXml called with null or empty JSON for {0}. Returning default empty XML.", rootElementName);
                return XDocument.Parse(string.Format("<Root><{0}></{0}></Root>", rootElementName));
            }

            string truncatedJson = (jsonRaw.Length > 2000) ? jsonRaw.Substring(0, 2000) + "..." : jsonRaw;
            string logMsgStart = string.Format("Processing raw JSON for {0} (truncated): {1}", rootElementName, truncatedJson);
            Log.Debug(logMsgStart);

            string preCleanedJson = Regex.Replace(jsonRaw ?? string.Empty, @"[\u0000-\u001F]", "");
            if (string.IsNullOrEmpty(preCleanedJson)) {
                Log.Warn("preCleanedJson is null or empty after cleaning control characters for {0}. Returning empty XML.", rootElementName);
                return XDocument.Parse(string.Format("<Root><{0}></{0}></Root>", rootElementName));
            }

            JToken rootToken = null;
            try {
                rootToken = JToken.Parse(preCleanedJson);
            } catch (JsonReaderException jsonEx) {
                 string errorMsg = string.Format("Failed to parse pre-cleaned JSON for {0}. Exception: {1}", rootElementName, jsonEx.ToString());
                 Log.Error(errorMsg);
                 throw new Exception(string.Format("Ошибка парсинга JSON для {0}.", rootElementName), jsonEx);
            }
            
            JObject finalObjectForXml = null;
            Dictionary<string, JObject> globalIdMap = new Dictionary<string, JObject>();

            try {
                BuildGlobalIdMap(rootToken, globalIdMap);
                Log.Info(string.Format("Built GLOBAL ID map with {0} entries for {1} structure.", globalIdMap.Count, rootElementName));
            } catch (Exception mapEx) {
                 string errorMsg = string.Format("Error building global ID map for {0}. Exception: {1}", rootElementName, mapEx.ToString());
                 Log.Error(errorMsg);
                 // Proceed without map? Might be risky.
            }

            // --- Pre-populate the static cache with partially cleaned objects --- 
            Log.Debug("Pre-populating resolved reference cache...");
            foreach (KeyValuePair<string, JObject> entry in globalIdMap)
            {
                if (!_resolvedRefCache.ContainsKey(entry.Key))
                {
                    try {
                        // Clone the original object found by $id
                        JObject clonedObj = (JObject)entry.Value.DeepClone(); 
                        // Remove $id from the clone ONLY. Do NOT recursively clean children here.
                        clonedObj.Remove("$id"); 
                        _resolvedRefCache.Add(entry.Key, clonedObj);
                        Log.Trace("Cached object for $id = {0}", entry.Key);
                    } catch (Exception cacheEx) {
                         Log.Error("Error cloning/pre-caching object with $id = {0}. Exception: {1}", entry.Key, cacheEx.ToString());
                         // Skipping for now, CleanAndTransformJsonToken will handle missing cache entry.
                    }
                }
            }
            Log.Debug("Finished pre-populating cache with {0} entries.", _resolvedRefCache.Count);
            // --- End Cache Pre-population ---

            JObject initialObj = rootToken as JObject;
            JArray initialArray = rootToken as JArray;

            if (initialObj != null && initialObj.Property("$values") != null &&
                initialObj.Property("$values").Value.Type == JTokenType.Array &&
                (initialObj.Count == 1 || (initialObj.Count == 2 && initialObj.Property("$id") != null)))
            {
                Log.Debug(string.Format("Detected root as object containing $values array for {0}.", rootElementName));
                JArray innerArray = (JArray)initialObj.Property("$values").Value;
                List<JToken> resolvedItems = new List<JToken>();

                foreach (JToken item in innerArray)
                {
                    JObject itemObj = item as JObject;
                    JProperty refProp = itemObj != null ? itemObj.Property("$ref") : null;

                    if (itemObj != null && refProp != null && itemObj.Count == 1)
                    {
                        string refValue = refProp.Value.ToString();
                        if (globalIdMap.ContainsKey(refValue))
                        {
                            Log.Trace(string.Format("Resolving top-level $ref '{0}'...", refValue));
                            resolvedItems.Add(globalIdMap[refValue].DeepClone());
                }
                else
                {
                            Log.Warn(string.Format("Could not resolve top-level $ref '{0}' for {1}. Ref not found in GLOBAL map. Skipping.", refValue, rootElementName));
                        }
                    }
                    else if (itemObj != null && itemObj.Property("$id") != null)
                    {
                        resolvedItems.Add(item);
                    }
                }
                Log.Debug(string.Format("Resolved {0} top-level items for {1}.", resolvedItems.Count, rootElementName));

                List<JToken> cleanedItems = new List<JToken>();
                foreach (JToken resItem in resolvedItems)
                {
                    // Start recursion tracking for each top-level item
                    HashSet<string> currentlyProcessingRefs = new HashSet<string>();
                    try
                    {
                        JToken cleanedItem = CleanAndTransformJsonToken(resItem, currentlyProcessingRefs);
                        Log.Trace("Cleaned top-level item type: {0}", cleanedItem != null ? cleanedItem.Type.ToString() : "null");
                        if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                        {
                            cleanedItems.Add(cleanedItem);
                        }
                 }
                 catch (Exception cleanEx)
                 {
                        string errorMsg = string.Format("Error cleaning resolved item for {0}. Item (truncated): {1}. Exception: {2}", rootElementName, (resItem.ToString(Newtonsoft.Json.Formatting.None).Length > 200 ? resItem.ToString(Newtonsoft.Json.Formatting.None).Substring(0,200) : resItem.ToString(Newtonsoft.Json.Formatting.None)), cleanEx.ToString());
                        Log.Error(errorMsg);
                    }
                }

                var filteredItems = cleanedItems.Where(delegate(JToken t) {
                    JObject jobj = t as JObject;
                    return (jobj == null || jobj.HasValues);
                }).ToList();
                string filterLogMsg = string.Format("Filtered {0} empty objects from {1} cleaned items for {2}", cleanedItems.Count - filteredItems.Count, cleanedItems.Count, rootElementName);
                Log.Debug(filterLogMsg);

                 finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredItems)));
            }
            else if (initialArray != null)
            {
                 Log.Debug(string.Format("Root token for {0} is a JArray. Cleaning array directly.", rootElementName));
                 List<JToken> cleanedItems = new List<JToken>();
                 foreach(JToken item in initialArray)
                 {
                     // Start recursion tracking for each root array item
                     HashSet<string> currentlyProcessingRefs = new HashSet<string>();
                     try {
                         JToken cleanedItem = CleanAndTransformJsonToken(item, currentlyProcessingRefs);
                         Log.Trace("Cleaned root array item type: {0}", cleanedItem != null ? cleanedItem.Type.ToString() : "null");
                         if (cleanedItem != null && cleanedItem.Type != JTokenType.Null) { cleanedItems.Add(cleanedItem); }
                     } catch (Exception cleanEx) {
                          string errorMsg = string.Format("Error cleaning root array item for {0}. Item (truncated): {1}. Exception: {2}", rootElementName, (item.ToString(Newtonsoft.Json.Formatting.None).Length > 200 ? item.ToString(Newtonsoft.Json.Formatting.None).Substring(0,200) : item.ToString(Newtonsoft.Json.Formatting.None)), cleanEx.ToString());
                           Log.Error(errorMsg);
                     }
                 }
                  var filteredItems = cleanedItems.Where(delegate(JToken t) {
                     JObject jobj = t as JObject;
                     return (jobj == null || jobj.HasValues);
                 }).ToList();
                  finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredItems)));
                }
                else
                {
                Log.Debug(string.Format("Root token for {0} is not {$id,$values} or JArray (Type: {1}). Cleaning token directly and wrapping.", rootElementName, rootToken.Type));
                JToken cleanedToken = null;
                // Start recursion tracking for the root token
                HashSet<string> currentlyProcessingRefs = new HashSet<string>();
                try {
                    cleanedToken = CleanAndTransformJsonToken(rootToken, currentlyProcessingRefs);
                } catch (Exception cleanEx) {
                     string errorMsg = string.Format("Error cleaning root token (Case 3) for {0}. Token (truncated): {1}. Exception: {2}", rootElementName, (rootToken.ToString(Newtonsoft.Json.Formatting.None).Length > 200 ? rootToken.ToString(Newtonsoft.Json.Formatting.None).Substring(0,200) : rootToken.ToString(Newtonsoft.Json.Formatting.None)), cleanEx.ToString());
                      Log.Error(errorMsg);
                      cleanedToken = new JObject();
                }

                if (cleanedToken is JArray)
                {
                     finalObjectForXml = new JObject(new JProperty(rootElementName, cleanedToken));
                }
                else
                {
                     JObject cleanedObj = cleanedToken as JObject;
                     if (cleanedObj == null || cleanedObj.HasValues)
                     {
                         finalObjectForXml = new JObject(new JProperty(rootElementName, cleanedToken ?? new JObject()));
                     }
                     else
                     {
                          Log.Debug("Cleaned root token resulted in an empty object. Creating empty root for XML.");
                          finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray()));
                     }
                 }

                 JObject potentiallyStillWrapped = cleanedToken as JObject;
                 if (potentiallyStillWrapped != null && potentiallyStillWrapped.Count == 1 && potentiallyStillWrapped.Property("$values") != null && potentiallyStillWrapped.Property("$values").Value is JArray)
                 {
                     Log.Warn(string.Format("Cleaned token (Case 3) for {0} still contained $values wrapper. Extracting inner array.", rootElementName));
                     finalObjectForXml = new JObject(new JProperty(rootElementName, potentiallyStillWrapped.Property("$values").Value));

                     JArray extractedArray = (JArray)potentiallyStillWrapped.Property("$values").Value;
                     var filteredExtracted = extractedArray.Where(delegate(JToken t) {
                         JObject jobj = t as JObject; return (jobj == null || jobj.HasValues);
                     }).ToList();
                     if (filteredExtracted.Count < extractedArray.Count) {
                          finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredExtracted)));
        }

                 }
            }

            string finalJsonForXml = "{}";
             try {
                 if (finalObjectForXml != null) {
                     finalJsonForXml = finalObjectForXml.ToString(Newtonsoft.Json.Formatting.None);
                 } else {
                     Log.Warn("finalObjectForXml was unexpectedly null for {0} before final XML conversion. Using default empty.", rootElementName);
                     finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray()));
                     finalJsonForXml = finalObjectForXml.ToString(Newtonsoft.Json.Formatting.None);
                 }

                 string truncatedFinalJson = (finalJsonForXml.Length > 2000) ? finalJsonForXml.Substring(0, 2000) + "..." : finalJsonForXml;
                 string logMsgFinal = string.Format("Final {0} JSON prepared for XML conversion (truncated): {1}", rootElementName, truncatedFinalJson);
                 Log.Debug(logMsgFinal);

                 XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(finalJsonForXml, "Root", false);
                 return XDocument.Parse(xmlDoc.OuterXml);
             }
             catch (Exception xmlEx)
             {
                 // Log the final JSON that caused the error
                 string failingJson = "[Error retrieving final JSON]";
                 try { failingJson = finalObjectForXml != null ? finalObjectForXml.ToString(Newtonsoft.Json.Formatting.Indented) : "[finalObjectForXml was null]"; }
                 catch (Exception jsonLogEx) { failingJson = "[Error converting final JObject to string: " + jsonLogEx.Message + "]"; }
                 Log.Error("--- Failing JSON structure for {0} ---:\n{1}", rootElementName, failingJson);
                 
                 string errorMsg = string.Format("Final XML conversion failed for {0}. JSON used (truncated): {1}. Exception: {2}", rootElementName, (finalJsonForXml.Length > 500 ? finalJsonForXml.Substring(0,500)+"..." : finalJsonForXml), xmlEx.ToString());
                 Log.Error(errorMsg);
                 throw new Exception(string.Format("Ошибка конвертации {0} JSON в XML.", rootElementName), xmlEx);
             }
        }

        private static JToken CleanAndTransformJsonToken(JToken token, HashSet<string> currentlyProcessingRefs)
        {
            if (token == null) return null;

            // --- Fully Cleaned Cache Check ---
            string initialIdForCache = null;
            JObject initialObjForCache = token as JObject;
            if (initialObjForCache != null)
            {
                JProperty initialIdPropForCache = initialObjForCache.Property("$id");
                // Check if $id property exists, its value isn't null, and the string representation is valid
                if (initialIdPropForCache != null && initialIdPropForCache.Value != null && initialIdPropForCache.Value.Type != JTokenType.Null)
                {
                    initialIdForCache = initialIdPropForCache.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(initialIdForCache) && _fullyCleanedCache.ContainsKey(initialIdForCache))
                    {
                        Log.Trace("Returning fully cleaned result from cache for $id = {0}", initialIdForCache);
                        return _fullyCleanedCache[initialIdForCache].DeepClone();
                    }
                    // Reset if it was invalid for cache key use, but existed
                    if (string.IsNullOrWhiteSpace(initialIdForCache)) { initialIdForCache = null; }
                }
            }
            // --- End Fully Cleaned Cache Check ---

            // We proceed with processing if not found in the fully cleaned cache.

            switch (token.Type)
            {
                case JTokenType.Object:
        {
                        // --- Recursion Detection ---
                        JObject objToken = token as JObject;
                        string originalId = null;
                        JProperty idProp = null;
                        if (objToken != null) { idProp = objToken.Property("$id"); }
                        if (idProp != null) {
                            originalId = idProp.Value.ToString();
                            if (!currentlyProcessingRefs.Add(originalId)) {
                                Log.Warn("Recursion detected for $id = {0}. Returning empty object to break loop.", originalId);
                                return new JObject(); // Break the loop
                            }
                        }
                        // --- End Recursion Detection ---
  
                 JObject obj = (JObject)token;
                        JProperty refProp = obj.Property("$ref");
 
                         // --- Modified $ref Handling --- 
                        if (refProp != null && obj.Count == 1) // If it's purely a $ref object
                 {                            string refValue = refProp.Value.ToString();
                             if (_resolvedRefCache.ContainsKey(refValue))
                             {
                                 JObject cachedObject = _resolvedRefCache[refValue];
                                 if (cachedObject == null) {
                                     Log.Warn("$ref '{0}' resolved to a null object in cache. Returning empty object.", refValue);
                                     if (originalId != null) currentlyProcessingRefs.Remove(originalId); // Clean up tracker
                                     return new JObject();
                                 }
                                 // --- Recursion Check for $ref target ---
                                 if (!currentlyProcessingRefs.Add(refValue)) { // Try adding $ref value to tracker
                                     Log.Warn("Recursion detected: trying to resolve $ref = {0} which is already being processed. Returning empty object.", refValue);
                                     if (originalId != null) currentlyProcessingRefs.Remove(originalId); // Clean up tracking for current level
                                     return new JObject(); // Break loop
                                 }
                                 // --- End Recursion Check ---
 
                                 Log.Trace("Resolving $ref '{0}' from cache and recursively cleaning...", refValue);
                                 JObject clonedResolvedObject = (JObject)cachedObject.DeepClone();
                                 JToken result = CleanAndTransformJsonToken(clonedResolvedObject, currentlyProcessingRefs); // Pass tracker down
                                 currentlyProcessingRefs.Remove(refValue);
                                 if (originalId != null) currentlyProcessingRefs.Remove(originalId); // Clean up tracker
                                 return result;
                             }
                         }
                         // --- End Modified $ref Handling ---

                         // If not a pure $ref object, proceed with cleaning properties
                         JObject cleanedObj = new JObject();
                         foreach (var property in obj.Properties().ToList()) // Keep ToList() for safe iteration
                         {
                             if (property.Name.Equals("$id", StringComparison.OrdinalIgnoreCase))
                             {
                                 continue;
                             }

                             // Recursively clean the property value
                             JToken cleanedValue = null;
                             string propertyValueTruncated = (property.Value.ToString(Newtonsoft.Json.Formatting.None).Length > 100) ? property.Value.ToString(Newtonsoft.Json.Formatting.None).Substring(0,100) + "..." : property.Value.ToString(Newtonsoft.Json.Formatting.None);
                             Log.Trace("Recursively cleaning property '{0}'. Value type: {1}, Value (truncated): {2}", property.Name, property.Value.Type, propertyValueTruncated);
                             try {
                                 cleanedValue = CleanAndTransformJsonToken(property.Value, currentlyProcessingRefs); // Pass tracker down
                             } catch (Exception exCleanInner) {
                                 Log.Error("Error cleaning inner property '{0}'. Exception: {1}", property.Name, exCleanInner.ToString());
                                 cleanedValue = JValue.CreateNull(); 
                             }

                             Log.Trace("Cleaned property '{0}' resulted in type: {1}", property.Name, cleanedValue != null ? cleanedValue.Type.ToString() : "null");
                             // Existing logic for handling $values wrappers within properties remains important
                             JObject valueObj = cleanedValue as JObject;
                             if (property.Name.Equals("$values", StringComparison.OrdinalIgnoreCase) && cleanedValue is JArray) 
                             {
                                 // If the property *was* $values and its cleaned value is an array, add it as "Items"
                                 Log.Debug("Renaming cleaned '$values' property to 'Items'");
                                 cleanedObj.Add("Items", cleanedValue);
                                 continue; // Skip default property addition below
                             }
                             else if (valueObj != null && valueObj.Count == 1 && valueObj.Property("Items") != null && valueObj.Property("Items").Value.Type == JTokenType.Array) // Check if it contains ONLY the renamed "Items"
                             {
                                 // This handles cases where an object *contains* a $values (now Items) property.
                                 // We want to extract the array and add it directly using the original property's name.
                                 Log.Debug("Extracting 'Items' array from cleaned property '{0}' wrapper object", property.Name);
                                 // IMPORTANT: Use the *already cleaned* inner array directly.
                                 // The recursive call above should have handled cleaning items inside $values.
                                 cleanedValue = valueObj.Property("Items").Value; 
                             }

                             // Add the cleaned property if it's not null or an empty object (unless it's an empty array)
                             if (cleanedValue != null && cleanedValue.Type != JTokenType.Null)
                             {
                                 JObject cleanedValueAsObject = cleanedValue as JObject;
                                 JArray cleanedValueAsArray = cleanedValue as JArray;

                                // Add if it's not an object OR if it's an object with properties OR if it's an array (even empty)
                                 if (cleanedValueAsObject == null || cleanedValueAsObject.HasValues || cleanedValueAsArray != null)
                                 {
                                     string sanitizedName = SanitizeXmlName(property.Name);
                                     cleanedObj.Add(sanitizedName, cleanedValue);
                                 }
                                 else {
                                      Log.Trace("Skipping property '{0}' because its cleaned value is an empty object.", property.Name);
                                 }
                            }
                 }
                 if (originalId != null) {
                      currentlyProcessingRefs.Remove(originalId);
                      // --- Store in Fully Cleaned Cache ---
                     if (!string.IsNullOrWhiteSpace(originalId) && !_fullyCleanedCache.ContainsKey(originalId))
                     {
                         Log.Trace("Storing fully cleaned result in cache for $id = {0}", originalId);
                         _fullyCleanedCache.Add(originalId, cleanedObj.DeepClone());
                     }
                     // --- End Store ---                     
                 }
                 return cleanedObj;
             }

                case JTokenType.Array:
             {
                 JArray array = (JArray)token;
                        JArray cleanedArray = new JArray();
                        foreach (var item in array.ToList())
                 {
                            JToken cleanedItem = null;
                            string itemValueTruncated = (item.ToString(Newtonsoft.Json.Formatting.None).Length > 100) ? item.ToString(Newtonsoft.Json.Formatting.None).Substring(0, 100) + "..." : item.ToString(Newtonsoft.Json.Formatting.None);
                            Log.Trace("Recursively cleaning array item. Type: {0}, Value (truncated): {1}", item.Type, itemValueTruncated);
                            try {
                                 cleanedItem = CleanAndTransformJsonToken(item, currentlyProcessingRefs); // Pass tracker down
                            } catch (Exception exCleanItem) {
                                 Log.Error("Error cleaning array item. Item (truncated): {0}. Exception: {1}", itemValueTruncated, exCleanItem.ToString());
                                 continue; 
                            }
                            if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                            {
                                JObject cleanedItemAsObject = cleanedItem as JObject;
                                if (cleanedItemAsObject == null || cleanedItemAsObject.HasValues || cleanedItem is JArray)
                                {
                                     cleanedArray.Add(cleanedItem);
                                }
                            }
                 }
                 return cleanedArray;
             }

                default:
                 Log.Trace("Returning primitive token of type {0}", token.Type);
                 return token;
             }
        }

        // --- End JSON Processing Helper Methods ---

        // Modified ApplyFiltersAndBindData to use _allSalesData
        private void ApplyFiltersAndBindData()
        {
             string logMsgStart = "Applying client-side filters...";
             Log.Info(logMsgStart);
             try
             {
                 SetLoadingState(true); // Use SetLoadingState
                gridControlReport.DataSource = null; // Clear previous filtered data

                 // Get filter criteria (synchronous read from UI controls)
                 DateTime? fromDate = null, toDate = null;
                 int? routeId = null;

                Action getFiltersAction = delegate() {
                     if (dateFromFilter.EditValue != null && dateFromFilter.EditValue != DBNull.Value)
                         fromDate = dateFromFilter.DateTime.Date;
                     if (dateToFilter.EditValue != null && dateToFilter.EditValue != DBNull.Value)
                         toDate = dateToFilter.DateTime.Date.AddDays(1).AddTicks(-1); // Include whole end date

                     if (lueRouteFilter.EditValue != null && lueRouteFilter.EditValue != DBNull.Value) {
                         int selectedId = Convert.ToInt32(lueRouteFilter.EditValue);
                         if (selectedId > 0) {
                             routeId = selectedId;
                         }
                     }
                 };
                 if (this.InvokeRequired) { this.Invoke(getFiltersAction); } else { getFiltersAction(); }


                 // Filter the local _allSalesData list using delegates for C# 4.0
                 IEnumerable<IncomeReport_SaleViewModel> filteredData = _allSalesData; // Start with all data

                 if (fromDate.HasValue) {
                     filteredData = filteredData.Where(delegate(IncomeReport_SaleViewModel s) { return s.SaleDate >= fromDate.Value; });
                     Log.Debug("Applying date filter: >= {0}", fromDate.Value.ToString("yyyy-MM-dd"));
                 }
                 if (toDate.HasValue) {
                     filteredData = filteredData.Where(delegate(IncomeReport_SaleViewModel s) { return s.SaleDate <= toDate.Value; });
                      Log.Debug("Applying date filter: <= {0}", toDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                 }
                 if (routeId.HasValue) {
                     // Filter using the parsed RouteId
                     filteredData = filteredData.Where(delegate(IncomeReport_SaleViewModel s) { return s.RouteId == routeId.Value; });
                      Log.Debug("Applying filter for Route ID: {0}", routeId.Value);
                }

                 // Convert the filtered IEnumerable back to a List for binding and calculations
                 List<IncomeReport_SaleViewModel> finalFilteredList = filteredData.ToList();

                // Set filtered data source
                 gridControlReport.DataSource = finalFilteredList;
                 string logMsgCount = string.Format("Client-side filtering applied. Displaying {0} items.", finalFilteredList.Count);
                 Log.Info(logMsgCount);

                 // --- Recalculate Summaries (using delegates) ---
                _monthlyIncomes.Clear();
                _routeIncomes.Clear();
                _totalIncome = 0;
                _totalTicketsSold = 0;
                _averageTicketPrice = 0;

                 if (finalFilteredList.Any())
                {
                     // Monthly Data
                     var monthlyData = finalFilteredList
                         .GroupBy(delegate(IncomeReport_SaleViewModel s) { return new { Year = s.SaleDate.Year, Month = s.SaleDate.Month }; })
                        .Select(delegate(IGrouping<dynamic, IncomeReport_SaleViewModel> g) { 
                              decimal groupSum = g.Sum(delegate(IncomeReport_SaleViewModel s) { return (decimal)s.TotalAmount; });
                              int groupCount = g.Count();
                             return new MonthlyIncome {
                                 Year = g.Key.Year,
                                  Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM", CultureInfo.CurrentCulture),
                                  TotalIncome = groupSum,
                                  TicketsSold = groupCount,
                                  AverageTicketPrice = groupCount > 0 ? groupSum / groupCount : 0
                            }; 
                         })
                        .OrderBy(delegate(MonthlyIncome m) { return m.Year; })
                         .ThenBy(delegate(MonthlyIncome m) { return DateTime.ParseExact(m.Month, "MMMM", CultureInfo.CurrentCulture).Month; })
                        .ToList();
                    _monthlyIncomes = new ObservableCollection<MonthlyIncome>(monthlyData);

                     // Route Data
                     var routeData = finalFilteredList
                         .GroupBy(delegate(IncomeReport_SaleViewModel s) { return s.RouteDescription ?? "[Неизвестный маршрут]"; }) // Handle potential null description
                        .Select(delegate(IGrouping<string, IncomeReport_SaleViewModel> g) { 
                             decimal groupSum = g.Sum(delegate(IncomeReport_SaleViewModel s) { return (decimal)s.TotalAmount; });
                             int groupCount = g.Count();
                            return new RouteIncome
                            {
                                RouteName = g.Key,
                                 TotalIncome = groupSum,
                                 TicketsSold = groupCount,
                                 AverageTicketPrice = groupCount > 0 ? groupSum / groupCount : 0
                            };
                         })
                        .OrderByDescending(delegate(RouteIncome r) { return r.TotalIncome; })
                        .ToList();
                    _routeIncomes = new ObservableCollection<RouteIncome>(routeData);
                        
                     // Overall Totals
                     _totalIncome = finalFilteredList.Sum(delegate(IncomeReport_SaleViewModel s) { return (decimal)s.TotalAmount; });
                     _totalTicketsSold = finalFilteredList.Count;
                     _averageTicketPrice = _totalTicketsSold > 0 ? _totalIncome / _totalTicketsSold : 0;
                }

                 // --- End Recalculate Summaries ---

                  gridViewReport.UpdateSummary(); // Refresh grid footer summaries
                  UpdateButtonStates(); // Update button enable/disable states
             }
             catch (Exception ex)
             {
                 string errorMsg = string.Format("Error applying client-side filters. Exception: {0}", ex.ToString());
                 Log.Error(errorMsg);
                 XtraMessageBox.Show(string.Format("Произошла ошибка при применении фильтров: {0}", ex.Message), "Ошибка Фильтрации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
             }
             finally
             {
                  SetLoadingState(false); // Reset loading state
             }
        }

        // Keep CustomColumnDisplayText as is
        private void gridViewReport_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == "TotalAmount" && e.Value is double)
            {
                double amount = (double)e.Value;
                e.DisplayText = string.Format("{0:C}", amount);
            }
            else if (e.Value is DateTime)
            {
                 DateTime dt = (DateTime)e.Value;
                 if (e.Column.FieldName == "SaleDate")
                 {
                      e.DisplayText = dt.ToString("dd.MM.yyyy HH:mm");
                 }
                 else if (e.Column.FieldName == "DepartureTime" || e.Column.FieldName == "ArrivalTime")
                 {
                      e.DisplayText = dt.ToString("dd.MM HH:mm");
                 }
            }
        }

        // Modified Apply Filter button click handler
        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
             string logMsg = "Apply Filter button clicked for income report.";
             Log.Info(logMsg);
             // Directly apply filters to the existing _allSalesData
            ApplyFiltersAndBindData();
        }

        // Keep Export button click handler as is
        private void btnExport_Click(object sender, EventArgs e)
        {
            string logMsg = "Export button clicked for income report.";
            Log.Info(logMsg);
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
                    FileName = string.Format("IncomeReport_{0:yyyyMMdd_HHmmss}", DateTime.Now)
                };

                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    Cursor = Cursors.WaitCursor;
                    string fileExt = System.IO.Path.GetExtension(saveDialog.FileName).ToLowerInvariant();
                    
                    // Use WYSIWYG export options
                    var options = new XlsxExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG };
                    
                    if (fileExt == ".xlsx")
                    {
                        gridControlReport.ExportToXlsx(saveDialog.FileName, options);
                    }
                    else if (fileExt == ".csv")
                    {
                        var csvOptions = new CsvExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG };
                        gridControlReport.ExportToCsv(saveDialog.FileName, csvOptions); 
                    }
                    
                    Log.Info(string.Format("Income report exported successfully to {0}", saveDialog.FileName));
                    XtraMessageBox.Show("Отчет успешно экспортирован.", "Экспорт завершен",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Optional: Open file logic
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
                            string errorMsg = string.Format("Failed to open exported file {FileName}. Exception: {0}", saveDialog.FileName, exOpen.ToString());
                            Log.Error(errorMsg);
                            XtraMessageBox.Show(string.Format("Не удалось открыть файл: {0}", exOpen.Message), "Ошибка открытия файла",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Error exporting income report. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                XtraMessageBox.Show(string.Format("Ошибка при экспорте отчета: {0}", ex.Message), "Ошибка экспорта",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}