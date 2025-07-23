using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using TicketSalesApp.Core.Models; // Ensure Marshut, Avtobus, Employee, Bilet are here
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization; // Needed for ReferenceHandler

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmRouteManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private List<Marshut> _allRoutes = new List<Marshut>();
        private List<Avtobus> _buses = new List<Avtobus>();
        private List<Employee> _drivers = new List<Employee>();
        // Common JsonSerializerOptions for handling reference loops
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve // Handle $id, $values, $ref
        };

        public frmRouteManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            gridViewRoutes.CustomUnboundColumnData += gridViewRoutes_CustomUnboundColumnData;
            
            // Subscribe to auth token changes
            _apiClient.OnAuthTokenChanged += (sender, token) => {
                // Reload data with the new token
                LoadRoutesAsync().ConfigureAwait(false);
                LoadBusesAsync().ConfigureAwait(false);
                LoadDriversAsync().ConfigureAwait(false);
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
            try
            {
                using var client = _apiClient.CreateClient();
                // Assume the endpoint includes related data needed for the grid
                var response = await client.GetAsync("routes?includeBus=true&includeEmployee=true&includeTickets=true"); 
                if (response.IsSuccessStatusCode)
                {
                    // --- Debugging: Log raw JSON ---
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/routes: {JsonResponse}", jsonResponse); 
                    // --- End Debugging ---
                    
                    // Use pre-configured options with ReferenceHandler.Preserve
                    _allRoutes = JsonSerializer.Deserialize<List<Marshut>>(jsonResponse, _jsonOptions) ?? new List<Marshut>();
                    
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
                XtraMessageBox.Show($"Произошла ошибка при загрузке маршрутов: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allRoutes = new List<Marshut>();
                FilterAndBindRoutes();
            }
            finally
            {
                UpdateButtonStates();
            }
        }

        private async Task LoadBusesAsync()
        {
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync("buses");
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/buses: {JsonResponse}", jsonResponse);
                    _buses = JsonSerializer.Deserialize<List<Avtobus>>(jsonResponse, _jsonOptions) ?? new List<Avtobus>();
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
        }

        private async Task LoadDriversAsync()
        {
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync("employees");
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/employees: {JsonResponse}", jsonResponse);
                    _drivers = JsonSerializer.Deserialize<List<Employee>>(jsonResponse, _jsonOptions) ?? new List<Employee>();
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
        }

        private void FilterAndBindRoutes()
        {
            var searchText = txtSearch.Text.Trim().ToLowerInvariant();
            List<Marshut> filteredRoutes;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredRoutes = _allRoutes;
            }
            else
            {
                filteredRoutes = _allRoutes.Where(r =>
                    (r.StartPoint?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (r.EndPoint?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (r.Avtobus?.Model?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (r.Employee?.Surname?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (r.Employee?.Name?.ToLowerInvariant().Contains(searchText) ?? false)
                ).ToList();
            }

            routeBindingSource.DataSource = filteredRoutes;
            gridControlRoutes.RefreshDataSource();
        }

        private void gridViewRoutes_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (!(e.Row is Marshut route)) return;

            if (e.IsGetData)
            {
                if (e.Column.FieldName == "Avtobus.Model")
                {
                    e.Value = route.Avtobus?.Model;
                }
                else if (e.Column.FieldName == "Employee.Surname")
                {
                    e.Value = route.Employee?.Surname;
                }
                else if (e.Column.FieldName == "Tickets.Count")
                {
                    e.Value = route.Tickets?.Count ?? 0;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
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

                addButton.Click += async (s, args) => {
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

                        using var client = _apiClient.CreateClient();
                        var response = await client.PostAsJsonAsync("routes", newRoute);

                        if (response.IsSuccessStatusCode)
                        {
                            await LoadRoutesAsync();
                            form.DialogResult = DialogResult.OK;
                            form.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            XtraMessageBox.Show($"Не удалось добавить маршрут: {error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error adding route");
                        XtraMessageBox.Show($"Ошибка при добавлении маршрута: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                updateButton.Click += async (s, args) => {
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

                        using var client = _apiClient.CreateClient();
                        var response = await client.PutAsJsonAsync($"routes/{selectedRoute.RouteId}", updatedRoute);

                        if (response.IsSuccessStatusCode)
                        {
                            await LoadRoutesAsync();
                            form.DialogResult = DialogResult.OK;
                            form.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            XtraMessageBox.Show($"Не удалось обновить маршрут: {error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error updating route");
                        XtraMessageBox.Show($"Ошибка при обновлении маршрута: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.ShowDialog();
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedRoute = gridViewRoutes.GetFocusedRow() as Marshut;
            if (selectedRoute == null) return;

            var result = XtraMessageBox.Show($"Вы уверены, что хотите удалить маршрут '{selectedRoute.StartPoint} - {selectedRoute.EndPoint}' (ID: {selectedRoute.RouteId})?",
                                              "Подтверждение удаления",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using var client = _apiClient.CreateClient();
                    var response = await client.DeleteAsync($"routes/{selectedRoute.RouteId}");

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
                        // Check for specific conflict errors (e.g., route has tickets)
                        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                             XtraMessageBox.Show($"Не удалось удалить маршрут: к нему привязаны билеты или расписания.", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            XtraMessageBox.Show($"Не удалось удалить маршрут: {response.ReasonPhrase}\n{error}", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting route");
                    XtraMessageBox.Show($"Произошла ошибка при удалении маршрута: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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