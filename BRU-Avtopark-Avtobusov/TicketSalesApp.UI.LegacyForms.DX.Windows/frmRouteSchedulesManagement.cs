using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Views.Base;
using Serilog;
using TicketSalesApp.Core.Models;
using System.Threading;


namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmRouteSchedulesManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private ObservableCollection<Marshut> _routes = new ObservableCollection<Marshut>();
        private RouteSchedules _selectedSchedule;
        private Marshut _selectedRoute;
        private bool _isBusy;
        private string _errorMessage = string.Empty;
        private bool _hasError;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        public frmRouteSchedulesManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            _httpClient = _apiClient.CreateClient();
            _baseUrl = "http://localhost:5000/api";
            
            // Add a placeholder item for "All Routes"
            _routes.Add(new Marshut { RouteId = -1, StartPoint = "[Все маршруты]", EndPoint = "" });

            lueRouteFilter.Properties.DataSource = _routes;
            lueRouteFilter.Properties.DisplayMember = "StartPoint";
            lueRouteFilter.Properties.ValueMember = "RouteId";

            dateFilter.Properties.NullText = "[Все даты]";
            dateFilter.EditValue = null;

            UpdateButtonStates();

            gridViewSchedules.FocusedRowChanged += gridViewSchedules_FocusedRowChanged;
            gridViewSchedules.CustomUnboundColumnData += gridViewSchedules_CustomUnboundColumnData;
            
            // Subscribe to auth token changes
            _apiClient.OnAuthTokenChanged += (s, e) => { 
                var oldClient = _httpClient;
                _httpClient = _apiClient.CreateClient();
                oldClient.Dispose();
                
                if (this.Visible) {
                    LoadRoutesAsync().ConfigureAwait(false);
                } 
            };
        }

        private async void frmRouteSchedulesManagement_Load(object sender, EventArgs e)
        {
            await LoadRoutesAsync();
        }

        private async Task LoadRoutesAsync()
        {
            if (_isBusy)
            {
                Log.Warning("LoadRoutesAsync called while already busy. Exiting.");
                return;
            }

            try
            {
                Log.Verbose("LoadRoutesAsync: Entering try block.");
                _isBusy = true;
                _hasError = false;
                _errorMessage = string.Empty;
                Log.Information("Starting LoadRoutesAsync...");
                
                var response = await _httpClient.GetAsync("routes");
                Log.Information("Finished _httpClient.GetAsync(\"routes\"). Status: {StatusCode}", response.StatusCode);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var loadedRoutes = JsonSerializer.Deserialize<List<Marshut>>(jsonResponse, _jsonOptions) ?? new List<Marshut>();

                    this.Invoke(new Action(() => {
                        Log.Verbose("LoadRoutesAsync (Invoke): Clearing and repopulating _routes collection.");
                        _routes.Clear();
                        _routes.Add(new Marshut { RouteId = -1, StartPoint = "[Все маршруты]", EndPoint = "" });
                        
                        foreach (var route in loadedRoutes)
                        {
                            _routes.Add(route);
                        }

                        Log.Verbose("LoadRoutesAsync (Invoke): Refreshing LookUpEdit datasource.");
                        lueRouteFilter.Properties.DataSource = null;
                        lueRouteFilter.Properties.DataSource = _routes;
                        lueRouteFilter.EditValue = -1;
                        Log.Verbose("LoadRoutesAsync (Invoke): LookUpEdit datasource refreshed and EditValue set to -1.");
                    }));

                    Log.Debug("Successfully loaded {0} routes.", loadedRoutes.Count);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _hasError = true;
                    _errorMessage = $"Failed to load routes: {errorContent}";
                    Log.Error("Failed to load routes. Status: {StatusCode}, Reason: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    this.Invoke(new Action(() => {
                         XtraMessageBox.Show($"Ошибка загрузки маршрутов: {response.ReasonPhrase}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }
            catch (Exception ex)
            {
                _hasError = true;
                _errorMessage = $"Error loading routes: {ex.Message}";
                Log.Error(ex, "Exception loading routes");
                this.Invoke(new Action(() => {
                     XtraMessageBox.Show($"Произошла ошибка при загрузке маршрутов: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 }));
            }
            finally
            {
                Log.Verbose("LoadRoutesAsync: Entering finally block.");
                _isBusy = false;
                Log.Information("Finished LoadRoutesAsync.");
            }
        }

        private async Task LoadSchedulesAsync(int? routeIdFilterParam, DateTime? dateFilterParam)
        {
            // Ensure this runs only once at a time
            if (_isBusy) 
            {
                 Log.Warning("LoadSchedulesAsync called while already busy. Exiting.");
                 return;
            }

            try
            {
                _isBusy = true; // Set busy flag
                Log.Verbose("LoadSchedulesAsync: Entering try block.");
                Cursor = Cursors.WaitCursor;
                _hasError = false;
                _errorMessage = string.Empty;
                
                // Clear previous data *before* fetching new data
                Log.Verbose("LoadSchedulesAsync: Clearing existing data source.");
                routeScheduleBindingSource.DataSource = null;
                
                var queryParams = new Dictionary<string, string>();
                // Use parameters passed to the method
                int? routeId = routeIdFilterParam;
                DateTime? selectedDate = dateFilterParam;

                // Always fetch all schedules from the API
                var url = $"{_baseUrl}/routeschedules";
                Log.Information("Fetching all route schedules from: {ApiUrl}", url);

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("LoadSchedulesAsync: Raw JSON response received:\n{JsonResponse}", jsonResponse); // Log raw JSON

                    var allSchedules = JsonSerializer.Deserialize<List<RouteSchedules>>(jsonResponse, _jsonOptions) ?? new List<RouteSchedules>();
                    
                    Log.Information("Successfully loaded {Count} schedules from API.", allSchedules.Count);

                    // Create a new list for filtered schedules
                    List<RouteSchedules> filteredSchedules = new List<RouteSchedules>();
                    
                    // Manually filter schedules based on selected criteria
                    Log.Verbose("Starting manual client-side filtering. Route Filter: {RouteIdFilter}, Date Filter: {DateFilter}", 
                              routeId?.ToString() ?? "None", selectedDate?.ToString("yyyy-MM-dd") ?? "None");

                    foreach (var schedule in allSchedules) // Iterate through API results
                    {
                        Log.Verbose("-- Checking Schedule ID: {ScheduleId}, Route ID: {SchedRouteId}, Departure: {SchedDepartureDate}", 
                                  schedule.RouteScheduleId, schedule.RouteId, schedule.DepartureTime.Date.ToString("yyyy-MM-dd"));

                        bool routeMatch = true; // Assume match unless filtered out
                        bool dateMatch = true;  // Assume match unless filtered out

                        // Check Route Filter (only if a specific route is selected)
                        if (routeId.HasValue && routeId.Value != -1)
                        {
                             routeMatch = schedule.RouteId == routeId.Value;
                             Log.Verbose("   Route Filter Check ({FilterValue}): Match = {RouteMatch}", routeId.Value, routeMatch);
                        }
                        else
                        {
                             Log.Verbose("   Route Filter Check: Skipped (No specific route selected).");
                        }

                        // Check Date Filter (only if a date is selected)
                        if (selectedDate.HasValue)
                        {
                             dateMatch = schedule.DepartureTime.Date == selectedDate.Value.Date;
                             Log.Verbose("   Date Filter Check ({FilterValue}): Match = {DateMatch}", selectedDate.Value.Date.ToString("yyyy-MM-dd"), dateMatch);
                        }
                        else
                        {
                            Log.Verbose("   Date Filter Check: Skipped (No date selected).");
                        }

                        // Verify schedule data integrity
                        if (schedule.RouteId == null || schedule.RouteId <= 0)
                        {
                            Log.Warning("Schedule {Id} has invalid RouteId: {RouteId}", 
                                schedule.RouteScheduleId, schedule.RouteId);
                            routeMatch = false;
                        }
                        
                        if (string.IsNullOrEmpty(schedule.StartPoint) || string.IsNullOrEmpty(schedule.EndPoint))
                        {
                            Log.Warning("Schedule {Id} has missing start/end points: Start={Start}, End={End}", 
                                schedule.RouteScheduleId, schedule.StartPoint, schedule.EndPoint);
                            routeMatch = false;
                        }
                        
                        if (schedule.RouteStops == null || schedule.RouteStops.Length < 2)
                        {
                            Log.Warning("Schedule {Id} has invalid route stops: {StopCount}", 
                                schedule.RouteScheduleId, schedule.RouteStops?.Length ?? 0);
                            routeMatch = false;
                        }
                        
                        if (schedule.DepartureTime >= schedule.ArrivalTime)
                        {
                            Log.Warning("Schedule {Id} has invalid times: Departure={Departure}, Arrival={Arrival}", 
                                schedule.RouteScheduleId, schedule.DepartureTime, schedule.ArrivalTime);
                            routeMatch = false;
                        }
                        
                        if (!schedule.Price.HasValue || schedule.Price < 0)
                        {
                            Log.Warning("Schedule {Id} has invalid price: {Price}", 
                                schedule.RouteScheduleId, schedule.Price);
                            routeMatch = false;
                        }
                        
                        if (!schedule.AvailableSeats.HasValue || schedule.AvailableSeats < 0)
                        {
                            Log.Warning("Schedule {Id} has invalid seat count: {Seats}", 
                                schedule.RouteScheduleId, schedule.AvailableSeats);
                            routeMatch = false;
                        }
                        
                        // Add to list ONLY if all active filters match
                        if (routeMatch && dateMatch)
                        {
                            filteredSchedules.Add(schedule);
                            Log.Verbose("   >> Added Schedule ID: {ScheduleId} to filtered list.", schedule.RouteScheduleId);
                        }
                        else
                        {
                            Log.Verbose("   -- Schedule ID: {ScheduleId} did NOT match all filters.", schedule.RouteScheduleId);
                        }
                    }
                    Log.Information("Finished manual client-side filtering. {FilteredCount} of {OriginalCount} schedules remain.", filteredSchedules.Count, allSchedules.Count);

                    // Sort by departure time
                    filteredSchedules.Sort((a, b) => a.DepartureTime.CompareTo(b.DepartureTime));
                    
                    Log.Information("Manual filtering complete. {FilteredCount} of {TotalCount} schedules remain", 
                        filteredSchedules.Count, allSchedules.Count);

                    // Update the grid with filtered results
                    Log.Verbose("LoadSchedulesAsync: Calling gridViewSchedules.BeginUpdate()");
                    gridViewSchedules.BeginUpdate();
                    try
                    {
                        Log.Verbose("LoadSchedulesAsync: Assigning filtered data source to grid");
                        routeScheduleBindingSource.DataSource = filteredSchedules;
                        Log.Verbose("LoadSchedulesAsync: Data source assigned.");
                    }
                    finally
                    {
                        Log.Verbose("LoadSchedulesAsync: Calling gridViewSchedules.EndUpdate()");
                        gridViewSchedules.EndUpdate(); 
                        Log.Verbose("LoadSchedulesAsync: gridViewSchedules.EndUpdate() completed.");
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _hasError = true;
                    _errorMessage = $"Failed to load schedules: {errorContent}";
                    Log.Error("Failed to load route schedules. Status: {StatusCode}, Reason: {ReasonPhrase}, Content: {ErrorContent}",
                                     response.StatusCode, response.ReasonPhrase, errorContent);
                    // Use Invoke for UI updates
                    this.Invoke(new Action(() => {
                        XtraMessageBox.Show($"Ошибка загрузки расписаний: {response.ReasonPhrase} ({errorContent})", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        routeScheduleBindingSource.DataSource = new List<RouteSchedules>(); // Clear grid on error
                    }));
                }
            }
            catch (Exception ex)
            {
                _hasError = true;
                _errorMessage = $"Error loading schedules: {ex.Message}";
                Log.Error(ex, "Exception loading route schedules");
                 // Use Invoke for UI updates
                 this.Invoke(new Action(() => {
                    XtraMessageBox.Show($"Произошла ошибка при загрузке расписаний: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    routeScheduleBindingSource.DataSource = new List<RouteSchedules>(); // Clear grid on error
                 }));
            }
            finally
            {
                Log.Verbose("LoadSchedulesAsync: Entering finally block.");
                // Use Invoke for UI updates
                this.Invoke(new Action(() => {
                    Cursor = Cursors.Default;
                    UpdateButtonStates();
                    // gridViewSchedules.BestFitColumns(); // Maybe move BestFitColumns after EndUpdate or remove if too slow
                    Log.Verbose("LoadSchedulesAsync (finally-Invoke): UI updates done (Cursor, Buttons).");
                }));
                _isBusy = false; // Reset busy flag
                Log.Verbose("LoadSchedulesAsync: Exiting finally block. _isBusy = {IsBusy}", _isBusy);
            }
        }

        private void UpdateButtonStates()
        {
            _selectedSchedule = gridViewSchedules.GetFocusedRow() as RouteSchedules;
            bool isRowSelected = _selectedSchedule != null;

            btnEdit.Enabled = isRowSelected;
            btnDelete.Enabled = isRowSelected;
        }

        private void gridViewSchedules_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void gridViewSchedules_CustomUnboundColumnData(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDataEventArgs e)
        {
            if (e.IsGetData) // Only process when data is needed for display
            {
                if (e.Row is RouteSchedules schedule)
                {
                    // Try to get route info directly from the schedule's Marshut object first
                    var route = schedule.Marshut;

                    // If Marshut is null but RouteId exists, try finding it in the loaded _routes collection
                    if (route == null && schedule.RouteId > 0)
                    {
                        route = _routes.FirstOrDefault(r => r.RouteId == schedule.RouteId);
                    }

                    // Set the value based on the found route
                    if (route != null)
                    {
                        if (e.Column == colRouteStartPoint)
                        {
                            e.Value = route.StartPoint;
                        }
                        else if (e.Column == colRouteEndPoint)
                        {
                            e.Value = route.EndPoint;
                        }
                    }
                    else
                    {
                         // Handle cases where the route cannot be determined
                         if (e.Column == colRouteStartPoint || e.Column == colRouteEndPoint)
                         {
                             e.Value = schedule.RouteId > 0 ? "(Маршрут не загружен)" : "(ID маршрута не указан)";
                         }
                    }
                }
                else
                {
                     // Handle cases where the row object is not a RouteSchedules
                     if (e.Column == colRouteStartPoint || e.Column == colRouteEndPoint)
                     {
                         e.Value = "(Ошибка данных)";
                     }
                }
            }
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            if (_routes == null || _routes.Count <= 1) // Only has the placeholder
            {
                XtraMessageBox.Show("Список маршрутов пуст. Невозможно добавить расписание.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ShowEditRouteScheduleForm(null);
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
            if (_selectedSchedule == null)
            {
                XtraMessageBox.Show("Пожалуйста, выберите расписание для редактирования.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Log.Information("Editing schedule with ID: {0}", _selectedSchedule.RouteScheduleId);
            ShowEditRouteScheduleForm(_selectedSchedule);
        }

        private void ShowEditRouteScheduleForm(RouteSchedules scheduleToEdit)
        {
            bool isAdding = scheduleToEdit == null;
            
            using (var form = new XtraForm())
            {
                form.Text = isAdding ? "Добавить расписание" : "Редактировать расписание";
                form.Width = 600;
                form.Height = 500;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var panel = new PanelControl { 
                    Dock = DockStyle.Fill, 
                    Padding = new Padding(15) 
                };
                form.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 160;
                int controlWidth = 350;
                int spacing = 35;
                
                // Route selection
                var routeLabel = new LabelControl { 
                    Text = "Маршрут:", 
                    Location = new Point(15, yPos), 
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Width = labelWidth 
                };
                
                var routeCombo = new LookUpEdit { 
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth 
                };
                
                // Set up route ComboBox properties
                routeCombo.Properties.DisplayMember = "StartPoint";
                routeCombo.Properties.ValueMember = "RouteId";
                routeCombo.Properties.DataSource = _routes.Where(r => r.RouteId != -1).ToList(); // Exclude placeholder
                routeCombo.Properties.Columns.Add(new LookUpColumnInfo("RouteId", "ID", 40));
                routeCombo.Properties.Columns.Add(new LookUpColumnInfo("StartPoint", "Отправление", 130));
                routeCombo.Properties.Columns.Add(new LookUpColumnInfo("EndPoint", "Прибытие", 130));
                routeCombo.Properties.NullText = "[Выберите маршрут]";
                routeCombo.Properties.SearchMode = SearchMode.AutoComplete;
                routeCombo.Properties.AutoSearchColumnIndex = 1; // Search by StartPoint
                
                if (!isAdding && scheduleToEdit.RouteId > 0)
                {
                    routeCombo.EditValue = scheduleToEdit.RouteId;
                }
                
                panel.Controls.Add(routeLabel);
                panel.Controls.Add(routeCombo);
                yPos += spacing;
                
                // Departure time
                var departureLabel = new LabelControl { 
                    Text = "Время отправления:", 
                    Location = new Point(15, yPos), 
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Width = labelWidth 
                };
                
                var departureDateEdit = new DateEdit { 
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth / 2 - 5,
                    EditValue = isAdding ? DateTime.Today : scheduleToEdit?.DepartureTime.Date
                };
                departureDateEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                departureDateEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                
                var departureTimeEdit = new TimeEdit { 
                    Location = new Point(labelWidth + 20 + controlWidth / 2 + 5, yPos),
                    Width = controlWidth / 2 - 5,
                    EditValue = isAdding ? DateTime.Now.TimeOfDay : scheduleToEdit?.DepartureTime.TimeOfDay
                };
                departureTimeEdit.Properties.Mask.EditMask = "HH:mm";
                departureTimeEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                departureTimeEdit.Properties.Buttons.Clear(); // Remove calendar button for time
                
                panel.Controls.Add(departureLabel);
                panel.Controls.Add(departureDateEdit);
                panel.Controls.Add(departureTimeEdit);
                yPos += spacing;
                
                // Arrival time
                var arrivalLabel = new LabelControl { 
                    Text = "Время прибытия:", 
                    Location = new Point(15, yPos), 
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Width = labelWidth 
                };
                
                var arrivalDateEdit = new DateEdit { 
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth / 2 - 5,
                    EditValue = isAdding ? DateTime.Today : scheduleToEdit?.ArrivalTime.Date
                };
                arrivalDateEdit.Properties.Mask.EditMask = "dd.MM.yyyy";
                arrivalDateEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                
                var arrivalTimeEdit = new TimeEdit { 
                    Location = new Point(labelWidth + 20 + controlWidth / 2 + 5, yPos),
                    Width = controlWidth / 2 - 5,
                    EditValue = isAdding ? DateTime.Now.AddHours(1).TimeOfDay : scheduleToEdit?.ArrivalTime.TimeOfDay
                };
                arrivalTimeEdit.Properties.Mask.EditMask = "HH:mm";
                arrivalTimeEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                arrivalTimeEdit.Properties.Buttons.Clear(); // Remove calendar button for time
                
                panel.Controls.Add(arrivalLabel);
                panel.Controls.Add(arrivalDateEdit);
                panel.Controls.Add(arrivalTimeEdit);
                yPos += spacing;
                
                // Price
                var priceLabel = new LabelControl { 
                    Text = "Цена:", 
                    Location = new Point(15, yPos), 
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Width = labelWidth 
                };
                
                var priceEdit = new SpinEdit { 
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    EditValue = isAdding ? 0m : (scheduleToEdit?.Price.HasValue == true ? (decimal)scheduleToEdit.Price.Value : 0m)
                };
                priceEdit.Properties.Mask.EditMask = "c";
                priceEdit.Properties.Mask.UseMaskAsDisplayFormat = true;
                priceEdit.Properties.Increment = 100m;
                priceEdit.Properties.MinValue = 0;
                
                panel.Controls.Add(priceLabel);
                panel.Controls.Add(priceEdit);
                yPos += spacing;
                
                // Available Seats
                var seatsLabel = new LabelControl { 
                    Text = "Доступные места:", 
                    Location = new Point(15, yPos), 
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Width = labelWidth 
                };
                
                var seatsEdit = new SpinEdit { 
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    EditValue = isAdding ? 40 : scheduleToEdit?.AvailableSeats ?? 40
                };
                seatsEdit.Properties.Mask.EditMask = "d";
                seatsEdit.Properties.MinValue = 0;
                seatsEdit.Properties.MaxValue = 100;
                
                panel.Controls.Add(seatsLabel);
                panel.Controls.Add(seatsEdit);
                yPos += spacing;
                
                // Active status
                var activeLabel = new LabelControl { 
                    Text = "Активен:", 
                    Location = new Point(15, yPos), 
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Width = labelWidth 
                };
                
                var activeCheck = new CheckEdit { 
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    EditValue = isAdding ? true : scheduleToEdit?.IsActive ?? true
                };
                
                panel.Controls.Add(activeLabel);
                panel.Controls.Add(activeCheck);
                yPos += spacing;
                
                // Recurring status
                var recurringLabel = new LabelControl { 
                    Text = "Повторяющееся:", 
                    Location = new Point(15, yPos), 
                    AutoSizeMode = LabelAutoSizeMode.None,
                    Width = labelWidth 
                };
                
                var recurringCheck = new CheckEdit { 
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    EditValue = isAdding ? true : scheduleToEdit?.IsRecurring ?? true
                };
                
                panel.Controls.Add(recurringLabel);
                panel.Controls.Add(recurringCheck);
                yPos += spacing + 20;
                
                // Buttons
                var btnCancel = new SimpleButton { 
                    Text = "Отмена", 
                    Location = new Point(form.Width / 2 - 110, yPos),
                    Width = 100,
                    DialogResult = DialogResult.Cancel
                };
                
                var btnSave = new SimpleButton { 
                    Text = isAdding ? "Добавить" : "Сохранить", 
                    Location = new Point(form.Width / 2 + 10, yPos),
                    Width = 100
                };
                
                panel.Controls.Add(btnCancel);
                panel.Controls.Add(btnSave);
                
                form.CancelButton = btnCancel;
                
                // Save button click handler
                btnSave.Click += async (s, args) => {
                    // Validate inputs
                    if (routeCombo.EditValue == null)
                    {
                        XtraMessageBox.Show("Необходимо выбрать маршрут.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    if (departureDateEdit.EditValue == null || departureTimeEdit.EditValue == null)
                    {
                        XtraMessageBox.Show("Необходимо указать дату и время отправления.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    if (arrivalDateEdit.EditValue == null || arrivalTimeEdit.EditValue == null)
                    {
                        XtraMessageBox.Show("Необходимо указать дату и время прибытия.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    // Create combined DateTime objects
                    var departureDate = (DateTime)departureDateEdit.EditValue;
                    var departureTime = (TimeSpan)departureTimeEdit.EditValue;
                    var departureDateTime = departureDate.Date.Add(departureTime);
                    
                    var arrivalDate = (DateTime)arrivalDateEdit.EditValue;
                    var arrivalTime = (TimeSpan)arrivalTimeEdit.EditValue;
                    var arrivalDateTime = arrivalDate.Date.Add(arrivalTime);
                    
                    // Validate departure and arrival times
                    if (arrivalDateTime <= departureDateTime)
                    {
                        XtraMessageBox.Show("Время прибытия должно быть позже времени отправления.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    try
                    {
                        // Get selected route for start/end points
                        var selectedRouteId = (int)routeCombo.EditValue;
                        var selectedRoute = _routes.FirstOrDefault(r => r.RouteId == selectedRouteId);
                        
                        // Get route configuration (stops)
                        var routeConfig = GetRouteConfiguration(selectedRoute);
                        var routeStops = routeConfig?.stops ?? new[] { selectedRoute.StartPoint, selectedRoute.EndPoint };
                        
                        // Calculate estimated times and distances
                        var estimatedTimes = new string[routeStops.Length];
                        var stopDistances = new double[routeStops.Length];
                        var totalMinutes = (arrivalDateTime - departureDateTime).TotalMinutes;
                        var minutesPerStop = totalMinutes / (routeStops.Length - 1);
                        
                        for (int i = 0; i < routeStops.Length; i++)
                        {
                            estimatedTimes[i] = departureDateTime.AddMinutes(i * minutesPerStop).ToString("HH:mm");
                            stopDistances[i] = Math.Round(i * (6.0 / (routeStops.Length - 1)), 2); // Assuming average route length of 6km
                        }
                        
                        var schedule = new RouteSchedules
                        {
                            RouteScheduleId = isAdding ? 0 : scheduleToEdit.RouteScheduleId,
                            RouteId = selectedRouteId,
                            StartPoint = routeStops.First(),
                            EndPoint = routeStops.Last(),
                            RouteStops = routeStops,
                            DepartureTime = departureDateTime,
                            ArrivalTime = arrivalDateTime,
                            Price = (double?)((decimal)priceEdit.EditValue),
                            AvailableSeats = (int)seatsEdit.EditValue,
                            IsActive = (bool)activeCheck.EditValue,
                            IsRecurring = (bool)recurringCheck.EditValue,
                            DaysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                            BusTypes = new[] { "МАЗ-103", "МАЗ-107" },
                            ValidFrom = departureDate.Date,
                            ValidUntil = departureDate.Date.AddMonths(3),
                            StopDurationMinutes = 5,
                            EstimatedStopTimes = estimatedTimes,
                            StopDistances = stopDistances,
                            Notes = $"Маршрут {routeStops.First()} - {routeStops.Last()}",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now,
                            UpdatedBy = "Admin"
                        };
                        
                        var client = _apiClient.CreateClient();
                        string jsonData = JsonSerializer.Serialize(schedule, _jsonOptions);
                        Log.Information("Sending route schedule data to API: {Json}", jsonData);
                        StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                        HttpResponseMessage response;
                        
                        if (isAdding)
                        {
                            response = await client.PostAsync("routeschedules", content);
                        }
                        else
                        {
                            response = await client.PutAsync($"routeschedules/{schedule.RouteScheduleId}", content);
                        }
                        
                        if (response.IsSuccessStatusCode)
                        {
                            Log.Information(isAdding ? 
                                "Successfully added new route schedule with {Stops} stops from {Start} to {End}" : 
                                $"Successfully updated route schedule ID: {schedule.RouteScheduleId}",
                                routeStops.Length, schedule.StartPoint, schedule.EndPoint);
                                
                            await LoadSchedulesAsync(null, null);
                            form.DialogResult = DialogResult.OK;
                            form.Close();
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            Log.Error("Failed to {0} route schedule. Status: {1}, Content: {2}", 
                                isAdding ? "add" : "update", response.StatusCode, errorContent);
                                
                            XtraMessageBox.Show($"Ошибка при {(isAdding ? "добавлении" : "обновлении")} расписания: {errorContent}", 
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error {0} route schedule", isAdding ? "adding" : "updating");
                        XtraMessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                
                form.ShowDialog(this);
            }
        }
        
        private (string start, string end, string[] stops)? GetRouteConfiguration(Marshut route)
        {
            if (route == null) return null;
            
            // Define route configurations based on DbInitializer
            var routeConfigs = new Dictionary<(string start, string end), string[]> {
                {("Вейнянка", "Фатина"), new[] {"Вейнянка", "Площадь Орджоникидзе", "Областная больница", "Фатина"}},
                {("Малая Боровка", "Солтановка"), new[] {"Малая Боровка", "Машековка", "Центр", "Солтановка"}},
                {("Железнодорожный вокзал", "Спутник"), new[] {"Вокзал", "Площадь Ленина", "Универмаг", "Спутник"}},
                {("Мясокомбинат", "Заводская"), new[] {"Мясокомбинат", "Димитрова", "Юбилейный", "Заводская"}},
                {("Броды", "Казимировка"), new[] {"Броды", "Центр", "Площадь Славы", "Казимировка"}},
                {("Гребеневский рынок", "Холмы"), new[] {"Гребеневский рынок", "Площадь Орджоникидзе", "Мир", "Холмы"}},
                {("Автовокзал", "Полыковичи"), new[] {"Автовокзал", "Площадь Ленина", "Димитрова", "Полыковичи"}},
                {("Центр", "Сидоровичи"), new[] {"Центр", "Площадь Славы", "Заднепровье", "Сидоровичи"}},
                {("Площадь Славы", "Буйничи"), new[] {"Площадь Славы", "Областная больница", "Зоосад", "Буйничи"}},
                {("Заднепровье", "Химволокно"), new[] {"Заднепровье", "Центр", "Юбилейный", "Химволокно"}},
                {("Вокзал", "Соломинка"), new[] {"Вокзал", "Центр", "Димитрова", "Соломинка"}},
                {("Площадь Ленина", "Чаусы"), new[] {"Площадь Ленина", "Центр", "Заднепровье", "Чаусы"}},
                {("Могилев-2", "Дашковка"), new[] {"Могилев-2", "Центр", "Юбилейный", "Дашковка"}},
                {("Кожзавод", "Сухари"), new[] {"Кожзавод", "Центр", "Площадь Славы", "Сухари"}},
                {("Гребеневский рынок", "Любуж"), new[] {"Гребеневский рынок", "Центр", "Заднепровье", "Любуж"}}
            };
            
            var key = routeConfigs.Keys.FirstOrDefault(k => k.start == route.StartPoint && k.end == route.EndPoint);
            if (key != default)
            {
                return (key.start, key.end, routeConfigs[key]);
            }
            
            return null;
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedSchedule == null)
            {
                XtraMessageBox.Show("Пожалуйста, выберите расписание для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await DeleteScheduleAsync(_selectedSchedule);
        }
        
        private async Task DeleteScheduleAsync(RouteSchedules schedule)
        {
            string routeDisplay = "(неизвестно)";
            if (schedule.Marshut != null)
            {
                routeDisplay = $"{schedule.Marshut.StartPoint} - {schedule.Marshut.EndPoint}";
            }

            var result = XtraMessageBox.Show($"Вы уверены, что хотите удалить расписание для маршрута '{routeDisplay}' от {schedule.DepartureTime:dd.MM.yyyy HH:mm}?",
                                               "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Log.Information("Attempting to delete schedule with ID: {0}", schedule.RouteScheduleId);
                    var client = _apiClient.CreateClient();
                    var response = await client.DeleteAsync($"routeschedules/{schedule.RouteScheduleId}");

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Successfully deleted schedule with ID: {0}", schedule.RouteScheduleId);
                        XtraMessageBox.Show("Расписание успешно удалено.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadSchedulesAsync(null, null); // Reload schedules after deleting
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to delete schedule. Status: {StatusCode}, Reason: {ReasonPhrase}, Content: {ErrorContent}",
                                         response.StatusCode, response.ReasonPhrase, errorContent);
                        XtraMessageBox.Show($"Ошибка при удалении расписания: {response.ReasonPhrase} ({errorContent})", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting schedule with ID: {0}", schedule.RouteScheduleId);
                    XtraMessageBox.Show($"Произошла ошибка при удалении расписания: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                Log.Information("Deletion cancelled for schedule ID: {0}", schedule.RouteScheduleId);
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            Log.Information("Refresh button clicked. Reading filters and calling LoadSchedulesAsync.");
            
            // Read values directly from controls ON THE UI THREAD
            int? currentRouteId = lueRouteFilter.EditValue as int?;
            DateTime? currentDate = dateFilter.EditValue as DateTime?;

            Log.Information("Passing filters to LoadSchedulesAsync - Route ID: {RouteId}, Date: {Date}", 
                currentRouteId?.ToString() ?? "None", currentDate?.ToString("yyyy-MM-dd") ?? "None");

            await LoadSchedulesAsync(currentRouteId, currentDate); // Pass values
        }

    }
} 