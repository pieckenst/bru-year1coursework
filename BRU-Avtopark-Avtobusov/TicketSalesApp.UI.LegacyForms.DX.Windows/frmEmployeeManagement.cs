using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using Serilog;
using TicketSalesApp.Core.Models; // Requires Employee, Job models

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmEmployeeManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private List<Employee> _allEmployees = new List<Employee>();
        private List<Job> _jobs = new List<Job>(); // For Job selection
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve 
        };

        public frmEmployeeManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            gridViewEmployees.CustomUnboundColumnData += gridViewEmployees_CustomUnboundColumnData;
            
            // Subscribe to auth token changes if needed (similar to other forms)
            _apiClient.OnAuthTokenChanged += (s, e) => { 
                if (this.Visible) {
                    LoadAllDataAsync().ConfigureAwait(false); 
                }
            };

            UpdateButtonStates();
        }

        private async void frmEmployeeManagement_Load(object sender, EventArgs e)
        {
            await LoadAllDataAsync();
        }

        private async Task LoadAllDataAsync()
        {
            await LoadEmployeesAsync();
            await LoadJobsAsync(); // Load jobs for dropdowns
        }

        private async Task LoadEmployeesAsync()
        {
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync("employees?includeJob=true"); 
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/employees: {JsonResponse}", jsonResponse);
                    _allEmployees = JsonSerializer.Deserialize<List<Employee>>(jsonResponse, _jsonOptions) ?? new List<Employee>();
                    FilterAndBindEmployees();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load employees. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    XtraMessageBox.Show($"Не удалось загрузить сотрудников: {response.ReasonPhrase}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _allEmployees = new List<Employee>();
                    FilterAndBindEmployees();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading employees");
                XtraMessageBox.Show($"Произошла ошибка при загрузке сотрудников: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allEmployees = new List<Employee>();
                FilterAndBindEmployees();
            }
            finally
            {
                UpdateButtonStates();
            }
        }

        private async Task LoadJobsAsync()
        {
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync("jobs"); 
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    _jobs = JsonSerializer.Deserialize<List<Job>>(jsonResponse, _jsonOptions) ?? new List<Job>();
                }
                else
                {
                    Log.Error("Failed to load jobs. Status: {StatusCode}", response.StatusCode);
                    _jobs = new List<Job>();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading jobs");
                _jobs = new List<Job>();
            }
        }

        private void FilterAndBindEmployees()
        {
            var searchText = txtSearch.Text.Trim().ToLowerInvariant();
            List<Employee> filteredEmployees;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredEmployees = _allEmployees;
            }
            else
            {
                filteredEmployees = _allEmployees.Where(e =>
                    (e.Surname?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (e.Name?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (e.Patronym?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (e.Job?.JobTitle?.ToLowerInvariant().Contains(searchText) ?? false)
                ).ToList();
            }

            employeeBindingSource.DataSource = filteredEmployees;
            gridControlEmployees.RefreshDataSource();
        }

        private void gridViewEmployees_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
            if (e.Column.FieldName == "Job.JobTitle" && e.IsGetData)
            {
                if (e.Row is Employee employee)
                {
                    e.Value = employee.Job?.JobTitle;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ShowEditEmployeeForm(null); // Pass null for adding a new employee
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var selectedEmployee = gridViewEmployees.GetFocusedRow() as Employee;
            if (selectedEmployee == null) return;
            ShowEditEmployeeForm(selectedEmployee); // Pass selected employee for editing
        }

        private void ShowEditEmployeeForm(Employee? employeeToEdit)
        {
             if (_jobs == null || !_jobs.Any()) {
                 XtraMessageBox.Show("Список должностей не загружен. Невозможно добавить или редактировать сотрудника.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                 return;
             }

            using (var form = new XtraForm())
            {
                bool isAdding = employeeToEdit == null;
                form.Text = isAdding ? "Добавить сотрудника" : "Редактировать сотрудника";
                form.Width = 500;
                form.Height = 400;
                form.StartPosition = FormStartPosition.CenterParent;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
                form.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 150;
                int controlWidth = 300;
                int spacing = 30;

                // Surname
                var surnameLabel = new LabelControl { Text = "Фамилия:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var surnameBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = employeeToEdit?.Surname ?? "" };
                panel.Controls.AddRange(new Control[] { surnameLabel, surnameBox });
                yPos += spacing;

                // Name
                var nameLabel = new LabelControl { Text = "Имя:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var nameBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = employeeToEdit?.Name ?? "" };
                panel.Controls.AddRange(new Control[] { nameLabel, nameBox });
                yPos += spacing;

                // Patronym
                var patronymLabel = new LabelControl { Text = "Отчество:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var patronymBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = employeeToEdit?.Patronym ?? "" };
                panel.Controls.AddRange(new Control[] { patronymLabel, patronymBox });
                yPos += spacing;
                
                // Employed Since (Date)
                var employedSinceLabel = new LabelControl { Text = "Дата приема:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var employedSincePicker = new DateEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                employedSincePicker.Properties.Mask.EditMask = "dd.MM.yyyy";
                employedSincePicker.Properties.Mask.UseMaskAsDisplayFormat = true;
                employedSincePicker.EditValue = isAdding ? (object)DateTime.Today : employeeToEdit?.EmployedSince;
                panel.Controls.AddRange(new Control[] { employedSinceLabel, employedSincePicker });
                yPos += spacing;

                // Job
                var jobLabel = new LabelControl { Text = "Должность:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var jobComboBox = new LookUpEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos) };
                jobComboBox.Properties.DataSource = _jobs;
                jobComboBox.Properties.DisplayMember = "JobTitle";
                jobComboBox.Properties.ValueMember = "JobId";
                jobComboBox.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("JobTitle", "Должность"));
                jobComboBox.Properties.NullText = "[Выберите должность]";
                jobComboBox.EditValue = employeeToEdit?.JobId;
                panel.Controls.AddRange(new Control[] { jobLabel, jobComboBox });
                yPos += spacing + 10; // More space before button

                // Save Button
                var saveButton = new SimpleButton { Text = isAdding ? "Добавить" : "Обновить", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 - 50, yPos) };
                panel.Controls.Add(saveButton);

                saveButton.Click += async (s, args) => {
                    // Basic Validation
                    if (string.IsNullOrWhiteSpace(surnameBox.Text) || 
                        string.IsNullOrWhiteSpace(nameBox.Text) || 
                        employedSincePicker.EditValue == null ||
                        jobComboBox.EditValue == null)
                    {
                        XtraMessageBox.Show("Фамилия, Имя, Дата приема и Должность обязательны.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        var employeeData = new Employee
                        {
                             // EmpId is set by backend on add, included for update
                            EmpId = isAdding ? 0 : employeeToEdit!.EmpId, 
                            Surname = surnameBox.Text,
                            Name = nameBox.Text,
                            Patronym = patronymBox.Text,
                            EmployedSince = (DateTime)employedSincePicker.EditValue,
                            JobId = (int)jobComboBox.EditValue
                        };

                        using var client = _apiClient.CreateClient();
                        HttpResponseMessage response;
                        
                        string jsonPayload = JsonSerializer.Serialize(employeeData, _jsonOptions);
                        HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        if (isAdding)
                        {
                            response = await client.PostAsync("employees", content);
                        }
                        else
                        {
                            response = await client.PutAsync($"employees/{employeeToEdit!.EmpId}", content);
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            await LoadEmployeesAsync(); // Refresh the main grid
                            form.DialogResult = DialogResult.OK;
                            form.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            Log.Error("Failed to save employee. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                            XtraMessageBox.Show($"Не удалось сохранить сотрудника: {error}", "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error saving employee");
                        XtraMessageBox.Show($"Ошибка при сохранении сотрудника: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.ShowDialog();
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedEmployee = gridViewEmployees.GetFocusedRow() as Employee;
            if (selectedEmployee == null) return;

            var result = XtraMessageBox.Show($"Вы уверены, что хотите удалить сотрудника '{selectedEmployee.Surname} {selectedEmployee.Name}' (ID: {selectedEmployee.EmpId})?",
                                              "Подтверждение удаления",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using var client = _apiClient.CreateClient();
                    var response = await client.DeleteAsync($"employees/{selectedEmployee.EmpId}");

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Employee deleted successfully: ID {EmpId}", selectedEmployee.EmpId);
                        XtraMessageBox.Show("Сотрудник успешно удален.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadEmployeesAsync(); // Refresh the list
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to delete employee. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            XtraMessageBox.Show("Не удалось удалить сотрудника: возможно, он назначен на маршруты.", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                             XtraMessageBox.Show($"Не удалось удалить сотрудника: {response.ReasonPhrase}\n{error}", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                       
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting employee");
                    XtraMessageBox.Show($"Произошла ошибка при удалении сотрудника: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            FilterAndBindEmployees();
        }

        private void gridViewEmployees_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool isRowSelected = gridViewEmployees.GetFocusedRow() is Employee;
            btnEdit.Enabled = isRowSelected;
            btnDelete.Enabled = isRowSelected;
        }
    }
} 