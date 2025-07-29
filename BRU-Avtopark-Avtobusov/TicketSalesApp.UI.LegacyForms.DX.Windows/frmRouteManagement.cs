using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using TicketSalesApp.Core.Models; // Ensure Marshut, Avtobus, Employee, Bilet are here
using Serilog;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmRouteManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private readonly string _baseUrl = "http://localhost:5000/api"; // Added Base API URL
        private List<Marshut> _allRoutes = new List<Marshut>();
        private List<Avtobus> _buses = new List<Avtobus>();
        private List<Employee> _drivers = new List<Employee>();
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        public frmRouteManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            gridViewRoutes.CustomUnboundColumnData += new CustomColumnDataEventHandler(gridViewRoutes_CustomUnboundColumnData);
            
            _apiClient.OnAuthTokenChanged += async delegate(object sender, string token) {
                await LoadRoutesAsync();
                await LoadBusesAsync();
                await LoadDriversAsync();
            };
            
            UpdateButtonStates();
        }

        private async void frmRouteManagement_Load(object sender, EventArgs e)
        {
            await LoadAllDataAsync();
        }

        private async Task LoadAllDataAsync()
        {
            await LoadRoutesAsync();
            await LoadBusesAsync();
            await LoadDriversAsync();
        }

        private async Task LoadRoutesAsync()
        {
            HttpClient client = null;
            try
            {
                client = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/Routes?includeBus=true&includeDriver=true&includeTickets=true", _baseUrl); // Use Routes and string.Format
                Log.Debug("Fetching routes from: {0}", apiUrl);
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/routes: {JsonResponse}", jsonResponse); 
                    
                    _allRoutes = JsonConvert.DeserializeObject<List<Marshut>>(jsonResponse, _jsonSettings);
                    
                    if (_allRoutes == null) { _allRoutes = new List<Marshut>(); }
                    
                    FilterAndBindRoutes();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load routes. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    _allRoutes = new List<Marshut>();
                    FilterAndBindRoutes();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading routes");
                XtraMessageBox.Show(string.Format("Произошла ошибка при загрузке маршрутов: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allRoutes = new List<Marshut>();
                FilterAndBindRoutes();
            }
            finally
            {
                if (client != null) client.Dispose();
                UpdateButtonStates();
            }
        }

        private async Task LoadBusesAsync()
        {
            HttpClient client = null;
            try
            {
                client = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/Buses", _baseUrl); // Use Buses and string.Format
                Log.Debug("Fetching buses from: {0}", apiUrl);
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/buses: {JsonResponse}", jsonResponse);
                    
                    _buses = JsonConvert.DeserializeObject<List<Avtobus>>(jsonResponse, _jsonSettings);
                    
                    if (_buses == null) { _buses = new List<Avtobus>(); }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load buses. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    _buses = new List<Avtobus>();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading buses");
                _buses = new List<Avtobus>();
            }
            finally { if (client != null) client.Dispose(); }
        }

        private async Task LoadDriversAsync()
        {
            HttpClient client = null;
            try
            {
                client = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/Employees/search?jobTitle=Водитель", _baseUrl); // Use Employees/search and string.Format
                Log.Debug("Fetching drivers from: {0}", apiUrl);
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/employees: {JsonResponse}", jsonResponse);
                    
                    _drivers = JsonConvert.DeserializeObject<List<Employee>>(jsonResponse, _jsonSettings);
                    
                    if (_drivers == null) { _drivers = new List<Employee>(); }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load drivers. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    _drivers = new List<Employee>();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading drivers");
                _drivers = new List<Employee>();
            }
            finally { if (client != null) client.Dispose(); }
        }

        private void FilterAndBindRoutes()
        {
            var searchText = txtSearch.Text.Trim().ToLowerInvariant();
            List<Marshut> filteredRoutes = new List<Marshut>();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredRoutes = _allRoutes;
            }
            else
            {
                filteredRoutes = _allRoutes.Where(delegate(Marshut r) {
                    bool startMatch = (r.StartPoint != null && r.StartPoint.ToLowerInvariant().Contains(searchText));
                    bool endMatch = (r.EndPoint != null && r.EndPoint.ToLowerInvariant().Contains(searchText));
                    bool busMatch = (r.Avtobus != null && r.Avtobus.Model != null && r.Avtobus.Model.ToLowerInvariant().Contains(searchText));
                    bool driverSurnameMatch = (r.Employee != null && r.Employee.Surname != null && r.Employee.Surname.ToLowerInvariant().Contains(searchText));
                    bool driverNameMatch = (r.Employee != null && r.Employee.Name != null && r.Employee.Name.ToLowerInvariant().Contains(searchText));
                    return startMatch || endMatch || busMatch || driverSurnameMatch || driverNameMatch;
                }).ToList();
            }

            routeBindingSource.DataSource = filteredRoutes;
            gridControlRoutes.RefreshDataSource();
        }

        private void gridViewRoutes_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (!(e.Row is Marshut)) return;
            Marshut route = (Marshut)e.Row; 

            if (e.IsGetData)
            {
                if (e.Column.FieldName == "Avtobus.Model")
                {
                    e.Value = (route.Avtobus != null) ? route.Avtobus.Model : null;
                }
                else if (e.Column.FieldName == "Employee.Surname")
                {
                    e.Value = (route.Employee != null) ? route.Employee.Surname : null;
                }
                else if (e.Column.FieldName == "Tickets.Count")
                {
                    e.Value = (route.Tickets != null) ? route.Tickets.Count : 0;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if ((_buses == null || !_buses.Any()) || (_drivers == null || !_drivers.Any()))
            {
                XtraMessageBox.Show("Данные об автобусах или водителях не загружены. Добавление невозможно.", "Ошибка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new XtraForm())
            {
                form.Text = "Добавить маршрут";
                form.Width = 500;
                form.Height = 400;
                form.StartPosition = FormStartPosition.CenterParent;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
                form.Controls.Add(panel);

                var startPointLabel = new LabelControl { Text = "Начальная точка:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var startPointBox = new TextEdit { Width = 300 };
                startPointLabel.Location = new System.Drawing.Point(10, 20);
                startPointBox.Location = new System.Drawing.Point(170, 20);

                var endPointLabel = new LabelControl { Text = "Конечная точка:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var endPointBox = new TextEdit { Width = 300 };
                endPointLabel.Location = new System.Drawing.Point(10, 50);
                endPointBox.Location = new System.Drawing.Point(170, 50);

                var travelTimeLabel = new LabelControl { Text = "Время в пути:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var travelTimeBox = new TextEdit { Width = 300 };
                travelTimeLabel.Location = new System.Drawing.Point(10, 80);
                travelTimeBox.Location = new System.Drawing.Point(170, 80);

                var busLabel = new LabelControl { Text = "Автобус:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var busComboBox = new LookUpEdit { Width = 300 };
                busComboBox.Properties.DataSource = _buses;
                busComboBox.Properties.DisplayMember = "Model";
                busComboBox.Properties.ValueMember = "BusId";
                busLabel.Location = new System.Drawing.Point(10, 110);
                busComboBox.Location = new System.Drawing.Point(170, 110);

                var driverLabel = new LabelControl { Text = "Водитель:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var driverComboBox = new LookUpEdit { Width = 300 };
                driverComboBox.Properties.DataSource = _drivers;
                driverComboBox.Properties.DisplayMember = "Surname";
                driverComboBox.Properties.ValueMember = "EmpId";
                driverLabel.Location = new System.Drawing.Point(10, 140);
                driverComboBox.Location = new System.Drawing.Point(170, 140);

                var addButton = new SimpleButton { Text = "Добавить", Width = 100 };
                addButton.Location = new System.Drawing.Point(200, 180);

                panel.Controls.AddRange(new Control[] { 
                    startPointLabel, startPointBox, 
                    endPointLabel, endPointBox, 
                    travelTimeLabel, travelTimeBox, 
                    busLabel, busComboBox, 
                    driverLabel, driverComboBox, 
                    addButton 
                });

                addButton.Click += async delegate(object s, EventArgs args) { 
                    if (string.IsNullOrWhiteSpace(startPointBox.Text) || 
                        string.IsNullOrWhiteSpace(endPointBox.Text) || 
                        string.IsNullOrWhiteSpace(travelTimeBox.Text) || 
                        busComboBox.EditValue == null || 
                        driverComboBox.EditValue == null)
                    {
                        XtraMessageBox.Show("Все поля обязательны для заполнения", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    try
                    {
                        var newRoute = new
                        {
                            StartPoint = startPointBox.Text,
                            EndPoint = endPointBox.Text,
                            TravelTime = travelTimeBox.Text,
                            BusId = (int)busComboBox.EditValue,
                            DriverId = (int)driverComboBox.EditValue
                        };

                         HttpClient client = null;
                         try {
                             client = _apiClient.CreateClient();
                            string jsonPayload = JsonConvert.SerializeObject(newRoute, _jsonSettings);
                            HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                            var response = await client.PostAsync(string.Format("{0}/Routes", _baseUrl), content);

                            if (response.IsSuccessStatusCode)
                            {
                                await LoadRoutesAsync();
                                form.DialogResult = DialogResult.OK;
                                form.Close();
                            }
                            else
                            {
                                var error = await response.Content.ReadAsStringAsync();
                                XtraMessageBox.Show(string.Format("Не удалось добавить маршрут: {0}", error), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                         }
                         finally {
                              if (client != null) client.Dispose();
                         }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error adding route");
                        XtraMessageBox.Show(string.Format("Ошибка при добавлении маршрута: {0}", ex.Message), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.ShowDialog();
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var selectedRoute = gridViewRoutes.GetFocusedRow() as Marshut;
            if (selectedRoute == null) return;

            using (var form = new XtraForm())
            {
                form.Text = "Редактировать маршрут";
                form.Width = 500;
                form.Height = 400;
                form.StartPosition = FormStartPosition.CenterParent;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
                form.Controls.Add(panel);

                var startPointLabel = new LabelControl { Text = "Начальная точка:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var startPointBox = new TextEdit { Width = 300, Text = selectedRoute.StartPoint };
                startPointLabel.Location = new System.Drawing.Point(10, 20);
                startPointBox.Location = new System.Drawing.Point(170, 20);

                var endPointLabel = new LabelControl { Text = "Конечная точка:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var endPointBox = new TextEdit { Width = 300, Text = selectedRoute.EndPoint };
                endPointLabel.Location = new System.Drawing.Point(10, 50);
                endPointBox.Location = new System.Drawing.Point(170, 50);

                var travelTimeLabel = new LabelControl { Text = "Время в пути:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var travelTimeBox = new TextEdit { Width = 300, Text = selectedRoute.TravelTime };
                travelTimeLabel.Location = new System.Drawing.Point(10, 80);
                travelTimeBox.Location = new System.Drawing.Point(170, 80);

                var busLabel = new LabelControl { Text = "Автобус:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var busComboBox = new LookUpEdit { Width = 300 };
                busComboBox.Properties.DataSource = _buses;
                busComboBox.Properties.DisplayMember = "Model";
                busComboBox.Properties.ValueMember = "BusId";
                busComboBox.EditValue = selectedRoute.BusId;
                busLabel.Location = new System.Drawing.Point(10, 110);
                busComboBox.Location = new System.Drawing.Point(170, 110);

                var driverLabel = new LabelControl { Text = "Водитель:", AutoSizeMode = LabelAutoSizeMode.None, Width = 150 };
                var driverComboBox = new LookUpEdit { Width = 300 };
                driverComboBox.Properties.DataSource = _drivers;
                driverComboBox.Properties.DisplayMember = "Surname";
                driverComboBox.Properties.ValueMember = "EmpId";
                driverComboBox.EditValue = selectedRoute.DriverId;
                driverLabel.Location = new System.Drawing.Point(10, 140);
                driverComboBox.Location = new System.Drawing.Point(170, 140);

                var updateButton = new SimpleButton { Text = "Обновить", Width = 100 };
                updateButton.Location = new System.Drawing.Point(200, 180);

                panel.Controls.AddRange(new Control[] { 
                    startPointLabel, startPointBox, 
                    endPointLabel, endPointBox, 
                    travelTimeLabel, travelTimeBox, 
                    busLabel, busComboBox, 
                    driverLabel, driverComboBox, 
                    updateButton 
                });

                updateButton.Click += async delegate(object s, EventArgs args) { 
                    if (string.IsNullOrWhiteSpace(startPointBox.Text) || 
                        string.IsNullOrWhiteSpace(endPointBox.Text) || 
                        string.IsNullOrWhiteSpace(travelTimeBox.Text) || 
                        busComboBox.EditValue == null || 
                        driverComboBox.EditValue == null)
                    {
                        XtraMessageBox.Show("Все поля обязательны для заполнения", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    try
                    {
                        var updatedRoute = new
                        {
                            StartPoint = startPointBox.Text,
                            EndPoint = endPointBox.Text,
                            TravelTime = travelTimeBox.Text,
                            BusId = (int)busComboBox.EditValue,
                            DriverId = (int)driverComboBox.EditValue
                        };

                        HttpClient client = null;
                        try {
                            client = _apiClient.CreateClient();
                             string jsonPayload = JsonConvert.SerializeObject(updatedRoute, _jsonSettings);
                            HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                             var response = await client.PutAsync(string.Format("{0}/Routes/{1}", _baseUrl, selectedRoute.RouteId), content);

                            if (response.IsSuccessStatusCode)
                            {
                                await LoadRoutesAsync();
                                form.DialogResult = DialogResult.OK;
                                form.Close();
                            }
                            else
                            {
                                var error = await response.Content.ReadAsStringAsync();
                                XtraMessageBox.Show(string.Format("Не удалось обновить маршрут: {0}", error), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        finally {
                            if (client != null) client.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error updating route");
                        XtraMessageBox.Show(string.Format("Ошибка при обновлении маршрута: {0}", ex.Message), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.ShowDialog();
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedRoute = gridViewRoutes.GetFocusedRow() as Marshut;
            if (selectedRoute == null) return;

            var result = XtraMessageBox.Show(string.Format("Вы уверены, что хотите удалить маршрут '{0} - {1}' (ID: {2})?",
                                                selectedRoute.StartPoint ?? "N/A",
                                                selectedRoute.EndPoint ?? "N/A",
                                                selectedRoute.RouteId),
                                              "Подтверждение удаления",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    HttpClient client = null;
                    try {
                        client = _apiClient.CreateClient();
                        var response = await client.DeleteAsync(string.Format("{0}/Routes/{1}", _baseUrl, selectedRoute.RouteId));

                        if (response.IsSuccessStatusCode)
                        {
                            Log.Information("Route deleted successfully: ID {RouteId}", selectedRoute.RouteId);
                            XtraMessageBox.Show("Маршрут успешно удален.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            await LoadRoutesAsync();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            Log.Error("Failed to delete route. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                            {
                                XtraMessageBox.Show("Не удалось удалить маршрут: к нему привязаны билеты или расписания.", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                XtraMessageBox.Show(string.Format("Не удалось удалить маршрут: {0}\n{1}", response.ReasonPhrase, error), "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    finally {
                        if (client != null) client.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting route");
                    XtraMessageBox.Show(string.Format("Произошла ошибка при удалении маршрута: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            await LoadAllDataAsync();
        }

        private void txtSearch_EditValueChanged(object sender, EventArgs e)
        {
            FilterAndBindRoutes();
        }

        private void gridViewRoutes_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool isRowSelected = gridViewRoutes.GetFocusedRow() is Marshut;
            btnEdit.Enabled = isRowSelected;
            btnDelete.Enabled = isRowSelected;
        }
    }
}