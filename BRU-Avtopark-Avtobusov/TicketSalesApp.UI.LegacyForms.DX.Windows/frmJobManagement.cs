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
using DevExpress.XtraGrid.Views.Base;
using Serilog;
using TicketSalesApp.Core.Models;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmJobManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private List<Job> _allJobs = new List<Job>();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve
        };

        public frmJobManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            gridViewJobs.CustomUnboundColumnData += gridViewJobs_CustomUnboundColumnData;

            _apiClient.OnAuthTokenChanged += (s, e) => { 
                if (this.Visible) { 
                    LoadJobsAsync().ConfigureAwait(false); 
                }
            };
            UpdateButtonStates();
        }

        private async void frmJobManagement_Load(object sender, EventArgs e)
        {
            await LoadJobsAsync();
        }

        private async Task LoadJobsAsync()
        {
            try
            {
                using var client = _apiClient.CreateClient();
                // Include Employees to get the count
                var response = await client.GetAsync("jobs?includeEmployees=true"); 
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/jobs: {JsonResponse}", jsonResponse);
                    _allJobs = JsonSerializer.Deserialize<List<Job>>(jsonResponse, _jsonOptions) ?? new List<Job>();
                    FilterAndBindJobs();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load jobs. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    XtraMessageBox.Show($"Не удалось загрузить должности: {response.ReasonPhrase}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _allJobs = new List<Job>();
                    FilterAndBindJobs();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading jobs");
                XtraMessageBox.Show($"Произошла ошибка при загрузке должностей: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allJobs = new List<Job>();
                FilterAndBindJobs();
            }
            finally
            {
                UpdateButtonStates();
            }
        }

        private void FilterAndBindJobs()
        {
            var searchText = txtSearch.Text.Trim().ToLowerInvariant();
            List<Job> filteredJobs;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                filteredJobs = _allJobs;
            }
            else
            {
                filteredJobs = _allJobs.Where(j =>
                    (j.JobTitle?.ToLowerInvariant().Contains(searchText) ?? false) ||
                    (j.Internship?.ToLowerInvariant().Contains(searchText) ?? false)
                ).ToList();
            }

            jobBindingSource.DataSource = filteredJobs;
            gridControlJobs.RefreshDataSource();
        }

        private void gridViewJobs_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
             if (e.Column.FieldName == "Employees.Count" && e.IsGetData)
            {
                if (e.Row is Job job)
                {
                    // Accessing navigation property might be null if not included/handled by Preserve
                    e.Value = job.Employees?.Count ?? 0; 
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ShowEditJobForm(null);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var selectedJob = gridViewJobs.GetFocusedRow() as Job;
            if (selectedJob == null) return;
            ShowEditJobForm(selectedJob);
        }

        private void ShowEditJobForm(Job? jobToEdit)
        {
            using (var form = new XtraForm())
            {
                bool isAdding = jobToEdit == null;
                form.Text = isAdding ? "Добавить должность" : "Редактировать должность";
                form.Width = 500;
                form.Height = 250; // Adjusted height
                form.StartPosition = FormStartPosition.CenterParent;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
                form.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 150;
                int controlWidth = 300;
                int spacing = 30;

                // Job Title
                var titleLabel = new LabelControl { Text = "Название должности:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var titleBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = jobToEdit?.JobTitle ?? "" };
                panel.Controls.AddRange(new Control[] { titleLabel, titleBox });
                yPos += spacing;

                // Internship
                var internshipLabel = new LabelControl { Text = "Требования к стажу:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var internshipBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = jobToEdit?.Internship ?? "" };
                panel.Controls.AddRange(new Control[] { internshipLabel, internshipBox });
                yPos += spacing + 10;

                // Save Button
                var saveButton = new SimpleButton { Text = isAdding ? "Добавить" : "Обновить", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 - 50, yPos) };
                panel.Controls.Add(saveButton);

                saveButton.Click += async (s, args) => {
                    if (string.IsNullOrWhiteSpace(titleBox.Text))
                    {
                        XtraMessageBox.Show("Название должности обязательно.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        var jobData = new Job
                        {
                            JobId = isAdding ? 0 : jobToEdit!.JobId,
                            JobTitle = titleBox.Text,
                            Internship = internshipBox.Text // Can be null or empty
                        };

                        using var client = _apiClient.CreateClient();
                        HttpResponseMessage response;
                        string jsonPayload = JsonSerializer.Serialize(jobData, _jsonOptions);
                        HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                        if (isAdding)
                        {
                            response = await client.PostAsync("jobs", content);
                        }
                        else
                        {
                            response = await client.PutAsync($"jobs/{jobToEdit!.JobId}", content);
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            await LoadJobsAsync();
                            form.DialogResult = DialogResult.OK;
                            form.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            Log.Error("Failed to save job. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                            XtraMessageBox.Show($"Не удалось сохранить должность: {error}", "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error saving job");
                        XtraMessageBox.Show($"Ошибка при сохранении должности: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.ShowDialog();
            }
        }


        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedJob = gridViewJobs.GetFocusedRow() as Job;
            if (selectedJob == null) return;

            var result = XtraMessageBox.Show($"Вы уверены, что хотите удалить должность '{selectedJob.JobTitle}' (ID: {selectedJob.JobId})?",
                                              "Подтверждение удаления",
                                              MessageBoxButtons.YesNo,
                                              MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    using var client = _apiClient.CreateClient();
                    var response = await client.DeleteAsync($"jobs/{selectedJob.JobId}");

                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Job deleted successfully: ID {JobId}", selectedJob.JobId);
                        XtraMessageBox.Show("Должность успешно удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadJobsAsync(); // Refresh the list
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to delete job. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                         if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            XtraMessageBox.Show("Не удалось удалить должность: она используется сотрудниками.", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                             XtraMessageBox.Show($"Не удалось удалить должность: {response.ReasonPhrase}\n{error}", "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception deleting job");
                    XtraMessageBox.Show($"Произошла ошибка при удалении должности: {ex.Message}", "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Text = string.Empty;
            await LoadJobsAsync();
        }

        private void txtSearch_EditValueChanged(object sender, EventArgs e)
        {
            FilterAndBindJobs();
        }

        private void gridViewJobs_FocusedRowChanged(object sender, FocusedRowChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool isRowSelected = gridViewJobs.GetFocusedRow() is Job;
            btnEdit.Enabled = isRowSelected;
            btnDelete.Enabled = isRowSelected;
        }
    }
} 