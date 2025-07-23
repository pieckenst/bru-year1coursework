using System;
using System.Collections.Generic;
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

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmMaintenanceManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private List<Obsluzhivanie> _allMaintenanceRecords = new List<Obsluzhivanie>();
        private List<Avtobus> _buses = new List<Avtobus>(); // For Bus selection
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        public frmMaintenanceManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            gridViewMaintenance.CustomUnboundColumnData += gridViewMaintenance_CustomUnboundColumnData;

            _apiClient.OnAuthTokenChanged += (s, e) => { 
                if (this.Visible) {
                    LoadAllDataAsync().ConfigureAwait(false);
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
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync("maintenance?includeBus=true"); 
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/maintenance: {JsonResponse}", jsonResponse);
                    _allMaintenanceRecords = JsonSerializer.Deserialize<List<Obsluzhivanie>>(jsonResponse, _jsonOptions) ?? new List<Obsluzhivanie>();
                    FilterAndBindMaintenance();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load maintenance records. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    XtraMessageBox.Show($"Не удалось загрузить записи об обслуживании: {response.ReasonPhrase}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _allMaintenanceRecords = new List<Obsluzhivanie>();
                    FilterAndBindMaintenance();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading maintenance records");
                XtraMessageBox.Show($"Произошла ошибка при загрузке записей об обслуживании: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allMaintenanceRecords = new List<Obsluzhivanie>();
                FilterAndBindMaintenance();
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
                    _buses = JsonSerializer.Deserialize<List<Avtobus>>(jsonResponse, _jsonOptions) ?? new List<Avtobus>();
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
                filteredRecords = _allMaintenanceRecords.Where(m =>
                    (m.ServiceEngineer?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (m.FoundIssues?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (m.Roadworthiness?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (m.Avtobus?.Model?.ToLowerInvariant().Contains(searchText) ?? false)
                ).ToList();
            }

            maintenanceBindingSource.DataSource = filteredRecords;
            gridControlMaintenance.RefreshDataSource();
        }

        private void gridViewMaintenance_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "Avtobus.Model" && e.IsGetData)
            {
                if (e.Row is Obsluzhivanie maintenance)
                {
                    e.Value = maintenance.Avtobus?.Model;
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

        private void ShowEditMaintenanceForm(Obsluzhivanie? recordToEdit)
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

                // Bus Selection
                var busLabel = new LabelControl { Text = "Автобус:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var busComboBox = new LookUpEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                busComboBox.Properties.DataSource = _buses;
                busComboBox.Properties.DisplayMember = "Model";
                busComboBox.Properties.ValueMember = "BusId";
                busComboBox.Properties.Columns.Add(new LookUpColumnInfo("BusId", "ID", 50));
                busComboBox.Properties.Columns.Add(new LookUpColumnInfo("Model", "Модель"));
                busComboBox.Properties.NullText = "[Выберите автобус]";
                busComboBox.EditValue = recordToEdit?.BusId;
                panel.Controls.AddRange(new Control[] { busLabel, busComboBox });
                yPos += spacing;

                // Last Service Date
                var lastDateLabel = new LabelControl { Text = "Дата обслуживания:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var lastDatePicker = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                lastDatePicker.Properties.Mask.EditMask = "dd.MM.yyyy";
                lastDatePicker.Properties.Mask.UseMaskAsDisplayFormat = true;
                lastDatePicker.EditValue = isAdding ? (object)DateTime.Today : recordToEdit?.LastServiceDate;
                panel.Controls.AddRange(new Control[] { lastDateLabel, lastDatePicker });
                yPos += spacing;

                // Next Service Date
                var nextDateLabel = new LabelControl { Text = "След. обслуживание:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var nextDatePicker = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                nextDatePicker.Properties.Mask.EditMask = "dd.MM.yyyy";
                nextDatePicker.Properties.Mask.UseMaskAsDisplayFormat = true;
                nextDatePicker.EditValue = recordToEdit?.NextServiceDate;
                panel.Controls.AddRange(new Control[] { nextDateLabel, nextDatePicker });
                yPos += spacing;

                // Service Engineer
                var engineerLabel = new LabelControl { Text = "Инженер:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var engineerBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = recordToEdit?.ServiceEngineer ?? "" };
                panel.Controls.AddRange(new Control[] { engineerLabel, engineerBox });
                yPos += spacing;

                // Found Issues
                var issuesLabel = new LabelControl { Text = "Найденные проблемы:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var issuesBox = new MemoEdit { Width = controlWidth, Height = 60, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = recordToEdit?.FoundIssues ?? "" };
                panel.Controls.AddRange(new Control[] { issuesLabel, issuesBox });
                yPos += spacing + 30; // Extra space for MemoEdit

                // Roadworthiness
                var roadworthinessLabel = new LabelControl { Text = "Состояние (пригодность):", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var roadworthinessBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = recordToEdit?.Roadworthiness ?? "" };
                panel.Controls.AddRange(new Control[] { roadworthinessLabel, roadworthinessBox });
                yPos += spacing + 10;

                // Save Button
                var saveButton = new SimpleButton { Text = isAdding ? "Добавить" : "Обновить", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 - 50, yPos) };
                panel.Controls.Add(saveButton);

                saveButton.Click += async (s, args) => {
                    if (busComboBox.EditValue == null || lastDatePicker.EditValue == null || string.IsNullOrWhiteSpace(engineerBox.Text))
                    {
                        XtraMessageBox.Show("Автобус, Дата обслуживания и Инженер обязательны.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        var maintenanceData = new Obsluzhivanie
                        {
                            MaintenanceId = isAdding ? 0 : recordToEdit!.MaintenanceId,
                            BusId = (int)busComboBox.EditValue,
                            LastServiceDate = (DateTime)lastDatePicker.EditValue,
                            NextServiceDate = (DateTime)(DateTime?)nextDatePicker.EditValue, // Allow null
                            ServiceEngineer = engineerBox.Text,
                            FoundIssues = issuesBox.Text,
                            Roadworthiness = roadworthinessBox.Text
                        };

                        using var client = _apiClient.CreateClient();
                        HttpResponseMessage response;
                        string jsonPayload = JsonSerializer.Serialize(maintenanceData, _jsonOptions);
                        HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        if (isAdding)
                        {
                            response = await client.PostAsync("maintenance", content);
                        }
                        else
                        {
                            response = await client.PutAsync($"maintenance/{recordToEdit!.MaintenanceId}", content);
                        }

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
                            XtraMessageBox.Show($"Не удалось сохранить запись об обслуживании: {error}", "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error saving maintenance record");
                        XtraMessageBox.Show($"Ошибка при сохранении записи: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.ShowDialog();
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedRecord = gridViewMaintenance.GetFocusedRow() as Obsluzhivanie;
            if (selectedRecord == null) return;

            var result = XtraMessageBox.Show($"Вы уверены, что хотите удалить запись об обслуживании (ID: {selectedRecord.MaintenanceId}) для автобуса '{selectedRecord.Avtobus?.Model}'?",
                                              "Подтверждение удаления",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using var client = _apiClient.CreateClient();
                    var response = await client.DeleteAsync($"maintenance/{selectedRecord.MaintenanceId}");

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
                        XtraMessageBox.Show($"Не удалось удалить запись: {response.ReasonPhrase}\n{error}", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting maintenance record");
                    XtraMessageBox.Show($"Произошла ошибка при удалении записи: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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