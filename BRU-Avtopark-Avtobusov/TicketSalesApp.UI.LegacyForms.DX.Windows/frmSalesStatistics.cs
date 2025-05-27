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
using System.Net.Http;
using System.Web;
using DevExpress.XtraCharts;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using TicketSalesApp.Core.Models;
using System.Xml;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    // ViewModel for route statistics
    public class RouteStatistic
    {
        public string RouteName { get; set; } // = string.Empty; // Initialize in constructor
        public int TotalSales { get; set; }
        public decimal TotalRevenue { get; set; }
        public double SalesPercentage { get; set; }

        public RouteStatistic()
        {
            RouteName = string.Empty; // Initialize here
        }
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
        public List<SalesByRouteStatistic> SalesByRoute { get; set; } 
        public double AverageGrowthRate { get; set; }
        
        public SalesStatisticsViewModel()
        {
            SalesByRoute = new List<SalesByRouteStatistic>(); // Initialize here
        }
    }

    public class SalesByRouteStatistic
    {
        public string RouteName { get; set; } 
        public int SalesCount { get; set; }
        public double TotalIncome { get; set; }
        
        public SalesByRouteStatistic()
        {
             RouteName = string.Empty; // Initialize here
        }
    }

    // --- Renamed RouteLookupViewModel for Sales Statistics Scope ---
    public class SalesStats_RouteLookupViewModel
    {
        public int RouteId { get; set; }
        public string DisplayName { get; set; }

        public SalesStats_RouteLookupViewModel()
        {
            DisplayName = string.Empty;
        }
    }
    // --- End Renamed RouteLookupViewModel ---

    public partial class frmSalesStatistics : DevExpress.XtraEditors.XtraForm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger(); // NLog instance
        private static Dictionary<string, JObject> _resolvedRefCache = new Dictionary<string, JObject>(); // Added static cache for cleaned refs
        private static Dictionary<string, JToken> _fullyCleanedCache = new Dictionary<string, JToken>(); // Cache for fully processed results by $id
        private static Dictionary<string, JToken> _fullyCleanedCachebackupfortypeone = new Dictionary<string, JToken>(); //Backup the  Cache for fully processed results by $id - will be sales backup
        private static Dictionary<string, JToken> _fullyCleanedCachebackupfortypetwo = new Dictionary<string, JToken>(); //Backup the  Cache for fully processed results by $id - will be routes backup
        private static string _lastProcessedRootElementName = null; // Track the root element name for cache invalidation
        private readonly ApiClientService _apiClient; // Changed name
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly string _baseUrl; 
        private ObservableCollection<RouteStatistic> _routeStatistics;
        private List<SalesStats_RouteLookupViewModel> _availableRoutes = new List<SalesStats_RouteLookupViewModel>(); // UPDATED TYPE
        private bool _isBusy; // Loading state flag
        private string _errorMessage;
        private bool _hasError;
        private bool _formLoadComplete = false; // Added

        public frmSalesStatistics()
        {
            InitializeComponent();

            // Get instance directly
            _apiClient = ApiClientService.Instance; // Use Instance
            _jsonSettings = new JsonSerializerSettings 
            { 
                 PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                 ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Added for safety
                 DateFormatHandling = DateFormatHandling.IsoDateFormat,
                 DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
             _baseUrl = "http://localhost:5000/api"; // Use /api suffix based on others
             _routeStatistics = new ObservableCollection<RouteStatistic>();
             _errorMessage = string.Empty;

            // Set default dates (current month and previous month)
            dateFromFilter.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            dateToFilter.DateTime = DateTime.Now.Date;

            // Initialize Chart
            ConfigureChart();

            // Bind events
            this.Load += frmSalesStatistics_Load;
            // Handle auth token changes - use delegate
            _apiClient.OnAuthTokenChanged += HandleAuthTokenChanged;
            this.FormClosing += FrmSalesStatistics_FormClosing; // Added

            UpdateButtonStates(); // Initial state
        }

        // Added FormClosing event handler
        private void FrmSalesStatistics_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Debug("Form closing.");
            _apiClient.OnAuthTokenChanged -= HandleAuthTokenChanged; // Unsubscribe
        }

        // Added Token Changed Handler
        private void HandleAuthTokenChanged(object sender, string token)
        {
            string logMsg = "Auth token changed, triggering synchronous data reload.";
            Log.Debug(logMsg);
            // Use BeginInvoke for safe UI thread execution
            if (this.Visible && this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(delegate {
                    try
                    {
                        LoadDataSynchronously(); // Reload data
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

        // Added Load event handler
        private void frmSalesStatistics_Load(object sender, EventArgs e)
        {
            if (_formLoadComplete)
            {
                Log.Warn("frmSalesStatistics_Load fired again after initial load completed. Ignoring.");
                return;
            }
            bool initialLoadSuccess = false;
            try
            {
                Log.Debug("frmSalesStatistics_Load event triggered (initial run).");
                // Load routes first synchronously
                if (LoadRoutesSynchronously()) // ADDED: Load routes first
                {
                    LoadDataSynchronously(); // Load data on initial load
                    initialLoadSuccess = true;
                }
                else
                {
                     Log.Error("Failed to load initial routes for statistics form.");
                     XtraMessageBox.Show("Не удалось загрузить список маршрутов. Статистика может отображаться некорректно.", "Ошибка Инициализации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     // Attempt to load stats anyway, might show unknowns
                     LoadDataSynchronously();
                }

                 if (initialLoadSuccess) {
                    _formLoadComplete = true;
                    Log.Info("Initial form load sequence completed successfully.");
                }

            }
            catch (Exception ex)
            {
                _formLoadComplete = true; // Mark as complete even on error
                string errorMsg = string.Format("Critical error during form load sequence. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                 XtraMessageBox.Show(string.Format("Произошла критическая ошибка при загрузке формы: {0}", ex.Message), "Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 // Optionally close form on critical load error
                 // this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) this.Close(); }));
            }
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
            series.ArgumentDataMember = "RouteName"; 
            // ValueDataMembers should match the property for the value (TotalSales)
            series.ValueDataMembers.AddRange(new string[] { "TotalSales" }); 

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
                 Log.Warn("SetLoadingState called but form or controls are disposed.");
                 return;
            }

            Log.Debug(isLoading ? "Setting UI to loading state." : "Setting UI to normal state.");
            _isBusy = isLoading;
            Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;

            // Enable/Disable controls
            if (dateFromFilter != null && !dateFromFilter.IsDisposed) dateFromFilter.Enabled = !isLoading;
            if (dateToFilter != null && !dateToFilter.IsDisposed) dateToFilter.Enabled = !isLoading;
            if (btnApplyFilter != null && !btnApplyFilter.IsDisposed) btnApplyFilter.Enabled = !isLoading;
            if (chartControlSalesByRoute != null && !chartControlSalesByRoute.IsDisposed) chartControlSalesByRoute.Enabled = !isLoading;
            // Labels are usually kept enabled but cleared/updated

            if (!isLoading) {
                UpdateButtonStates();
            }
        }

        // --- New UpdateButtonStates Method ---
        private void UpdateButtonStates()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) UpdateButtonStates(); }));
                return;
            }
            if (this.IsDisposed) return;

            bool isLoading = _isBusy;

            // Enable filters and apply button when not loading
            if (dateFromFilter != null && !dateFromFilter.IsDisposed) dateFromFilter.Enabled = !isLoading;
            if (dateToFilter != null && !dateToFilter.IsDisposed) dateToFilter.Enabled = !isLoading;
            if (btnApplyFilter != null && !btnApplyFilter.IsDisposed) btnApplyFilter.Enabled = !isLoading;
            // Add more specific logic if needed (e.g., based on data loaded)
        }

        // --- Modified LoadStatisticsAsync to LoadDataSynchronously ---
        private void LoadDataSynchronously()
        {
            string logStart = "Loading sales statistics synchronously...";
            Log.Info(logStart);
            SetLoadingState(true);

            HttpClient client = null;
            string salesJsonRaw = null; // Changed from statsJsonRaw
            XDocument salesXml = null; // Changed from statsXml

            int totalSalesCount = 0;
            double totalIncome = 0;
            List<SalesByRouteStatistic> loadedSalesByRoute = new List<SalesByRouteStatistic>(); // Temp list for aggregation

            try
            {
                _hasError = false;
                _errorMessage = string.Empty;
                
                // Get filter dates from UI thread
                DateTime fromDate = DateTime.MinValue, toDate = DateTime.MinValue;
                Action getDatesAction = delegate() {
                    fromDate = dateFromFilter.DateTime.Date;
                    toDate = dateToFilter.DateTime.Date.AddDays(1).AddTicks(-1); // Include whole end day
                };
                if (this.InvokeRequired) { this.Invoke(getDatesAction); } else { getDatesAction(); }


                var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
                // Use the date range filters for the search endpoint
                queryBuilder["startDate"] = fromDate.ToString("o"); // ISO 8601 format
                queryBuilder["endDate"] = toDate.ToString("o");

                client = _apiClient.CreateClient();
                // CORRECTED ENDPOINT: Use TicketSales/search
                var apiUrl = string.Format("{0}/TicketSales/search?{1}", _baseUrl, queryBuilder.ToString());
                Log.Debug("Fetching sales data for statistics from: {0}", apiUrl);

                HttpResponseMessage response = client.GetAsync(apiUrl).Result; // Synchronous call
               
                string salesJsonRawdebug = response.Content.ReadAsStringAsync().Result;
                Log.Debug("Raw JSON response: {0}", salesJsonRawdebug); // DEBUG STRING DISPLAY - NOT USED IN PARSING
                

                if (response.IsSuccessStatusCode)
                {
                    byte[] salesBytes = response.Content.ReadAsByteArrayAsync().Result; 
                    salesJsonRaw = Encoding.UTF8.GetString(salesBytes);

                    if (!string.IsNullOrEmpty(salesJsonRaw))
                    {
                        // Process JSON to XML using the robust method
                        salesXml = ProcessJsonToXml(salesJsonRaw, "Sales"); // Root element is Sales

                        // --- Parse Sales XML and Aggregate --- 
                        Log.Debug("Parsing Sales XML and aggregating statistics...");
                        List<Prodazha> parsedSales = new List<Prodazha>(); // Temp list to hold parsed sales before aggregation

                        foreach (XElement saleNode in salesXml.Root.Elements("Sales"))
                        { // *** VALIDATION: Check if the node actually represents a Sale ***
                            XElement saleIdEl = saleNode.Element("saleId");
                            XElement saleDateEl = saleNode.Element("saleDate"); // Check for either ID or Date as indicator
                            if (saleIdEl == null && saleDateEl == null)
                            {
                                Log.Debug("Skipping XML node in <Sales> as it doesn't appear to be a Sale record (missing saleId/saleDate). Node: {0}", saleNode.ToString().Substring(0, Math.Min(saleNode.ToString().Length, 150)) + "...");
                                continue; // Skip this node
                            }
                            // *** END VALIDATION ***

                            try
                            {
                                if (!saleNode.HasElements) continue;

                                // Parse relevant fields for aggregation
                                double price = 0;
                                long routeId = 0; // Added to parse route ID
                                string routeName = "[Неизвестный маршрут]"; // Default

                                // --- Robust Bilet Node Finding --- 
                                // Try to find the <bilet> element directly
                                XElement biletNode = saleNode.Element("bilet");
                                XElement biletNodeCapital = saleNode.Element("Bilet"); // Check alternative casing
                                XElement sourceNodeForBiletData = null; // Node containing ticketPrice, marshut etc.

                                if (biletNode != null && biletNode.HasElements)
                                {
                                    // Bilet data is within the <bilet> tag as expected
                                    sourceNodeForBiletData = biletNode;
                                    Log.Debug("Found Bilet data within <bilet> tag for Sale node.");
                                }
                                else if (biletNodeCapital != null && biletNodeCapital.HasElements)
                                {
                                    // Bilet data is within the <Bilet> tag (alternative case)
                                    sourceNodeForBiletData = biletNodeCapital;
                                    Log.Debug("Found Bilet data within <Bilet> tag (capital B) for Sale node.");
                                }
                                else
                                {
                                    // Bilet data might be directly under the <Sale> tag due to $ref resolution
                                    // Check for presence of key bilet properties directly under saleNode
                                    if (saleNode.Element("ticketPrice") != null && saleNode.Element("marshut") != null)
                                    {
                                        sourceNodeForBiletData = saleNode;
                                        Log.Debug("Found Bilet data directly under <Sale> node (likely resolved $ref).");
                                    }
                                    else
                                    {
                                        Log.Warn("Could not locate Bilet data either in <bilet> tag or directly under <Sale> node. Cannot determine price or route for this item. XML: {0}", saleNode.ToString());
                                        // Skip this sale item if Bilet data is essential and missing
                                        // REMOVED: continue; -- Now we process even if bilet info is missing
                                    }
                                }
                                // --- End Robust Bilet Node Finding ---

                                // Now parse data using sourceNodeForBiletData if it was found
                                if (sourceNodeForBiletData != null)
                                {
                                    XElement priceEl = sourceNodeForBiletData.Element("ticketPrice");
                                    if (priceEl != null) double.TryParse(priceEl.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out price);

                                    XElement marshutNode = sourceNodeForBiletData.Element("marshut");
                                    if (marshutNode != null)
                                    {
                                        // Parse RouteId first
                                        XElement routeIdEl = marshutNode.Element("routeId");
                                        if (routeIdEl != null) long.TryParse(routeIdEl.Value, out routeId);

                                        // Attempt to parse start/end points
                                        XElement startPointEl = marshutNode.Element("startPoint");
                                        string startPoint = (startPointEl != null) ? startPointEl.Value : null;

                                        XElement endPointEl = marshutNode.Element("endPoint");
                                        string endPoint = (endPointEl != null) ? endPointEl.Value : null;

                                        // Construct route name if points are valid
                                        if (!string.IsNullOrEmpty(startPoint) && !string.IsNullOrEmpty(endPoint))
                                        {
                                            routeName = string.Format("{0} -> {1}", startPoint, endPoint);
                                        }
                                        else if (routeId > 0 && _availableRoutes != null) // If name invalid, try lookup via ID
                                        {
                                            Log.Debug("Route name missing/incomplete in sale XML (RouteId: {0}). Attempting lookup.", routeId);
                                            var foundRoute = _availableRoutes.FirstOrDefault(delegate(SalesStats_RouteLookupViewModel r) { return r.RouteId == routeId; }); // UPDATED TYPE in delegate
                                            if (foundRoute != null)
                                            {
                                                routeName = foundRoute.DisplayName;
                                                Log.Debug("Found route name via lookup: '{0}'", routeName);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Log.Warn("Marshut node missing within Bilet data source. Cannot determine route name or ID. Source XML: {0}", sourceNodeForBiletData.ToString());
                                    }
                                }
                                // If sourceNodeForBiletData was null, the warning was logged above,
                                // and we proceed with default price=0, routeId=0, routeName="[Неизвестный маршрут]"

                                // Add minimal info needed for aggregation
                                // Use the determined routeName (either parsed or looked up or default)
                                parsedSales.Add(new Prodazha
                                {
                                    Bilet = new Bilet
                                    {
                                        TicketPrice = (decimal)price, // Store price
                                        Marshut = new Marshut
                                        {
                                            RouteId = routeId, // Store RouteId
                                            StartPoint = routeName // Use StartPoint temporarily for the final route name
                                        }
                                    }
                                });
                            }
                            catch (Exception exNode)
                            {
                                // CORRECTED: Ensure proper string formatting
                                string errorMsgNode = string.Format("Error parsing individual Sale XML node during statistics aggregation: {0}. Node: {1}", exNode.ToString(), (saleNode != null ? saleNode.ToString() : "[null node]"));
                                Log.Error(errorMsgNode);
                            }

                            Log.Debug("Parsed {0} sales records for aggregation.", parsedSales.Count);

                            // --- Aggregate Data Client-Side --- 
                            if (parsedSales.Any())
                            {
                                totalSalesCount = parsedSales.Count;
                                totalIncome = (double)parsedSales.Sum(delegate(Prodazha s) { return s.Bilet.TicketPrice; }); // Sum prices

                                // Group by RouteName (stored temporarily in StartPoint)
                                loadedSalesByRoute = parsedSales
                                    .GroupBy(delegate(Prodazha s) { return s.Bilet.Marshut.StartPoint; }) // Group by the constructed route name
                                    .Select(delegate(IGrouping<string, Prodazha> g)
                                    {
                                        int groupSalesCount = g.Count();
                                        double groupTotalIncome = (double)g.Sum(delegate(Prodazha s) { return s.Bilet.TicketPrice; });
                                        return new SalesByRouteStatistic
                                        {
                                            RouteName = g.Key,
                                            SalesCount = groupSalesCount,
                                            TotalIncome = groupTotalIncome
                                        };
                                    }).ToList();
                                Log.Info("Successfully aggregated sales statistics client-side.");
                            }
                            else
                            {
                                Log.Info("No sales data parsed from XML to aggregate.");
                                // Keep totals as 0
                            }
                        }
                            
                        // --- End Aggregation ---
                }
                else
                {
                        Log.Warn("Sales API returned success but content was empty.");
                    _hasError = true;
                         _errorMessage = "Сервер вернул пустой ответ.";
                    }
                }
                else
                {
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    // CORRECTED: Use Environment.NewLine for multi-line string
                    string errorMsg = string.Format("Failed to load sales data for statistics (used /TicketSales/search). Status: {0}, Content: {1}", response.StatusCode, errorContent);
                    Log.Error(errorMsg);
                    _hasError = true;
                    _errorMessage = string.Format("Ошибка загрузки данных: {0}{1}{2}", response.ReasonPhrase, Environment.NewLine, errorContent);
                }
            }
            catch (HttpRequestException httpEx)
            {
                 string errorMsg = string.Format("Network error loading sales statistics. Exception: {0}", httpEx.ToString());
                 Log.Error(errorMsg);
                _hasError = true;
                _errorMessage = string.Format("Сетевая ошибка при загрузке статистики: {0}", httpEx.Message);
            }
            catch (JsonException jsonEx)
            {
                 string errorMsg = string.Format("Error deserializing sales statistics JSON. Exception: {0}", jsonEx.ToString());
                 Log.Error(errorMsg);
                _hasError = true;
                _errorMessage = string.Format("Ошибка обработки данных статистики: {0}", jsonEx.Message);
            }
            catch (Exception ex)
            {
                 string errorMsg = string.Format("Generic error loading sales statistics. Exception: {0}", ex.ToString());
                 Log.Error(errorMsg);
                _hasError = true;
                _errorMessage = string.Format("Произошла ошибка при загрузке статистики: {0}", ex.Message);
            }
            finally
            {
                if (client != null) client.Dispose();

                // Update UI on the UI thread
                Action updateUiAction = delegate()
                {
                     if (this.IsDisposed) return;

                     if (_hasError)
                     {
                         // Show error message and clear UI
                         XtraMessageBox.Show(_errorMessage, "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                         lblTotalSalesValue.Text = "-";
                         lblTotalIncomeValue.Text = "-";
                         _routeStatistics.Clear(); // Clear existing chart data
                         chartControlSalesByRoute.DataSource = null; // Unbind chart
                         chartControlSalesByRoute.RefreshData();
                     }
                     else
                     {
                         // Update UI elements with loaded data
                         lblTotalSalesValue.Text = totalSalesCount.ToString("N0"); // Format number
                         lblTotalIncomeValue.Text = totalIncome.ToString("C", CultureInfo.CurrentCulture); // Format currency

                         // Update the chart data source
                         _routeStatistics.Clear(); // Clear previous data
                         // Use delegates for LINQ methods for C# 4.0
                         // Map aggregated SalesByRouteStatistic to RouteStatistic for the chart
                         var chartStats = loadedSalesByRoute
                             .Select(delegate(SalesByRouteStatistic sbr) { 
                                 return new RouteStatistic { 
                                     RouteName = sbr.RouteName,
                                     TotalSales = sbr.SalesCount,
                                     TotalRevenue = (decimal)sbr.TotalIncome, // Cast back if needed
                                     SalesPercentage = totalSalesCount > 0 ? ((double)sbr.SalesCount / totalSalesCount * 100) : 0
                                 };
                              })
                             .OrderByDescending(delegate(RouteStatistic r) { return r.TotalSales; })
                             .ToList();

                         foreach (var stat in chartStats) {
                             _routeStatistics.Add(stat);
                         }

                         // Bind data to chart (only top 5 for Pie chart clarity)
                         if (_routeStatistics.Any()) // Use Any() check
                         {
                             // Take only top 5 for Pie chart clarity
                             chartControlSalesByRoute.DataSource = _routeStatistics.Take(5).ToList();
                             Log.Debug("Binding top 5 route statistics to chart.");
                         }
                         else
                         {
                             Log.Info("No 'Sales by Route' data to display in chart.");
                             chartControlSalesByRoute.DataSource = null;
                         }
                         chartControlSalesByRoute.RefreshData(); // Refresh chart display
                     }
                     SetLoadingState(false); // Reset loading state
                 };

                 if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(updateUiAction); }
                 else if (!this.IsDisposed) { updateUiAction(); }
            }
        }

        // --- Start JSON Processing Helper Methods (C# 4.0 Compatible - Copied from frmIncomeReport) ---

        /// <summary>
        /// Builds a map of $id to JObject for resolving $ref during JSON cleaning.
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
            // Conditional Cache Management based on root element type
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
             
             JObject finalObjectForXml = null; // <<< ADDED DECLARATION BACK
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
                          // Add null to indicate failure? Or just skip?
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
                     else
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
                  // Update tracker even on error, as caches were potentially used/modified
                  _lastProcessedRootElementName = rootElementName;

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
                 {
                              string refValue = refProp.Value.ToString();
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
  
                                  Log.Trace("Resolving $ref '{0}' from cache and recursively cleaning...", refValue);
                                  JObject clonedResolvedObject = (JObject)cachedObject.DeepClone();
                                  JToken result = CleanAndTransformJsonToken(clonedResolvedObject, currentlyProcessingRefs); // Pass tracker down
                                  
                                  // *** IMPORTANT: Remove the refValue from tracker AFTER the recursive call completes ***
                                  currentlyProcessingRefs.Remove(refValue);
  
                                  if (originalId != null) currentlyProcessingRefs.Remove(originalId); // Clean up tracker for the object containing the ref
                                  return result;
                    }
                    else
                    {
                                  // This case indicates an issue - a $ref exists but wasn't in the pre-resolved cache.
                                  Log.Warn("$ref '{0}' found but not present in the pre-resolved cache. Returning empty object.", refValue);
                                  if (originalId != null) currentlyProcessingRefs.Remove(originalId); // Clean up tracking
                                  return new JObject(); // Avoid infinite loops / errors
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
                 // --- Remove from tracker after processing ---
                 if (originalId != null) { currentlyProcessingRefs.Remove(originalId); }
                 // --- Store in Fully Cleaned Cache ---
                 if (!string.IsNullOrWhiteSpace(originalId) && !_fullyCleanedCache.ContainsKey(originalId))
                 {
                     Log.Trace("Storing fully cleaned result in cache for $id = {0}", originalId);
                     _fullyCleanedCache.Add(originalId, cleanedObj.DeepClone());
                 }
                 // --- End Store ---
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
                                if (cleanedItemAsObject == null || cleanedItemAsObject.HasValues)
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

        // Modified Apply Filter button click
        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
             string logMsg = "Apply Filter button clicked for statistics.";
             Log.Info(logMsg);
             // Basic date validation (optional but good practice)
            if (dateFromFilter.DateTime > dateToFilter.DateTime)
            {
                XtraMessageBox.Show("Дата 'С' не может быть позже даты 'По'.", "Неверный диапазон дат",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ADDED: Reload routes first, in case new routes appeared or needed for lookup
            bool routesLoaded = LoadRoutesSynchronously();
            if (!routesLoaded)
            {
                Log.Warn("Failed to reload routes on filter apply. Statistics might use cached/incomplete route names.");
                // Optionally show a non-critical warning
                XtraMessageBox.Show("Не удалось обновить список маршрутов. Статистика может отображаться с устаревшими названиями.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

             // Trigger synchronous data load with new date range
             LoadDataSynchronously();
        }

        // --- Added LoadRoutesSynchronously Method (Adapted from frmIncomeReport) ---
        private bool LoadRoutesSynchronously()
        {
            string logStart = "Loading routes synchronously for statistics form...";
            Log.Info(logStart);
            HttpClient client = null;
            string routesJsonRaw = null;
            XDocument routesXml = null;
            List<SalesStats_RouteLookupViewModel> loadedRoutes = new List<SalesStats_RouteLookupViewModel>();
            bool success = false;

            // Do not call SetLoadingState here, it's handled by the caller (LoadDataSynchronously or btnApply)

            try
            {
                client = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/Routes", _baseUrl);
                Log.Debug("Fetching route lookup data from: {0}", apiUrl);
                HttpResponseMessage response = client.GetAsync(apiUrl).Result; // Synchronous call

                if (response.IsSuccessStatusCode)
                {
                    byte[] routesBytes = response.Content.ReadAsByteArrayAsync().Result;
                    routesJsonRaw = Encoding.UTF8.GetString(routesBytes);

                    if (!string.IsNullOrEmpty(routesJsonRaw))
                    {
                        routesXml = ProcessJsonToXml(routesJsonRaw, "Routes");
                        foreach (XElement routeNode in routesXml.Root.Elements("Routes"))
                        {
                            try {
                                if (!routeNode.HasElements) continue;
                                int routeId = 0;
                                string startPoint = string.Empty;
                                string endPoint = string.Empty;

                                XElement idEl = routeNode.Element("routeId");
                                if (idEl != null) int.TryParse(idEl.Value, out routeId); else continue;

                                XElement startEl = routeNode.Element("startPoint");
                                if (startEl != null) startPoint = startEl.Value;
                                XElement endEl = routeNode.Element("endPoint");
                                if (endEl != null) endPoint = endEl.Value;

                                loadedRoutes.Add(new SalesStats_RouteLookupViewModel {
                                    RouteId = routeId,
                                    DisplayName = string.Format("{0} -> {1}", startPoint, endPoint)
                                });
                            } catch (Exception exNode) {
                                string errorMsgNode = string.Format("Error parsing individual Route XML node for lookup: {0}. Node: {1}", exNode.ToString(), routeNode.ToString());
                                Log.Error(errorMsgNode);
                            }
                        }
                        _availableRoutes = loadedRoutes.OrderBy(delegate(SalesStats_RouteLookupViewModel r) { return r.DisplayName; }).ToList();
                        success = true;
                        Log.Info("Successfully loaded {0} routes for lookup.", _availableRoutes.Count);
                    }
                    else {
                        Log.Warn("Route lookup API returned success but content was empty.");
                        _availableRoutes = new List<SalesStats_RouteLookupViewModel>();
                    }
                }
                else
                {
                    var errorContent = response.Content.ReadAsStringAsync().Result;
                    string errorMsg = string.Format("Failed to load route lookup data. Status: {0}, Content: {1}", response.StatusCode, errorContent);
                    Log.Error(errorMsg);
                    _availableRoutes = new List<SalesStats_RouteLookupViewModel>();
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Error fetching/processing route lookup data. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                _availableRoutes = new List<SalesStats_RouteLookupViewModel>();
                success = false;
            }
            finally
            {
                if (client != null) client.Dispose();
                // No UI update needed here directly, data is used internally
            }
            return success;
        }
         // --- End Added LoadRoutesSynchronously ---
    }
} 
