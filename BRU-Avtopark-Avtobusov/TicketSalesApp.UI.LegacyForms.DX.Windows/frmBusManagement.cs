using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using TicketSalesApp.Core.Models;
using Serilog;
using System.Text.Json.Serialization;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmBusManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private List<Avtobus> _allBuses = new List<Avtobus>();
        private readonly string _baseUrl = "http://localhost:5000/api";
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        public frmBusManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            
            // Handle unbound columns (Count properties)
            gridViewBuses.CustomUnboundColumnData += GridViewBuses_CustomUnboundColumnData;

            // Initial button state
            UpdateButtonStates();
            
            // Subscribe to auth token changes
            _apiClient.OnAuthTokenChanged += (sender, token) => {
                // Reload data with the new token if the form is visible/active
                if (this.Visible) { 
                    LoadBusesAsync().ConfigureAwait(false); 
                }
            };
        }

        private async void frmBusManagement_Load(object sender, EventArgs e)
        {
            await LoadBusesAsync();
        }

        private async Task LoadBusesAsync()
        {
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync($"{_baseUrl}/Buses?includeRoutes=true&includeMaintenance=true");
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/buses: {JsonResponse}", jsonResponse);
                    _allBuses = JsonSerializer.Deserialize<List<Avtobus>>(jsonResponse, _jsonOptions) ?? new List<Avtobus>();
                    FilterAndBindBuses();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load buses. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    XtraMessageBox.Show($"Не удалось загрузить автобусы: {response.ReasonPhrase}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _allBuses = new List<Avtobus>(); // Clear list on error
                    FilterAndBindBuses(); // Update grid even on error (empty)
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading buses");
                XtraMessageBox.Show($"Произошла ошибка при загрузке автобусов: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allBuses = new List<Avtobus>(); // Clear list on error
                FilterAndBindBuses(); // Update grid even on error (empty)
            }
            finally
            {
                 UpdateButtonStates();
            }
        }

        private void FilterAndBindBuses()
        {
            var searchText = txtSearch.Text.Trim();
            List<Avtobus> filteredBuses;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredBuses = _allBuses;
            }
            else
            {
                filteredBuses = _allBuses
                    .Where(b => b.Model != null && b.Model.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            
            // Use BindingList for better grid updates if needed, but List is fine for now
            busBindingSource.DataSource = filteredBuses;
            gridControlBuses.RefreshDataSource(); // Ensure grid updates
        }

        private void GridViewBuses_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "Routes.Count" && e.IsGetData)
            {
                if (e.Row is Avtobus bus)
                {
                    e.Value = bus.Routes?.Count ?? 0;
                }
            }
            else if (e.Column.FieldName == "Obsluzhivanies.Count" && e.IsGetData)
            {
                if (e.Row is Avtobus bus)
                {
                    e.Value = bus.Obsluzhivanies?.Count ?? 0;
                }
            }
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            // Open a dialog/form to get new bus details
            // For simplicity, using InputBox here, replace with a proper form
            string? newModel = XtraInputBox.Show("Введите модель нового автобуса:", "Добавить автобус", "");
            if (!string.IsNullOrWhiteSpace(newModel))
            {
                var newBus = new Avtobus { Model = newModel };
                try
                {
                    using var client = _apiClient.CreateClient();
                    var json = JsonSerializer.Serialize(newBus, _jsonOptions);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"{_baseUrl}/Buses", content);

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Bus added successfully: {Model}", newModel);
                        XtraMessageBox.Show("Автобус успешно добавлен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadBusesAsync(); // Refresh the list
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to add bus. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                        XtraMessageBox.Show($"Не удалось добавить автобус: {response.ReasonPhrase}\n{error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception adding bus");
                    XtraMessageBox.Show($"Произошла ошибка при добавлении автобуса: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
            var selectedBus = gridViewBuses.GetFocusedRow() as Avtobus;
            if (selectedBus == null) return;

            // Open a dialog/form to edit bus details, pre-populated with selectedBus.Model
            // For simplicity, using InputBox here, replace with a proper form
            string? updatedModel = XtraInputBox.Show("Введите новую модель автобуса:", "Редактировать автобус", selectedBus.Model);
            if (!string.IsNullOrWhiteSpace(updatedModel) && updatedModel != selectedBus.Model)
            {
                var updatedBusData = new Avtobus { BusId = selectedBus.BusId, Model = updatedModel };

                try
                {
                    using var client = _apiClient.CreateClient();
                    var json = JsonSerializer.Serialize(updatedBusData, _jsonOptions);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PutAsync($"{_baseUrl}/Buses/{selectedBus.BusId}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Bus updated successfully: ID {BusId}, New Model {Model}", selectedBus.BusId, updatedModel);
                        XtraMessageBox.Show("Автобус успешно обновлен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadBusesAsync(); // Refresh the list
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to update bus. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                        XtraMessageBox.Show($"Не удалось обновить автобус: {response.ReasonPhrase}\n{error}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception updating bus");
                    XtraMessageBox.Show($"Произошла ошибка при обновлении автобуса: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedBus = gridViewBuses.GetFocusedRow() as Avtobus;
            if (selectedBus == null) return;

            var result = XtraMessageBox.Show($"Вы уверены, что хотите удалить автобус '{selectedBus.Model}' (ID: {selectedBus.BusId})?", 
                                              "Подтверждение удаления", 
                                              MessageBoxButtons.YesNo, 
                                              MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using var client = _apiClient.CreateClient();
                    var response = await client.DeleteAsync($"{_baseUrl}/Buses/{selectedBus.BusId}");

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Bus deleted successfully: ID {BusId}, Model {Model}", selectedBus.BusId, selectedBus.Model);
                        XtraMessageBox.Show("Автобус успешно удален.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadBusesAsync(); // Refresh the list
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to delete bus. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                        // Check for specific conflict errors (e.g., bus assigned to routes)
                        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            XtraMessageBox.Show($"Не удалось удалить автобус: он используется в маршрутах или обслуживании.", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            XtraMessageBox.Show($"Не удалось удалить автобус: {response.ReasonPhrase}\n{error}", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting bus");
                    XtraMessageBox.Show($"Произошла ошибка при удалении автобуса: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty; // Clear search on refresh
            await LoadBusesAsync();
        }

        private void txtSearch_EditValueChanged(object sender, EventArgs e)
        {
            // Optional: Add a debounce timer here if performance is an issue
            FilterAndBindBuses();
        }

        private void gridViewBuses_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
             UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
             bool isRowSelected = gridViewBuses.GetFocusedRow() is Avtobus;
             btnEdit.Enabled = isRowSelected;
             btnDelete.Enabled = isRowSelected;
        }
    }
} 