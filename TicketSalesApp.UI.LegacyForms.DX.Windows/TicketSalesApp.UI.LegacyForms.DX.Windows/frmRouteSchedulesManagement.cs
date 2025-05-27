using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Views.Base;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TicketSalesApp.Core.Models;
using System.Threading;
using TicketSalesApp.UI.LegacyForms.DX.Windows;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Xml;
using DevExpress.Data;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmRouteSchedulesManagement : DevExpress.XtraEditors.XtraForm
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ApiClientService _apiClient;
        private readonly string _baseUrl = "http://localhost:5000/api";
        private ObservableCollection<Marshut> _routes;
        private BindingList<RouteSchedules> _schedules;
        private List<Avtobus> _availableBuses;
        private List<Employee> _availableDrivers;
        
        private RouteSchedules _selectedSchedule;
        private bool _isBusy;
        private string _errorMessage = string.Empty;
        private bool _hasError;
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        private long? _selectedRouteIdFilter = null;
        private bool _formLoadComplete = false;

        public frmRouteSchedulesManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            _routes = new ObservableCollection<Marshut>();
            _schedules = new BindingList<RouteSchedules>();
            _availableBuses = new List<Avtobus>();
            _availableDrivers = new List<Employee>();

            lueRouteFilter.Properties.DataSource = _routes;
            lueRouteFilter.Properties.DisplayMember = "StartPoint";
            lueRouteFilter.Properties.ValueMember = "RouteId";

            dateFilter.Properties.NullText = "[Все даты]";
            dateFilter.EditValue = null;

            routeScheduleBindingSource.DataSource = _schedules; 

            // Ensure ColumnAutoWidth is off to respect defined widths
            gridViewSchedules.OptionsView.ColumnAutoWidth = false;

            UpdateButtonStates();

            gridViewSchedules.FocusedRowChanged += gridViewSchedules_FocusedRowChanged;
            gridViewSchedules.CustomUnboundColumnData += gridViewSchedules_CustomUnboundColumnData;
            this.Load += frmRouteSchedulesManagement_Load;
            this.FormClosing += FrmRouteSchedulesManagement_FormClosing;
            lueRouteFilter.EditValueChanged += lueRouteFilter_EditValueChanged;
            dateFilter.EditValueChanged += dateFilter_EditValueChanged;

            _apiClient.OnAuthTokenChanged += HandleAuthTokenChanged;
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

        private void FrmRouteSchedulesManagement_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Debug("Form closing.");
            _apiClient.OnAuthTokenChanged -= HandleAuthTokenChanged;
        }

        private void frmRouteSchedulesManagement_Load(object sender, EventArgs e)
        {
            if (_formLoadComplete)
            {
                Log.Warn("frmRouteSchedulesManagement_Load fired again after initial load completed. Ignoring.");
                return;
            }

            bool initialLoadSuccess = false;
            try
            {
                Log.Debug("frmRouteSchedulesManagement_Load event triggered (initial run).");
                
                initialLoadSuccess = LoadRoutesAndSelectFilter();
                
                if (initialLoadSuccess)
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
            Log.Info("Starting initial route loading and selection process...");
            SetLoadingState(true);

            XtraForm waitMessageBox = null;
            HttpClient client = null;
            string routesJsonRaw = null;
            XDocument routesXml = null;
            List<Marshut> loadedRoutes = new List<Marshut>();
            bool routeSelected = false;

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
                waitMessageBox.Show(this);

                client = _apiClient.CreateClient();

                Log.Debug("Fetching Routes synchronously for initial selection...");
                var routesApiUrl = string.Format("{0}/Routes", _baseUrl);
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
                     catch (Exception exNode) { 
                         string errorMsg = string.Format("Error parsing individual Route XML node during pre-load: {0}. Node: {1}", exNode.ToString(), routeNode.ToString());
                         Log.Error(errorMsg);
                     }
                 }
                 Log.Debug("Parsed {0} initial routes from XML.", loadedRoutes.Count);
                 
                 if (!loadedRoutes.Any()) {
                     throw new Exception("No routes were loaded after parsing XML.");
                 }

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
                     
                     var selectLabel = new LabelControl { Text = "Выберите маршрут для загрузки расписаний:", Location = new Point(15, 20), AutoSize = true };
                     panel.Controls.Add(selectLabel);

                     var routeCombo = new LookUpEdit
                     {
                         Location = new Point(15, 45),
                         Width = 400,
                         Properties = {
                             DataSource = loadedRoutes,
                             DisplayMember = "StartPoint", 
                             ValueMember = "RouteId",
                             NullText = "[Выберите маршрут...]",
                             ShowHeader = true
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
                         
                         if (selectedValue != null && selectedValue != DBNull.Value && !string.IsNullOrEmpty(selectedDisplayText) && selectedDisplayText != "[Выберите маршрут...]")
                         {
                             Marshut selectedRouteObject = loadedRoutes.FirstOrDefault(r => r.StartPoint == selectedDisplayText);
                             
                             if (selectedRouteObject != null)
                             {
                                 _selectedRouteIdFilter = selectedRouteObject.RouteId;
                                 txtHiddenSelectedRouteId.Text = _selectedRouteIdFilter.Value.ToString();
                                 
                                 Log.Debug("Route ID {0} determined via display text '{1}' lookup and stored.", _selectedRouteIdFilter.Value, selectedDisplayText);
                                 
                                 selectionForm.DialogResult = DialogResult.OK;
                                 selectionForm.Close();
                     }
                     else
                     {
                                 string errorMsg = string.Format("Could not find route object matching display text: {0}", selectedDisplayText);
                                 Log.Error(errorMsg);
                                 XtraMessageBox.Show("Не удалось найти выбранный маршрут по отображаемому имени. Пожалуйста, попробуйте снова.", "Ошибка Поиска", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                     Log.Info("Route ID {0} selected by user.", _selectedRouteIdFilter.Value);
                    
                     _routes.Clear(); 
                     foreach (var route in loadedRoutes.OrderBy(r => r.StartPoint))
                     {
                         _routes.Add(route);
                     }
                     
                     Action updateMainFilterAction = delegate() {
                         if (this.IsDisposed || lueRouteFilter == null || lueRouteFilter.IsDisposed) return;
                         lueRouteFilter.Properties.DataSource = null;
                         lueRouteFilter.Properties.DataSource = _routes; 
                         lueRouteFilter.EditValue = _selectedRouteIdFilter;
                     };
                     if (this.IsHandleCreated && !this.IsDisposed)
                     {
                         this.BeginInvoke(updateMainFilterAction);
                     }
                     else if(!this.IsDisposed)
                     {
                         updateMainFilterAction();
                     }
                     
                     return true;
                 }
                 else {
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
                
                Action finalUiAction = delegate()
                {
                    if (this.IsDisposed) { Log.Debug("Form disposed before final UI state could be reset."); return; }
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
            
            if (this.IsDisposed || gridControlSchedules == null || gridControlSchedules.IsDisposed)
            {
                 Log.Warn("SetLoadingState called but form or controls are disposed.");
                 return;
            }

            Log.Debug(isLoading ? "Setting UI to loading state." : "Setting UI to normal state.");
            _isBusy = isLoading;
            Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;
            gridControlSchedules.Enabled = !isLoading;
            btnAdd.Enabled = !isLoading;
            btnRefresh.Enabled = !isLoading;
            lueRouteFilter.Enabled = !isLoading;
            dateFilter.Enabled = !isLoading;
            
            if (!isLoading)
            {
                 UpdateButtonStates();
            }
            else
            {
                if (btnEdit != null && !btnEdit.IsDisposed) btnEdit.Enabled = false;
                if (btnDelete != null && !btnDelete.IsDisposed) btnDelete.Enabled = false;
            }
        }

        private void LoadDataSynchronously()
        {
            if (!_selectedRouteIdFilter.HasValue)
            {
                Log.Warn("LoadDataSynchronously called but no initial route selected. Aborting.");
                return;
            }

            string selectedRouteIdStr = _selectedRouteIdFilter.Value.ToString();
            Log.Info("Starting synchronous data load sequence for selected route: {0}...", selectedRouteIdStr);
            SetLoadingState(true);
            
            XtraForm waitMessageBox = null;
            try
            {
                Action showWaitBoxAction = delegate()
                {
                    if (this.IsDisposed) return;
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
            string busesJsonRaw = null;
            string driversJsonRaw = null;
            string schedulesJsonRaw = null;
            
            List<Avtobus> loadedBuses = new List<Avtobus>();
            List<Employee> loadedDrivers = new List<Employee>();
            List<RouteSchedules> loadedSchedules = new List<RouteSchedules>();
            
            XDocument busesXml = XDocument.Parse("<Root><Buses></Buses></Root>");
            XDocument driversXml = XDocument.Parse("<Root><Drivers></Drivers></Root>");
            XDocument schedulesXml = XDocument.Parse("<Root><Schedules></Schedules></Root>");
            
            try
            {
                client = _apiClient.CreateClient();

                try
                {
                    Log.Debug("Fetching Buses synchronously...");
                    var busesApiUrl = string.Format("{0}/Buses", _baseUrl);
                    HttpResponseMessage busesResponse = client.GetAsync(busesApiUrl).Result;
                    
                    if (busesResponse.IsSuccessStatusCode)
                    {
                        byte[] busesBytes = busesResponse.Content.ReadAsByteArrayAsync().Result;
                        busesJsonRaw = Encoding.UTF8.GetString(busesBytes);
                        Log.Debug("Buses JSON fetched successfully.");
                    }
                    else
                    {
                        throw new Exception("Failed to load Buses: " + busesResponse.ReasonPhrase);
                    }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error fetching Buses. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                }
                
                try
                {
                    Log.Debug("Fetching Drivers synchronously...");
                    var driversApiUrl = string.Format("{0}/Employees/search?jobTitle=Водитель", _baseUrl);
                    HttpResponseMessage driversResponse = client.GetAsync(driversApiUrl).Result;
                    
                    if (driversResponse.IsSuccessStatusCode)
                    {
                        byte[] driversBytes = driversResponse.Content.ReadAsByteArrayAsync().Result;
                        driversJsonRaw = Encoding.UTF8.GetString(driversBytes);
                        Log.Debug("Drivers JSON fetched successfully.");
                }
                else
                {
                        throw new Exception("Failed to load Drivers: " + driversResponse.ReasonPhrase);
                    }
                }
                catch (Exception ex)
                {
                     string errorMsg = string.Format("Error fetching Drivers. Exception: {0}", ex.ToString());
                     Log.Error(errorMsg);
                }
                
                try
                {
                    DateTime? dateFilterValue = null;
                    Action getDateFilter = delegate() {
                         object dateVal = dateFilter.EditValue;
                         if (dateVal != null && dateVal != DBNull.Value) { dateFilterValue = (DateTime)dateVal; }
                    };
                    if (this.InvokeRequired) { this.Invoke(getDateFilter); } else { getDateFilter(); }

                    var query = HttpUtility.ParseQueryString(string.Empty);
                    query["routeId"] = _selectedRouteIdFilter.Value.ToString();
                    if (dateFilterValue.HasValue) query["date"] = dateFilterValue.Value.ToString("yyyy-MM-dd");
                    query["ReturnAll"] = "true";
                    
                    string schedulesApiUrl = string.Format("{0}/RouteSchedules/search?{1}", _baseUrl, query.ToString());
                    Log.Debug("Fetching Schedules synchronously from URL: {0}", schedulesApiUrl);
                    HttpResponseMessage schedulesResponse = client.GetAsync(schedulesApiUrl).Result;
                    
                    if (schedulesResponse.IsSuccessStatusCode)
                    {
                        byte[] schedulesBytes = schedulesResponse.Content.ReadAsByteArrayAsync().Result;
                        schedulesJsonRaw = Encoding.UTF8.GetString(schedulesBytes);
                        Log.Debug("Schedules JSON fetched successfully (Payload size: {0} bytes).", schedulesBytes.Length);
                    }
                    else
                    {
                        throw new Exception("Failed to load Schedules: " + schedulesResponse.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                    string errorMsg = string.Format("Error fetching Schedules for route {0}. Exception: {1}", _selectedRouteIdFilter.Value, ex.ToString());
                    Log.Error(errorMsg);
                }
                
                Log.Debug("Manually handling arrays and converting to XML...");
                
                try
                {
                    if (!string.IsNullOrEmpty(busesJsonRaw))
                    {
                        busesXml = ProcessJsonToXml(busesJsonRaw, "Buses");
                        Log.Debug("Buses JSON processed for XML conversion.");
                    } else { Log.Warn("busesJsonRaw was null or empty, using default empty Buses XML."); }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Buses JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                }
                
                try
                {
                    if (!string.IsNullOrEmpty(driversJsonRaw))
                    {
                        driversXml = ProcessJsonToXml(driversJsonRaw, "Drivers");
                        Log.Debug("Drivers JSON processed for XML conversion.");
                    } else { Log.Warn("driversJsonRaw was null or empty, using default empty Drivers XML."); }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Drivers JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                }
                
                try
                {
                    if (!string.IsNullOrEmpty(schedulesJsonRaw))
                    {
                        schedulesXml = ProcessJsonToXml(schedulesJsonRaw, "Schedules"); 
                        Log.Debug("Schedules JSON processed for XML conversion.");
                    } else { Log.Warn("schedulesJsonRaw was null or empty, using default empty Schedules XML."); }
                }
                catch (Exception ex)
                {
                    string errorMsg = string.Format("Error processing Schedules JSON to XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsg);
                    XtraMessageBox.Show("Произошла ошибка при обработке данных расписаний. Данные могут быть неполными.", "Ошибка Обработки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                Log.Debug("Parsing XML data into objects...");
                
                try
                {
                    foreach (XElement busNode in busesXml.Root.Elements("Buses"))
                    {
                        try
                        {
                            if (!busNode.HasElements)
                            {
                                Log.Debug("Skipping empty <Buses> element potentially from cleaned $ref.");
                                continue;
                            }
                            
                            Avtobus bus = new Avtobus();
                            
                            long busId;
                            XElement busIdElement = busNode.Element("busId");
                            if (busIdElement != null && long.TryParse(busIdElement.Value, out busId))
                            {
                                bus.BusId = busId;
                }
                else
                {
                                Log.Warn(string.Format("Could not parse busId for element: {0}. Skipping bus.", busNode.ToString()));
                                continue;
                            }
                            
                            XElement modelElement = busNode.Element("model");
                            bus.Model = (modelElement != null) ? modelElement.Value : string.Empty;
                            
                            loadedBuses.Add(bus);
                        }
                        catch (Exception exNode)
                        {
                            Log.Error(string.Format("Error parsing individual Bus XML node: {0}. Node: {1}", exNode.ToString(), busNode.ToString()));
                        }
                    }
                     Log.Debug("Parsed {0} buses from XML.", loadedBuses.Count);
                }
                catch (Exception ex)
                {
                    string errorMsgXml = string.Format("Error parsing Buses XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsgXml);
                }
                
                try
                {
                    foreach (XElement driverNode in driversXml.Root.Elements("Drivers"))
                    {
                        try
                        {
                            if (!driverNode.HasElements)
                            {
                                Log.Debug("Skipping empty <Drivers> element potentially from cleaned $ref.");
                                continue;
                            }
                            
                            Employee driver = new Employee();
                            
                            long empId;
                            XElement empIdElement = driverNode.Element("empId");
                            if (empIdElement != null && long.TryParse(empIdElement.Value, out empId))
                            {
                                driver.EmpId = empId;
            }
            else
            {
                                Log.Warn(string.Format("Could not parse empId for element: {0}. Skipping driver.", driverNode.ToString()));
                            continue; 
                        }

                            XElement surnameElement = driverNode.Element("surname");
                            driver.Surname = (surnameElement != null) ? surnameElement.Value : string.Empty;
                            
                            XElement nameElement = driverNode.Element("name");
                            driver.Name = (nameElement != null) ? nameElement.Value : string.Empty;
                            
                            XElement patronymElement = driverNode.Element("patronym");
                            driver.Patronym = (patronymElement != null) ? patronymElement.Value : string.Empty;
                            
                            long jobId;
                            XElement jobIdElement = driverNode.Element("jobId");
                            if (jobIdElement != null && long.TryParse(jobIdElement.Value, out jobId))
                            {
                                driver.JobId = jobId;
                            }
                            
                            loadedDrivers.Add(driver);
                        }
                        catch (Exception exNode)
                        {
                            Log.Error(string.Format("Error parsing individual Driver XML node: {0}. Node: {1}", exNode.ToString(), driverNode.ToString()));
                        }
                    }
                     Log.Debug("Parsed {0} drivers from XML.", loadedDrivers.Count);
                }
                catch (Exception ex)
                {
                    string errorMsgXml = string.Format("Error parsing Drivers XML. Exception: {0}", ex.ToString());
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
                            
                            // Parse route stops once and efficiently
                            var routeStopsElements = scheduleNode.Elements("routeStops").ToList();
                            if (routeStopsElements.Count > 0 && routeStopsElements.Any(e => !string.IsNullOrWhiteSpace(e.Value)))
                            {
                                // Only collect non-empty values
                                var validStops = routeStopsElements
                                    .Where(e => !string.IsNullOrWhiteSpace(e.Value))
                                    .Select(e => e.Value)
                                    .ToArray();
                                
                                if (validStops.Length > 0)
                                {
                                    schedule.RouteStops = validStops;
                                    // Only log at Debug level to prevent excessive logging
                                    if (Log.IsDebugEnabled)
                                    {
                                        Log.Debug("Schedule ID {0} has {1} stops.", schedule.RouteScheduleId, validStops.Length);
                                    }
                        }
                        else
                        {
                                    // All elements were empty/whitespace
                                    schedule.RouteStops = new string[] { schedule.StartPoint ?? "[?]", schedule.EndPoint ?? "[?]" };
                                    Log.Warn("Schedule ID {0} had route stops elements but all were empty. Using fallback.", schedule.RouteScheduleId);
                                }
                            }
                            else
                            {
                                // No elements found
                                schedule.RouteStops = new string[] { schedule.StartPoint ?? "[?]", schedule.EndPoint ?? "[?]" };
                                if (Log.IsTraceEnabled)
                                {
                                    Log.Trace("No route stops found for schedule ID {0}. Using fallback.", schedule.RouteScheduleId);
                                }
                            }
                            
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
                            
                            double price;
                            XElement priceElement = scheduleNode.Element("price");
                            if (priceElement != null && double.TryParse(priceElement.Value, out price))
                            {
                                schedule.Price = price;
                            }
                            
                            int availableSeats;
                            XElement availableSeatsElement = scheduleNode.Element("availableSeats");
                            if (availableSeatsElement != null && int.TryParse(availableSeatsElement.Value, out availableSeats))
                            {
                                schedule.AvailableSeats = availableSeats;
                            }
                            
                            XElement marshutElement = scheduleNode.Element("marshut");
                            if (marshutElement != null && marshutElement.HasElements)
                            {
                                long marshutIdFromNested = 0;
                                XElement marshutIdElement = marshutElement.Element("routeId");
                                if (marshutIdElement != null && long.TryParse(marshutIdElement.Value, out marshutIdFromNested))
                                {
                                    schedule.Marshut = _routes.FirstOrDefault(r => r.RouteId == marshutIdFromNested);
                                }
                            }
                            else if (schedule.Marshut == null && schedule.RouteId.HasValue)
                            {
                                schedule.Marshut = _routes.FirstOrDefault(r => r.RouteId == schedule.RouteId);
                            }
                            
                            loadedSchedules.Add(schedule);
                        }
                        catch (Exception exNode)
                        {
                            Log.Error(string.Format("Error parsing individual Schedule XML node: {0}. Node: {1}", exNode.ToString(), scheduleNode.ToString()));
                        }
                    }
                     Log.Debug("Parsed {0} schedules from XML.", loadedSchedules.Count);
                }
                catch (Exception ex)
                {
                    string errorMsgXml = string.Format("Error parsing Schedules XML. Exception: {0}", ex.ToString());
                    Log.Error(errorMsgXml);
                }
                
                Log.Debug("Populating internal collections and UI...");
                
                _availableBuses = loadedBuses;
                _availableDrivers = loadedDrivers;
                
                foreach (var schedule in loadedSchedules)
                {
                    if (schedule.Marshut == null && schedule.RouteId.HasValue)
                    {
                        schedule.Marshut = _routes.FirstOrDefault(r => r.RouteId == schedule.RouteId);
                    }
                }
                
                Action updateAction = delegate()
                {
                    if (this.IsDisposed) return;
                    
                            _schedules.Clear();
                    foreach(var s in loadedSchedules) { _schedules.Add(s); }
                    
                    gridControlSchedules.RefreshDataSource();
                };
                
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(updateAction);
                }
                
                Log.Info("Synchronous data load completed successfully for route {0}. Loaded {1} buses, {2} drivers, {3} schedules.",
                    selectedRouteIdStr, _availableBuses.Count, _availableDrivers.Count, loadedSchedules.Count);

            }
            catch (Exception ex)
            {
                string criticalErrorMsg = string.Format("Critical error during synchronous data load sequence. Exception: {0}", ex.ToString());
                Log.Error(criticalErrorMsg);
                
                Action errorAction = delegate()
                {
                    if (this.IsDisposed) return;
                    
                    _availableBuses.Clear();
                    _availableDrivers.Clear();
                        _schedules.Clear();
                    gridControlSchedules.RefreshDataSource();
                    
                    XtraMessageBox.Show("Произошла критическая ошибка при загрузке данных. См. лог.\n" + ex.Message, 
                        "Критическая Ошибка Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                
                if (this.IsHandleCreated && !this.IsDisposed) { this.BeginInvoke(errorAction); }
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
                
                Action finalUiAction = delegate()
                {
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
                Log.Debug(string.Format("Built GLOBAL ID map with {0} entries for {1} structure.", globalIdMap.Count, rootElementName));
            } catch (Exception mapEx) {
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
                     try {
                         JToken cleanedItem = CleanAndTransformJsonToken(item);
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
                try {
                    cleanedToken = CleanAndTransformJsonToken(rootToken);
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

        private void UpdateButtonStates()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(delegate() { if (!this.IsDisposed) UpdateButtonStates(); }));
                return;
            }
             if (this.IsDisposed) return;

            bool isLoading = _isBusy;
            bool isRowSelected = (gridControlSchedules != null && !gridControlSchedules.IsDisposed && gridViewSchedules != null) 
                                 ? gridViewSchedules.GetFocusedRow() is RouteSchedules 
                                 : false;

            if (btnAdd != null && !btnAdd.IsDisposed) btnAdd.Enabled = !isLoading;
            if (btnEdit != null && !btnEdit.IsDisposed) btnEdit.Enabled = !isLoading && isRowSelected;
            if (btnDelete != null && !btnDelete.IsDisposed) btnDelete.Enabled = !isLoading && isRowSelected;
            if (btnRefresh != null && !btnRefresh.IsDisposed) btnRefresh.Enabled = !isLoading;
            if (lueRouteFilter != null && !lueRouteFilter.IsDisposed) lueRouteFilter.Enabled = !isLoading;
            if (dateFilter != null && !dateFilter.IsDisposed) dateFilter.Enabled = !isLoading;
        }

        private void gridViewSchedules_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            _selectedSchedule = gridViewSchedules.GetRow(e.FocusedRowHandle) as RouteSchedules;
            UpdateButtonStates();
        }

        private void gridViewSchedules_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            // Use UnboundType check for compatibility
            if (e.Column.UnboundType != DevExpress.Data.UnboundColumnType.Bound && e.IsGetData)
            {
                 // Add verbose logging for the specific column
                 bool isStopsColumn = e.Column.FieldName == "RouteStopsDisplayString";
                 if (isStopsColumn) {
                     Log.Trace("CustomUnboundColumnData Fired for RouteStopsDisplayString. ListSourceRowIndex: {0}", e.ListSourceRowIndex);
                 }

                 RouteSchedules schedule = gridViewSchedules.GetRow(e.ListSourceRowIndex) as RouteSchedules;
                 if (schedule != null)
                 {
                     if (e.Column.FieldName == "Marshut.StartPoint") 
                     {
                         e.Value = (schedule.Marshut != null) ? schedule.Marshut.StartPoint : "[N/A]";
                     }
                     else if (e.Column.FieldName == "Marshut.EndPoint") 
                     {
                         e.Value = (schedule.Marshut != null) ? schedule.Marshut.EndPoint : "[N/A]";
                     }
                     else if (isStopsColumn) // Use the boolean flag checked earlier
                     {
                         string stopsValue = "[N/A]"; // Default value
                         if (schedule.RouteStops != null && schedule.RouteStops.Length > 0)
                         {
                             stopsValue = string.Join(", ", schedule.RouteStops);
                             // Log the actual stops being joined for the first few rows
                             if (e.ListSourceRowIndex < 5) {
                                 Log.Debug("Row {0}: Assigning stops: {1}", e.ListSourceRowIndex, stopsValue);
                             }
                }
                else
                {
                             stopsValue = "[Нет остановок]"; 
                             // Log if stops are null or empty for the first few rows
                             if (e.ListSourceRowIndex < 5) {
                                 Log.Debug("Row {0}: RouteStops array is null or empty. Assigning: {1}", e.ListSourceRowIndex, stopsValue);
                             }
                         }
                         e.Value = stopsValue;
                     }
                 }
                 else
                 {
                     // Log if the row object is not a RouteSchedules
                     if (isStopsColumn) {
                         Log.Warn("CustomUnboundColumnData: Row at index {0} is not a RouteSchedules object.", e.ListSourceRowIndex);
                     }
                     e.Value = null;
                 }
            }
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            Log.Info("Add button clicked.");
            if (_routes == null || _routes.Count <= 1 ||
                _availableBuses == null || !_availableBuses.Any() ||
                _availableDrivers == null || !_availableDrivers.Any())
            {
                Log.Warn("Add clicked but dependent data (routes/buses/drivers) is not loaded/available.");
                XtraMessageBox.Show("Необходимые данные для добавления не загружены. Попробуйте обновить.", "Данные Отсутствуют", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowEditRouteScheduleForm(null);
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
            Log.Info("Edit button clicked.");
            var selectedSchedule = gridViewSchedules.GetFocusedRow() as RouteSchedules;
            if (selectedSchedule == null)
            {
                Log.Warn("Edit button clicked, but no schedule selected.");
                return;
            }
            ShowEditRouteScheduleForm(selectedSchedule);
        }

        private void ShowEditRouteScheduleForm(RouteSchedules scheduleToEdit)
        {
            if (_routes == null || _routes.Count <= 1 ||
                _availableBuses == null || !_availableBuses.Any() ||
                _availableDrivers == null || !_availableDrivers.Any())
            {
                string missingData = "";
                if (_routes == null || _routes.Count <= 1) missingData += "маршруты, ";
                if (_availableBuses == null || _availableBuses.Count <= 0) missingData += "автобусы, ";
                if (_availableDrivers == null || _availableDrivers.Count <= 0) missingData += "водители, ";
                missingData = missingData.TrimEnd(' ', ',');

                string errorMsg = string.Format("Не удалось загрузить необходимые данные ({0}). Добавление/редактирование невозможно.", missingData);
                Log.Error("ShowEditRouteScheduleForm: " + errorMsg);
                XtraMessageBox.Show(errorMsg, "Ошибка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            using (var form = new XtraForm())
            {
                bool isAdding = scheduleToEdit == null;
                form.Text = isAdding ? "Добавить Расписание" : "Редактировать Расписание";
                form.Width = 600;
                form.Height = 550;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;

                var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(15) };
                form.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 150;
                int controlWidth = 400;
                int spacing = 35;
                
                var routeLabel = new LabelControl { Text = "Маршрут:", Location = new Point(15, yPos), Width = labelWidth };
                var routeCombo = new LookUpEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    Properties = {
                        DataSource = _routes.Where(r => r.RouteId != -1).OrderBy(r => r.StartPoint).ToList(),
                        DisplayMember = "StartPoint",
                        ValueMember = "RouteId",
                        NullText = "[Выберите маршрут...]",
                        ShowHeader = true
                    }
                };
                routeCombo.Properties.Columns.Add(new LookUpColumnInfo("RouteId", "ID", 50));
                routeCombo.Properties.Columns.Add(new LookUpColumnInfo("StartPoint", "Начало", 150));
                routeCombo.Properties.Columns.Add(new LookUpColumnInfo("EndPoint", "Конец", 150));
                panel.Controls.Add(routeLabel);
                panel.Controls.Add(routeCombo);
                yPos += spacing;

                var departureDateLabel = new LabelControl { Text = "Дата отправления:", Location = new Point(15, yPos), Width = labelWidth };
                var departureDateEdit = new DateEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth / 2 - 5,
                    Properties = { Mask = { EditMask = "dd.MM.yyyy", UseMaskAsDisplayFormat = true }, AllowNullInput = DevExpress.Utils.DefaultBoolean.False }
                };
                 panel.Controls.Add(departureDateLabel);
                panel.Controls.Add(departureDateEdit);

                var departureTimeLabel = new LabelControl { Text = "Время:", Location = new Point(labelWidth + 20 + controlWidth / 2 + 5, yPos), Width = 50 };
                var departureTimeEdit = new TimeEdit
                {
                    Location = new Point(labelWidth + 20 + controlWidth / 2 + 5 + 50 + 5, yPos),
                    Width = controlWidth / 2 - 5 - 50 - 5,
                    Properties = { Mask = { EditMask = "HH:mm", UseMaskAsDisplayFormat = true }, AllowNullInput = DevExpress.Utils.DefaultBoolean.False }
                };
                panel.Controls.Add(departureTimeLabel);
                panel.Controls.Add(departureTimeEdit);
                yPos += spacing;
                
                var arrivalDateLabel = new LabelControl { Text = "Дата прибытия:", Location = new Point(15, yPos), Width = labelWidth };
                var arrivalDateEdit = new DateEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth / 2 - 5,
                    Properties = { Mask = { EditMask = "dd.MM.yyyy", UseMaskAsDisplayFormat = true }, AllowNullInput = DevExpress.Utils.DefaultBoolean.False }
                };
                panel.Controls.Add(arrivalDateLabel);
                panel.Controls.Add(arrivalDateEdit);

                var arrivalTimeLabel = new LabelControl { Text = "Время:", Location = new Point(labelWidth + 20 + controlWidth / 2 + 5, yPos), Width = 50 };
                var arrivalTimeEdit = new TimeEdit
                {
                    Location = new Point(labelWidth + 20 + controlWidth / 2 + 5 + 50 + 5, yPos),
                    Width = controlWidth / 2 - 5 - 50 - 5,
                    Properties = { Mask = { EditMask = "HH:mm", UseMaskAsDisplayFormat = true }, AllowNullInput = DevExpress.Utils.DefaultBoolean.False }
                };
                panel.Controls.Add(arrivalTimeLabel);
                panel.Controls.Add(arrivalTimeEdit);
                yPos += spacing;
                
                var priceLabel = new LabelControl { Text = "Цена:", Location = new Point(15, yPos), Width = labelWidth };
                var priceEdit = new SpinEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    Properties = { Mask = { EditMask = "c2", UseMaskAsDisplayFormat = true }, MinValue = 0, AllowNullInput = DevExpress.Utils.DefaultBoolean.False }
                };
                panel.Controls.Add(priceLabel);
                panel.Controls.Add(priceEdit);
                yPos += spacing;
                
                var seatsLabel = new LabelControl { Text = "Доступные места:", Location = new Point(15, yPos), Width = labelWidth };
                var seatsEdit = new SpinEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    Properties = { Mask = { EditMask = "d" }, IsFloatValue = false, MinValue = 0, AllowNullInput = DevExpress.Utils.DefaultBoolean.False }
                };
                panel.Controls.Add(seatsLabel);
                panel.Controls.Add(seatsEdit);
                yPos += spacing;
                
                var activeLabel = new LabelControl { Text = "Активен:", Location = new Point(15, yPos), Width = labelWidth };
                var activeCheck = new CheckEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    Checked = true
                };
                panel.Controls.Add(activeLabel);
                panel.Controls.Add(activeCheck);
                yPos += spacing + 10;
                
                if (!isAdding && scheduleToEdit != null)
                {
                     routeCombo.EditValue = scheduleToEdit.RouteId;
                     departureDateEdit.DateTime = scheduleToEdit.DepartureTime.Date;
                    departureTimeEdit.Time = scheduleToEdit.DepartureTime;
                     arrivalDateEdit.DateTime = scheduleToEdit.ArrivalTime.Date;
                    arrivalTimeEdit.Time = scheduleToEdit.ArrivalTime;
                    priceEdit.Value = (decimal)scheduleToEdit.Price;
                    seatsEdit.Value = (decimal)scheduleToEdit.AvailableSeats;
                    activeCheck.Checked = scheduleToEdit.IsActive;
                }
                else
                {
                    departureDateEdit.DateTime = DateTime.Today;
                    arrivalDateEdit.DateTime = DateTime.Today;
                    priceEdit.Value = 0;
                    seatsEdit.Value = 0;
                    activeCheck.Checked = true;
                }

                var btnSave = new SimpleButton { Text = isAdding ? "Добавить" : "Сохранить", Location = new Point(form.ClientSize.Width / 2 + 10, yPos), Width = 100 };
                var btnCancel = new SimpleButton { Text = "Отмена", Location = new Point(form.ClientSize.Width / 2 - 110, yPos), Width = 100, DialogResult = DialogResult.Cancel };
                panel.Controls.Add(btnSave);
                panel.Controls.Add(btnCancel);
                form.CancelButton = btnCancel;
                
                btnSave.Click += async (s, args) => {
                    if (routeCombo.EditValue == null || routeCombo.EditValue == DBNull.Value || !(routeCombo.EditValue is long))
                    {
                        XtraMessageBox.Show("Пожалуйста, выберите действительный маршрут.", "Ошибка Валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        routeCombo.Focus();
                        return;
                    }
                    if (departureDateEdit.DateTime == DateTime.MinValue || arrivalDateEdit.DateTime == DateTime.MinValue || departureTimeEdit.Time == DateTime.MinValue || arrivalTimeEdit.Time == DateTime.MinValue)
                    {
                         XtraMessageBox.Show("Пожалуйста, укажите корректные дату и время отправления и прибытия.", "Ошибка Валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                         return;
                    }

                    DateTime departureDateTime = departureDateEdit.DateTime.Date + (departureTimeEdit.Time > DateTime.MinValue ? departureTimeEdit.Time.TimeOfDay : TimeSpan.Zero);
                    DateTime arrivalDateTime = arrivalDateEdit.DateTime.Date + (arrivalTimeEdit.Time > DateTime.MinValue ? arrivalTimeEdit.Time.TimeOfDay : TimeSpan.Zero);

                    if (departureDateTime >= arrivalDateTime)
                    {
                         XtraMessageBox.Show("Дата и время отправления должны быть раньше даты и времени прибытия.", "Ошибка Валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                         arrivalDateEdit.Focus();
                         return;
                    }

                    if (priceEdit.Value < 0) {
                        XtraMessageBox.Show("Цена не может быть отрицательной.", "Ошибка Валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        priceEdit.Focus();
                        return;
                    }
                     if (seatsEdit.Value < 0) {
                        XtraMessageBox.Show("Количество мест не может быть отрицательным.", "Ошибка Валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        seatsEdit.Focus();
                        return;
                    }

                    long selectedRouteId = (long)routeCombo.EditValue;
                    var selectedRoute = _routes.FirstOrDefault(r => r.RouteId == selectedRouteId);
                    if (selectedRoute == null)
                    {
                        Log.Error("Validation passed but could not find selected route object for ID: {0}", selectedRouteId);
                        XtraMessageBox.Show("Произошла внутренняя ошибка: выбранный маршрут не найден.", "Критическая Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                         }

                         var scheduleData = new RouteSchedules
                        {
                            RouteScheduleId = isAdding ? 0 : scheduleToEdit.RouteScheduleId,
                            RouteId = selectedRouteId,
                             StartPoint = selectedRoute.StartPoint,
                             EndPoint = selectedRoute.EndPoint,
                        DepartureTime = departureDateTime,
                        ArrivalTime = arrivalDateTime,
                        Price = (double)priceEdit.Value,
                        AvailableSeats = (int)seatsEdit.Value,
                        IsActive = activeCheck.Checked,
                             Marshut = null,
                        RouteStops = scheduleToEdit != null ? scheduleToEdit.RouteStops : new string[] { selectedRoute.StartPoint, selectedRoute.EndPoint },
                        DaysOfWeek = scheduleToEdit != null ? scheduleToEdit.DaysOfWeek : new string[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }
                    };

                    HttpClient crudClient = null;
                    string actionType = isAdding ? "Adding" : "Updating";

                    try
                    {
                        form.Cursor = Cursors.WaitCursor;
                        btnSave.Enabled = false;
                        btnCancel.Enabled = false;
                        panel.Enabled = false;

                        crudClient = _apiClient.CreateClient();
                        HttpResponseMessage response;

                        string jsonPayload = JsonConvert.SerializeObject(scheduleData, _jsonSettings);

                        HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        Log.Debug("{0} schedule. Payload (truncated): {1}", actionType, (jsonPayload.Length > 500 ? jsonPayload.Substring(0, 500) + "..." : jsonPayload));
                        
                        if (isAdding)
                        {
                            response = await crudClient.PostAsync(string.Format("{0}/RouteSchedules", _baseUrl), content).ConfigureAwait(false);
                        }
                        else
                        {
                            response = await crudClient.PutAsync(string.Format("{0}/RouteSchedules/{1}", _baseUrl, scheduleData.RouteScheduleId), content).ConfigureAwait(false);
                        }

                        if (!form.IsDisposed)
                        {
                            form.BeginInvoke(new Action(async delegate() {
                                if (form.IsDisposed) return;
                        
                        if (response.IsSuccessStatusCode)
                        {
                                    Log.Info("Schedule {0} successful.", actionType.ToLower());
                            form.DialogResult = DialogResult.OK;
                            form.Close();
                                    LoadDataSynchronously();
                        }
                        else
                        {
                                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                    string errorDetails = string.Format("Failed to {0} schedule. Status: {1}, Error: {2}", actionType.ToLower(), response.StatusCode, error);
                                    Log.Error(errorDetails);
                                    XtraMessageBox.Show(string.Format("Не удалось {0} расписание: {1}{2}{3}", 
                                        (isAdding ? "добавить" : "сохранить"), 
                                response.ReasonPhrase, 
                                        Environment.NewLine,
                                        error), 
                                "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                    form.Cursor = Cursors.Default;
                                    btnSave.Enabled = true;
                                    btnCancel.Enabled = true;
                                    panel.Enabled = true;
                                }
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorDetails = string.Format("Exception during {0} schedule: {1}", actionType.ToLower(), ex.ToString());
                        Log.Error(errorDetails);
                        if (!form.IsDisposed)
                        {
                            form.BeginInvoke(new Action(delegate() {
                                if (form.IsDisposed) return;
                                XtraMessageBox.Show("Критическая ошибка при сохранении расписания: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                form.Cursor = Cursors.Default;
                         btnSave.Enabled = true;
                         btnCancel.Enabled = true;
                                panel.Enabled = true;
                            }));
                        }
                    }
                };
                
                form.ShowDialog(this);
            }
        }
        
        private async void btnDelete_Click(object sender, EventArgs e)
        {
            Log.Info("Delete button clicked.");
            var selectedSchedule = gridViewSchedules.GetFocusedRow() as RouteSchedules;
            if (selectedSchedule == null) return;

            var result = XtraMessageBox.Show(string.Format("Вы уверены, что хотите удалить расписание ID {0} ({1} - {2})?",
                selectedSchedule.RouteScheduleId,
                (selectedSchedule.Marshut != null ? selectedSchedule.Marshut.StartPoint : selectedSchedule.StartPoint ?? "N/A"),
                (selectedSchedule.Marshut != null ? selectedSchedule.Marshut.EndPoint : selectedSchedule.EndPoint ?? "N/A")),
                "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Log.Debug("User confirmed deletion for schedule ID: {0}", selectedSchedule.RouteScheduleId);
                await DeleteScheduleAsync(selectedSchedule);
            }
            else
            {
                Log.Debug("User cancelled deletion for schedule ID: {0}", selectedSchedule.RouteScheduleId);
            }
        }
        
        private async Task DeleteScheduleAsync(RouteSchedules scheduleToDelete)
        {
            if (scheduleToDelete == null) {
                Log.Warn("DeleteScheduleAsync called with null schedule.");
                return;
            }

            Log.Debug("Attempting to delete schedule ID: {0}", scheduleToDelete.RouteScheduleId);
            SetLoadingState(true);
            HttpClient crudClient = null;
            try
            {
                crudClient = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/RouteSchedules/{1}", _baseUrl, scheduleToDelete.RouteScheduleId);
                Log.Debug("Sending DELETE request to: {0}", apiUrl);

                var response = await crudClient.DeleteAsync(apiUrl).ConfigureAwait(false);

                if (this.IsDisposed) return;
                this.BeginInvoke(new Action(async delegate() {
                    if (this.IsDisposed) return;
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Info("Schedule deleted successfully: ID {0}", scheduleToDelete.RouteScheduleId);
                        XtraMessageBox.Show("Расписание успешно удалено.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _schedules.Remove(scheduleToDelete);
                        gridControlSchedules.RefreshDataSource();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        XtraMessageBox.Show(string.Format("Ошибка при удалении расписания: {0}{1}{2}", 
                            response.ReasonPhrase, 
                            Environment.NewLine,
                            error), 
                                 "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                    SetLoadingState(false);
                }));
            }
                catch (Exception ex)
                {
                string errorMsg = string.Format("Exception deleting schedule ID {0}. Exception: {1}", scheduleToDelete.RouteScheduleId, ex.ToString());
                Log.Error(errorMsg);

                if (!this.IsDisposed) {
                    this.BeginInvoke(new Action(delegate() {
                        if (this.IsDisposed) return;
                        XtraMessageBox.Show("Произошла критическая ошибка при удалении расписания: " + ex.Message, "Критическая Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        SetLoadingState(false);
                    }));
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            Log.Info("Refresh button clicked.");
            if (!_selectedRouteIdFilter.HasValue)
            {
                Log.Warn("Refresh clicked, but no route is selected in the filter.");
                XtraMessageBox.Show("Пожалуйста, сначала выберите маршрут в фильтре.", "Требуется Маршрут", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            LoadDataSynchronously();
        }

        private void lueRouteFilter_EditValueChanged(object sender, EventArgs e)
        {
              if (_isBusy) return;

            LookUpEdit editor = sender as LookUpEdit;
            if (editor != null && editor.EditValue != null && editor.EditValue != DBNull.Value && editor.EditValue is long)
            {
                long newlySelectedRouteId = Convert.ToInt64(editor.EditValue);

                if (_selectedRouteIdFilter == null || _selectedRouteIdFilter.Value != newlySelectedRouteId)
                {
                    Log.Debug("Route filter changed via UI. New Route ID: {0}. Triggering data reload.", newlySelectedRouteId);
                    _selectedRouteIdFilter = newlySelectedRouteId;
                    LoadDataSynchronously();
                }
                else {
                    Log.Debug("Route filter EditValueChanged fired, but value ({0}) is the same as current filter. No reload triggered.", newlySelectedRouteId);
                }
            }
            else if (editor != null && editor.EditValue == null)
            {
                if (_selectedRouteIdFilter != null)
                {
                    Log.Debug("Route filter cleared via UI. Setting filter to null. Triggering reload (will likely show message).");
                    _selectedRouteIdFilter = null;
                    LoadDataSynchronously();
                }
            }
        }
        
        private void dateFilter_EditValueChanged(object sender, EventArgs e)
        {
             if (_isBusy) return;
            Log.Debug("Date filter changed. Triggering data reload.");
            LoadDataSynchronously();
        }
    }
} 