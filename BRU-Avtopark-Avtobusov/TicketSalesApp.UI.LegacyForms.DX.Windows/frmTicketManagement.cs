using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using System.Net.Http;
using System.Web;
using DevExpress.XtraGrid.Views.Base;
using NLog;
using TicketSalesApp.Core.Models;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Xml;
using System.Threading;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmTicketManagement : DevExpress.XtraEditors.XtraForm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ApiClientService _apiClientService;
        private readonly JsonSerializerSettings _jsonSettings;
        private readonly string _baseUrl;
        private List<Bilet> _allTicketsData;
        private List<RouteSchedules> _routeSchedules;
        private bool _isBusy = false;
        private long? _selectedRouteIdFilter = null;
        private bool _formLoadComplete = false;
        private List<Marshut> _allRoutes = new List<Marshut>();

        public frmTicketManagement()
        {
            InitializeComponent();

            _apiClientService = ApiClientService.Instance;
            _jsonSettings = new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            _baseUrl = "http://localhost:5000";
            _allTicketsData = new List<Bilet>();
            _routeSchedules = new List<RouteSchedules>();
            _allRoutes = new List<Marshut>();

            // Configure lookup editor for route schedule filter
            lueRouteScheduleFilter.Properties.DataSource = null;
            lueRouteScheduleFilter.Properties.DisplayMember = "StartPoint";
            lueRouteScheduleFilter.Properties.ValueMember = "RouteScheduleId";
            lueRouteScheduleFilter.Properties.NullText = "[Все рейсы]";

            // Configure grid view
            gridViewTickets.OptionsBehavior.Editable = false;
            gridViewTickets.OptionsBehavior.ReadOnly = true;
            gridViewTickets.OptionsView.ShowGroupPanel = false;
            gridViewTickets.OptionsView.ColumnAutoWidth = false;
            gridViewTickets.OptionsFind.AlwaysVisible = true;

            // Set up event handlers
            gridViewTickets.CustomColumnDisplayText += new CustomColumnDisplayTextEventHandler(gridViewTickets_CustomColumnDisplayText);
            gridViewTickets.FocusedRowChanged += new FocusedRowChangedEventHandler(GridViewTickets_FocusedRowChanged);
            btnApplyFilter.Click += new EventHandler(btnApplyFilter_Click);
            lueRouteScheduleFilter.EditValueChanged += new EventHandler(lueRouteScheduleFilter_EditValueChanged);
            this.Load += new EventHandler(frmTicketManagement_Load);
            this.FormClosing += FrmTicketManagement_FormClosing;

            // Register for auth token changes
            _apiClientService.OnAuthTokenChanged += HandleAuthTokenChanged;
            
            UpdateButtonStates();
        }

        private void FrmTicketManagement_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Debug("Form closing.");
            _apiClientService.OnAuthTokenChanged -= HandleAuthTokenChanged;
        }

        private void HandleAuthTokenChanged(object sender, string token)
        {
            Log.Debug("Auth token changed, triggering synchronous data reload.");
            if (this.Visible && this.IsHandleCreated)
            {
                this.BeginInvoke(new Action(delegate {
                    try
                    {
                        LoadDataSynchronously();
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = string.Format("Error during background refresh triggered by token change. Exception: {0}", ex.ToString());
                        Log.Error(errorMsg);
                    }
                }));
            }
        }

        private void ConfigureGridView()
        {
            gridViewTickets.OptionsBehavior.Editable = false;
            gridViewTickets.OptionsBehavior.ReadOnly = true;
            gridViewTickets.OptionsView.ShowGroupPanel = false;
            gridViewTickets.OptionsFind.AlwaysVisible = true;

            lueRouteScheduleFilter.Properties.DataSource = null;
            lueRouteScheduleFilter.Properties.DisplayMember = "StartPoint";
            lueRouteScheduleFilter.Properties.ValueMember = "RouteScheduleId";
            lueRouteScheduleFilter.Properties.NullText = "[Все рейсы]";
            lueRouteScheduleFilter.Properties.BestFitMode = BestFitMode.BestFitResizePopup;
            lueRouteScheduleFilter.Properties.SearchMode = SearchMode.AutoComplete;
            lueRouteScheduleFilter.Properties.Columns.Clear();
            lueRouteScheduleFilter.Properties.Columns.Add(new LookUpColumnInfo("RouteScheduleId", "ID", 50));
            lueRouteScheduleFilter.Properties.Columns.Add(new LookUpColumnInfo("StartPoint", "Начало", 150));
            lueRouteScheduleFilter.Properties.Columns.Add(new LookUpColumnInfo("EndPoint", "Конец", 150));
            lueRouteScheduleFilter.Properties.ShowHeader = true;
        }

        private void frmTicketManagement_Load(object sender, EventArgs e)
        {
            Log.Debug("frmTicketManagement_Load event triggered.");

            if (_formLoadComplete)
            {
                Log.Warn("frmTicketManagement_Load fired again after initial load completed. Ignoring.");
                return;
            }

            bool initialRouteSelected = false;
            try
            {
                initialRouteSelected = LoadRoutesAndSelectFilter();

                if (initialRouteSelected)
                {
                    Log.Debug("Initial route selected. Proceeding with main data load.");
                    LoadDataSynchronously();
                    _formLoadComplete = true;
                    Log.Info("Initial form load sequence completed successfully.");
                }
                else
                {
                    Log.Error("Failed to load initial routes or user cancelled selection. Aborting form load.");
                    XtraMessageBox.Show("Не удалось загрузить список маршрутов или выбор был отменен. Форма не может быть загружена.", "Ошибка Инициализации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) this.Close(); }));
                }
            }
            catch (Exception ex)
            {
                _formLoadComplete = true;
                string errorMsg = string.Format("Critical error during form load sequence. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                XtraMessageBox.Show(string.Format("Произошла критическая ошибка при загрузке формы: {0}\nФорма будет закрыта.", ex.Message), "Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) this.Close(); }));
            }
        }

        private bool LoadRoutesAndSelectFilter()
        {
            Log.Info("Starting initial route loading and selection process for ticket management...");
            SetLoadingState(true);

            XtraForm waitMessageBox = null;
            HttpClient client = null;
            string routesJsonRaw = null;
            XDocument routesXml = null;
            List<Marshut> loadedRoutes = new List<Marshut>();

            try
            {
                waitMessageBox = new XtraForm
                {
                    Text = "Загрузка Маршрутов...", 
                    StartPosition = FormStartPosition.CenterScreen, 
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false, 
                    MinimizeBox = false, 
                    Size = new Size(320, 120), 
                    ControlBox = false
                };
                var label = new Label { Text = "Загрузка списка маршрутов...", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                waitMessageBox.Controls.Add(label);
                if (this.IsHandleCreated && !this.IsDisposed) waitMessageBox.Show(this); else waitMessageBox.Show();
                Application.DoEvents();

                client = _apiClientService.CreateClient();

                Log.Debug("Fetching Routes synchronously for initial selection...");
                var routesApiUrl = string.Format("{0}/api/Routes", _baseUrl);
                HttpResponseMessage routesResponse = client.GetAsync(routesApiUrl).Result;

                if (routesResponse.IsSuccessStatusCode)
                {
                    byte[] routesBytes = routesResponse.Content.ReadAsByteArrayAsync().Result;
                    routesJsonRaw = Encoding.UTF8.GetString(routesBytes);
                    Log.Debug("Initial Routes JSON fetched successfully.");
                }
                else
                {
                    throw new Exception("Failed to load initial Routes: " + routesResponse.ReasonPhrase);
                }

                if (!string.IsNullOrEmpty(routesJsonRaw))
                {
                    routesXml = ProcessJsonToXml(routesJsonRaw, "Routes");
                    Log.Debug("Initial Routes JSON processed for XML conversion.");
                }
                else
                {
                    Log.Warn("Initial routesJsonRaw was null or empty.");
                    throw new Exception("Initial Routes JSON was empty.");
                }

                foreach (XElement routeNode in routesXml.Root.Elements("Routes"))
                {
                    try
                    {
                        if (!routeNode.HasElements) continue;
                        Marshut route = new Marshut();
                        long routeId;
                        if (routeNode.Element("routeId") != null && long.TryParse(routeNode.Element("routeId").Value, out routeId))
                        {
                            route.RouteId = routeId;
                        } else continue;
                        route.StartPoint = (routeNode.Element("startPoint") != null) ? routeNode.Element("startPoint").Value : string.Empty;
                        route.EndPoint = (routeNode.Element("endPoint") != null) ? routeNode.Element("endPoint").Value : string.Empty;
                        loadedRoutes.Add(route);
                    }
                    catch (Exception exNode) 
                    { 
                        Log.Error(string.Format("Error parsing individual Route XML node during pre-load: {0}. Node: {1}", exNode.ToString(), routeNode.ToString())); 
                    }
                }
                Log.Debug(string.Format("Parsed {0} initial routes from XML.", loadedRoutes.Count));
                
                if (!loadedRoutes.Any()) 
                {
                    throw new Exception("No routes were loaded after parsing XML.");
                }

                _allRoutes = loadedRoutes;

                if (waitMessageBox != null && !waitMessageBox.IsDisposed)
                {
                    Action closeWaitBox = delegate() { if (!waitMessageBox.IsDisposed) waitMessageBox.Close(); };
                    if (waitMessageBox.InvokeRequired) { waitMessageBox.BeginInvoke(closeWaitBox); } else { closeWaitBox(); }
                    waitMessageBox.Dispose();
                    waitMessageBox = null;
                }

                using (var selectionForm = new XtraForm())
                {
                    selectionForm.Text = "Выберите Маршрут";
                    selectionForm.StartPosition = FormStartPosition.CenterParent;
                    selectionForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    selectionForm.MinimizeBox = false; 
                    selectionForm.MaximizeBox = false; 
                    selectionForm.ControlBox = false;
                    selectionForm.Size = new Size(450, 180);

                    var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(15) };
                    selectionForm.Controls.Add(panel);

                    var selectLabel = new LabelControl { Text = "Выберите маршрут для загрузки билетов:", Location = new Point(15, 20), AutoSize = true };
                    panel.Controls.Add(selectLabel);

                    var routeCombo = new LookUpEdit
                    {
                        Location = new Point(15, 45), 
                        Width = 400,
                        Properties = {
                            DataSource = loadedRoutes.OrderBy(r => r.StartPoint).ToList(),
                            DisplayMember = "StartPoint", 
                            ValueMember = "RouteId", 
                            NullText = "[Выберите маршрут...]",
                            ShowHeader = true, 
                            SearchMode = SearchMode.AutoComplete
                        }
                    };
                    routeCombo.Properties.Columns.Add(new LookUpColumnInfo("RouteId", "ID", 50));
                    routeCombo.Properties.Columns.Add(new LookUpColumnInfo("StartPoint", "Начало", 150));
                    routeCombo.Properties.Columns.Add(new LookUpColumnInfo("EndPoint", "Конец", 150));
                    panel.Controls.Add(routeCombo);

                    var txtHiddenSelectedRouteId = new TextEdit { 
                        Name = "txtHiddenSelectedRouteId", 
                        Location = new Point(-200, -200), 
                        Visible = false, 
                        Width = 10 
                    };
                    panel.Controls.Add(txtHiddenSelectedRouteId);

                    var btnOk = new SimpleButton { Text = "OK", Location = new Point(selectionForm.ClientSize.Width - 115, 100), Width = 100 };
                    panel.Controls.Add(btnOk);
                    selectionForm.AcceptButton = btnOk;

                    btnOk.Click += (s, args) => {
                        string selectedDisplayText = routeCombo.Text;
                        object selectedValue = routeCombo.EditValue;
                        
                        if (selectedValue != null && selectedValue != DBNull.Value && 
                            !string.IsNullOrEmpty(selectedDisplayText) && 
                            selectedDisplayText != "[Выберите маршрут...]")
                        {
                            Marshut selectedRouteObject = loadedRoutes.FirstOrDefault(r => r.StartPoint == selectedDisplayText);
                            if (selectedRouteObject != null)
                            {
                                _selectedRouteIdFilter = selectedRouteObject.RouteId;
                                txtHiddenSelectedRouteId.Text = _selectedRouteIdFilter.Value.ToString();
                                Log.Debug(string.Format("Route ID {0} determined via display text '{1}' lookup and stored.", 
                                    _selectedRouteIdFilter.Value, selectedDisplayText));
                                selectionForm.DialogResult = DialogResult.OK;
                                selectionForm.Close();
                }
                else
                {
                                string errorMsg = string.Format("Could not find route object matching display text: {0}", selectedDisplayText);
                                Log.Error(errorMsg);
                                XtraMessageBox.Show("Не удалось найти выбранный маршрут по отображаемому имени. Пожалуйста, попробуйте снова.", 
                                    "Ошибка Поиска", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            XtraMessageBox.Show("Пожалуйста, выберите маршрут.", "Требуется выбор", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    };

                    selectionForm.ShowDialog(this);
                }

                if (_selectedRouteIdFilter.HasValue)
                {
                    Log.Info(string.Format("Route ID {0} will be used for loading data.", _selectedRouteIdFilter.Value));
                    return true;
                }
                else
                {
                    Log.Warn("Route selection was cancelled or failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Error during initial route loading/selection. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                XtraMessageBox.Show("Ошибка при загрузке списка маршрутов: " + ex.Message, "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                if (client != null) client.Dispose();
                if (waitMessageBox != null && !waitMessageBox.IsDisposed)
                {
                    Action closeWaitBoxFinal = delegate() { if (!waitMessageBox.IsDisposed) waitMessageBox.Close(); };
                    if (waitMessageBox.InvokeRequired) { waitMessageBox.BeginInvoke(closeWaitBoxFinal); } else { closeWaitBoxFinal(); }
                    waitMessageBox.Dispose();
                }
                
                Action finalUiAction = delegate() { 
                    if (this.IsDisposed) { 
                        Log.Debug("Form disposed before final UI state could be reset."); 
                        return; 
                    } 
                    SetLoadingState(false); 
                    Log.Debug("Finished final UI state reset after synchronous load sequence attempt."); 
                };
                this.BeginInvoke(finalUiAction);
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) SetLoadingState(isLoading); }));
                return;
            }

            if (this.IsDisposed || gridControlTickets == null || gridControlTickets.IsDisposed)
            {
                Log.Warn("SetLoadingState called but form or controls are disposed.");
                return;
            }

            Log.Debug(isLoading ? "Setting UI to loading state." : "Setting UI to normal state.");
            _isBusy = isLoading;
            Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;
            gridControlTickets.Enabled = !isLoading;
            lueRouteScheduleFilter.Enabled = !isLoading;
            btnApplyFilter.Enabled = !isLoading;
            btnRefresh.Enabled = !isLoading;
           
            if (!isLoading)
            {
                UpdateButtonStates();
            }
            else
            {
                if (btnViewDetails != null && !btnViewDetails.IsDisposed) btnViewDetails.Enabled = false;
                if (btnCancelTicket != null && !btnCancelTicket.IsDisposed) btnCancelTicket.Enabled = false;
            }
        }
        private void LoadDataSynchronously()
        {
            if (!_selectedRouteIdFilter.HasValue)
            {
                Log.Warn("LoadDataSynchronously called but no initial route selected. Aborting.");
                Action showMsgAction = delegate() {
                    if (!this.IsDisposed) {
                        XtraMessageBox.Show("Маршрут не выбран. Невозможно загрузить данные.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        SetLoadingState(false);
                    }
                };
                if (this.InvokeRequired) { this.BeginInvoke(showMsgAction); } else { showMsgAction(); }
                return;
            }

            string selectedRouteIdStr = _selectedRouteIdFilter.Value.ToString();
            Log.Info("Starting synchronous data load sequence for selected route: {0}...", selectedRouteIdStr);
            SetLoadingState(true);

            XtraForm waitMessageBox = null;
            try
            {
                Action showWaitBoxAction = delegate() {
                    if (this.IsDisposed) return;
                    waitMessageBox = new XtraForm {
                        Text = "Загрузка данных...",
                        StartPosition = FormStartPosition.CenterScreen,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false, MinimizeBox = false,
                        Size = new Size(320, 120),
                        ControlBox = false
                    };
                    var label = new Label {
                        Text = string.Format("Загрузка данных для маршрута {0}... Пожалуйста, подождите...", selectedRouteIdStr),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    waitMessageBox.Controls.Add(label);
                    waitMessageBox.Show(this);
                };
                if (this.InvokeRequired) { this.Invoke(showWaitBoxAction); } else { showWaitBoxAction(); }
            }
            catch (Exception)
            {
                // Continue without wait box
            }

            HttpClient client = null;
            string schedulesJsonRaw = null;
            string ticketsJsonRaw = null;
            string routesJsonRaw = null;
            XDocument schedulesXml = XDocument.Parse("<Root><Schedules></Schedules></Root>");
            XDocument ticketsXml = XDocument.Parse("<Root><Tickets></Tickets></Root>");
            XDocument routesXml = XDocument.Parse("<Root><Routes></Routes></Root>");

            List<RouteSchedules> loadedSchedules = new List<RouteSchedules>();
            List<Bilet> loadedTickets = new List<Bilet>();
            List<Marshut> loadedRoutes = new List<Marshut>();

            try
            {
                client = _apiClientService.CreateClient();

                try
                {
                    var query = HttpUtility.ParseQueryString(string.Empty);
                    query["routeId"] = _selectedRouteIdFilter.Value.ToString();
                    string schedulesApiUrl = string.Format("{0}/api/RouteSchedules/search?{1}", _baseUrl, query.ToString());
                    Log.Debug(string.Format("Fetching Route Schedules synchronously from: {0}", schedulesApiUrl));
                    HttpResponseMessage schedulesResponse = client.GetAsync(schedulesApiUrl).Result;

                    if (schedulesResponse.IsSuccessStatusCode)
                    {
                        byte[] schedulesBytes = schedulesResponse.Content.ReadAsByteArrayAsync().Result;
                        schedulesJsonRaw = Encoding.UTF8.GetString(schedulesBytes);
                        Log.Debug("Route Schedules JSON fetched successfully (Payload size: {0} bytes).", schedulesBytes.Length);
                    }
                    else
                    {
                        throw new Exception(string.Format("Failed to load Route Schedules: {0}", schedulesResponse.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error fetching Route Schedules for route {0}. Exception: {1}", _selectedRouteIdFilter.Value, ex.ToString());
                    Log.Error(errorMsg);
                }

                try
                {
                    string routesApiUrl = string.Format("{0}/api/Routes", _baseUrl);
                    Log.Debug(string.Format("Fetching Routes synchronously from: {0}", routesApiUrl));
                    HttpResponseMessage routesResponse = client.GetAsync(routesApiUrl).Result;
                    if (routesResponse.IsSuccessStatusCode)
                    {
                        byte[] routesBytes = routesResponse.Content.ReadAsByteArrayAsync().Result;
                        routesJsonRaw = Encoding.UTF8.GetString(routesBytes);
                        Log.Debug("Routes JSON fetched successfully.");
                }
                else
                {
                        throw new Exception(string.Format("Failed to load Routes: {0}", routesResponse.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error fetching Routes. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                }

                try
                {
                    string ticketsApiUrl = string.Format("{0}/api/Tickets", _baseUrl);
                    Log.Debug(string.Format("Fetching Tickets synchronously from: {0}", ticketsApiUrl));
                    HttpResponseMessage ticketsResponse = client.GetAsync(ticketsApiUrl).Result;

                    if (ticketsResponse.IsSuccessStatusCode)
                    {
                        byte[] ticketsBytes = ticketsResponse.Content.ReadAsByteArrayAsync().Result;
                        ticketsJsonRaw = Encoding.UTF8.GetString(ticketsBytes);
                        Log.Debug("Tickets JSON fetched successfully (Payload size: {0} bytes).", ticketsBytes.Length);
                    }
                    else
                    {
                        throw new Exception(string.Format("Failed to load Tickets: {0}", ticketsResponse.ReasonPhrase));
                    }
            }
            catch (Exception ex)
            {
                    string errorMsg = string.Format("Error fetching Tickets. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                }

                Log.Debug("Manually handling arrays and converting to XML...");
                try
                {
                    if (!string.IsNullOrEmpty(schedulesJsonRaw))
                    {
                        schedulesXml = ProcessJsonToXml(schedulesJsonRaw, "Schedules");
                        Log.Debug("Schedules JSON processed for XML conversion.");
                    }
                    else
                    {
                        Log.Warn("schedulesJsonRaw was null or empty, using default empty Schedules XML.");
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Schedules JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                    XtraMessageBox.Show("Произошла ошибка при обработке данных расписаний. Данные могут быть неполными.", "Ошибка Обработки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                try
                {
                    if (!string.IsNullOrEmpty(routesJsonRaw))
                    {
                        routesXml = ProcessJsonToXml(routesJsonRaw, "Routes");
                        Log.Debug("Routes JSON processed for XML conversion.");
                    }
                    else
                    {
                        Log.Warn("routesJsonRaw was null or empty, using default empty Routes XML.");
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Routes JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                }

                try
                {
                    if (!string.IsNullOrEmpty(ticketsJsonRaw))
                    {
                        ticketsXml = ProcessJsonToXml(ticketsJsonRaw, "Tickets");
                        Log.Debug("Tickets JSON processed for XML conversion.");
                    }
                    else
                    {
                        Log.Warn("ticketsJsonRaw was null or empty, using default empty Tickets XML.");
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Tickets JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                }

                Log.Debug("Parsing XML data into objects...");
                try
                {
                    foreach (XElement routeNode in routesXml.Root.Elements("Routes"))
                    {
                        try
                        {
                            if (!routeNode.HasElements)
                            {
                                Log.Debug("Skipping empty <Routes> element potentially from cleaned $ref.");
                                continue;
                            }
                            Marshut route = new Marshut();
                            long routeId;
                            if (routeNode.Element("routeId") != null && long.TryParse(routeNode.Element("routeId").Value, out routeId))
                            {
                                route.RouteId = routeId;
                            }
                            else
                            {
                                Log.Warn(string.Format("Could not parse routeId for element: {0}. Skipping route.", routeNode.ToString()));
                                continue;
                            }
                            route.StartPoint = (routeNode.Element("startPoint") != null) ? routeNode.Element("startPoint").Value : string.Empty;
                            route.EndPoint = (routeNode.Element("endPoint") != null) ? routeNode.Element("endPoint").Value : string.Empty;
                            loadedRoutes.Add(route);
                        }
                        catch (Exception exNode)
                        {
                            Log.Error(string.Format("Error parsing individual Route XML node: {0}. Node: {1}", exNode.ToString(), routeNode.ToString()));
                        }
                    }
                    Log.Debug(string.Format("Parsed {0} routes from XML.", loadedRoutes.Count));
                }
                catch (Exception ex)
                {
                    string errorMsgXml = string.Format("Error parsing Routes XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsgXml);
                }

                try
                {
                    foreach (XElement scheduleNode in schedulesXml.Root.Elements("Schedules"))
                    {
                        try
                        {
                            if (!scheduleNode.HasElements)
                            {
                                Log.Debug("Skipping empty <Schedules> element potentially from cleaned $ref.");
                                continue;
                            }

                            RouteSchedules schedule = new RouteSchedules();
                            long scheduleId;
                            XElement scheduleIdElement = scheduleNode.Element("routeScheduleId");
                            if (scheduleIdElement != null && long.TryParse(scheduleIdElement.Value, out scheduleId))
                            {
                                schedule.RouteScheduleId = scheduleId;
                            }
                            else
                            {
                                Log.Warn(string.Format("Could not parse routeScheduleId for element: {0}. Skipping.", scheduleNode.ToString()));
                                continue;
                            }

                            long routeId;
                            XElement routeIdElement = scheduleNode.Element("routeId");
                            if (routeIdElement != null && long.TryParse(routeIdElement.Value, out routeId))
                            {
                                schedule.RouteId = routeId;
                            }

                            XElement startPointElement = scheduleNode.Element("startPoint");
                            schedule.StartPoint = (startPointElement != null) ? startPointElement.Value : string.Empty;
                            XElement endPointElement = scheduleNode.Element("endPoint");
                            schedule.EndPoint = (endPointElement != null) ? endPointElement.Value : string.Empty;

                            DateTime departureTime;
                            XElement departureTimeElement = scheduleNode.Element("departureTime");
                            if (departureTimeElement != null && DateTime.TryParse(departureTimeElement.Value, out departureTime))
                            {
                                schedule.DepartureTime = departureTime;
                            }

                            DateTime arrivalTime;
                            XElement arrivalTimeElement = scheduleNode.Element("arrivalTime");
                            if (arrivalTimeElement != null && DateTime.TryParse(arrivalTimeElement.Value, out arrivalTime))
                            {
                                schedule.ArrivalTime = arrivalTime;
                            }

                            XElement marshutElement = scheduleNode.Element("marshut");
                            if (marshutElement != null && marshutElement.HasElements)
                            {
                                long marshutIdFromNested = 0;
                                XElement marshutIdElement = marshutElement.Element("routeId");
                                if (marshutIdElement != null && long.TryParse(marshutIdElement.Value, out marshutIdFromNested))
                                {
                                    schedule.Marshut = loadedRoutes.FirstOrDefault(r => r.RouteId == marshutIdFromNested);
                                }
                            }

                            loadedSchedules.Add(schedule);
                        }
                        catch (Exception exNode)
                        {
                            Log.Error(string.Format("Error parsing individual Schedule XML node: {0}. Node: {1}", exNode.ToString(), scheduleNode.ToString()));
                        }
                    }
                    Log.Debug(string.Format("Parsed {0} schedules from XML.", loadedSchedules.Count));
                }
                catch (Exception ex)
                {
                    string errorMsgXml = string.Format("Error parsing Schedules XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsgXml);
                }

                try
                {
                    foreach (XElement ticketNode in ticketsXml.Root.Elements("Tickets"))
                    {
                        try
                        {
                            if (!ticketNode.HasElements)
                            {
                                Log.Debug("Skipping empty <Tickets> element potentially from cleaned $ref.");
                                continue;
                            }

                            Bilet ticket = new Bilet();
                            long ticketId;
                            XElement ticketIdElement = ticketNode.Element("ticketId");
                            if (ticketIdElement != null && long.TryParse(ticketIdElement.Value, out ticketId))
                            {
                                ticket.TicketId = ticketId;
                            }
                            else
                            {
                                Log.Warn(string.Format("Could not parse ticketId for element: {0}. Skipping ticket.", ticketNode.ToString()));
                                continue;
                            }

                            long routeId = 0;
                            XElement routeIdElement = ticketNode.Element("routeId");
                            XElement marshutElement = ticketNode.Element("marshut");

                            if (routeIdElement != null && long.TryParse(routeIdElement.Value, out routeId))
                            {
                                ticket.RouteId = routeId;
                            }
                            else if (marshutElement != null)
                            {
                                XElement nestedRouteIdElement = marshutElement.Element("routeId");
                                if (nestedRouteIdElement != null && long.TryParse(nestedRouteIdElement.Value, out routeId))
                                {
                                    ticket.RouteId = routeId;
                                }
                                else if (!marshutElement.HasElements)
                                {
                                    Log.Debug(string.Format("Found empty placeholder for marshut property for TicketId {0}. RouteId remains 0.", ticket.TicketId));
                                }
                                else
                                {
                                    Log.Warn(string.Format("Could not parse routeId from nested marshut element for TicketId {0}. Node: {1}", ticket.TicketId, marshutElement.ToString()));
                                }
                            }
                            else
                            {
                                Log.Warn(string.Format("Could not determine RouteId for TicketId {0}", ticket.TicketId));
                            }

                            decimal price;
                            XElement priceElement = ticketNode.Element("ticketPrice");
                            if (priceElement != null && decimal.TryParse(priceElement.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out price))
                            {
                                ticket.TicketPrice = price;
                            }

                            XElement salesContainer = ticketNode.Element("sales");
                            ticket.Sales = new List<Prodazha>();
                            if (salesContainer != null)
                            {
                                foreach (XElement saleNode in salesContainer.Elements())
                                {
                                    if (!saleNode.HasElements) continue;
                                    try
                                    {
                                        Prodazha sale = new Prodazha();
                                        long saleId;
                                        XElement saleIdElement = saleNode.Element("saleId");
                                        if (saleIdElement != null && long.TryParse(saleIdElement.Value, out saleId))
                                        {
                                            // Parse SaleDate
                                            DateTime saleDate = DateTime.MinValue;
                                            XElement saleDateElement = saleNode.Element("saleDate");
                                            if (saleDateElement != null && DateTime.TryParse(saleDateElement.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out saleDate))
                                            {
                                                sale.SaleDate = saleDate.ToLocalTime();
                                            } else {
                                                 Log.Warn(string.Format("Could not parse SaleDate for SaleId {0} on Ticket {1}", saleId, ticket.TicketId));
                                            }

                                            // Parse TicketSoldToUser
                                            XElement userElement = saleNode.Element("ticketSoldToUser");
                                            sale.TicketSoldToUser = (userElement != null) ? userElement.Value : string.Empty;

                                            // Assign SaleId (already parsed)
                                            sale.SaleId = saleId;
                                            ticket.Sales.Add(sale);
                                        }
                                    }
                                    catch (Exception exSaleNode)
                                    {
                                        Log.Error(string.Format("Error parsing individual Sale XML node for Ticket {0}: {1}. Node: {2}", ticket.TicketId, exSaleNode.ToString(), saleNode.ToString()));
                                    }
                                }
                            }

                            loadedTickets.Add(ticket);
                        }
                        catch (Exception exNode)
                        {
                            Log.Error(string.Format("Error parsing individual Ticket XML node: {0}. Node: {1}", exNode.ToString(), ticketNode.ToString()));
                        }
                    }
                    Log.Debug(string.Format("Parsed {0} tickets from XML.", loadedTickets.Count));
                }
                catch (Exception ex)
                {
                    string errorMsgXml = string.Format("Error parsing Tickets XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsgXml);
                }

                Log.Debug("Linking tickets to routes and populating internal collections...");
                _routeSchedules = loadedSchedules;

                foreach (var ticket in loadedTickets)
                {
                    ticket.Marshut = loadedRoutes.FirstOrDefault(r => r.RouteId == ticket.RouteId);
                    if (ticket.Marshut == null && ticket.RouteId != 0)
                    {
                        Log.Warn(string.Format("Could not link Marshut for TicketId {0} (RouteId: {1}). Route not found in loaded list.", ticket.TicketId, ticket.RouteId));
                    }
                }
                _allTicketsData = loadedTickets;

                Action updateUiAction = delegate()
                {
                    if (this.IsDisposed) return;

                    var scheduleDataSource = new List<RouteSchedules>(_routeSchedules);
                    scheduleDataSource.Insert(0, new RouteSchedules { RouteScheduleId = -1, StartPoint = "[Все рейсы]", EndPoint = "" });
                    lueRouteScheduleFilter.Properties.DataSource = scheduleDataSource;
                    try
                    {
                        if (lueRouteScheduleFilter.Properties.GetKeyValueByDisplayValue("[Все рейсы]") != null)
                            lueRouteScheduleFilter.EditValue = -1;
                        else if (scheduleDataSource.Count > 0)
                            lueRouteScheduleFilter.EditValue = scheduleDataSource[0].RouteScheduleId;
                        else
                            lueRouteScheduleFilter.EditValue = null;
                    }
                    catch (Exception exLue)
                    {
                        Log.Warn(string.Format("Could not set default value for Route Schedule Filter: {0}", exLue.Message));
                        lueRouteScheduleFilter.EditValue = null;
                    }
                    lueRouteScheduleFilter.Enabled = true;

                    ApplyFiltersAndBindData();
                };

                if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(updateUiAction); } else { updateUiAction(); }

                Log.Info(string.Format("Synchronous data load completed successfully for route {0}. Loaded {1} schedules, {2} routes, {3} tickets.",
                    selectedRouteIdStr, _routeSchedules.Count, loadedRoutes.Count, _allTicketsData.Count));
            }
            catch (Exception ex)
            {
                string criticalErrorMsg = string.Format("Critical error during synchronous data load. Exception: {0}", ex.ToString());
                Log.Error(criticalErrorMsg);
                Action errorAction = delegate()
                {
                    if (this.IsDisposed) return;
                    _routeSchedules.Clear();
                    _allTicketsData.Clear();
                    lueRouteScheduleFilter.Properties.DataSource = null;
                    lueRouteScheduleFilter.Enabled = false;
                gridControlTickets.DataSource = null;
                    XtraMessageBox.Show("Произошла критическая ошибка при загрузке данных. См. лог.\n" + ex.Message,
                        "Критическая Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(errorAction); } else { errorAction(); }
            }
            finally
            {
                if (client != null) client.Dispose();

                if (waitMessageBox != null && !waitMessageBox.IsDisposed)
                {
                    Action closeWaitBoxFinal = delegate() { if (!waitMessageBox.IsDisposed) waitMessageBox.Close(); };
                    if (waitMessageBox.InvokeRequired) { waitMessageBox.BeginInvoke(closeWaitBoxFinal); } else { closeWaitBoxFinal(); }
                    waitMessageBox.Dispose();
                }

                Action finalUiAction = delegate() {
                    if (this.IsDisposed) { Log.Debug("Form disposed before final UI state could be reset."); return; }
                    SetLoadingState(false);
                    Log.Debug("Finished final UI state reset after synchronous load sequence attempt.");
                };
                this.BeginInvoke(finalUiAction);
            }
        }

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
            string logMsgStart = string.Format("Processing raw JSON for {0} (truncated): {1}", rootElementName, truncatedJson);
            Log.Debug(logMsgStart);

            string preCleanedJson = Regex.Replace(jsonRaw ?? string.Empty, @"[\u0000-\u001F]", "");
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
            Dictionary<string, JObject> globalIdMap = new Dictionary<string, JObject>();

            try
            {
                BuildGlobalIdMap(rootToken, globalIdMap);
                Log.Debug(string.Format("Built GLOBAL ID map with {0} entries for {1} structure.", globalIdMap.Count, rootElementName));
            }
            catch (Exception mapEx)
            {
                string errorMsg = string.Format("Error building global ID map for {0}. Exception: {1}", rootElementName, mapEx.ToString());
                Log.Error(errorMsg);
            }

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
                            Log.Debug(string.Format("Resolving top-level $ref '{0}'...", refValue));
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
                    try
                    {
                        JToken cleanedItem = CleanAndTransformJsonToken(resItem);
                        if (cleanedItem != null && cleanedItem.Type != JTokenType.Null)
                        {
                            cleanedItems.Add(cleanedItem);
                        }
                    }
                    catch (Exception cleanEx)
                    {
                        string itemStr = (resItem != null) ? resItem.ToString(Newtonsoft.Json.Formatting.None) : "NULL";
                        string truncatedItemStr = (itemStr.Length > 200) ? itemStr.Substring(0, 200) + "..." : itemStr;
                        string errorMsg = string.Format("Error cleaning resolved item for {0}. Item (truncated): {1}. Exception: {2}", rootElementName, truncatedItemStr, cleanEx.ToString());
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
                    try {
                        JToken cleanedItem = CleanAndTransformJsonToken(item);
                        if (cleanedItem != null && cleanedItem.Type != JTokenType.Null) {
                            cleanedItems.Add(cleanedItem);
                        }
                    } catch (Exception cleanEx) {
                        string itemStr = (item != null) ? item.ToString(Newtonsoft.Json.Formatting.None) : "NULL";
                        string truncatedItemStr = (itemStr.Length > 200) ? itemStr.Substring(0, 200) + "..." : itemStr;
                        string errorMsg = string.Format("Error cleaning root array item for {0}. Item (truncated): {1}. Exception: {2}", rootElementName, truncatedItemStr, cleanEx.ToString());
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
                try {
                    cleanedToken = CleanAndTransformJsonToken(rootToken);
                } catch (Exception cleanEx) {
                    string tokenStr = (rootToken != null) ? rootToken.ToString(Newtonsoft.Json.Formatting.None) : "NULL";
                    string truncatedTokenStr = (tokenStr.Length > 200) ? tokenStr.Substring(0, 200) + "..." : tokenStr;
                    string errorMsg = string.Format("Error cleaning root token (Case 3) for {0}. Token (truncated): {1}. Exception: {2}", rootElementName, truncatedTokenStr, cleanEx.ToString());
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
                if (potentiallyStillWrapped != null && potentiallyStillWrapped.Count == 1 && 
                    potentiallyStillWrapped.Property("$values") != null && 
                    potentiallyStillWrapped.Property("$values").Value is JArray)
                {
                    Log.Warn(string.Format("Cleaned token (Case 3) for {0} still contained $values wrapper. Extracting inner array.", rootElementName));
                    finalObjectForXml = new JObject(new JProperty(rootElementName, potentiallyStillWrapped.Property("$values").Value));
                    JArray extractedArray = (JArray)potentiallyStillWrapped.Property("$values").Value;
                    var filteredExtracted = extractedArray.Where(delegate(JToken t) {
                        JObject jobj = t as JObject;
                        return (jobj == null || jobj.HasValues);
                    }).ToList();
                    if (filteredExtracted.Count < extractedArray.Count)
                    {
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
                return XDocument.Load(new XmlNodeReader(xmlDoc));
            }
            catch (Exception xmlEx)
            {
                string errorMsg = string.Format("Final XML conversion failed for {0}. JSON used (truncated): {1}. Exception: {2}", rootElementName, (finalJsonForXml.Length > 500 ? finalJsonForXml.Substring(0,500)+"..." : finalJsonForXml), xmlEx.ToString());
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

                        if (obj.Count == 1 && obj.Property("$ref") != null)
                        {
                            string refValue = obj.Property("$ref").Value.ToString();
                            return new JObject();
                        }

                        JObject cleanedObj = new JObject();
                        foreach (var property in obj.Properties().ToList())
                        {
                            if (property.Name.Equals("$id", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            JToken cleanedValue = null;
                            try {
                                cleanedValue = CleanAndTransformJsonToken(property.Value);
                            } catch (Exception exCleanInner) {
                                continue;
                            }

                            JObject valueObj = cleanedValue as JObject;
                            if (valueObj != null && valueObj.Count == 1 && valueObj.Property("$values") != null && valueObj.Property("$values").Value.Type == JTokenType.Array)
                            {
                                cleanedValue = CleanAndTransformJsonToken(valueObj.Property("$values").Value);
                            }

                            if (cleanedValue != null && cleanedValue.Type != JTokenType.Null)
                            {
                                JObject cleanedValueAsObject = cleanedValue as JObject;
                                if (cleanedValueAsObject == null || cleanedValueAsObject.HasValues)
                                {
                                    cleanedObj.Add(property.Name, cleanedValue);
                                }
                            }
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
                            try {
                                cleanedItem = CleanAndTransformJsonToken(item);
                            } catch (Exception exCleanItem) {
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
                    return token;
            }
        }

        private void ApplyFiltersAndBindData()
        {
            Log.Debug("Applying client-side filters for tickets...");
            if (_isBusy)
            {
                Log.Warn("ApplyFiltersAndBindData called while busy, skipping.");
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                gridControlTickets.DataSource = null;

                long routeScheduleIdFilterValue = -1;

                object editValue = lueRouteScheduleFilter.EditValue;
                if (editValue != null && editValue != DBNull.Value)
                {
                    try
                    {
                        routeScheduleIdFilterValue = Convert.ToInt64(editValue);
                    }
                    catch (Exception exConvert)
                    {
                         Log.Warn(string.Format("Could not convert filter EditValue ('{0}') to long: {1}. Using default (-1).", editValue, exConvert.Message));
                        routeScheduleIdFilterValue = -1;
                    }
                }
                else
                {
                    routeScheduleIdFilterValue = -1;
                }

                Log.Debug(string.Format("Current Route Filter Value: {0}", routeScheduleIdFilterValue));

                IEnumerable<Bilet> filteredData = _allTicketsData;

                if (routeScheduleIdFilterValue != -1)
                {
                    Log.Debug(string.Format("Applying filter for Route ID: {0}", routeScheduleIdFilterValue));
                    filteredData = filteredData.Where(delegate(Bilet t)
                    {
                        return t.RouteId == routeScheduleIdFilterValue;
                    });
                } else {
                     Log.Debug("No specific route filter applied (Value is -1 or null).");
                }

                var finalFilteredList = filteredData.ToList();

                gridControlTickets.DataSource = finalFilteredList;
                gridControlTickets.RefreshDataSource();

                 Log.Info(string.Format("Client-side filtering applied. Displaying {0} tickets.", finalFilteredList.Count));
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Error applying client-side filters for tickets. Exception: {0}", ex.ToString());
                Log.Error(errorMsg);
                XtraMessageBox.Show(string.Format("Произошла ошибка при применении фильтров: {0}", ex.Message), "Ошибка Фильтрации",
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
                gridControlTickets.DataSource = null;
            }
            finally
            {
                Cursor = Cursors.Default;
                UpdateButtonStates();
                if (gridViewTickets.RowCount > 0) {
                gridViewTickets.BestFitColumns();
                }
            }
        }
        
        private bool CanCancelSelectedTicket()
        {
            var selectedTicket = gridViewTickets.GetFocusedRow() as Bilet;
             if (selectedTicket == null) return false;
            
            bool hasSales = false;
             if (selectedTicket.Sales != null) {
                 hasSales = selectedTicket.Sales.Any(delegate(Prodazha s) { return true; });
            }
             return !hasSales;
        }
        
        private void UpdateButtonStates()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateButtonStates));
                return;
            }
             if (this.IsDisposed || _isBusy) return;

            var selectedTicket = gridViewTickets.GetFocusedRow() as Bilet;
            bool isSelected = selectedTicket != null;
            
            btnViewDetails.Enabled = isSelected;
            btnCancelTicket.Enabled = isSelected && CanCancelSelectedTicket();
        }

        private void gridViewTickets_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            Bilet ticket = gridViewTickets.GetRow(e.ListSourceRowIndex) as Bilet;
            if (ticket == null) return;

            if (e.Column.FieldName == "TicketPrice")
            {
                try {
                     decimal price = Convert.ToDecimal(e.Value);
                e.DisplayText = string.Format("{0:C}", price);
                } catch { /* Ignore conversion errors */ }
            }
            else if (e.Column.FieldName == "RouteDisplayString")
            {
                e.DisplayText = (ticket.Marshut != null) 
                                     ? string.Format("{0} - {1}", ticket.Marshut.StartPoint ?? "?", ticket.Marshut.EndPoint ?? "?") 
                                     : "[N/A]";
            }
            else if (e.Column.FieldName == "IsSold")
            {
                bool hasSales = ticket.Sales != null && ticket.Sales.Any(delegate(Prodazha s){ return true;});
                e.DisplayText = hasSales ? "Да" : "Нет";
            }
            else if (e.Column.FieldName == "PassengerName")
            {
                Prodazha firstSale = (ticket.Sales != null && ticket.Sales.Count > 0) ? ticket.Sales[0] : null;
                e.DisplayText = (firstSale != null && !string.IsNullOrEmpty(firstSale.TicketSoldToUser)) 
                                    ? firstSale.TicketSoldToUser 
                                    : "";
            }
            else if (e.Column.FieldName == "PurchaseDate")
            {
                // Get date from the first sale, if available
                Prodazha firstSale = (ticket.Sales != null && ticket.Sales.Any()) ? ticket.Sales[0] : null;
                if (firstSale != null && firstSale.SaleDate > DateTime.MinValue) {
                    e.DisplayText = firstSale.SaleDate.ToString("dd.MM.yyyy HH:mm");
                } else {
                    e.DisplayText = ""; // Or "[N/A]"
                }
            }
        }

        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
            Log.Debug("Apply Filter button clicked for tickets.");
            ApplyFiltersAndBindData();
        }

        private void btnViewDetails_Click(object sender, EventArgs e)
        {
            var selectedTicket = gridViewTickets.GetFocusedRow() as Bilet;
            if (selectedTicket == null) return;
            
             Log.Info(string.Format("Viewing details for Ticket ID: {0}", selectedTicket.TicketId));
            
            var details = new StringBuilder();
            details.AppendFormat("Билет ID: {0}\n", selectedTicket.TicketId);
            details.AppendFormat("Маршрут ID: {0}\n", selectedTicket.RouteId);
            if (selectedTicket.Marshut != null)
            {
                details.AppendFormat("Начальный пункт: {0}\n", selectedTicket.Marshut.StartPoint ?? "[N/A]");
                details.AppendFormat("Конечный пункт: {0}\n", selectedTicket.Marshut.EndPoint ?? "[N/A]");
            }
            else {
                 details.AppendFormat("Маршрут: [Информация недоступна]\n");
            }
            details.AppendFormat("Цена билета: {0:C}\n", selectedTicket.TicketPrice);

            bool hasSales = false;
            if (selectedTicket.Sales != null) {
                 hasSales = selectedTicket.Sales.Any(delegate(Prodazha s) { return true; });
            }
            details.AppendFormat("Продан: {0}\n", (hasSales ? "Да" : "Нет"));

            if(hasSales) {
                 details.Append("\nДетали Продаж:\n");
                 foreach(var sale in selectedTicket.Sales) {
                     details.AppendFormat("- Продажа ID: {0}\n", sale.SaleId);
                 }
            }
            
            XtraMessageBox.Show(details.ToString(), "Детали Билета", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnCancelTicket_Click(object sender, EventArgs e)
        {
            var selectedTicket = gridViewTickets.GetFocusedRow() as Bilet;
            if (selectedTicket == null || !CanCancelSelectedTicket()) return;
            
             Log.Warn(string.Format("Attempting to cancel Ticket ID: {0}", selectedTicket.TicketId));
            
            var confirmResult = XtraMessageBox.Show(
                string.Format("Вы уверены, что хотите отменить билет #{0}?", selectedTicket.TicketId),
                "Подтверждение отмены", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
                
            if (confirmResult == DialogResult.Yes)
            {
                HttpClient client = null;
                SetLoadingState(true);
                try
                {
                    client = _apiClientService.CreateClient();
                    var apiUrl = string.Format("{0}/api/Tickets/{1}", _baseUrl, selectedTicket.TicketId);
                    
                    Log.Debug(string.Format("Calling DELETE synchronously {0}", apiUrl));
                    var response = client.DeleteAsync(apiUrl).Result;

                    Action processResponseAction = delegate() {
                         if (this.IsDisposed) return;
                    
                    if (response.IsSuccessStatusCode)
                    {
                             Log.Info(string.Format("Ticket {0} successfully cancelled.", selectedTicket.TicketId));
                        XtraMessageBox.Show("Билет успешно отменен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                             LoadDataSynchronously();
                    }
                    else
                    {
                              try {
                                 var errorContent = response.Content.ReadAsStringAsync().Result;
                                 string errorMsgLog = string.Format("Failed to cancel ticket {0}. Status: {1}, Content: {2}",
                                        selectedTicket.TicketId, response.StatusCode, errorContent);
                                 Log.Error(errorMsgLog);
                        XtraMessageBox.Show(string.Format("Ошибка при отмене билета: {0}\n{1}", response.ReasonPhrase, errorContent), "Ошибка API",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                             } catch (Exception readEx) {
                                  string errorMsgLog = string.Format("Failed to cancel ticket {0}. Status: {1}. Could not read error content: {2}",
                                                        selectedTicket.TicketId, response.StatusCode, readEx.Message);
                                 Log.Error(errorMsgLog);
                                 XtraMessageBox.Show(string.Format("Ошибка при отмене билета: {0}", response.ReasonPhrase), "Ошибка API",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                         }
                          SetLoadingState(false);
                    };

                     if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(processResponseAction); } else { processResponseAction(); }

                }
                catch (HttpRequestException httpEx)
                {
                    string errorMsg = string.Format("Network error cancelling ticket {0}. Exception: {1}", selectedTicket.TicketId, httpEx.ToString());
                    Log.Error(errorMsg);
                    Action errorAction = delegate() {
                    XtraMessageBox.Show(string.Format("Сетевая ошибка при отмене билета: {0}", httpEx.Message), "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                         SetLoadingState(false);
                    };
                     if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(errorAction); } else { errorAction(); }
                }
                catch (Exception ex)
                {
                     string errorMsg = string.Format("Error cancelling ticket {0}. Exception: {1}", selectedTicket.TicketId, ex.ToString());
                     Log.Error(errorMsg);
                     Action errorAction = delegate() {
                    XtraMessageBox.Show(string.Format("Произошла ошибка при отмене билета: {0}", ex.Message), "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                         SetLoadingState(false);
                     };
                     if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(errorAction); } else { errorAction(); }
                }
                finally
                {
                    if (client != null) client.Dispose();
                }
            }
            else
            {
                 Log.Info(string.Format("Cancellation aborted for Ticket ID: {0}", selectedTicket.TicketId));
            }
        }
        
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            Log.Info("Refresh button clicked for tickets.");
            LoadDataSynchronously();
        }

        private void GridViewTickets_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
             UpdateButtonStates();
        }

        private void lueRouteScheduleFilter_EditValueChanged(object sender, EventArgs e)
        {
            if (_isBusy) return;

            Log.Debug("Route Schedule filter changed. Applying filters...");
            ApplyFiltersAndBindData();
        }
    }
} 