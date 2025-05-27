using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraGrid.Views.Base;
using Serilog;
using TicketSalesApp.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmMaintenanceManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private readonly string _baseUrl = "http://localhost:5000/api"; // Added Base API URL
        private List<Obsluzhivanie> _allMaintenanceRecords = new List<Obsluzhivanie>();
        private List<Avtobus> _buses = new List<Avtobus>(); // For Bus selection
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        public frmMaintenanceManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            gridViewMaintenance.CustomUnboundColumnData += new CustomColumnDataEventHandler(gridViewMaintenance_CustomUnboundColumnData);

            _apiClient.OnAuthTokenChanged += async delegate(object s, string e_token) {
                if (this.Visible) {
                    await LoadAllDataAsync(); 
                } 
            };
            UpdateButtonStates();
        }

        private async void frmMaintenanceManagement_Load(object sender, EventArgs e)
        {
            await LoadAllDataAsync();
        }

        private async Task LoadAllDataAsync()
        {
            await LoadMaintenanceRecordsAsync();
            await LoadBusesAsync(); // Load buses for the Add/Edit dialog
        }

        private async Task LoadMaintenanceRecordsAsync()
        {
            HttpClient client = null;
            try
            {
                client = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/Maintenance?includeBus=true", _baseUrl); // Use Maintenance and string.Format
                Log.Debug("Fetching maintenance records from: {0}", apiUrl);
                var response = await client.GetAsync(apiUrl); 
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/maintenance: {JsonResponse}", jsonResponse);
                    _allMaintenanceRecords = JsonConvert.DeserializeObject<List<Obsluzhivanie>>(jsonResponse, _jsonSettings);
                    if (_allMaintenanceRecords == null) { _allMaintenanceRecords = new List<Obsluzhivanie>(); }
                    FilterAndBindMaintenance();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load maintenance records. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    XtraMessageBox.Show(string.Format("Не удалось загрузить записи об обслуживании: {0}", response.ReasonPhrase), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _allMaintenanceRecords = new List<Obsluzhivanie>();
                    FilterAndBindMaintenance();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading maintenance records");
                XtraMessageBox.Show(string.Format("Произошла ошибка при загрузке записей об обслуживании: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allMaintenanceRecords = new List<Obsluzhivanie>();
                FilterAndBindMaintenance();
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
                    _buses = JsonConvert.DeserializeObject<List<Avtobus>>(jsonResponse, _jsonSettings);
                    if (_buses == null) { _buses = new List<Avtobus>(); }
                }
                else
                {
                    Log.Error("Failed to load buses for maintenance form. Status: {StatusCode}", response.StatusCode);
                    _buses = new List<Avtobus>();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading buses for maintenance form");
                _buses = new List<Avtobus>();
            }
            finally { if (client != null) client.Dispose(); }
        }

        private void FilterAndBindMaintenance()
        {
            var searchText = txtSearch.Text.Trim().ToLowerInvariant();
            List<Obsluzhivanie> filteredRecords;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredRecords = _allMaintenanceRecords;
            }
            else
            {
                filteredRecords = _allMaintenanceRecords.Where(delegate(Obsluzhivanie m) {
                    bool engineerMatch = (m.ServiceEngineer != null && m.ServiceEngineer.ToLowerInvariant().Contains(searchText));
                    bool issuesMatch = (m.FoundIssues != null && m.FoundIssues.ToLowerInvariant().Contains(searchText));
                    bool roadworthinessMatch = (m.Roadworthiness != null && m.Roadworthiness.ToLowerInvariant().Contains(searchText));
                    bool busMatch = (m.Avtobus != null && m.Avtobus.Model != null && m.Avtobus.Model.ToLowerInvariant().Contains(searchText));
                    return engineerMatch || issuesMatch || roadworthinessMatch || busMatch;
                 }).ToList();
            }

            maintenanceBindingSource.DataSource = filteredRecords;
            gridControlMaintenance.RefreshDataSource();
        }

        private void gridViewMaintenance_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "Avtobus.Model" && e.IsGetData)
            {
                Obsluzhivanie maintenance = e.Row as Obsluzhivanie;
                if (maintenance != null && maintenance.Avtobus != null)
                {
                    e.Value = maintenance.Avtobus.Model;
                }
                else 
                {
                    e.Value = null;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ShowEditMaintenanceForm(null);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var selectedRecord = gridViewMaintenance.GetFocusedRow() as Obsluzhivanie;
            if (selectedRecord == null) return;
            ShowEditMaintenanceForm(selectedRecord);
        }

        private void ShowEditMaintenanceForm(Obsluzhivanie recordToEdit)
        {
            if (_buses == null || !_buses.Any())
            {
                 XtraMessageBox.Show("Список автобусов не загружен. Невозможно добавить или редактировать запись об обслуживании.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 return;
            }

            using (var form = new XtraForm())
            {
                bool isAdding = recordToEdit == null;
                form.Text = isAdding ? "Добавить запись об обслуживании" : "Редактировать запись об обслуживании";
                form.Width = 600;
                form.Height = 450;
                form.StartPosition = FormStartPosition.CenterParent;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
                form.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 180;
                int controlWidth = 350;
                int spacing = 30;

                var busLabel = new LabelControl { Text = "Автобус:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var busComboBox = new LookUpEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                busComboBox.Properties.DataSource = _buses;
                busComboBox.Properties.DisplayMember = "Model";
                busComboBox.Properties.ValueMember = "BusId";
                busComboBox.Properties.Columns.Add(new LookUpColumnInfo("BusId", "ID", 50));
                busComboBox.Properties.Columns.Add(new LookUpColumnInfo("Model", "Модель"));
                busComboBox.Properties.NullText = "[Выберите автобус]";
                busComboBox.EditValue = recordToEdit != null ? (object)recordToEdit.BusId : null;
                panel.Controls.AddRange(new Control[] { busLabel, busComboBox });
                yPos += spacing;

                var lastDateLabel = new LabelControl { Text = "Дата обслуживания:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var lastDatePicker = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                lastDatePicker.Properties.Mask.EditMask = "dd.MM.yyyy";
                lastDatePicker.Properties.Mask.UseMaskAsDisplayFormat = true;
                lastDatePicker.EditValue = isAdding ? (object)DateTime.Today : (recordToEdit != null ? (object)recordToEdit.LastServiceDate : null);
                panel.Controls.AddRange(new Control[] { lastDateLabel, lastDatePicker });
                yPos += spacing;

                var nextDateLabel = new LabelControl { Text = "След. обслуживание:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var nextDatePicker = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                nextDatePicker.Properties.Mask.EditMask = "dd.MM.yyyy";
                nextDatePicker.Properties.Mask.UseMaskAsDisplayFormat = true;
                nextDatePicker.Properties.AllowNullInput = DevExpress.Utils.DefaultBoolean.True;
                nextDatePicker.Properties.NullText = "[Не указана]";
                nextDatePicker.EditValue = (recordToEdit != null) ? (object)recordToEdit.NextServiceDate : null;
                panel.Controls.AddRange(new Control[] { nextDateLabel, nextDatePicker });
                yPos += spacing;

                var engineerLabel = new LabelControl { Text = "Инженер:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var engineerBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = (recordToEdit != null ? recordToEdit.ServiceEngineer : "") };
                panel.Controls.AddRange(new Control[] { engineerLabel, engineerBox });
                yPos += spacing;

                var issuesLabel = new LabelControl { Text = "Найденные проблемы:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var issuesBox = new MemoEdit { Width = controlWidth, Height = 60, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = (recordToEdit != null ? recordToEdit.FoundIssues : "") };
                panel.Controls.AddRange(new Control[] { issuesLabel, issuesBox });
                yPos += spacing + 30;

                var roadworthinessLabel = new LabelControl { Text = "Состояние (пригодность):", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var roadworthinessBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = (recordToEdit != null ? recordToEdit.Roadworthiness : "") ?? "" };
                panel.Controls.AddRange(new Control[] { roadworthinessLabel, roadworthinessBox });
                yPos += spacing + 10;

                var saveButton = new SimpleButton { Text = isAdding ? "Добавить" : "Обновить", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 - 50, yPos) };
                panel.Controls.Add(saveButton);

                saveButton.Click += async delegate(object s, EventArgs args) {
                    if (busComboBox.EditValue == null || lastDatePicker.EditValue == null || string.IsNullOrWhiteSpace(engineerBox.Text))
                    {
                        XtraMessageBox.Show("Автобус, Дата обслуживания и Инженер обязательны.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        var maintenanceData = new Obsluzhivanie
                        {
                            MaintenanceId = isAdding ? 0 : recordToEdit.MaintenanceId,
                            BusId = Convert.ToInt64(busComboBox.EditValue),
                            LastServiceDate = lastDatePicker.DateTime,
                            NextServiceDate = (nextDatePicker.EditValue != null && nextDatePicker.EditValue != DBNull.Value) ? nextDatePicker.DateTime : default(DateTime),
                            ServiceEngineer = engineerBox.Text,
                            FoundIssues = issuesBox.Text,
                            Roadworthiness = roadworthinessBox.Text
                        };

                        HttpClient client = null;
                        try
                        {
                            client = _apiClient.CreateClient();
                            string jsonPayload = JsonConvert.SerializeObject(maintenanceData, _jsonSettings);
                            HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                            if (isAdding)
                            {
                                var apiUrl = string.Format("{0}/Maintenance", _baseUrl); // Use string.Format
                                Log.Debug("Posting new maintenance record to: {0}", apiUrl);
                                var response = await client.PostAsync(apiUrl, content);

                                if (response.IsSuccessStatusCode)
                                {
                                    await LoadMaintenanceRecordsAsync();
                                    form.DialogResult = DialogResult.OK;
                                    form.Close();
                                }
                                else
                                {
                                    var error = await response.Content.ReadAsStringAsync();
                                    Log.Error("Failed to save maintenance record. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                                    XtraMessageBox.Show(string.Format("Не удалось сохранить запись об обслуживании: {0}", error), "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                var apiUrl = string.Format("{0}/Maintenance/{1}", _baseUrl, recordToEdit.MaintenanceId); // Use string.Format
                                Log.Debug("Putting updated maintenance record to: {0}", apiUrl);
                                var response = await client.PutAsync(apiUrl, content);

                                if (response.IsSuccessStatusCode)
                                {
                                    await LoadMaintenanceRecordsAsync();
                                    form.DialogResult = DialogResult.OK;
                                    form.Close();
                                }
                                else
                                {
                                    var error = await response.Content.ReadAsStringAsync();
                                    Log.Error("Failed to save maintenance record. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                                    XtraMessageBox.Show(string.Format("Не удалось сохранить запись об обслуживании: {0}", error), "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                        }
                        finally
                        {
                            if (client != null) client.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error saving maintenance record");
                        XtraMessageBox.Show(string.Format("Ошибка при сохранении записи: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.ShowDialog();
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedRecord = gridViewMaintenance.GetFocusedRow() as Obsluzhivanie;
            if (selectedRecord == null) return;
            
            string busModel = (selectedRecord.Avtobus != null) ? selectedRecord.Avtobus.Model : "[Неизвестно]";
            var result = XtraMessageBox.Show(string.Format("Вы уверены, что хотите удалить запись об обслуживании (ID: {0}) для автобуса '{1}'?", 
                                              selectedRecord.MaintenanceId, busModel),
                                              "Подтверждение удаления",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    HttpClient client = null;
                    try
                    {
                        client = _apiClient.CreateClient();
                        var apiUrl = string.Format("{0}/Maintenance/{1}", _baseUrl, selectedRecord.MaintenanceId); // Use string.Format
                        Log.Debug("Deleting maintenance record from: {0}", apiUrl);
                        var response = await client.DeleteAsync(apiUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            Log.Information("Maintenance record deleted successfully: ID {MaintenanceId}", selectedRecord.MaintenanceId);
                            XtraMessageBox.Show("Запись успешно удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            await LoadMaintenanceRecordsAsync(); // Refresh the list
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            Log.Error("Failed to delete maintenance record. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                            XtraMessageBox.Show(string.Format("Не удалось удалить запись: {0}\n{1}", response.ReasonPhrase, error), "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    finally
                    {
                        if (client != null) client.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting maintenance record");
                    XtraMessageBox.Show(string.Format("Произошла ошибка при удалении записи: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            FilterAndBindMaintenance();
        }

        private void gridViewMaintenance_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool isRowSelected = gridViewMaintenance.GetFocusedRow() is Obsluzhivanie;
            btnEdit.Enabled = isRowSelected;
            btnDelete.Enabled = isRowSelected;
        }
    }
} 