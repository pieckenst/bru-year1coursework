using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using TicketSalesApp.Core.Models;
using Serilog;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using DevExpress.XtraLayout;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmBusManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private List<Avtobus> _allBuses = new List<Avtobus>();
        private readonly string _baseUrl = "http://localhost:5000/api";
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        #region Helper: Input Dialog

        private static string ShowInputDialog(string caption, string prompt, string defaultValue = "")
        {
            using (var form = new XtraForm())
            {
                form.Text = caption;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(350, 120);

                var lc = new LayoutControl() { Dock = DockStyle.Fill };
                form.Controls.Add(lc);

                var txtInput = new TextEdit();
                txtInput.Text = defaultValue;

                var btnOK = new SimpleButton() { Text = "OK" };
                var btnCancel = new SimpleButton() { Text = "Отмена" };
                btnOK.DialogResult = DialogResult.OK;
                btnCancel.DialogResult = DialogResult.Cancel;

                lc.Root.AddItem(prompt, txtInput).TextLocation = DevExpress.Utils.Locations.Top;
                var itemOK = lc.Root.AddItem(string.Empty, btnOK);
                var itemCancel = lc.Root.AddItem(string.Empty, btnCancel);

                // Basic horizontal button layout (adjust spacing/alignment as needed)
                lc.Root.AddItem(itemCancel).Move(itemOK, DevExpress.XtraLayout.Utils.InsertType.Right);

                form.AcceptButton = btnOK;
                form.CancelButton = btnCancel;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    return txtInput.Text;
                }
                else
                {
                    return null; // Indicate cancellation
                }
            }
        }

        #endregion

        public frmBusManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            
            gridViewBuses.CustomUnboundColumnData += GridViewBuses_CustomUnboundColumnData;

            UpdateButtonStates();
            
            _apiClient.OnAuthTokenChanged += async delegate(object sender, string token) {
                if (this.Visible) { 
                    await LoadBusesAsync(); 
                }
            };
        }

        private async void frmBusManagement_Load(object sender, EventArgs e)
        {
            await LoadBusesAsync();
        }

        private async Task LoadBusesAsync()
        {
            HttpClient client = null;
            try
            {
                client = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/Buses?includeRoutes=true&includeMaintenance=true", _baseUrl);
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/buses: {JsonResponse}", jsonResponse);
                    _allBuses = JsonConvert.DeserializeObject<List<Avtobus>>(jsonResponse, _jsonSettings);
                    if (_allBuses == null) { _allBuses = new List<Avtobus>(); }
                    FilterAndBindBuses();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load buses. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    XtraMessageBox.Show(string.Format("Не удалось загрузить автобусы: {0}", response.ReasonPhrase), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _allBuses = new List<Avtobus>();
                    FilterAndBindBuses();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading buses");
                XtraMessageBox.Show(string.Format("Произошла ошибка при загрузке автобусов: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allBuses = new List<Avtobus>();
                FilterAndBindBuses();
            }
            finally
            {
                 if (client != null) client.Dispose();
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
                    .Where(delegate(Avtobus b) { 
                        return b.Model != null && b.Model.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                     })
                    .ToList();
            }
            
            busBindingSource.DataSource = filteredBuses;
            gridControlBuses.RefreshDataSource();
        }

        private void GridViewBuses_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "Routes.Count" && e.IsGetData)
            {
                if (e.Row is Avtobus)
                {
                    Avtobus bus = (Avtobus)e.Row;
                    e.Value = (bus.Routes != null) ? bus.Routes.Count : 0;
                }
            }
            else if (e.Column.FieldName == "Obsluzhivanies.Count" && e.IsGetData)
            {
                if (e.Row is Avtobus)
                {
                    Avtobus bus = (Avtobus)e.Row;
                    e.Value = (bus.Obsluzhivanies != null) ? bus.Obsluzhivanies.Count : 0;
                }
            }
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            string busModel = ShowInputDialog("Добавить автобус", "Введите модель нового автобуса:");

            if (!string.IsNullOrWhiteSpace(busModel))
            {
                var newBus = new Avtobus { Model = busModel };
                HttpClient client = null;
                try
                {
                    client = _apiClient.CreateClient();
                    var json = JsonConvert.SerializeObject(newBus, _jsonSettings);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var apiUrl = string.Format("{0}/Buses", _baseUrl);
                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Bus added successfully: {Model}", busModel);
                        XtraMessageBox.Show("Автобус успешно добавлен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadBusesAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to add bus. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                        XtraMessageBox.Show(string.Format("Не удалось добавить автобус: {0}\n{1}", response.ReasonPhrase, error), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception adding bus");
                    XtraMessageBox.Show(string.Format("Произошла ошибка при добавлении автобуса: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { if (client != null) client.Dispose(); }
            }
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
            var selectedBus = gridViewBuses.GetFocusedRow() as Avtobus;
            if (selectedBus == null) return;

            string newBusModel = ShowInputDialog("Редактировать автобус", "Введите новую модель для автобуса:", selectedBus.Model);
            if (!string.IsNullOrWhiteSpace(newBusModel) && newBusModel != selectedBus.Model)
            {
                var updatedBusData = new Avtobus { BusId = selectedBus.BusId, Model = newBusModel };
                HttpClient client = null;
                try
                {
                    client = _apiClient.CreateClient();
                    var json = JsonConvert.SerializeObject(updatedBusData, _jsonSettings);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var apiUrl = string.Format("{0}/Buses/{1}", _baseUrl, selectedBus.BusId);
                    var response = await client.PutAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Bus updated successfully: ID {BusId}, New Model {Model}", selectedBus.BusId, newBusModel);
                        XtraMessageBox.Show("Автобус успешно обновлен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadBusesAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to update bus. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                        XtraMessageBox.Show(string.Format("Не удалось обновить автобус: {0}\n{1}", response.ReasonPhrase, error), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception updating bus");
                    XtraMessageBox.Show(string.Format("Произошла ошибка при обновлении автобуса: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { if (client != null) client.Dispose(); }
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedBus = gridViewBuses.GetFocusedRow() as Avtobus;
            if (selectedBus == null) return;

            var result = XtraMessageBox.Show(string.Format("Вы уверены, что хотите удалить автобус '{0}' (ID: {1})?", selectedBus.Model, selectedBus.BusId), 
                                              "Подтверждение удаления", 
                                              MessageBoxButtons.YesNo, 
                                              MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                HttpClient client = null;
                try
                {
                    client = _apiClient.CreateClient();
                    var apiUrl = string.Format("{0}/Buses/{1}", _baseUrl, selectedBus.BusId);
                    var response = await client.DeleteAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Bus deleted successfully: ID {BusId}, Model {Model}", selectedBus.BusId, selectedBus.Model);
                        XtraMessageBox.Show("Автобус успешно удален.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadBusesAsync();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to delete bus. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            XtraMessageBox.Show("Не удалось удалить автобус: он используется в маршрутах или обслуживании.", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            XtraMessageBox.Show(string.Format("Не удалось удалить автобус: {0}\n{1}", response.ReasonPhrase, error), "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting bus");
                    XtraMessageBox.Show(string.Format("Произошла ошибка при удалении автобуса: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { if (client != null) client.Dispose(); }
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            await LoadBusesAsync();
        }

        private void txtSearch_EditValueChanged(object sender, EventArgs e)
        {
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