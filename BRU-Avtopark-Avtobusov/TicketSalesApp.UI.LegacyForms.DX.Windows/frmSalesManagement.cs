using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using DevExpress.Data;
using DevExpress.XtraPrinting;
using System.Net.Http;
using System.Diagnostics;
using System.Web; // For HttpUtility
using DevExpress.XtraEditors.Controls;
using DevExpress.Utils;
using System.Drawing; // Added for Point, Size etc.
using DevExpress.XtraLayout; // Added for dynamic layout (optional but good)
using NLog; // Replaced Serilog with NLog
using Newtonsoft.Json; // Keep for payload serialization
using Newtonsoft.Json.Linq; // Needed for JSON helpers
using TicketSalesApp.Core.Models;
using System.Threading; // Added
using System.Collections.Concurrent; // Added for ConcurrentBag
using System.Xml; // Added
using System.Xml.Linq; // Added
using System.Globalization; // Added
using System.Collections; // Added for IEnumerable
using System.Text.RegularExpressions; // Added
using DevExpress.XtraGrid.Views.Base; // Added
using Newtonsoft.Json.Serialization; // Added
using System.ComponentModel; // Added for BindingList
using System.Threading.Tasks; // Added for Task

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    // ViewModel for displaying sales in the grid
    public class SaleViewModel
    {
        // Properties match grid columns and data model
        public int SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        public int RouteScheduleId { get; set; } // Assuming this relates to Bilet.RouteScheduleId if available
        public long RouteId { get; set; } // Store RouteId for filtering and context
        public string RouteDescription { get; set; } // Display friendly route info
        public DateTime DepartureTime { get; set; } // From related RouteSchedule/Bilet
        public DateTime ArrivalTime { get; set; } // From related RouteSchedule/Bilet
        public int SeatNumber { get; set; } // From Bilet
        public double TotalAmount { get; set; } // From Bilet.TicketPrice
        public string PaymentMethod { get; set; } // Defaulted or from Prodazha if available
        public string Status { get; set; } // Calculated or default
        public string StartPoint { get; set; } // From Marshut
        public string EndPoint { get; set; } // From Marshut

        // Constructor to initialize default values
        public SaleViewModel()
        {
            // Initialize default values, especially for strings if needed
            RouteDescription = "[N/A]";
            PaymentMethod = "N/A"; // Default or determine later
            Status = "N/A";       // Default or determine later
            StartPoint = "[N/A]";
            EndPoint = "[N/A]";
        }
    }

    // ViewModel for the Route Schedule LookupEdit in the Add/Edit form
    public class RouteScheduleLookupViewModel
    {
        public int RouteScheduleId { get; set; } // The ID to store
        public string DisplayName { get; set; } // Text shown in the dropdown
        public double Price { get; set; } // To auto-fill price when selected
        public string StartPoint { get; set; }
        public string EndPoint { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int AvailableSeats { get; set; } // Added for information

        // Constructor
        public RouteScheduleLookupViewModel()
        {
            DisplayName = "[N/A]";
        }
    }

    // Helper class to deserialize the X-Pagination header
    public class PaginationMetadata
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
        public int NextPage { get; set; }
        public int PreviousPage { get; set; }
        public int FirstPage { get; set; }
        public int LastPage { get; set; }
    }


    public partial class frmSalesManagement : DevExpress.XtraEditors.XtraForm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger(); // NLog instance
        private readonly ApiClientService _apiClient; // Changed from _apiClientService
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings 
        { 
            PreserveReferencesHandling = PreserveReferencesHandling.Objects, // Match example
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Keep if needed, example might omit
            DateFormatHandling = DateFormatHandling.IsoDateFormat, // Match example
            DateTimeZoneHandling = DateTimeZoneHandling.Utc // Match example
        };
        private readonly string _baseUrl = "http://localhost:5000/api"; // Adjusted to match example pattern
        private BindingList<RouteScheduleLookupViewModel> _availableRouteSchedules;
        private BindingList<SaleViewModel> _salesData; // Renamed from _allSalesData and changed type
        private bool _isBusy = false; // Loading state flag
        private long? _selectedRouteIdFilter = null; // Added for pre-filtering
        private ObservableCollection<Marshut> _allRoutes; // Reinstated: Stores all available routes
        private Label _waitMessageLabel = null; // Field to hold reference to the wait message label
        private System.Windows.Forms.Timer _waitFormUpdateTimer; // Timer for progress updates
        private DateTime _scheduleLoadStartTime; // Start time for schedule fetching
        private int _scheduleLoadCurrentPage = 0; // Current page being fetched/processed
        private int _scheduleLoadTotalPages = 1; // Total pages to fetch
        private volatile bool _scheduleLoadInProgress = false; // Flag for timer tick
        // private Dictionary<int, double> _schedulePricing = new Dictionary<int, double>(); // Removed, handled by ViewModel now
        private bool _formLoadComplete = false; // Added

        public frmSalesManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance; // Use Instance
            _allRoutes = new ObservableCollection<Marshut>(); // Initialize
            _salesData = new BindingList<SaleViewModel>(); // Initialize
            _availableRouteSchedules = new BindingList<RouteScheduleLookupViewModel>(); // Initialize

            // Configure Route Filter LookUpEdit (Assuming lueRouteFilter exists in the designer)
            // NOTE: Requires adding lueRouteFilter to frmSalesManagement.Designer.cs if needed
            /* // Uncomment and adapt if lueRouteFilter is added
            lueRouteFilter.Properties.DataSource = _allRoutes;
            lueRouteFilter.Properties.DisplayMember = "StartPoint"; // Or a combined display string
            lueRouteFilter.Properties.ValueMember = "RouteId";
            lueRouteFilter.Properties.Columns.Add(new LookUpColumnInfo("RouteId", "ID", 50));
            lueRouteFilter.Properties.Columns.Add(new LookUpColumnInfo("StartPoint", "Начало", 150));
            lueRouteFilter.Properties.Columns.Add(new LookUpColumnInfo("EndPoint", "Конец", 150));
            lueRouteFilter.Properties.NullText = "[Выберите маршрут...]";
            */

            // Configure Date Filters
            dateFromFilter.Properties.NullText = "[Дата С]";
            dateToFilter.Properties.NullText = "[Дата По]";
            dateFromFilter.EditValue = null;
            dateToFilter.EditValue = null;

            // Configure Grid
            salesBindingSource.DataSource = _salesData; // Bind to the new BindingList
            gridViewSales.OptionsView.ColumnAutoWidth = false; // Match example

            UpdateButtonStates(); // Initial button state

            // Event Subscriptions (Match example pattern)
            gridViewSales.FocusedRowChanged += gridViewSales_FocusedRowChanged; // Added
            gridViewSales.DoubleClick += gridViewSales_DoubleClick; // Keep existing double-click edit
            gridViewSales.CustomUnboundColumnData += gridViewSales_CustomUnboundColumnData; // Added for potential unbound columns
            gridViewSales.CustomColumnDisplayText += gridViewSales_CustomColumnDisplayText; // Keep existing display text handling

            this.Load += frmSalesManagement_Load;
            this.FormClosing += FrmSalesManagement_FormClosing;
            
            // Filter Event Subscriptions (Replace btnApplyFilter logic)
            // lueRouteFilter.EditValueChanged += lueRouteFilter_EditValueChanged; // Add if lueRouteFilter exists
            dateFromFilter.EditValueChanged += dateFilter_EditValueChanged; // Added
            dateToFilter.EditValueChanged += dateFilter_EditValueChanged; // Added

            _apiClient.OnAuthTokenChanged += HandleAuthTokenChanged; // Subscribe to token changes

            Log.Debug("frmSalesManagement initialized.");
        }


        private void FrmSalesManagement_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Debug("Form closing.");
            // Unsubscribe from events to prevent memory leaks
            _apiClient.OnAuthTokenChanged -= HandleAuthTokenChanged;
        }

        private void HandleAuthTokenChanged(object sender, string token)
        {
             string logMsg = "Auth token changed, triggering synchronous data reload.";
             Log.Debug(logMsg);
            // Use BeginInvoke to ensure UI updates happen on the UI thread
            if (this.Visible && this.IsHandleCreated && !this.IsDisposed)
            {
                this.BeginInvoke(new Action(delegate {
                    try
                    {
                        // Only reload if a route is selected, otherwise wait for user interaction
                        if (_selectedRouteIdFilter.HasValue)
                        {
                            LoadDataSynchronously();
                        }
                        else
                        {
                           string warnMsg = "Auth token changed, but no route selected. Data reload deferred.";
                           Log.Warn(warnMsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = string.Format("Error during background refresh triggered by token change. Exception: {0}", ex.ToString());
                        Log.Error(errorMsg);
                         // Optionally show a message to the user, but avoid blocking
                         // XtraMessageBox.Show("Ошибка фонового обновления данных.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                     }
                 }));
            } else {
                 string skipMsg = "Auth token changed, but form is not visible or handle not created. Skipping reload.";
                 Log.Debug(skipMsg);
                }
        }

         private void frmSalesManagement_Load(object sender, EventArgs e)
         {
             if (_formLoadComplete)
             {
                 string warnMsg = "frmSalesManagement_Load fired again after initial load completed. Ignoring.";
                 Log.Warn(warnMsg);
                 return;
             }

             bool initialLoadSuccess = false;
             try
             {
                 string logMsg1 = "frmSalesManagement_Load event triggered (initial run).";
                 Log.Debug(logMsg1);

                 // Force user to select a route first
                 initialLoadSuccess = LoadRoutesAndSelectFilter();

                 if (initialLoadSuccess)
                 {
                     string logMsg2 = "Initial route selected. Proceeding with main data load.";
                     Log.Debug(logMsg2);
                     LoadDataSynchronously(); // Load data for the selected route

                     _formLoadComplete = true; // Mark initial load as complete
                     string logMsg3 = "Initial form load sequence completed successfully.";
                     Log.Info(logMsg3);
                 }
                 else
                 {
                      string errorMsg = "Failed to load initial routes or user cancelled selection. Aborting form load.";
                      Log.Error(errorMsg);
                      XtraMessageBox.Show("Не удалось загрузить список маршрутов или выбор был отменен. Форма управления продажами не может быть загружена без выбора маршрута.", "Ошибка Инициализации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                      // Close the form gracefully using BeginInvoke
                     this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) this.Close(); }));
                 }
             }
             catch (Exception ex)
             {
                 _formLoadComplete = true; // Ensure this is set even on error
                 string errorMsg = string.Format("Critical error during form load sequence. Exception: {0}", ex.ToString());
                 Log.Error(errorMsg);
                 // Use Environment.NewLine for line break in message box
                 XtraMessageBox.Show(string.Format("Произошла критическая ошибка при загрузке формы: {0}{1}Форма будет закрыта.", ex.Message, Environment.NewLine), "Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  // Close the form gracefully using BeginInvoke
                 this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) this.Close(); }));
             }
         }


        // --- JSON Helper Methods (Copied from frmRouteSchedulesManagement / frmEmployeeManagement - MAINTAINED COMPLEXITY) ---

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

         private static XDocument ProcessJsonToXml(string jsonRaw, string rootElementName)
         {
             if (string.IsNullOrWhiteSpace(jsonRaw))
             {
                Log.Warn(string.Format("ProcessJsonToXml called with null or empty JSON for {0}. Returning default empty XML.", rootElementName));
                 return XDocument.Parse(string.Format("<Root><{0}></{0}></Root>", rootElementName));
             }

             string truncatedJson = (jsonRaw.Length > 2000) ? jsonRaw.Substring(0, 2000) + "..." : jsonRaw;
            Log.Debug(string.Format("Processing raw JSON for {0} (truncated): {1}", rootElementName, truncatedJson));

            // Pre-clean JSON by removing control characters - use parallel processing for large strings
            string preCleanedJson;
            if (jsonRaw.Length > 100000) // Only use parallel for large strings
            {
                // Split the string into chunks for parallel processing
                int chunkSize = Math.Max(10000, jsonRaw.Length / Environment.ProcessorCount);
                int chunksCount = (int)Math.Ceiling((double)jsonRaw.Length / chunkSize);
                StringBuilder[] results = new StringBuilder[chunksCount];
                
                // Initialize all StringBuilder instances with capacity
                for (int i = 0; i < chunksCount; i++)
                {
                    int currentChunkSize = Math.Min(chunkSize, jsonRaw.Length - i * chunkSize);
                    results[i] = new StringBuilder(currentChunkSize);
                }
                
                // Process chunks in parallel
                Parallel.For(0, chunksCount, i =>
                {
                    int start = i * chunkSize;
                    int end = Math.Min(start + chunkSize, jsonRaw.Length);
                    StringBuilder sb = results[i];
                    
                    for (int j = start; j < end; j++)
                    {
                        char c = jsonRaw[j];
                        if (c > '\u001F') // Skip control characters (0-31)
                        {
                            sb.Append(c);
                        }
                    }
                });
                
                // Combine results
                StringBuilder finalResult = new StringBuilder(jsonRaw.Length);
                foreach (StringBuilder sb in results)
                {
                    finalResult.Append(sb);
                }
                preCleanedJson = finalResult.ToString();
            }
            else
            {
                // For smaller strings, use the original sequential approach
                StringBuilder cleanedJsonBuilder = new StringBuilder(jsonRaw.Length);
                for (int i = 0; i < jsonRaw.Length; i++)
                {
                    char c = jsonRaw[i];
                    if (c > '\u001F') // Skip control characters (0-31)
                    {
                        cleanedJsonBuilder.Append(c);
                    }
                }
                preCleanedJson = cleanedJsonBuilder.ToString();
            }
            
            if (string.IsNullOrEmpty(preCleanedJson))
            {
                Log.Warn(string.Format("preCleanedJson is null or empty after cleaning control characters for {0}. Returning empty XML.", rootElementName));
                 return XDocument.Parse(string.Format("<Root><{0}></{0}></Root>", rootElementName));
             }

             JToken rootToken = null;
            try
            {
                 rootToken = JToken.Parse(preCleanedJson);
            }
            catch (JsonReaderException jsonEx)
            {
                string errorMsg = string.Format("Failed to parse pre-cleaned JSON for {0}. Exception: {1}", rootElementName, jsonEx.ToString());
                Log.Error(errorMsg);
                  throw new Exception(string.Format("Ошибка парсинга JSON для {0}.", rootElementName), jsonEx);
             }

             JObject finalObjectForXml = null;
            // Pre-allocate dictionary with expected capacity to avoid resizing
            Dictionary<string, JObject> globalIdMap = new Dictionary<string, JObject>(256, StringComparer.Ordinal);

            // Build the global ID map for reference resolution
            try
            {
                 BuildGlobalIdMap(rootToken, globalIdMap);
                Log.Debug(string.Format("Built GLOBAL ID map with {0} entries for {1} structure.", globalIdMap.Count, rootElementName));
            }
            catch (Exception mapEx)
            {
                string errorMsg = string.Format("Error building global ID map for {0}. Exception: {1}", rootElementName, mapEx.ToString());
                Log.Error(errorMsg);
                // Continue without the map, $ref resolution might fail
             }

             JObject initialObj = rootToken as JObject;
             JArray initialArray = rootToken as JArray;

            // CASE 1: Root is the common {$id:"...", $values:[...]} structure
             if (initialObj != null && initialObj.Property("$values") != null &&
                 initialObj.Property("$values").Value.Type == JTokenType.Array &&
                 (initialObj.Count == 1 || (initialObj.Count == 2 && initialObj.Property("$id") != null)))
             {
                Log.Debug(string.Format("Detected root as object containing $values array for {0}.", rootElementName));
                 JArray innerArray = (JArray)initialObj.Property("$values").Value;

                // Pre-allocate capacity for better performance
                int estimatedCapacity = innerArray.Count;
                List<JToken> resolvedItems = new List<JToken>(estimatedCapacity);

                // For large arrays, use parallel processing to resolve references
                if (innerArray.Count > 1000)
                {
                    // Convert to array for parallel processing
                    JToken[] itemsArray = innerArray.ToArray();
                    ConcurrentBag<JToken> concurrentResolvedItems = new ConcurrentBag<JToken>();
                    
                    Parallel.ForEach(itemsArray, item =>
                    {
                        JObject itemObj = item as JObject;
                        if (itemObj == null)
                        {
                            concurrentResolvedItems.Add(item);
                            return;
                        }

                        JProperty refProp = itemObj.Property("$ref");
                        if (refProp != null && itemObj.Count == 1) // It's a pure $ref object
                        {
                            string refValue = refProp.Value.ToString();
                            JObject resolvedObj;
                            if (globalIdMap.TryGetValue(refValue, out resolvedObj))
                            {
                                concurrentResolvedItems.Add(resolvedObj.DeepClone());
                            }
                        }
                        else if (itemObj.Property("$id") != null) // It's an object with $id defined at the top level
                        {
                            concurrentResolvedItems.Add(item);
                        }
                        else // Could be a simple value, another object without $id/$ref, or null
                        {
                            concurrentResolvedItems.Add(item);
                        }
                    });
                    
                    // Convert back to list
                    resolvedItems = concurrentResolvedItems.ToList();
                }
                else
                {
                    // Resolve top-level items (might be $ref or actual objects) using the GLOBAL map
                 foreach (JToken item in innerArray)
                 {
                     JObject itemObj = item as JObject;
                        if (itemObj == null)
                        {
                            resolvedItems.Add(item);
                            continue;
                        }

                        JProperty refProp = itemObj.Property("$ref");
                        if (refProp != null && itemObj.Count == 1) // It's a pure $ref object
                     {
                         string refValue = refProp.Value.ToString();
                            JObject resolvedObj;
                            if (globalIdMap.TryGetValue(refValue, out resolvedObj))
                         {
                                Log.Debug(string.Format("Resolving top-level $ref '{0}'...", refValue));
                                resolvedItems.Add(resolvedObj.DeepClone());
                         }
                         else
                         {
                                Log.Warn(string.Format("Could not resolve top-level $ref '{0}' for {1}. Ref not found in GLOBAL map. Skipping.", refValue, rootElementName));
                                // Skip adding unresolvable references
                            }
                        }
                        else if (itemObj.Property("$id") != null) // It's an object with $id defined at the top level
                        {
                            resolvedItems.Add(item);
                        }
                        else // Could be a simple value, another object without $id/$ref, or null
                        {
                            resolvedItems.Add(item);
                        }
                    }
                }
                Log.Debug(string.Format("Resolved {0} top-level items for {1}.", resolvedItems.Count, rootElementName));

                // Clean the RESOLVED items (remove $id, replace deeper $refs with {}, etc.)
                List<JToken> cleanedItems;
                
                // Use parallel processing for large collections
                if (resolvedItems.Count > 1000)
                {
                    JToken[] itemsArray = resolvedItems.ToArray();
                    ConcurrentBag<JToken> concurrentCleanedItems = new ConcurrentBag<JToken>();
                    
                    Parallel.ForEach(itemsArray, resItem =>
                    {
                        try
                        {
                            JToken cleanedItem = CleanAndTransformJsonToken(resItem);
                            // Add only if the cleaning didn't result in null
                            if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                            {
                                concurrentCleanedItems.Add(cleanedItem);
                            }
                        }
                        catch (Exception cleanEx)
                        {
                            string errorMsg = string.Format("Error cleaning resolved item for {0}. Item (truncated): {1}. Exception: {2}",
                                rootElementName,
                                (resItem.ToString(Newtonsoft.Json.Formatting.None).Length > 200 ? 
                                    resItem.ToString(Newtonsoft.Json.Formatting.None).Substring(0, 200) + "..." : 
                                    resItem.ToString(Newtonsoft.Json.Formatting.None)),
                                cleanEx.ToString());
                            Log.Error(errorMsg);
                        }
                    });
                    
                    cleanedItems = concurrentCleanedItems.ToList();
                }
                else
                {
                    cleanedItems = new List<JToken>(resolvedItems.Count);
                 foreach (JToken resItem in resolvedItems)
        {
            try
            {
                            JToken cleanedItem = CleanAndTransformJsonToken(resItem);
                            // Add only if the cleaning didn't result in null
                         if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                         {
                                cleanedItems.Add(cleanedItem);
                            }
                        }
                        catch (Exception cleanEx)
                        {
                            string errorMsg = string.Format("Error cleaning resolved item for {0}. Item (truncated): {1}. Exception: {2}",
                                rootElementName,
                                (resItem.ToString(Newtonsoft.Json.Formatting.None).Length > 200 ? 
                                    resItem.ToString(Newtonsoft.Json.Formatting.None).Substring(0, 200) + "..." : 
                                    resItem.ToString(Newtonsoft.Json.Formatting.None)),
                                cleanEx.ToString());
                            Log.Error(errorMsg);
                        }
                    }
                }

                // Filter out any resulting empty objects AFTER cleaning - use direct iteration for better performance
                List<JToken> filteredItems = new List<JToken>(cleanedItems.Count);
                foreach (JToken t in cleanedItems)
                {
                     JObject jobj = t as JObject;
                    if (jobj == null || jobj.HasValues)
                    {
                        filteredItems.Add(t);
                    }
                }

                Log.Debug(string.Format("Filtered {0} empty objects from {1} cleaned items for {2}", 
                    cleanedItems.Count - filteredItems.Count, cleanedItems.Count, rootElementName));

                // Create the final structure for XML conversion: { RootElement: [ filteredItems ] }
                 finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredItems)));
             }
            // CASE 2: Root is a JSON Array directly
             else if (initialArray != null)
             {
                Log.Debug(string.Format("Root token for {0} is a JArray. Cleaning array directly.", rootElementName));
                List<JToken> cleanedItems;
                
                // Use parallel processing for large arrays
                if (initialArray.Count > 1000)
                {
                    JToken[] itemsArray = initialArray.ToArray();
                    ConcurrentBag<JToken> concurrentCleanedItems = new ConcurrentBag<JToken>();
                    
                    Parallel.ForEach(itemsArray, item =>
                    {
                        try
                        {
                         JToken cleanedItem = CleanAndTransformJsonToken(item);
                            // Add only if the cleaning didn't result in null
                            if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                            {
                                concurrentCleanedItems.Add(cleanedItem);
                            }
                        }
                        catch (Exception cleanEx)
                        {
                            string errorMsg = string.Format("Error cleaning root array item for {0}. Item (truncated): {1}. Exception: {2}",
                                rootElementName,
                                (item.ToString(Newtonsoft.Json.Formatting.None).Length > 200 ? 
                                    item.ToString(Newtonsoft.Json.Formatting.None).Substring(0, 200) + "..." : 
                                    item.ToString(Newtonsoft.Json.Formatting.None)),
                                cleanEx.ToString());
                            Log.Error(errorMsg);
                        }
                    });
                    
                    cleanedItems = concurrentCleanedItems.ToList();
                }
                else
                {
                    cleanedItems = new List<JToken>(initialArray.Count);
                    foreach (JToken item in initialArray)
                    {
                        try
                        {
                            JToken cleanedItem = CleanAndTransformJsonToken(item);
                            // Add only if the cleaning didn't result in null
                            if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                            {
                                cleanedItems.Add(cleanedItem);
                            }
                        }
                        catch (Exception cleanEx)
                        {
                            string errorMsg = string.Format("Error cleaning root array item for {0}. Item (truncated): {1}. Exception: {2}",
                                rootElementName,
                                (item.ToString(Newtonsoft.Json.Formatting.None).Length > 200 ? 
                                    item.ToString(Newtonsoft.Json.Formatting.None).Substring(0, 200) + "..." : 
                                    item.ToString(Newtonsoft.Json.Formatting.None)),
                                cleanEx.ToString());
                            Log.Error(errorMsg);
                        }
                    }
                }

                // Filter out empty objects AFTER cleaning - use direct iteration for better performance
                List<JToken> filteredItems = new List<JToken>(cleanedItems.Count);
                foreach (JToken t in cleanedItems)
                {
                     JObject jobj = t as JObject;
                    if (jobj == null || jobj.HasValues)
                    {
                        filteredItems.Add(t);
                    }
                }

                // Create the final structure: { RootElement: [ filteredItems ] }
                  finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredItems)));
             }
            // CASE 3: Root is something else (likely a single object not wrapped in $values)
                else
                {
                Log.Debug(string.Format("Root token for {0} is not {$id,$values} or JArray (Type: {1}). Cleaning token directly and wrapping.", 
                    rootElementName, rootToken.Type));
                
                 JToken cleanedToken = null;
                try
                {
                     cleanedToken = CleanAndTransformJsonToken(rootToken);
                }
                catch (Exception cleanEx)
                {
                    string errorMsg = string.Format("Error cleaning root token (Case 3) for {0}. Token (truncated): {1}. Exception: {2}",
                        rootElementName,
                        (rootToken.ToString(Newtonsoft.Json.Formatting.None).Length > 200 ? 
                            rootToken.ToString(Newtonsoft.Json.Formatting.None).Substring(0, 200) + "..." : 
                            rootToken.ToString(Newtonsoft.Json.Formatting.None)),
                        cleanEx.ToString());
                    Log.Error(errorMsg);
                    cleanedToken = new JObject();
                }

                // Handle different token types appropriately
                 if (cleanedToken is JArray)
                {
                    // For arrays, wrap directly with the root element name
                    JArray cleanedArray = cleanedToken as JArray;
                    
                    if (cleanedArray != null && cleanedArray.Count > 0)
                    {
                        // Use direct iteration for better performance
                        List<JToken> filteredArray = new List<JToken>(cleanedArray.Count);
                        foreach (JToken t in cleanedArray)
                        {
                            JObject jobj = t as JObject;
                            if (jobj == null || jobj.HasValues)
                            {
                                filteredArray.Add(t);
                            }
                        }
                        
                        finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredArray)));
                    }
                    else
                 {
                      finalObjectForXml = new JObject(new JProperty(rootElementName, cleanedToken));
                 }
                }
                else
                 {
                      JObject cleanedObj = cleanedToken as JObject;
                    if (cleanedObj == null || cleanedObj.HasValues)
                      {
                        // For non-empty objects or other token types, wrap directly
                         finalObjectForXml = new JObject(new JProperty(rootElementName, cleanedToken ?? new JObject()));
                     }
                    else
                      {
                        Log.Debug("Cleaned root token resulted in an empty object. Creating empty root for XML.");
                        // For empty objects, use an empty array structure for consistency
                        finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray()));
                      }
                 }

                // Special handling for nested $values wrappers that might remain after cleaning
                  JObject potentiallyStillWrapped = cleanedToken as JObject;
                if (potentiallyStillWrapped != null && 
                    potentiallyStillWrapped.Count == 1 && 
                    potentiallyStillWrapped.Property("$values") != null)
                {
                    JToken innerValue = potentiallyStillWrapped.Property("$values").Value;
                    if (innerValue is JArray)
                    {
                        Log.Warn(string.Format("Cleaned token (Case 3) for {0} still contained $values wrapper with array. Extracting inner content.", rootElementName));
                        JArray extractedArray = (JArray)innerValue;
                        
                        // Use direct iteration for better performance
                        List<JToken> filteredExtracted = new List<JToken>(extractedArray.Count);
                        foreach (JToken t in extractedArray)
                        {
                            JObject jobj = t as JObject;
                            if (jobj == null || jobj.HasValues)
                            {
                                filteredExtracted.Add(t);
                            }
                        }
                        
                           finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray(filteredExtracted)));
                       }
                    else if (innerValue is JObject)
                    {
                        Log.Warn(string.Format("Cleaned token (Case 3) for {0} still contained $values wrapper with object. Extracting inner content.", rootElementName));
                        finalObjectForXml = new JObject(new JProperty(rootElementName, innerValue));
                       }
                  }
             }

            // Final conversion to XML
             string finalJsonForXml = "{}";
            try
            {
                if (finalObjectForXml != null)
                {
                    // Use Formatting.None for better performance
                      finalJsonForXml = finalObjectForXml.ToString(Newtonsoft.Json.Formatting.None);
                }
                else
                {
                    Log.Warn(string.Format("finalObjectForXml was unexpectedly null for {0} before final XML conversion. Using default empty.", rootElementName));
                      finalObjectForXml = new JObject(new JProperty(rootElementName, new JArray()));
                      finalJsonForXml = finalObjectForXml.ToString(Newtonsoft.Json.Formatting.None);
                  }

                  string truncatedFinalJson = (finalJsonForXml.Length > 2000) ? finalJsonForXml.Substring(0, 2000) + "..." : finalJsonForXml;
                Log.Debug(string.Format("Final {0} JSON prepared for XML conversion (truncated): {1}", rootElementName, truncatedFinalJson));

                // Convert to XML document with optimized settings
                XmlDocument xmlDoc = JsonConvert.DeserializeXmlNode(finalJsonForXml, "Root", false);
                
                // Use XmlReaderSettings for better performance
                XmlReaderSettings settings = new XmlReaderSettings { 
                    IgnoreWhitespace = true,
                    IgnoreComments = true,
                    IgnoreProcessingInstructions = true,
                    DtdProcessing = DtdProcessing.Prohibit,
                    CheckCharacters = false
                };
                
                using (XmlNodeReader nodeReader = new XmlNodeReader(xmlDoc))
                {
                    XmlReader reader = XmlReader.Create(nodeReader, settings);
                    return XDocument.Load(reader);
                }
              }
              catch (Exception xmlEx)
              {
                string errorMsg = string.Format("Final XML conversion failed for {0}. JSON used (truncated): {1}. Exception: {2}",
                    rootElementName,
                    (finalJsonForXml.Length > 500 ? finalJsonForXml.Substring(0, 500) + "..." : finalJsonForXml),
                    xmlEx.ToString());
                Log.Error(errorMsg);
                  throw new Exception(string.Format("Ошибка конвертации {0} JSON в XML.", rootElementName), xmlEx);
              }
         }

         private static JToken CleanAndTransformJsonToken(JToken token)
         {
             if (token == null) return null;

             switch (token.Type)
             {
                 case JTokenType.Object:
            {
                 JObject obj = (JObject)token;

                         // Check for pure {$ref: "..."} object
                         if (obj.Count == 1 && obj.Property("$ref") != null)
                         {
                              // Replace $ref objects entirely with an empty object {}
                              // This prevents them from appearing in the final XML.
                             string refValue = obj.Property("$ref").Value.ToString();
                              // Get parent property name if available for logging context
                              string parentPropName = (token.Parent is JProperty) ? ((JProperty)token.Parent).Name : "(root or array item)";
                              string refLog = string.Format("Replacing $ref ('{0}') found under property '{1}' with empty JObject.", refValue, parentPropName);
                              Log.Debug(refLog);
                              return new JObject();
                          }

                         // Process regular object properties recursively
                         JObject cleanedObj = new JObject();
                         // Use ToList() on properties to avoid issues if the structure is modified during iteration (though cleaning shouldn't do that here)
                         foreach (var property in obj.Properties().ToList())
                         {
                             // Remove $id properties completely
                             if (property.Name.Equals("$id", StringComparison.OrdinalIgnoreCase))
                             {
                                  // Log.Debug($"Removing $id property from object."); // Optional logging
                                 continue;
                              }

                             // Recursively clean the property's value FIRST
                             JToken cleanedValue = null;
                             try {
                                 cleanedValue = CleanAndTransformJsonToken(property.Value);
                             } catch (Exception exCleanInner) {
                                  // Log the error and skip this property
                                  string errorMsg = string.Format("Error cleaning inner property '{0}'. Skipping. Exception: {1}", property.Name, exCleanInner.ToString());
                                  Log.Error(errorMsg);
                                  continue; // Skip adding this property
                             }


                             // --- Check for nested {$values: [...]} wrapper in the *cleaned* value ---
                             JObject valueObj = cleanedValue as JObject;
                             if (valueObj != null && valueObj.Count == 1 && valueObj.Property("$values") != null)
                             {
                                 // Check if the value is an array (common case) or potentially just an object
                                 JToken innerValuesContent = valueObj.Property("$values").Value;
                                 if (innerValuesContent.Type == JTokenType.Array || innerValuesContent.Type == JTokenType.Object)
                                 {
                                      string nestLog = string.Format("Found nested $values wrapper in property '{0}', replacing with inner content.", property.Name);
                                      Log.Debug(nestLog);
                                     // Recursively clean the *inner* content and use that as the final value
                                     cleanedValue = CleanAndTransformJsonToken(innerValuesContent);
                                 }
                             }
                             // --- End nested $values check ---


                             // Add the property back if the cleaned value is not null
                             // AND if it's not an empty object (which likely resulted from cleaning a $ref)
                             if (cleanedValue != null && cleanedValue.Type != JTokenType.Null)
                             {
                                 JObject cleanedValueAsObject = cleanedValue as JObject;
                                 // Keep if it's NOT a JObject OR if it IS a JObject but HAS properties
                                 if (cleanedValueAsObject == null || cleanedValueAsObject.HasValues)
                                 {
                                      cleanedObj.Add(property.Name, cleanedValue);
                                 }
                                 // else: Skipped adding property because cleaned value was an empty object
                             }
                             // else: Skipped adding property because cleaned value was null
                 }
                         // Return the object containing only cleaned, non-null, non-empty-object properties
                 return cleanedObj;
             }

                 case JTokenType.Array:
             {
                 JArray array = (JArray)token;
                         JArray cleanedArray = new JArray();
                         // Use ToList() to safely iterate if cleaning could modify the array (it shouldn't here)
                         foreach (var item in array.ToList())
                         {
                              JToken cleanedItem = null;
                             try {
                                  cleanedItem = CleanAndTransformJsonToken(item); // Recursively clean each item
                             } catch (Exception exCleanItem) {
                                 // Log error cleaning item and continue
                                 string errorMsg = string.Format("Error cleaning array item. Skipping. Exception: {0}", exCleanItem.ToString());
                                 Log.Error(errorMsg);
                                  continue; // Skip this item
                             }

                             // Add item back if it's not null AND not an empty object
                             if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                             {
                                 JObject cleanedItemAsObject = cleanedItem as JObject;
                                 if (cleanedItemAsObject == null || cleanedItemAsObject.HasValues)
                                 {
                                      cleanedArray.Add(cleanedItem);
                                 }
                                 // else: Skipped adding item because it was an empty object
                             }
                             // else: Skipped adding item because it was null
                 }
                         // Return the array containing only cleaned, non-null, non-empty-object items
                 return cleanedArray;
             }

                 default:
                     // For simple types (String, Integer, Boolean, Date, Null, etc.), return the token as is.
                 return token;
             }
        }


        // --- End JSON Helper Methods ---

         private bool LoadRoutesAndSelectFilter()
         {
            string logStart = "Starting initial route loading and selection process...";
            Log.Info(logStart);
            SetLoadingState(true); // Show loading state

             XtraForm waitMessageBox = null;
             HttpClient client = null;
             string routesJsonRaw = null;
             XDocument routesXml = null;
             List<Marshut> loadedRoutes = new List<Marshut>();
            bool routeSelected = false;

             try
             {
                // Show a non-blocking wait message
                 waitMessageBox = new XtraForm
                 {
                     Text = "Загрузка Маршрутов...",
                     StartPosition = FormStartPosition.CenterScreen,
                     FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    Size = new Size(320, 120),
                    ControlBox = false // Prevent user from closing it
                 };
                 var label = new Label { Text = "Загрузка списка маршрутов...", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                 waitMessageBox.Controls.Add(label);
                waitMessageBox.Show(this); // Show modelessly relative to parent

                client = _apiClient.CreateClient();

                // --- Fetch Routes Synchronously ---
                string logFetch = "Fetching Routes synchronously for initial selection...";
                Log.Debug(logFetch);
                var routesApiUrl = string.Format("{0}/Routes", _baseUrl);
                // Use .Result for synchronous execution (blocks UI thread, hence the wait message)
                 HttpResponseMessage routesResponse = client.GetAsync(routesApiUrl).Result;

                 if (routesResponse.IsSuccessStatusCode)
                 {
                     byte[] routesBytes = routesResponse.Content.ReadAsByteArrayAsync().Result;
                     routesJsonRaw = Encoding.UTF8.GetString(routesBytes);
                    string logSuccess = "Initial Routes JSON fetched successfully.";
                    Log.Debug(logSuccess);
                 }
                 else
                 {
                    // Throw exception if fetch fails
                     throw new Exception("Failed to load initial Routes: " + routesResponse.ReasonPhrase);
                 }
                // --- End Fetch Routes ---

                // --- Process JSON to XML ---
                 if (!string.IsNullOrEmpty(routesJsonRaw))
                 {
                    routesXml = ProcessJsonToXml(routesJsonRaw, "Routes"); // Use the helper
                    string logXmlConv = "Initial Routes JSON processed for XML conversion.";
                    Log.Debug(logXmlConv);
                 }
                 else
                 {
                    string warnEmptyJson = "Initial routesJsonRaw was null or empty.";
                    Log.Warn(warnEmptyJson);
                     throw new Exception("Initial Routes JSON was empty.");
                 }
                // --- End Process JSON to XML ---

                // --- Parse XML to Route Objects ---
                // Iterate directly over <Routes> elements under <Root>
                 foreach (XElement routeNode in routesXml.Root.Elements("Routes"))
                 {
                     try
                     {
                        // Skip empty elements potentially left from cleaning $refs
                         if (!routeNode.HasElements) continue;

                         Marshut route = new Marshut();
                         long routeId;
                        // Use safe parsing
                        XElement routeIdElement = routeNode.Element("routeId");
                        if (routeIdElement != null && long.TryParse(routeIdElement.Value, out routeId))
                         {
                             route.RouteId = routeId;
                        }
                        else
                        {
                            string warnParseId = string.Format("Could not parse routeId for XML element: {0}. Skipping route.", routeNode.ToString());
                            Log.Warn(warnParseId);
                            continue; // Skip this route if ID is invalid
                        }

                        // Safely get string values, defaulting to empty string if element is missing
                         route.StartPoint = (routeNode.Element("startPoint") != null) ? routeNode.Element("startPoint").Value : string.Empty;
                         route.EndPoint = (routeNode.Element("endPoint") != null) ? routeNode.Element("endPoint").Value : string.Empty;

                         loadedRoutes.Add(route);
                     }
                    catch (Exception exNode)
                    {
                        // Log error parsing a specific node but continue with others
                        string errorMsg = string.Format("Error parsing individual Route XML node during pre-load: {0}. Node: {1}", exNode.ToString(), routeNode.ToString());
                        Log.Error(errorMsg);
                    }
                }
                string logParseCount = string.Format("Parsed {0} initial routes from XML.", loadedRoutes.Count);
                Log.Debug(logParseCount);

                // Check if any routes were actually loaded
                if (!loadedRoutes.Any())
                {
                    throw new Exception("No routes were loaded after parsing XML. Cannot proceed.");
                }
                // --- End Parse XML ---

                // Close the wait message box *before* showing the selection dialog
                 if (waitMessageBox != null && !waitMessageBox.IsDisposed)
                 {
                    // Use BeginInvoke to ensure it closes on the UI thread safely
                     Action closeWaitBox = delegate() { if (!waitMessageBox.IsDisposed) waitMessageBox.Close(); };
                     if (waitMessageBox.InvokeRequired) { waitMessageBox.BeginInvoke(closeWaitBox); } else { closeWaitBox(); }
                    waitMessageBox.Dispose(); // Dispose resources
                    waitMessageBox = null; // Prevent reuse
                 }

                // --- Show Route Selection Dialog ---
                 using (var selectionForm = new XtraForm())
                 {
                      selectionForm.Text = "Выберите Маршрут";
                    selectionForm.StartPosition = FormStartPosition.CenterParent; // Center on this form
                    selectionForm.FormBorderStyle = FormBorderStyle.FixedDialog; // Non-resizable
                    selectionForm.MinimizeBox = false;
                    selectionForm.MaximizeBox = false;
                    selectionForm.ControlBox = false; // No close button, force selection or cancel via OK
                    selectionForm.Size = new Size(450, 180); // Adjust size as needed

                      var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(15) };
                      selectionForm.Controls.Add(panel);

                      var selectLabel = new LabelControl { Text = "Выберите маршрут для загрузки продаж:", Location = new Point(15, 20), AutoSize = true };
                      panel.Controls.Add(selectLabel);

                    // LookUpEdit for route selection
                      var routeCombo = new LookUpEdit
                      {
                        Location = new Point(15, 45),
                        Width = 400, // Adjust width
                          Properties = {
                            DataSource = loadedRoutes.OrderBy(r => r.StartPoint).ToList(), // Provide sorted list
                            DisplayMember = "StartPoint", // Show StartPoint in the text box
                            ValueMember = "RouteId",    // Use RouteId as the actual value
                            NullText = "[Выберите маршрут...]", // Placeholder text
                            ShowHeader = true // Show column headers in dropdown
                        }
                    };
                    // Define columns for the dropdown
                      routeCombo.Properties.Columns.Add(new LookUpColumnInfo("RouteId", "ID", 50));
                      routeCombo.Properties.Columns.Add(new LookUpColumnInfo("StartPoint", "Начало", 150));
                      routeCombo.Properties.Columns.Add(new LookUpColumnInfo("EndPoint", "Конец", 150));
                      panel.Controls.Add(routeCombo);

                    // Hidden TextEdit to store the selected RouteId (MATCHES EXAMPLE REQUIREMENT)
                    var txtHiddenSelectedRouteId = new TextEdit {
                        Name = "txtHiddenSelectedRouteId",
                        Location = new Point(-200, -200), // Position off-screen
                        Visible = false,                 // Make it invisible
                        Width = 10                       // Minimal size
                    };
                    panel.Controls.Add(txtHiddenSelectedRouteId);

                    // OK button
                      var btnOk = new SimpleButton { Text = "OK", Location = new Point(selectionForm.ClientSize.Width - 115, 100), Width = 100 };
                      panel.Controls.Add(btnOk);

                    selectionForm.AcceptButton = btnOk; // Set OK as the default accept button

                    // OK Button Click Handler
                      btnOk.Click += (s, args) => {
                          // **IMPORTANT**: EditValue might be null or not represent the selection correctly if user just typed.
                          // Use the Text property and find the corresponding object in the DataSource.
                          string selectedDisplayText = routeCombo.Text; // Get displayed text
                          object selectedValue = routeCombo.EditValue; // Keep EditValue check for direct selection case

                          // Validate selection based on text primarily
                          if (!string.IsNullOrEmpty(selectedDisplayText) && selectedDisplayText != "[Выберите маршрут...]")
                          {
                              // Find the route object matching the display text
                              Marshut selectedRouteObject = loadedRoutes.FirstOrDefault(r =>
                                  string.Equals(r.StartPoint, selectedDisplayText, StringComparison.OrdinalIgnoreCase) || // Match StartPoint
                                  string.Equals(r.EndPoint, selectedDisplayText, StringComparison.OrdinalIgnoreCase) ||   // Or EndPoint (if combined display)
                                  string.Equals(string.Format("{0} -> {1}", r.StartPoint, r.EndPoint), selectedDisplayText, StringComparison.OrdinalIgnoreCase) // Or combined
                              );

                              // Fallback check using EditValue if direct lookup failed
                              if (selectedRouteObject == null && selectedValue != null && selectedValue != DBNull.Value && selectedValue is long) {
                                  selectedRouteObject = loadedRoutes.FirstOrDefault(r => r.RouteId == (long)selectedValue);
                              }


                              if (selectedRouteObject != null)
                              {
                                  _selectedRouteIdFilter = selectedRouteObject.RouteId; // Store the selected ID
                                  txtHiddenSelectedRouteId.Text = _selectedRouteIdFilter.Value.ToString(); // Update hidden field

                                  string logSelect = string.Format("Route ID {0} determined from display text '{1}' or EditValue, and stored.",
                                      _selectedRouteIdFilter.Value, selectedDisplayText);
                                  Log.Debug(logSelect);

                                  selectionForm.DialogResult = DialogResult.OK; // Set result to OK
                                  selectionForm.Close(); // Close the dialog
                 }
                 else
                 {
                                  // If still not found, show error
                                  string errorMsg = string.Format("Could not find route object matching display text '{0}' or EditValue '{1}'.",
                                       selectedDisplayText, (selectedValue != null ? selectedValue.ToString() : "null")); // C# 4.0 compatible null check
                                  Log.Error(errorMsg);
                                  XtraMessageBox.Show("Не удалось найти выбранный маршрут. Пожалуйста, выберите из списка или попробуйте снова.",
                                      "Ошибка Поиска", MessageBoxButtons.OK, MessageBoxIcon.Error);
                              }
                          }
                          else
                          {
                              // Show warning if nothing valid is selected/entered
                              XtraMessageBox.Show("Пожалуйста, выберите маршрут из списка.", "Требуется выбор", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                              routeCombo.Focus(); // Set focus back to the combo
                          }
                      };


                    // Show the dialog modally
                    selectionForm.ShowDialog(this); // Blocks until the dialog is closed
                }
                // --- End Route Selection Dialog ---

                // --- Process Selection Result ---
                if (_selectedRouteIdFilter.HasValue)
                {
                    string logUserSelect = string.Format("Route ID {0} selected by user.", _selectedRouteIdFilter.Value);
                    Log.Info(logUserSelect);

                    // Populate the main form's route collection (for potential display or future use)
                    _allRoutes.Clear();
                    foreach (var route in loadedRoutes.OrderBy(r => r.StartPoint))
                    {
                        _allRoutes.Add(route);
                    }

                    // Update the main form's route filter dropdown (if it exists)
                    Action updateMainFilterAction = delegate() {
                        if (this.IsDisposed) return; // Check if form is disposed

                        // If you have a route filter control, uncomment and adapt this code
                        /*
                        if (lueRouteFilter != null && !lueRouteFilter.IsDisposed) {
                            lueRouteFilter.Properties.DataSource = null; // Clear first
                            lueRouteFilter.Properties.DataSource = _allRoutes; // Set new source
                            lueRouteFilter.EditValue = _selectedRouteIdFilter; // Set selected value
                        }
                        */
                    };

                    if (this.IsHandleCreated && !this.IsDisposed) {
                        this.BeginInvoke(updateMainFilterAction);
                    }
                    else if (!this.IsDisposed) {
                        updateMainFilterAction();
                    }

                    return true; // Indicate success
                }
                else
                {
                    // User likely closed the dialog without selecting OK
                    string warnCancel = "Route selection was cancelled or failed.";
                    Log.Warn(warnCancel);
                    return false; // Indicate failure
                }
                // --- End Process Selection Result ---
             }
             catch (Exception ex)
             {
                // Catch any exception during the process (fetch, XML parse, dialog)
                 string errorMsg = string.Format("Error during initial route loading/selection. Exception: {0}", ex.ToString());
                 Log.Error(errorMsg);
                // Show error message to the user
                 XtraMessageBox.Show("Ошибка при загрузке списка маршрутов: " + ex.Message, "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false; // Indicate failure
             }
             finally
             {
                // Ensure resources are cleaned up
                 if (client != null) client.Dispose();

                // Ensure the wait message box is closed even if exceptions occurred
                 if (waitMessageBox != null && !waitMessageBox.IsDisposed)
                 {
                     Action closeWaitBoxFinal = delegate() { if (!waitMessageBox.IsDisposed) waitMessageBox.Close(); };
                     if (waitMessageBox.InvokeRequired) { waitMessageBox.BeginInvoke(closeWaitBoxFinal); } else { closeWaitBoxFinal(); }
                     waitMessageBox.Dispose();
                     _waitMessageLabel = null; // Clear label reference after closing
                 }

                 // Ensure timer is stopped and disposed if an exception occurred mid-load
                 if (_waitFormUpdateTimer != null) {
                     _waitFormUpdateTimer.Stop();
                     _waitFormUpdateTimer.Dispose();
                     _waitFormUpdateTimer = null;
                 }
                 _scheduleLoadInProgress = false; // Ensure flag is reset

                 // Reset loading state on UI thread
                 Action finalUiAction = delegate()
                 {
                     if (this.IsDisposed) {
                         Log.Debug("Form disposed before final UI state could be reset in finally block.");
                         return;
                     }
                     SetLoadingState(false);
                     Log.Debug("Finished final UI state reset after synchronous load sequence attempt.");
                 };

                 if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(finalUiAction); }
                 else if (!this.IsDisposed) { finalUiAction(); }
             }
         }

        private void SetLoadingState(bool isLoading)
        {
            // Ensure execution on the UI thread
            if (this.InvokeRequired)
            {
                // Use BeginInvoke for asynchronous call to UI thread
                this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) SetLoadingState(isLoading); }));
                return;
            }

            // Check if form or critical controls are disposed before proceeding
             if (this.IsDisposed || gridControlSales == null || gridControlSales.IsDisposed)
             {
                string warnMsg = "SetLoadingState called but form or gridControlSales are disposed.";
                Log.Warn(warnMsg);
                 return;
             }

            string logMsg = isLoading ? "Setting UI to loading state." : "Setting UI to normal state.";
            Log.Debug(logMsg);
            _isBusy = isLoading; // Update the busy flag

            // Change cursor
             Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;

            // Enable/disable controls based on loading state
             gridControlSales.Enabled = !isLoading;
             btnAdd.Enabled = !isLoading;
             btnRefresh.Enabled = !isLoading;
            btnExport.Enabled = !isLoading; // Also disable export during load
            // lueRouteFilter.Enabled = !isLoading; // Enable/disable route filter if added
             dateFromFilter.Enabled = !isLoading;
             dateToFilter.Enabled = !isLoading;
            // btnApplyFilter.Enabled = !isLoading; // Remove if replaced by event handlers

            // Special handling for Edit/Delete buttons:
            // If loading, always disable Edit/Delete.
            // If *not* loading, their state depends on row selection (handled by UpdateButtonStates).
             if (!isLoading)
             {
                UpdateButtonStates(); // Refresh button states based on selection etc.
                 }
                 else
                 {
                // Explicitly disable Edit/Delete when loading
                 if (btnEdit != null && !btnEdit.IsDisposed) btnEdit.Enabled = false;
                 if (btnDelete != null && !btnDelete.IsDisposed) btnDelete.Enabled = false;
             }
        }


         // Renamed and refactored from LoadPrerequisitesSynchronously + RefreshSalesDataSynchronously
        private void LoadDataSynchronously()
        {
             // --- Pre-check: Ensure a route has been selected ---
             if (!_selectedRouteIdFilter.HasValue)
             {
                 string warnMsg = "LoadDataSynchronously called but no route selected (_selectedRouteIdFilter is null). Aborting data load.";
                 Log.Warn(warnMsg);
                 // Optionally clear the grid and show a message
                 Action clearGridAction = delegate() {
                      if (this.IsDisposed) return;
                      _salesData.Clear();
                      _availableRouteSchedules.Clear();
                      gridControlSales.RefreshDataSource();
                      // Maybe show a status label: "Выберите маршрут для загрузки данных"
                 };
                  if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(clearGridAction); }
                  else if (!this.IsDisposed) { clearGridAction(); }
                 SetLoadingState(false); // Ensure loading state is reset
                 return;
             }
             // --- End Pre-check ---

             string selectedRouteIdStr = _selectedRouteIdFilter.Value.ToString();
             string logStart = string.Format("Starting synchronous data load sequence for selected route: {0}...", selectedRouteIdStr);
             Log.Info(logStart);
             SetLoadingState(true); // Set UI to busy state

             XtraForm waitMessageBox = null;
             try
             {
                 // Show a "Please wait" message dynamically
                 Action showWaitBoxAction = delegate()
                 {
                     if (this.IsDisposed) return; // Don't show if form is closing
                     waitMessageBox = new XtraForm
                     {
                         Text = "Загрузка данных...",
                         StartPosition = FormStartPosition.CenterScreen,
                         FormBorderStyle = FormBorderStyle.FixedDialog,
                         MaximizeBox = false,
                         MinimizeBox = false,
                         Size = new Size(320, 120),
                         ControlBox = false
                     };
                     var label = new Label
                     {
                         Text = string.Format("Загрузка данных (Шаг 1/2: Расписания) для маршрута {0}...{1}Пожалуйста, подождите...", // Initial text
                             selectedRouteIdStr, Environment.NewLine),
                         Dock = DockStyle.Fill,
                         TextAlign = ContentAlignment.MiddleCenter
                     };
                     waitMessageBox.Controls.Add(label);
                     waitMessageBox.Show(this); // Show relative to the main form
                 };
                 // Ensure this runs on the UI thread
                 if (this.InvokeRequired) { this.Invoke(showWaitBoxAction); } else { showWaitBoxAction(); }
             }
             catch (Exception exWaitBox)
             {
                 string warnMsg = string.Format("Could not display wait message box. Proceeding without it. Error: {0}", exWaitBox.Message);
                 Log.Warn(warnMsg);
                 // Continue without the wait box if it fails
             }

             HttpClient client = null;
             string salesJsonRaw = null;
             // string schedulesJsonRaw = null; // Removed - fetched page by page

             // Collections to hold loaded data before updating UI
             List<RouteScheduleLookupViewModel> loadedSchedules = new List<RouteScheduleLookupViewModel>();
             List<SaleViewModel> loadedSales = new List<SaleViewModel>();

             // XML Documents for intermediate storage
             XDocument salesXml = XDocument.Parse("<Root><Sales></Sales></Root>");
             // XDocument schedulesXml = XDocument.Parse("<Root><Schedules></Schedules></Root>"); // Removed - built after pagination
             List<XElement> allScheduleElements = new List<XElement>(); // To store schedule elements from all pages

             // Create a cancellation token source for potential cancellation
             var cts = new CancellationTokenSource();
             var token = cts.Token;

             // Performance tracking
             var stopwatch = new Stopwatch();
             stopwatch.Start();

             try
             {
                 client = _apiClient.CreateClient();

                 // Get date filter values from UI thread
                 DateTime? dateFrom = null;
                 DateTime? dateTo = null;
                 Action getDateFilters = delegate() {
                     if (!this.IsDisposed) {
                         object fromVal = dateFromFilter.EditValue;
                         object toVal = dateToFilter.EditValue;
                         if (fromVal != null && fromVal != DBNull.Value) { dateFrom = (DateTime)fromVal; }
                         if (toVal != null && toVal != DBNull.Value) { dateTo = (DateTime)toVal; }
                     }
                 };
                 if (this.InvokeRequired) { this.Invoke(getDateFilters); } else { getDateFilters(); }


                 // --- 1. Fetch RouteSchedules Page by Page ---
                 int currentPage = 1;
                 int totalPages = 1; // Initialize to 1 to start the loop
                 int pageSize = 200; // Adjust page size as needed (balance between requests and memory)
                 string schedulesFetchError = null;

                 DateTime scheduleFetchStartTime = DateTime.Now; // Record start time for estimation

                 string logSchedStart = string.Format("Starting paginated fetch for RouteSchedules for route {0}...", selectedRouteIdStr);
                 Log.Debug(logSchedStart);

                 // --- Initialize and Start Progress Timer --- 
                 _scheduleLoadCurrentPage = 1;
                 _scheduleLoadTotalPages = 1; // Start assuming 1 page
                 _scheduleLoadStartTime = DateTime.Now;
                 _scheduleLoadInProgress = true;

                 // Ensure timer is created and started on UI thread if needed, but Timer itself is fine
                 _waitFormUpdateTimer = new System.Windows.Forms.Timer();
                 _waitFormUpdateTimer.Interval = 500; // Update interval (milliseconds)
                 _waitFormUpdateTimer.Tick += WaitFormUpdateTimer_Tick;
                 _waitFormUpdateTimer.Start();
                 // --- End Timer Setup ---

                 while (currentPage <= totalPages)
                 {
                     if (token.IsCancellationRequested) {
                         Log.Warn("Cancellation requested during schedule pagination fetch.");
                         break;
                     }

                     try
                     {
                         var schedulesQuery = HttpUtility.ParseQueryString(string.Empty);
                         schedulesQuery["routeId"] = _selectedRouteIdFilter.Value.ToString();
                         schedulesQuery["pageNumber"] = currentPage.ToString();
                         schedulesQuery["pageSize"] = pageSize.ToString();
                         // Do NOT add ReturnAll=true here

                         string schedulesApiUrl = string.Format("{0}/RouteSchedules/search?{1}", _baseUrl, schedulesQuery.ToString());
                         string logSchedFetchPage = string.Format("Fetching RouteSchedules page {0}/{1} from URL: {2}",
                             currentPage, totalPages == 1 ? "?" : totalPages.ToString(), schedulesApiUrl);
                         Log.Debug(logSchedFetchPage);

                         // Synchronous call using .Result
                         HttpResponseMessage schedulesResponse = client.GetAsync(schedulesApiUrl).Result;

                         if (schedulesResponse.IsSuccessStatusCode)
                         {
                             // Read pagination header
                             IEnumerable<string> paginationHeaders;
                             if (schedulesResponse.Headers.TryGetValues("X-Pagination", out paginationHeaders))
                             {
                                 string paginationJson = paginationHeaders.FirstOrDefault();
                                 if (!string.IsNullOrEmpty(paginationJson))
                                 {
                                     try
                                     {
                                         PaginationMetadata metadata = JsonConvert.DeserializeObject<PaginationMetadata>(paginationJson);
                                         // Update shared variables for timer tick
                                         _scheduleLoadTotalPages = metadata.TotalPages;
                                         _scheduleLoadCurrentPage = metadata.CurrentPage; // Sync current page
                                         totalPages = metadata.TotalPages; // Update loop condition
                                         Log.Debug(string.Format("Pagination Header Found: Page {0}/{1}, Total Items: {2}",
                                             _scheduleLoadCurrentPage, _scheduleLoadTotalPages, metadata.TotalCount));
                                     }
                                     catch (Exception exHeader)
                                     {
                                         string errorMsg = string.Format("Failed to parse X-Pagination header: {0}. Header: {1}", exHeader.Message, paginationJson);
                    Log.Error(errorMsg);
                                         // Continue, but pagination might be inaccurate
                                         if (totalPages == 1) totalPages = 2; // Guess there might be more pages
                                     }
                                 }
                             }
                             else if (currentPage == 1)
                             {
                                 Log.Warn("X-Pagination header not found in response. Assuming single page.");
                                 _scheduleLoadTotalPages = 1;
                                 totalPages = 1; // Update loop condition
                             }

                             // --- Update Wait Message --- 
                             try {
                                 TimeSpan elapsed = DateTime.Now - scheduleFetchStartTime;
                                 string estimateString = "(оценка времени...)";
                                 if (currentPage > 1 && totalPages > 1) // Calculate estimate after first page
                                 {
                                     double avgTimePerPage = elapsed.TotalSeconds / (double)(currentPage -1); // Avg time for completed pages
                                     double estimatedTotalSeconds = avgTimePerPage * totalPages;
                                     TimeSpan estimatedRemaining = TimeSpan.FromSeconds(Math.Max(0, estimatedTotalSeconds - elapsed.TotalSeconds));
                                     estimateString = string.Format("~{0:mm} мин {1:ss} сек осталось", estimatedRemaining, estimatedRemaining);
                                 }
                                 string progressText = string.Format(
                                     "Загрузка данных (Шаг 1/2: Расписания)...{0}Маршрут: {1}{0}Страница: {2} / {3}{0}{4}", 
                                     Environment.NewLine, 
                                     selectedRouteIdStr,
                                     currentPage, 
                                     totalPages,
                                     estimateString
                                 );
                                 Action updateLabelAction = delegate() {
                                     if (_waitMessageLabel != null && !_waitMessageLabel.IsDisposed) {
                                         _waitMessageLabel.Text = progressText;
                                     }
                                 };
                                 if (waitMessageBox != null && !waitMessageBox.IsDisposed && waitMessageBox.IsHandleCreated) {
                                      if (waitMessageBox.InvokeRequired) { waitMessageBox.BeginInvoke(updateLabelAction); } else { updateLabelAction(); }
                                 }
                             } catch (Exception exLabel) {
                                 Log.Warn(string.Format("Failed to update wait message label: {0}", exLabel.Message));
                             }
                             // --- End Update Wait Message ---

                             // Process JSON for the current page
                             byte[] schedulesBytes = schedulesResponse.Content.ReadAsByteArrayAsync().Result;
                             string schedulesJsonRawPage = Encoding.UTF8.GetString(schedulesBytes);

                             if (!string.IsNullOrEmpty(schedulesJsonRawPage))
                 {
                     try
                     {
                                     XDocument pageXml = ProcessJsonToXml(schedulesJsonRawPage, "Schedules");
                                     // Add the <Schedules> elements from this page to the combined list
                                     if (pageXml != null && pageXml.Root != null)
                                     {
                                         allScheduleElements.AddRange(pageXml.Root.Elements("Schedules"));
                                         Log.Debug(string.Format("Processed page {0} JSON to XML, added {1} schedule elements.", currentPage, pageXml.Root.Elements("Schedules").Count()));
                                     }
                                 }
                                 catch (Exception exPageXml)
                                 {
                                     string errorMsg = string.Format("Error processing Schedules JSON to XML for page {0}. Exception: {1}. Skipping page.", currentPage, exPageXml.ToString());
                    Log.Error(errorMsg);
                                     schedulesFetchError = "Error processing schedule data from API."; // Store first error
                                 }
                             } else {
                                 Log.Warn(string.Format("Received empty JSON content for schedules page {0}.", currentPage));
                             }

                             currentPage++; // Move to the next page
                }
                else
                {
                             // Handle non-success status code
                             schedulesFetchError = string.Format("Failed to load RouteSchedules page {0} for route {1}: {2}.", currentPage, selectedRouteIdStr, schedulesResponse.ReasonPhrase);
                             Log.Error(schedulesFetchError);
                             break; // Stop pagination on error
                         }
                     }
                     catch (Exception exSchedPage)
                     {
                         schedulesFetchError = string.Format("Error fetching/processing RouteSchedules page {0} for route {1}. Exception: {2}", currentPage, selectedRouteIdStr, exSchedPage.ToString());
                         Log.Error(schedulesFetchError);
                         break; // Stop pagination on error
                     }
                 } // End while loop for schedule pagination

                 // Check if schedule fetch failed
                 if (schedulesFetchError != null)
                 {
                     throw new Exception("Failed to load complete schedule data due to pagination error: " + schedulesFetchError);
                 }
                 Log.Debug(string.Format("Finished fetching all schedule pages in {0}ms. Total elements collected: {1}", stopwatch.ElapsedMilliseconds, allScheduleElements.Count));
                 // --- End Fetch RouteSchedules ---


                 // --- 2. Fetch Sales Data (synchronously) ---
                 try
                 {
                     // Build query string for sales search
                     var salesQuery = HttpUtility.ParseQueryString(string.Empty);
                     if (dateFrom.HasValue) salesQuery["startDate"] = dateFrom.Value.ToString("yyyy-MM-dd");
                     if (dateTo.HasValue) salesQuery["endDate"] = dateTo.Value.ToString("yyyy-MM-dd");
                     // Sales API doesn't seem to support routeId filtering directly, will filter client-side
                     string salesApiUrl = string.Format("{0}/TicketSales/search?{1}", _baseUrl, salesQuery.ToString());

                     string logSalesFetch = string.Format("Fetching Sales synchronously (Route filter {0} applied client-side). URL: {1}", selectedRouteIdStr, salesApiUrl);
                     Log.Debug(logSalesFetch);
                     HttpResponseMessage salesResponse = client.GetAsync(salesApiUrl).Result;

                     if (salesResponse.IsSuccessStatusCode)
                     {
                         byte[] salesBytes = salesResponse.Content.ReadAsByteArrayAsync().Result;
                         salesJsonRaw = Encoding.UTF8.GetString(salesBytes);
                         string logSalesOk = string.Format("Sales JSON fetched successfully (Payload size: {0} bytes). Filtering by route {1} client-side.", salesBytes.Length, selectedRouteIdStr);
                         Log.Debug(logSalesOk);
                }
                else
                {
                         throw new Exception("Failed to load Sales data: " + salesResponse.ReasonPhrase);
                     }
                 }
                 catch (Exception exSales)
                 {
                     string errorMsg = string.Format("Error fetching Sales data. Exception: {0}", exSales.ToString());
                     Log.Error(errorMsg);
                     throw; // Propagate sales fetch error
                 }
                 // --- End Fetch Sales Data ---

                 // --- 3. Process Sales JSON to XML (Schedules XML is built from collected elements) ---
                 Log.Debug("Converting fetched Sales JSON to XML...");

                 // Process Sales JSON (synchronously)
                 try
                 {
                     if (!string.IsNullOrEmpty(salesJsonRaw))
                     {
                         salesXml = ProcessJsonToXml(salesJsonRaw, "Sales");
                         Log.Debug("Sales JSON processed for XML conversion.");
                     }
                     else
                     {
                         Log.Warn("salesJsonRaw was null or empty, using default empty Sales XML.");
                         // salesXml remains the default empty one
                     }
                 }
                 catch (Exception exSalesXml)
                 {
                     string errorMsg = string.Format("Error processing Sales JSON to XML. Exception: {0}", exSalesXml.ToString());
                Log.Error(errorMsg);
                     throw new Exception("Failed to process Sales JSON for XML conversion.", exSalesXml);
                 }

                 // Build the final combined schedules XML Document from collected elements
                 XDocument schedulesXml = new XDocument(new XElement("Root", allScheduleElements));
                 Log.Debug(string.Format("Combined Schedules XML created with {0} elements in {1}ms",
                     schedulesXml.Root.Elements("Schedules").Count(), stopwatch.ElapsedMilliseconds));
                 // --- End Process JSON to XML ---


                 // --- 4. Parse XML into Objects ---
                 Log.Debug("Parsing XML data into objects...");

                 // Parse Schedules XML into RouteScheduleLookupViewModel (synchronously from combined XML)
                 try
                 {
                     foreach (XElement scheduleNode in schedulesXml.Root.Elements("Schedules"))
                     {
                         try
                         {
                             if (!scheduleNode.HasElements)
                             {
                                 continue; // Skip empty elements
                             }

                             RouteScheduleLookupViewModel vm = new RouteScheduleLookupViewModel();
                             int scheduleId = 0; // Use int as per ViewModel
                             long routeId = 0;
                             DateTime departure = DateTime.MinValue, arrival = DateTime.MinValue;
                             double price = 0;
                             int availableSeats = 0;
                             string start = "[N/A]", end = "[N/A]";

                             XElement idEl = scheduleNode.Element("routeScheduleId");
                             if (idEl != null) int.TryParse(idEl.Value, out scheduleId); else continue; // Skip if ID invalid

                             XElement routeIdEl = scheduleNode.Element("routeId");
                             if (routeIdEl != null) long.TryParse(routeIdEl.Value, out routeId);

                             XElement startEl = scheduleNode.Element("startPoint");
                             if (startEl != null) start = startEl.Value;
                             XElement endEl = scheduleNode.Element("endPoint");
                             if (endEl != null) end = endEl.Value;

                             XElement depEl = scheduleNode.Element("departureTime");
                             // Use TryParse with specific styles for robustness
                             if (depEl != null) {
                                 bool depParsed = DateTime.TryParse(depEl.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out departure);
                                 // We don't strictly need to use depParsed here, but it makes the statement valid
                             }
                             XElement arrEl = scheduleNode.Element("arrivalTime");
                             if (arrEl != null) {
                                 bool arrParsed = DateTime.TryParse(arrEl.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out arrival);
                             }


                             XElement priceEl = scheduleNode.Element("price");
                             if (priceEl != null) double.TryParse(priceEl.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out price);

                             XElement seatsEl = scheduleNode.Element("availableSeats");
                             if (seatsEl != null) int.TryParse(seatsEl.Value, out availableSeats);

                             // Ensure it belongs to the selected route (should already be filtered by API, but double-check)
                             if (routeId == _selectedRouteIdFilter.Value)
                             {
                                 vm.RouteScheduleId = scheduleId;
                                 vm.Price = price;
                                 vm.StartPoint = start;
                                 vm.EndPoint = end;
                                 vm.DepartureTime = departure.ToLocalTime(); // Convert UTC from API to Local
                                 vm.ArrivalTime = arrival.ToLocalTime();   // Convert UTC from API to Local
                                 vm.AvailableSeats = availableSeats;
                                 vm.DisplayName = string.Format("{0} -> {1} ({2:dd.MM HH:mm})", start, end, vm.DepartureTime);

                                 loadedSchedules.Add(vm);
                             }
                         }
                         catch (Exception exNode)
                         {
                             string errorMsg = string.Format("Error parsing individual Schedule XML node for Lookup: {0}. Node: {1}", exNode.ToString(), scheduleNode.ToString());
                    Log.Error(errorMsg);
                         }
                     }
                     string logSchedParse = string.Format("Parsed {0} schedule lookups from combined XML for route {1}.", loadedSchedules.Count, selectedRouteIdStr);
                     Log.Debug(logSchedParse);
                 }
                 catch (Exception exSchedParse)
                 {
                     string errorMsg = string.Format("Error parsing combined Schedules XML into LookupViewModels. Exception: {0}", exSchedParse.ToString());
                     Log.Error(errorMsg);
                     // Potentially throw, or continue with potentially incomplete schedule data
                 }


                 // Create a lookup dictionary for faster schedule matching
                 var scheduleLookup = loadedSchedules.ToDictionary(s => s.RouteScheduleId, s => s);
                 Log.Debug(string.Format("Schedule lookup dictionary created with {0} entries in {1}ms", scheduleLookup.Count, stopwatch.ElapsedMilliseconds));

                 // Parse Sales XML into SaleViewModel (synchronously)
                 try
                 {
                     foreach (XElement saleNode in salesXml.Root.Elements("Sales"))
                 {
                     try
                     {
                             if (!saleNode.HasElements)
                             {
                                 Log.Debug("Skipping empty <Sales> element from XML.");
                                 continue;
                             }

                         SaleViewModel vm = new SaleViewModel();
                             int saleId = 0; // Use int as per ViewModel
                             DateTime saleDate = DateTime.MinValue;
                             long ticketId = 0; // Assuming Bilet uses long
                             int routeScheduleId = 0; // Should match scheduleLookup key type (int)
                             long routeId = 0; // From Marshut
                             int seatNumber = 0;
                             double price = 0;
                             string startPoint = "[N/A]";
                             string endPoint = "[N/A]";
                             DateTime departure = DateTime.MinValue;
                             DateTime arrival = DateTime.MinValue;
                             string soldToUser = "ФИЗ.ПРОДАЖА"; // Default
                             string status = "Продано"; // Default

                             XElement saleIdEl = saleNode.Element("saleId");
                             if (saleIdEl != null) int.TryParse(saleIdEl.Value, out saleId); else continue; // Skip if no SaleId

                             XElement saleDateEl = saleNode.Element("saleDate");
                             if (saleDateEl != null) DateTime.TryParse(saleDateEl.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out saleDate);


                             XElement soldToUserEl = saleNode.Element("ticketSoldToUser");
                             if (soldToUserEl != null && !string.IsNullOrEmpty(soldToUserEl.Value)) soldToUser = soldToUserEl.Value;

                             // Access nested Bilet data
                         XElement biletNode = saleNode.Element("bilet");
                             if (biletNode != null) // Bilet data exists
                             {
                                 XElement ticketIdEl = biletNode.Element("ticketId");
                                 if (ticketIdEl != null) long.TryParse(ticketIdEl.Value, out ticketId);

                                 XElement rsIdEl = biletNode.Element("routeScheduleId");
                                 if (rsIdEl != null) int.TryParse(rsIdEl.Value, out routeScheduleId);

                                 XElement seatEl = biletNode.Element("seatNumber");
                                 if (seatEl != null) int.TryParse(seatEl.Value, out seatNumber);

                                 XElement priceEl = biletNode.Element("ticketPrice");
                                 if (priceEl != null) double.TryParse(priceEl.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out price);

                                 // Access nested Marshut data within Bilet
                                 XElement marshutNode = biletNode.Element("marshut");
                                 if (marshutNode != null)
                                 {
                                     XElement routeIdEl = marshutNode.Element("routeId");
                                     if (routeIdEl != null) long.TryParse(routeIdEl.Value, out routeId);

                                     XElement startEl = marshutNode.Element("startPoint");
                                     if (startEl != null) startPoint = startEl.Value;
                                     XElement endEl = marshutNode.Element("endPoint");
                                     if (endEl != null) endPoint = endEl.Value;

                                     // Use dictionary lookup instead of LINQ for better performance
                                     RouteScheduleLookupViewModel matchingSchedule;
                                     if (scheduleLookup.TryGetValue(routeScheduleId, out matchingSchedule))
                                     {
                                         departure = matchingSchedule.DepartureTime; // Already local time
                                         arrival = matchingSchedule.ArrivalTime;   // Already local time
                                         // Ensure Start/End points from the looked-up schedule are used if Marshut data is missing/inconsistent
                                         if (string.IsNullOrEmpty(startPoint) || startPoint == "[N/A]") startPoint = matchingSchedule.StartPoint;
                                         if (string.IsNullOrEmpty(endPoint) || endPoint == "[N/A]") endPoint = matchingSchedule.EndPoint;
                                     } else {
                                         Log.Warn(string.Format("Sale ID {0} references RouteScheduleId {1}, but it was not found in the loaded schedule lookup. Times/Route Description might be incomplete.", saleId, routeScheduleId));
                                         // Explicitly set defaults if lookup fails
                                         departure = DateTime.MinValue;
                                         arrival = DateTime.MinValue;
                                         // Keep existing start/end points if parsed from Marshut, otherwise default
                                         if (string.IsNullOrEmpty(startPoint) || startPoint == "[N/A]") startPoint = "[N/A]";
                                         if (string.IsNullOrEmpty(endPoint) || endPoint == "[N/A]") endPoint = "[N/A]";
                                     }
                                 } else {
                                      Log.Warn(string.Format("Sale ID {0} Bilet node does not contain nested Marshut data.", saleId));
                                      // Set defaults if Marshut node missing
                                      routeId = 0;
                                      startPoint = "[N/A]";
                                      endPoint = "[N/A]";
                                      departure = DateTime.MinValue;
                                      arrival = DateTime.MinValue;
                                 }
                             } else { // Bilet node is missing entirely
                                 Log.Warn(string.Format("Sale ID {0} does not contain nested Bilet data.", saleId));
                                 // Explicitly set all Bilet/Marshut derived fields to defaults
                                 ticketId = 0;
                                 routeScheduleId = 0;
                                 seatNumber = 0;
                                 price = 0;
                                 routeId = 0;
                                 startPoint = "[N/A]";
                                 endPoint = "[N/A]";
                                 departure = DateTime.MinValue;
                                 arrival = DateTime.MinValue;
                             }

                             // Only add the sale if its RouteId matches the selected filter
                             if (routeId == _selectedRouteIdFilter.Value)
                             {
                                 vm.SaleId = saleId;
                                 vm.SaleDate = saleDate.ToLocalTime(); // Convert UTC SaleDate to Local
                                 vm.RouteScheduleId = routeScheduleId;
                                 vm.RouteId = routeId;
                                 vm.SeatNumber = seatNumber;
                                 vm.TotalAmount = price;
                                 vm.StartPoint = startPoint;
                                 vm.EndPoint = endPoint;
                                 vm.DepartureTime = departure; // Already local
                                 vm.ArrivalTime = arrival; // Already local
                                 vm.RouteDescription = string.Format("{0} -> {1}", startPoint, endPoint);
                                 vm.PaymentMethod = soldToUser.Equals("ФИЗ.ПРОДАЖА", StringComparison.OrdinalIgnoreCase) ? "Наличные/Карта" : "Онлайн";
                                 vm.Status = status;

                                 loadedSales.Add(vm);
                         } else {
                                 // Log if a sale was filtered out client-side (shouldn't happen if API filtered correctly, but good check)
                                 // Log.Debug($"Sale ID {saleId} skipped client-side filter (RouteId: {routeId}, Filter: {_selectedRouteIdFilter.Value})");
                         }
                     }
                     catch (Exception exNode)
                     {
                             string errorMsg = string.Format("Error parsing individual Sale XML node: {0}. Node: {1}", exNode.ToString(), saleNode.ToString());
                             Log.Error(errorMsg);
                         }
                     }
                     string logSalesParse = string.Format("Parsed {0} sales from XML after client-side filtering for route {1}.", loadedSales.Count, selectedRouteIdStr);
                     Log.Debug(logSalesParse);
                 }
                 catch (Exception exSalesParse)
                 {
                     string errorMsg = string.Format("Critical error parsing Sales XML into ViewModels. Exception: {0}", exSalesParse.ToString());
                Log.Error(errorMsg);
                     throw new Exception("Error parsing sales data from XML.", exSalesParse);
                 }
                 // --- End Parse XML ---

                 // --- 5. Update UI Collections ---
                 Log.Debug("Populating internal collections and refreshing UI...");

                 // Pre-sort collections before updating UI for better performance
                 var sortedSchedules = loadedSchedules.OrderBy(s => s.DepartureTime).ToList();
                 var sortedSales = loadedSales.OrderByDescending(s => s.SaleDate).ToList();

                 Action updateAction = delegate() {
                      if (this.IsDisposed) return;

                     // Update Available Schedules (for Add/Edit form lookup)
                     _availableRouteSchedules.Clear();
                     // Use AddRange for better performance with BindingList requires loop
                     foreach(var s in sortedSchedules) { _availableRouteSchedules.Add(s); }

                     // Update Sales Data Grid
                     _salesData.Clear();
                     foreach(var s in sortedSales) { _salesData.Add(s); }

                     // Refresh the grid control to show changes
                     gridControlSales.RefreshDataSource();
                 };

                 // Execute the update action safely on the UI thread
                 if (this.IsHandleCreated && !this.IsDisposed)
                 {
                     this.BeginInvoke(updateAction);
                 }
                 else if (!this.IsDisposed)
                 {
                     updateAction();
                 }
                 // --- End Update UI ---

                 stopwatch.Stop();
                 string logSuccess = string.Format("Synchronous data load completed successfully for route {0} in {1}ms. Loaded {2} sales, {3} schedule lookups.",
                     selectedRouteIdStr, stopwatch.ElapsedMilliseconds, loadedSales.Count, loadedSchedules.Count);
                 Log.Info(logSuccess);
             }
             catch (Exception ex)
             {
                 string criticalErrorMsg = string.Format("Critical error during synchronous data load sequence for route {0}. Exception: {1}", selectedRouteIdStr, ex.ToString());
                 Log.Error(criticalErrorMsg);

                 Action errorAction = delegate()
                 {
                     if (this.IsDisposed) return;

                     // Clear potentially partial data
                     _availableRouteSchedules.Clear();
                     _salesData.Clear();
                     gridControlSales.RefreshDataSource();

                     // Show error message to the user
                     XtraMessageBox.Show("Произошла критическая ошибка при загрузке данных продаж. См. лог." + Environment.NewLine + ex.Message,
                         "Критическая Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 };

                 if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(errorAction); }
                 else if (!this.IsDisposed) { errorAction(); }
             }
             finally
             {
                 // Cancel any pending tasks
                 try { cts.Cancel(); } catch { }
                 cts.Dispose();

                 if (client != null) client.Dispose();

                 // Ensure wait message box is closed
                 if (waitMessageBox != null && !waitMessageBox.IsDisposed)
                 {
                     Action closeWaitBoxFinal = delegate() { if (!waitMessageBox.IsDisposed) waitMessageBox.Close(); };
                     if (waitMessageBox.InvokeRequired) { waitMessageBox.BeginInvoke(closeWaitBoxFinal); } else { closeWaitBoxFinal(); }
                     waitMessageBox.Dispose();
                     _waitMessageLabel = null; // Clear label reference after closing
                 }

                 // Ensure timer is stopped and disposed if an exception occurred mid-load
                 if (_waitFormUpdateTimer != null) {
                     _waitFormUpdateTimer.Stop();
                     _waitFormUpdateTimer.Dispose();
                     _waitFormUpdateTimer = null;
                 }
                 _scheduleLoadInProgress = false; // Ensure flag is reset

                 // Reset loading state on UI thread
                 Action finalUiAction = delegate()
                 {
                     if (this.IsDisposed) {
                         Log.Debug("Form disposed before final UI state could be reset in finally block.");
                         return;
                     }
                     SetLoadingState(false);
                     Log.Debug("Finished final UI state reset after synchronous load sequence attempt.");
                 };

                 if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(finalUiAction); }
                 else if (!this.IsDisposed) { finalUiAction(); }
             }
         }

        // Removed LoadPrerequisitesSynchronously - logic merged into LoadDataSynchronously
        // Removed RefreshSalesDataSynchronously - logic merged into LoadDataSynchronously
        // Removed ApplyFiltersAndBindData - filtering is now part of LoadDataSynchronously (client-side) or handled by filter event handlers triggering LoadDataSynchronously

        private void gridViewSales_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
             if (e.Column.FieldName == "TotalAmount" && e.Value is double)
             {
                 e.DisplayText = ((double)e.Value).ToString("C2", CultureInfo.CurrentCulture); // Format as currency
             }
             // Keep other custom display text logic if needed
             else if (e.Column.FieldName == "SaleDate" && e.Value is DateTime)
             {
                  e.DisplayText = ((DateTime)e.Value).ToString("dd.MM.yyyy HH:mm"); // Include time maybe?
             }
             else if (e.Column.FieldName == "DepartureTime" && e.Value is DateTime && (DateTime)e.Value != DateTime.MinValue)
             {
                  e.DisplayText = ((DateTime)e.Value).ToString("dd.MM HH:mm");
             }
             else if (e.Column.FieldName == "ArrivalTime" && e.Value is DateTime && (DateTime)e.Value != DateTime.MinValue)
             {
                 e.DisplayText = ((DateTime)e.Value).ToString("dd.MM HH:mm");
             }
         }


        // Handles focusing rows for button state updates
        private void gridViewSales_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            UpdateButtonStates(); // Update buttons when row focus changes
        }

        // Placeholder for unbound columns if needed in the future
        private void gridViewSales_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            // Example: Combine Start/End Point if not done via ViewModel
            /*
            if (e.Column.FieldName == "RouteDisplay" && e.IsGetData) {
                SaleViewModel sale = e.Row as SaleViewModel;
                if (sale != null) {
                    e.Value = $"{sale.StartPoint} - {sale.EndPoint}";
                }
            }
            */
             // Add more unbound column logic here if necessary
             // Log.Trace("gridViewSales_CustomUnboundColumnData triggered for column: {0}", e.Column.FieldName); // Example trace log - NLog doesn't support structured logging easily here
        }


        private void gridViewSales_DoubleClick(object sender, EventArgs e)
        {
             // Get the clicked row handle
             GridView view = sender as GridView;
             System.Drawing.Point pt = view.GridControl.PointToClient(Control.MousePosition);
             DevExpress.XtraGrid.Views.Grid.ViewInfo.GridHitInfo info = view.CalcHitInfo(pt);

             // Check if a valid data row was double-clicked
             if (info.InRow || info.InRowCell)
             {
                 SaleViewModel selectedSale = view.GetRow(info.RowHandle) as SaleViewModel;
                 if (selectedSale != null)
                 {
                      string logMsg = string.Format("Row double-clicked for Sale ID: {0}. Opening edit form.", selectedSale.SaleId);
                      Log.Debug(logMsg);
                      // Call the Edit method or directly show the edit form
                      ShowEditSaleForm(selectedSale);
                 }
            }
        }

        private void UpdateButtonStates()
        {
             if (this.InvokeRequired)
             {
                 this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) UpdateButtonStates(); }));
                 return;
             }
              if (this.IsDisposed) return;

             bool isLoading = _isBusy;
             // Check if a valid row is selected in the grid view
             bool isRowSelected = (gridControlSales != null && !gridControlSales.IsDisposed && gridViewSales != null && gridViewSales.DataSource != null)
                                  ? gridViewSales.GetFocusedRow() is SaleViewModel
                                  : false;
             // Also check if prerequisites for adding are met (e.g., schedules loaded)
             bool canAdd = !isLoading && _availableRouteSchedules != null && _availableRouteSchedules.Any();


             // Enable/Disable buttons based on state
             if (btnAdd != null && !btnAdd.IsDisposed) btnAdd.Enabled = canAdd; // Enable Add only if schedules are loaded
             if (btnEdit != null && !btnEdit.IsDisposed) btnEdit.Enabled = !isLoading && isRowSelected && canAdd; // Need schedules for editing too
             if (btnDelete != null && !btnDelete.IsDisposed) btnDelete.Enabled = !isLoading && isRowSelected; // Delete doesn't need schedules
             if (btnRefresh != null && !btnRefresh.IsDisposed) btnRefresh.Enabled = !isLoading && _selectedRouteIdFilter.HasValue; // Enable Refresh only if a route is selected
             if (btnExport != null && !btnExport.IsDisposed) btnExport.Enabled = !isLoading && _salesData != null && _salesData.Any(); // Enable export if not loading and data exists

             // Enable/Disable filters
             // if (lueRouteFilter != null && !lueRouteFilter.IsDisposed) lueRouteFilter.Enabled = !isLoading; // If route filter exists
             if (dateFromFilter != null && !dateFromFilter.IsDisposed) dateFromFilter.Enabled = !isLoading;
             if (dateToFilter != null && !dateToFilter.IsDisposed) dateToFilter.Enabled = !isLoading;
             // if (btnApplyFilter != null && !btnApplyFilter.IsDisposed) btnApplyFilter.Enabled = !isLoading; // Remove if replaced by events
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
             string logMsg = "Add button clicked.";
             Log.Info(logMsg);
             // Check if prerequisite data (schedules for lookup) is available
             if (_availableRouteSchedules == null || !_availableRouteSchedules.Any())
             {
                 string warnMsg = "Add clicked but available route schedules are not loaded. Cannot open add form.";
                 Log.Warn(warnMsg);
                 XtraMessageBox.Show("Данные о доступных рейсах не загружены. Попробуйте обновить или выбрать другой маршрут.", "Данные Отсутствуют", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                 return;
             }
             ShowEditSaleForm(null); // Pass null for adding a new sale
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
             string logMsg = "Edit button clicked.";
             Log.Info(logMsg);
             var selectedSale = gridViewSales.GetFocusedRow() as SaleViewModel;
             if (selectedSale == null)
             {
                  string warnMsg = "Edit button clicked, but no sale selected in the grid.";
                  Log.Warn(warnMsg);
                 return; // Do nothing if no row is selected
             }
             // Check prerequisites again, although button state should handle this
             if (_availableRouteSchedules == null || !_availableRouteSchedules.Any())
             {
                 string warnMsg = "Edit clicked but available route schedules are not loaded. Cannot open edit form.";
                 Log.Warn(warnMsg);
                 XtraMessageBox.Show("Данные о доступных рейсах не загружены. Попробуйте обновить или выбрать другой маршрут.", "Данные Отсутствуют", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
             ShowEditSaleForm(selectedSale); // Pass the selected sale for editing
        }


        private void ShowEditSaleForm(SaleViewModel saleToEdit)
        {
             // Double-check prerequisites
             if (_availableRouteSchedules == null || !_availableRouteSchedules.Any() || !_selectedRouteIdFilter.HasValue)
             {
                 string errorMsg = "Необходимые данные (доступные рейсы для выбранного маршрута) не загружены. Добавление/редактирование продажи невозможно.";
                 Log.Error(errorMsg);
                 XtraMessageBox.Show(errorMsg, "Ошибка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  return;
             }


            using (var form = new XtraForm())
            {
                 bool isAdding = saleToEdit == null;
                 form.Text = isAdding ? "Добавить Продажу" : "Редактировать Продажу";
                 form.Width = 550; // Adjust size as needed
                form.Height = 400; 
                form.StartPosition = FormStartPosition.CenterParent;
                 form.FormBorderStyle = FormBorderStyle.FixedDialog; // Prevent resizing

                var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(15) };
                form.Controls.Add(panel);

                 // Layout variables
                int yPos = 20;
                int labelWidth = 150;
                 int controlWidth = 350;
                int spacing = 35;

                 // --- Controls ---

                 // Route Schedule Lookup
                 var scheduleLabel = new LabelControl { Text = "Рейс (Расписание):", Location = new Point(15, yPos), Width = labelWidth };
                 var scheduleCombo = new LookUpEdit
                 {
                     Location = new Point(labelWidth + 20, yPos),
                     Width = controlWidth,
                    Properties = {
                         // Use the pre-loaded list of schedules for the *selected* route
                         DataSource = _availableRouteSchedules,
                         DisplayMember = "DisplayName", // Show the formatted display name
                         ValueMember = "RouteScheduleId", // Store the ID
                         NullText = "[Выберите рейс...]",
                         ShowHeader = true, // Show headers in dropdown
                         SearchMode = SearchMode.AutoFilter, // Allow typing to search
                         AllowNullInput = DevExpress.Utils.DefaultBoolean.False // Must select a schedule
                     }
                 };
                 scheduleCombo.Properties.Columns.Add(new LookUpColumnInfo("DisplayName", "Рейс", 250));
                 scheduleCombo.Properties.Columns.Add(new LookUpColumnInfo("AvailableSeats", "Мест", 50));
                 panel.Controls.Add(scheduleLabel);
                 panel.Controls.Add(scheduleCombo);
                yPos += spacing;

                 // Seat Number
                 var seatLabel = new LabelControl { Text = "Номер Места:", Location = new Point(15, yPos), Width = labelWidth };
                 var seatEdit = new SpinEdit
                 {
                     Location = new Point(labelWidth + 20, yPos),
                     Width = controlWidth,
                     Properties = {
                         Mask = { EditMask = "d" }, // Integer mask
                         IsFloatValue = false,
                         MinValue = 1, // Seat numbers usually start from 1
                         MaxValue = 100, // Set a reasonable max based on bus capacity
                         AllowNullInput = DevExpress.Utils.DefaultBoolean.False // Seat number required
                     }
                 };
                 panel.Controls.Add(seatLabel);
                 panel.Controls.Add(seatEdit);
                yPos += spacing;

                 // Sale Date (Readonly or set automatically?) - Let's make it editable for flexibility
                 var saleDateLabel = new LabelControl { Text = "Дата Продажи:", Location = new Point(15, yPos), Width = labelWidth };
                 var saleDateEdit = new DateEdit
                 {
                     Location = new Point(labelWidth + 20, yPos),
                     Width = controlWidth / 2 - 5, // Half width for date
                     Properties = {
                         Mask = { EditMask = "dd.MM.yyyy HH:mm", UseMaskAsDisplayFormat = true }, // Include time
                         AllowNullInput = DevExpress.Utils.DefaultBoolean.False,
                         CalendarTimeEditing = DefaultBoolean.True // Allow time editing
                     }
                 };
                 panel.Controls.Add(saleDateLabel);
                 panel.Controls.Add(saleDateEdit);
                yPos += spacing;


                 // Price (Readonly, auto-filled from schedule?)
                 var priceLabel = new LabelControl { Text = "Цена:", Location = new Point(15, yPos), Width = labelWidth };
                 var priceEdit = new SpinEdit
                 {
                     Location = new Point(labelWidth + 20, yPos),
                     Width = controlWidth,
                     Properties = {
                         Mask = { EditMask = "c2", UseMaskAsDisplayFormat = true }, // Currency mask
                         IsFloatValue = true,
                         ReadOnly = true, // Price should be determined by the schedule
                         AllowNullInput = DevExpress.Utils.DefaultBoolean.False,
                         AppearanceReadOnly = { BackColor = SystemColors.Info } // Use SystemColors.Info
                     }
                 };
                 panel.Controls.Add(priceLabel);
                 panel.Controls.Add(priceEdit);
                 yPos += spacing;

                 // Sold To User (Optional field?) - Based on API model, default 'ФИЗ.ПРОДАЖА'
                  var userLabel = new LabelControl { Text = "Продано кому:", Location = new Point(15, yPos), Width = labelWidth };
                  var userEdit = new TextEdit {
                      Location = new Point(labelWidth + 20, yPos),
                      Width = controlWidth,
                      Text = "ФИЗ.ПРОДАЖА" // Default value
                  };
                  panel.Controls.Add(userLabel);
                  panel.Controls.Add(userEdit);
                  yPos += spacing;

                 // Sold To Phone (Optional field?)
                  var phoneLabel = new LabelControl { Text = "Телефон покупателя:", Location = new Point(15, yPos), Width = labelWidth };
                  var phoneEdit = new TextEdit {
                      Location = new Point(labelWidth + 20, yPos),
                      Width = controlWidth
                  };
                  panel.Controls.Add(phoneLabel);
                  panel.Controls.Add(phoneEdit);
                  yPos += spacing + 10; // Extra space before buttons


                 // --- Populate Controls ---
                 if (!isAdding && saleToEdit != null)
                 {
                     // Edit mode: Populate from existing sale
                     // Select the correct schedule in the combo box
                     scheduleCombo.EditValue = saleToEdit.RouteScheduleId;
                     // Ensure price is updated if schedule selected programmatically
                     priceEdit.Value = (decimal)saleToEdit.TotalAmount; // Use existing amount
                     seatEdit.Value = saleToEdit.SeatNumber;
                     saleDateEdit.DateTime = saleToEdit.SaleDate;
                     // Populate user/phone if those fields exist and are mapped in SaleViewModel
                     // We don't have TicketSoldToUser or Phone in SaleViewModel directly
                     // We might need to fetch the full Prodazha object or add fields to ViewModel if needed.
                     // For now, keep the default or let user edit them.
                     // userEdit.Text = saleToEdit.SoldToUser ?? "ФИЗ.ПРОДАЖА"; // Hypothetical
                     // phoneEdit.Text = saleToEdit.SoldToUserPhone ?? ""; // Hypothetical
                    }
                    else
                    {
                     // Add mode: Set defaults
                     saleDateEdit.DateTime = DateTime.Now; // Default sale date to now
                     priceEdit.Value = 0; // Price will be set when schedule is selected
                     seatEdit.Value = 1; // Default seat to 1
                 }

                 // --- Event Handlers (for auto-filling price) ---
                 scheduleCombo.EditValueChanged += (s, args) => {
                     LookUpEdit editor = s as LookUpEdit;
                     if (editor != null && editor.EditValue != null && editor.EditValue != DBNull.Value)
                     {
                         int selectedScheduleId = Convert.ToInt32(editor.EditValue);
                         var selectedSchedule = _availableRouteSchedules.FirstOrDefault(sch => sch.RouteScheduleId == selectedScheduleId);
                         if (selectedSchedule != null)
                         {
                             priceEdit.Value = (decimal)selectedSchedule.Price; // Update price display
                             // Optionally update seat MaxValue based on available seats?
                             // seatEdit.Properties.MaxValue = selectedSchedule.AvailableSeats > 0 ? selectedSchedule.AvailableSeats : 100;
                         } else {
                             priceEdit.Value = 0; // Reset if schedule not found
                         }
                     } else {
                         priceEdit.Value = 0; // Reset if selection cleared
                     }
                 };


                 // --- Save/Cancel Buttons ---
                 var btnSave = new SimpleButton { Text = isAdding ? "Добавить" : "Сохранить", Location = new Point(form.ClientSize.Width / 2 - 110, yPos), Width = 100 };
                 var btnCancel = new SimpleButton { Text = "Отмена", Location = new Point(form.ClientSize.Width / 2 + 10, yPos), Width = 100, DialogResult = DialogResult.Cancel };
                 panel.Controls.Add(btnSave);
                 panel.Controls.Add(btnCancel);
                 form.CancelButton = btnCancel; // Allow Esc key to cancel

                 // --- Save Button Logic ---
                 btnSave.Click += /*async*/ (s, args) => { // Keep synchronous for now, .Result will be used
                     // --- Validation ---
                     if (scheduleCombo.EditValue == null || scheduleCombo.EditValue == DBNull.Value || !(scheduleCombo.EditValue is int))
                     {
                         XtraMessageBox.Show("Пожалуйста, выберите действительный рейс (расписание).", "Ошибка Валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                         scheduleCombo.Focus();
                         return;
                     }
                     if (seatEdit.Value <= 0) {
                         XtraMessageBox.Show("Номер места должен быть положительным числом.", "Ошибка Валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                         seatEdit.Focus();
                         return;
                     }
                     if (saleDateEdit.DateTime == DateTime.MinValue) {
                          XtraMessageBox.Show("Пожалуйста, укажите корректную дату продажи.", "Ошибка Валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                          saleDateEdit.Focus();
                          return;
                     }
                     // Add more validation if needed (e.g., phone number format)

                     int selectedScheduleId = (int)scheduleCombo.EditValue;
                     var selectedScheduleData = _availableRouteSchedules.FirstOrDefault(sch => sch.RouteScheduleId == selectedScheduleId);
                     if (selectedScheduleData == null) {
                          string errorMsg = string.Format("Не удалось найти данные для выбранного рейса ID: {0}. Сохранение невозможно.", selectedScheduleId);
                          Log.Error(errorMsg);
                          XtraMessageBox.Show(errorMsg, "Ошибка данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
                         return;
                     }


                     // --- Prepare Data for API ---
                     // API Expects CreateTicketSaleModel or UpdateTicketSaleModel
                     // CreateTicketSaleModel needs TicketId. Update needs SaleId.
                     // This form works with RouteScheduleId and SeatNumber for Adding.

                     // PROBLEM: We need to find the TicketId matching the selected RouteScheduleId and SeatNumber.
                     // This requires a separate API call or a change in the CreateTicketSale endpoint.

                     // WORKAROUND for now: Assume the backend or another process handles finding/creating the ticket.
                     // We will attempt the POST/PUT as designed in the API sample, understanding it might fail
                     // if the TicketId isn't implicitly handled or found correctly by the API logic based on the Sale details.

                     string actionType = isAdding ? "Adding" : "Updating";
                     Log.Info(string.Format("Attempting Save action: {0}", actionType));

                     // --- API Call ---
                     HttpClient crudClient = null;
                     string apiUrl = "";
                     HttpContent content = null;
                    HttpResponseMessage response = null;

                    try
                    {
                         // Disable form during operation (synchronous call follows)
                         Action disableFormAction = delegate() {
                             if (form.IsDisposed) return;
                             form.Cursor = Cursors.WaitCursor;
                             btnSave.Enabled = false;
                             btnCancel.Enabled = false;
                             panel.Enabled = false;
                         };
                         if (form.InvokeRequired) { form.Invoke(disableFormAction); } else { disableFormAction(); }


                         crudClient = _apiClient.CreateClient();

                        if (isAdding)
                        {
                            // We need a TicketId for the POST /api/TicketSales endpoint.
                            // This form doesn't provide it directly. We can't proceed with the current API design.
                            // Show an error explaining this limitation.

                             string errMsgAdd = "Добавление новой продажи через эту форму пока не поддерживается. API требует ID Билета (TicketId), который не определяется на этой форме. Обратитесь к разработчику.";
                             Log.Error(errMsgAdd);
                             XtraMessageBox.Show(errMsgAdd, "Операция не поддерживается", MessageBoxButtons.OK, MessageBoxIcon.Error);
                             // Re-enable form immediately since we can't proceed
                             Action enableFormAction = delegate() {
                                  if (form.IsDisposed) return;
                                  form.Cursor = Cursors.Default;
                                  btnSave.Enabled = true;
                                  btnCancel.Enabled = true;
                                  panel.Enabled = true;
                             };
                             if (form.InvokeRequired) { form.BeginInvoke(enableFormAction); } else { enableFormAction(); }
                             return; // Stop processing the save click

                             /* // Hypothetical data if API used RouteScheduleId + Seat#
                             var addData = new {
                                 RouteScheduleId = selectedScheduleId, // Need API change
                                 SeatNumber = (int)seatEdit.Value,       // Need API change
                                 SaleDate = saleDateEdit.DateTime.ToUniversalTime(), // Send UTC
                                 TicketSoldToUser = userEdit.Text,
                                 TicketSoldToUserPhone = phoneEdit.Text
                             };
                             string jsonPayloadAdd = JsonConvert.SerializeObject(addData, _jsonSettings);
                             content = new StringContent(jsonPayloadAdd, Encoding.UTF8, "application/json");
                             apiUrl = string.Format("{0}/TicketSales/CreateBySchedule", _baseUrl); // NEEDS NEW API ENDPOINT
                             response = crudClient.PostAsync(apiUrl, content).Result; // Synchronous call
                             */
                         }
                         else // Updating existing sale
                         {
                             // Use PUT /api/TicketSales/{id} where id is SaleId
                             // The API Update model allows partial updates (SaleDate, User, Phone). TicketId is not required here.
                             // Correct anonymous type syntax for C# 4.0
                             var updateData = new
                             {
                                 // We don't need to send TicketId for PUT according to sample controller
                                 SaleDate = saleDateEdit.DateTime.ToUniversalTime(), // Send UTC
                                 TicketSoldToUser = userEdit.Text,
                                 TicketSoldToUserPhone = phoneEdit.Text
                             };
                             string jsonPayloadUpdate = JsonConvert.SerializeObject(updateData, _jsonSettings);
                             content = new StringContent(jsonPayloadUpdate, Encoding.UTF8, "application/json");
                             apiUrl = string.Format("{0}/TicketSales/{1}", _baseUrl, saleToEdit.SaleId); // Use SaleId for PUT
                             string logPut = string.Format("{0} sale. URL: {1}, Payload (trunc): {2}", actionType, apiUrl, (jsonPayloadUpdate.Length > 200 ? jsonPayloadUpdate.Substring(0, 200) + "..." : jsonPayloadUpdate));
                             Log.Debug(logPut);
                             response = crudClient.PutAsync(apiUrl, content).Result; // Synchronous call
                         }


                         // Process response (check response != null before accessing)
                         if (response != null) {
                             if (response.IsSuccessStatusCode)
                             {
                                  string logOk = string.Format("Sale {0} successful (Sale ID: {1}).", actionType.ToLower(), isAdding ? "(New - Not Supported)" : saleToEdit.SaleId.ToString());
                                  Log.Info(logOk);

                                  // Signal success and close form (UI thread)
                                  Action successAction = delegate() {
                                      if (form.IsDisposed) return;
                                      form.DialogResult = DialogResult.OK;
                                      form.Close();
                                  };
                                  if (form.InvokeRequired) { form.BeginInvoke(successAction); } else { successAction(); }

                                  // Reload data in the main grid asynchronously after closing
                                  BeginInvoke(new Action(LoadDataSynchronously));

                             }
                             else
                             {
                                 // Read error response synchronously
                                 var error = response.Content.ReadAsStringAsync().Result;
                                 string errorDetails = string.Format("Failed to {0} sale. Status: {1} ({2}), API Error: {3}", actionType.ToLower(), (int)response.StatusCode, response.ReasonPhrase, error);
                                 Log.Error(errorDetails);

                                  // Show error and re-enable form (UI thread)
                                  Action errorAction = delegate() {
                                      if (form.IsDisposed) return;
                                       XtraMessageBox.Show(string.Format("Не удалось {0} продажу: {1}{2}{3}",
                                           (isAdding ? "добавить" : "сохранить"),
                                           response.ReasonPhrase,
                                           Environment.NewLine,
                                           error),
                                           "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                       // Re-enable form for retry
                                       form.Cursor = Cursors.Default;
                                       btnSave.Enabled = true;
                                       btnCancel.Enabled = true;
                                       panel.Enabled = true;
                                  };
                                  if (form.InvokeRequired) { form.BeginInvoke(errorAction); } else { errorAction(); }
                             }
                         } else if (!isAdding) { // Only log error if update attempt was made and response was null
                              string errorMsgNull = "API response was null during update attempt.";
                              Log.Error(errorMsgNull);
                              Action errorAction = delegate() {
                                    if (form.IsDisposed) return;
                                    XtraMessageBox.Show("Не удалось получить ответ от сервера при обновлении.", "Ошибка сети", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    form.Cursor = Cursors.Default; btnSave.Enabled = true; btnCancel.Enabled = true; panel.Enabled = true;
                              };
                              if (form.InvokeRequired) { form.BeginInvoke(errorAction); } else { errorAction(); }
                         }
                    }
                     catch (Exception ex)
                    {
                         string errorDetails = string.Format("Exception during {0} sale: {1}", actionType.ToLower(), ex.ToString());
                         Log.Error(errorDetails);
                         // Show error and re-enable form (UI thread)
                         Action exceptionAction = delegate() {
                              if (form.IsDisposed) return;
                              XtraMessageBox.Show("Критическая ошибка при сохранении продажи: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                              // Re-enable form
                              form.Cursor = Cursors.Default;
                              btnSave.Enabled = true;
                              btnCancel.Enabled = true;
                              panel.Enabled = true;
                         };
                          if (form.InvokeRequired) { form.BeginInvoke(exceptionAction); } else { exceptionAction(); }
                     }
                     finally {
                         if (crudClient != null) crudClient.Dispose();
                         // Ensure form is re-enabled if an error occurred before response processing but after disabling
                         if (response == null || !response.IsSuccessStatusCode) {
                              Action finalEnableAction = delegate() {
                                   if (!form.IsDisposed && !panel.Enabled) { // Only re-enable if still disabled
                                       form.Cursor = Cursors.Default;
                                       btnSave.Enabled = true;
                                       btnCancel.Enabled = true;
                                       panel.Enabled = true;
                                   }
                              };
                              if (form.InvokeRequired) { form.BeginInvoke(finalEnableAction); } else { finalEnableAction(); }
                         }
                     }
                 }; // End Save Button Click

                 form.ShowDialog(this); // Show the form modally
             } // End using form
         }

         private /*async*/ void btnDelete_Click(object sender, EventArgs e) // Keep synchronous wrapper
         {
             string logStart = "Delete button clicked.";
             Log.Info(logStart);
             var selectedSale = gridViewSales.GetFocusedRow() as SaleViewModel;
             if (selectedSale == null)
             {
                 string warnMsg = "Delete button clicked, but no sale selected.";
                 Log.Warn(warnMsg);
                 return;
             }

             // Confirmation dialog
             var result = XtraMessageBox.Show(string.Format("Вы уверены, что хотите удалить продажу ID {0} ({1})?",
                 selectedSale.SaleId, selectedSale.RouteDescription),
                 "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

             if (result == DialogResult.Yes)
             {
                 string logConfirm = string.Format("User confirmed deletion for Sale ID: {0}", selectedSale.SaleId);
                 Log.Debug(logConfirm);
                 // Call synchronous delete method wrapper
                 DeleteSaleSynchronously(selectedSale);
                             }
                             else
                             {
                  string logCancel = string.Format("User cancelled deletion for Sale ID: {0}", selectedSale.SaleId);
                  Log.Debug(logCancel);
             }
         }

         // Synchronous wrapper for the delete operation
         private void DeleteSaleSynchronously(SaleViewModel saleToDelete)
         {
             if (saleToDelete == null) {
                 string warnMsg = "DeleteSaleSynchronously called with null sale.";
                 Log.Warn(warnMsg);
                 return;
             }

             string logAttempt = string.Format("Attempting to delete sale ID: {0}", saleToDelete.SaleId);
             Log.Debug(logAttempt);
             SetLoadingState(true); // Show loading state during delete
             HttpClient crudClient = null;
             HttpResponseMessage response = null;
             bool success = false;
             string apiError = null;

             try
             {
                 crudClient = _apiClient.CreateClient();
                 // Use the correct API endpoint for deleting a sale by ID
                 var apiUrl = string.Format("{0}/TicketSales/{1}", _baseUrl, saleToDelete.SaleId);
                 string logUrl = string.Format("Sending DELETE request to: {0}", apiUrl);
                 Log.Debug(logUrl);

                 // Perform DELETE request synchronously
                 response = crudClient.DeleteAsync(apiUrl).Result;

                 if (response.IsSuccessStatusCode)
                 {
                     success = true;
                     string logOk = string.Format("Sale deleted successfully via API: ID {0}", saleToDelete.SaleId);
                     Log.Info(logOk);
                        }
                        else
                        {
                     // Read error response synchronously
                     apiError = response.Content.ReadAsStringAsync().Result;
                     string errorDetails = string.Format("Failed to delete sale ID {0}. Status: {1} ({2}), API Error: {3}", saleToDelete.SaleId, (int)response.StatusCode, response.ReasonPhrase, apiError);
                     Log.Error(errorDetails);
                 }
            }
            catch (Exception ex)
            {
                 string errorMsg = string.Format("Exception deleting sale ID {0}. Exception: {1}", saleToDelete.SaleId, ex.ToString());
                 Log.Error(errorMsg);
                 apiError = "Критическая ошибка: " + ex.Message; // Assign exception message to show
                 success = false;
            }
             finally
             {
                if (crudClient != null) crudClient.Dispose();

                // Update UI on the UI thread
                Action updateUiAction = delegate()
                {
                    if (this.IsDisposed) return;

                      if (success)
                      {
                        XtraMessageBox.Show("Продажа успешно удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // Remove from the local data source and refresh grid
                        bool removed = _salesData.Remove(saleToDelete);
                        if (removed)
                        {
                            gridControlSales.RefreshDataSource(); // Refresh grid display
                        }
                        else
                        {
                            string warnRemove = string.Format("Sale ID {0} was deleted via API but not found in local BindingList.", saleToDelete.SaleId);
                            Log.Warn(warnRemove);
                            LoadDataSynchronously(); // Fallback to full reload if remove fails
                        }
                    }
                    else
                    {
                        // Show API error or exception message
                        XtraMessageBox.Show(string.Format("Ошибка при удалении продажи: {0}{1}{2}",
                            (response != null ? response.ReasonPhrase : "N/A"), // C# 4.0 compatible null check
                            Environment.NewLine,
                            apiError ?? "Неизвестная ошибка"), // Show the captured error string
                            "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    SetLoadingState(false); // Reset loading state after operation completes or fails
                };

                if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(updateUiAction); }
                else if (!this.IsDisposed) { updateUiAction(); }
            }
         }
         // Removed async Task DeleteSaleAsync

        private void btnRefresh_Click(object sender, EventArgs e)
        {
             string logMsg = "Refresh button clicked.";
             Log.Info(logMsg);
             // Check if a route is selected before refreshing
             if (!_selectedRouteIdFilter.HasValue)
             {
                 string warnMsg = "Refresh clicked, but no route is selected in the filter.";
                 Log.Warn(warnMsg);
                 XtraMessageBox.Show("Пожалуйста, сначала выберите маршрут.", "Требуется Маршрут", MessageBoxButtons.OK, MessageBoxIcon.Information);
                 return;
             }
             LoadDataSynchronously(); // Reload data for the current filters
         }

        // Removed btnApplyFilter_Click - replaced by dateFilter_EditValueChanged

         // Handles changes in either date filter
         private void dateFilter_EditValueChanged(object sender, EventArgs e)
         {
              // Prevent triggering reload during initial setup or while already loading
              if (_isBusy || !_formLoadComplete) return;

              DateEdit editor = sender as DateEdit;
              if (editor != null) {
                  string logMsg = string.Format("Date filter '{0}' changed. Triggering data reload.", editor.Name);
                  Log.Debug(logMsg);
                  // Reload data based on the new date range (and existing route filter)
                  LoadDataSynchronously();
              }
         }

         // Add lueRouteFilter_EditValueChanged if the route filter lookup is added to the designer
         /*
         private void lueRouteFilter_EditValueChanged(object sender, EventArgs e)
         {
               if (_isBusy || !_formLoadComplete) return;

             LookUpEdit editor = sender as LookUpEdit;
             if (editor != null) {
                 long? newlySelectedRouteId = null;
                 // Use Text property comparison primarily as EditValue might be unreliable
                 string currentDisplayText = editor.Text;
                 Marshut selectedRouteObject = _allRoutes.FirstOrDefault(r =>
                                  string.Equals(r.StartPoint, currentDisplayText, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(string.Format("{0} -> {1}", r.StartPoint, r.EndPoint), currentDisplayText, StringComparison.OrdinalIgnoreCase)
                                  );

                 if (selectedRouteObject != null) {
                     newlySelectedRouteId = selectedRouteObject.RouteId;
                 }
                 else if (editor.EditValue != null && editor.EditValue != DBNull.Value && editor.EditValue is long) {
                    // Fallback to EditValue if text lookup failed
                     newlySelectedRouteId = Convert.ToInt64(editor.EditValue);
                 }


                 // Reload only if the value actually changed
                 if (_selectedRouteIdFilter != newlySelectedRouteId)
                 {
                     string logMsg = string.Format("Route filter changed via UI. New Route ID: {0}. Triggering data reload.", newlySelectedRouteId.HasValue ? newlySelectedRouteId.Value.ToString() : "null");
                     Log.Debug(logMsg);
                     _selectedRouteIdFilter = newlySelectedRouteId; // Update the stored filter ID
                     LoadDataSynchronously(); // Trigger reload
                 }
                 else {
                     string logMsgSame = string.Format("Route filter EditValueChanged fired, but resolved value ({0}) is the same as current filter. No reload triggered.", newlySelectedRouteId.HasValue ? newlySelectedRouteId.Value.ToString() : "null");
                     Log.Debug(logMsgSame);
                 }
             }
         }
         */


        private void btnExport_Click(object sender, EventArgs e)
        {
             string logMsg = "Export button clicked.";
             Log.Info(logMsg);
             try
             {
                 using (SaveFileDialog saveDialog = new SaveFileDialog())
                 {
                     saveDialog.Filter = "Excel (2007+) File (*.xlsx)|*.xlsx|Excel (97-2003) File (*.xls)|*.xls|CSV File (*.csv)|*.csv";
                     saveDialog.Title = "Экспорт Продаж";
                     saveDialog.FileName = string.Format("Sales_Export_{0:yyyyMMddHHmmss}", DateTime.Now);

                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                         string exportFilePath = saveDialog.FileName;
                         string fileExt = System.IO.Path.GetExtension(exportFilePath).ToLowerInvariant();

                         // Ensure grid is visible and has data
                         if (gridViewSales == null || gridViewSales.RowCount == 0)
                         {
                              string warnMsg = "No data available in the grid to export.";
                              Log.Warn(warnMsg);
                              XtraMessageBox.Show(warnMsg, "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                             return;
                         }

                         Log.Debug(string.Format("Starting export to {0} format at: {1}", fileExt, exportFilePath));

                         // Use DevExpress exporting capabilities
                         if (fileExt == ".xlsx")
                         {
                             gridViewSales.ExportToXlsx(exportFilePath, new XlsxExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG });
                         }
                         else if (fileExt == ".xls")
                         {
                             gridViewSales.ExportToXls(exportFilePath, new XlsExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG });
                         }
                         else if (fileExt == ".csv")
                         {
                             gridViewSales.ExportToCsv(exportFilePath, new CsvExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG });
                         }

                          string successMsg = string.Format("Data successfully exported to: {0}", exportFilePath);
                          Log.Info(successMsg);
                          XtraMessageBox.Show(successMsg, "Экспорт Успешен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                      } else {
                           string cancelMsg = "Export cancelled by user.";
                           Log.Debug(cancelMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                   string errorMsg = string.Format("Error during export: {0}", ex.ToString());
                   Log.Error(errorMsg);
                   XtraMessageBox.Show("Произошла ошибка во время экспорта данных: " + ex.Message, "Ошибка Экспорта", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            }

        // Timer Tick event handler to update the wait message label
        private void WaitFormUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!_scheduleLoadInProgress || _waitMessageLabel == null || _waitMessageLabel.IsDisposed)
            {
                // Stop the timer if it's still running but shouldn't be
                if (_waitFormUpdateTimer != null && _waitFormUpdateTimer.Enabled) {
                    _waitFormUpdateTimer.Stop();
                    Log.Warn("Wait form timer stopped because schedule load is no longer in progress or label is invalid.");
                }
                return;
            }

            try
            {
                TimeSpan elapsed = DateTime.Now - _scheduleLoadStartTime;
                string estimateString = "(оценка времени...)";

                // Calculate estimate only after the first page is fetched and total pages are known
                if (_scheduleLoadCurrentPage > 1 && _scheduleLoadTotalPages > 1)
                {
                    // Use the *actual* current page number being processed (or about to be) for calculation
                    int completedPages = _scheduleLoadCurrentPage - 1;
                    if (completedPages > 0) {
                        double avgTimePerPage = elapsed.TotalSeconds / (double)completedPages;
                        double estimatedTotalSeconds = avgTimePerPage * _scheduleLoadTotalPages;
                        TimeSpan estimatedRemaining = TimeSpan.FromSeconds(Math.Max(0, estimatedTotalSeconds - elapsed.TotalSeconds));
                        // Format as MM:SS or HH:MM:SS if very long
                        if (estimatedRemaining.TotalHours >= 1) {
                             estimateString = string.Format("~{0:hh\\:mm\\:ss} осталось", estimatedRemaining);
                        } else {
                             estimateString = string.Format("~{0:mm} мин {1:ss} сек осталось", estimatedRemaining, estimatedRemaining);
                        }
                    }
                }

                // Ensure page numbers are within valid range for display
                int displayCurrentPage = Math.Min(_scheduleLoadCurrentPage, _scheduleLoadTotalPages);

                string progressText = string.Format(
                    "Загрузка данных (Шаг 1/2: Расписания)...{0}Маршрут: {1}{0}Страница: {2} / {3}{0}{4}",
                    Environment.NewLine,
                    _selectedRouteIdFilter.HasValue ? _selectedRouteIdFilter.Value.ToString() : "?", // Handle null case safely
                    displayCurrentPage, // Show current page, capped at total
                    _scheduleLoadTotalPages,
                    estimateString
                );

                // Update the label (already on UI thread due to Forms.Timer)
                _waitMessageLabel.Text = progressText;
            }
            catch (Exception exLabelUpdate)
            {
                // Log error but don't crash the timer
                Log.Warn(string.Format("Error updating wait message label in Timer Tick: {0}", exLabelUpdate.Message));
            }
        }
    }
} 