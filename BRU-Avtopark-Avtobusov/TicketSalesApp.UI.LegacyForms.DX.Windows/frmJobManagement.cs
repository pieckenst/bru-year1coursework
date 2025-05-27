using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using Serilog;
using TicketSalesApp.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmJobManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClient;
        private readonly string _baseUrl = "http://localhost:5000/api";
        private List<Job> _allJobs = new List<Job>();
        private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        public frmJobManagement()
        {
            InitializeComponent();
            _apiClient = ApiClientService.Instance;
            gridViewJobs.CustomUnboundColumnData += new CustomColumnDataEventHandler(gridViewJobs_CustomUnboundColumnData);

            _apiClient.OnAuthTokenChanged += async delegate(object s, string e_token) {
                if (this.Visible) { 
                    await LoadJobsAsync(); 
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
            HttpClient client = null;
            try
            {
                client = _apiClient.CreateClient();
                var apiUrl = string.Format("{0}/Jobs?includeEmployees=true", _baseUrl);
                Log.Debug("Fetching jobs from: {0}", apiUrl);
                var response = await client.GetAsync(apiUrl); 
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    Log.Debug("Raw JSON response from /api/jobs: {JsonResponse}", jsonResponse);
                    _allJobs = JsonConvert.DeserializeObject<List<Job>>(jsonResponse, _jsonSettings);
                    if (_allJobs == null) { _allJobs = new List<Job>(); }
                    FilterAndBindJobs();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load jobs. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    XtraMessageBox.Show(string.Format("Не удалось загрузить должности: {0}", response.ReasonPhrase), "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _allJobs = new List<Job>();
                    FilterAndBindJobs();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception loading jobs");
                XtraMessageBox.Show(string.Format("Произошла ошибка при загрузке должностей: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _allJobs = new List<Job>();
                FilterAndBindJobs();
            }
            finally
            {
                if (client != null) client.Dispose();
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
                filteredJobs = _allJobs.Where(delegate(Job j) {
                    bool titleMatch = (j.JobTitle != null && j.JobTitle.ToLowerInvariant().Contains(searchText));
                    bool internshipMatch = (j.Internship != null && j.Internship.ToLowerInvariant().Contains(searchText));
                    return titleMatch || internshipMatch;
                }).ToList();
            }

            jobBindingSource.DataSource = filteredJobs;
            gridControlJobs.RefreshDataSource();
        }

        private void gridViewJobs_CustomUnboundColumnData(object sender, CustomColumnDataEventArgs e)
        {
             if (e.Column.FieldName == "Employees.Count" && e.IsGetData)
            {
                Job job = e.Row as Job;
                if (job != null)
                {
                    e.Value = (job.Employees != null) ? job.Employees.Count : 0; 
                }
                else
                {
                    e.Value = 0;
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

        private void ShowEditJobForm(Job jobToEdit)
        {
            using (var form = new XtraForm())
            {
                bool isAdding = jobToEdit == null;
                form.Text = isAdding ? "Добавить должность" : "Редактировать должность";
                form.Width = 500;
                form.Height = 250;
                form.StartPosition = FormStartPosition.CenterParent;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
                form.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 150;
                int controlWidth = 300;
                int spacing = 30;

                var titleLabel = new LabelControl { Text = "Название должности:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var titleBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = (jobToEdit != null ? jobToEdit.JobTitle : "") };
                panel.Controls.AddRange(new Control[] { titleLabel, titleBox });
                yPos += spacing;

                var internshipLabel = new LabelControl { Text = "Требования к стажу:", AutoSizeMode = LabelAutoSizeMode.None, Width = labelWidth, Location = new System.Drawing.Point(10, yPos) };
                var internshipBox = new TextEdit { Width = controlWidth, Location = new System.Drawing.Point(10 + labelWidth + 10, yPos), Text = (jobToEdit != null ? jobToEdit.Internship : "") };
                panel.Controls.AddRange(new Control[] { internshipLabel, internshipBox });
                yPos += spacing + 10;

                var saveButton = new SimpleButton { Text = isAdding ? "Добавить" : "Обновить", Width = 100, Location = new System.Drawing.Point(form.ClientSize.Width / 2 - 50, yPos) };
                panel.Controls.Add(saveButton);

                saveButton.Click += async delegate(object s, EventArgs args) {
                    if (string.IsNullOrWhiteSpace(titleBox.Text))
                    {
                        XtraMessageBox.Show("Название должности обязательно.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        var jobData = new Job
                        {
                            JobId = isAdding ? 0 : jobToEdit.JobId,
                            JobTitle = titleBox.Text,
                            Internship = internshipBox.Text
                        };

                        HttpClient client = null;
                        try {
                            client = _apiClient.CreateClient();
                            HttpResponseMessage response;
                            string jsonPayload = JsonConvert.SerializeObject(jobData, _jsonSettings);
                            HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                            if (isAdding)
                            {
                                var apiUrl = string.Format("{0}/Jobs", _baseUrl);
                                Log.Debug("Posting new job to: {0}", apiUrl);
                                response = await client.PostAsync(apiUrl, content);
                            }
                            else
                            {
                                var apiUrl = string.Format("{0}/Jobs/{1}", _baseUrl, jobToEdit.JobId);
                                Log.Debug("Putting updated job to: {0}", apiUrl);
                                response = await client.PutAsync(apiUrl, content);
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
                                XtraMessageBox.Show(string.Format("Не удалось сохранить должность: {0}", error), "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        finally {
                            if (client != null) client.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error saving job");
                        XtraMessageBox.Show(string.Format("Ошибка при сохранении должности: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                form.ShowDialog();
            }
        }


        private async void btnDelete_Click(object sender, EventArgs e)
        {
            var selectedJob = gridViewJobs.GetFocusedRow() as Job;
            if (selectedJob == null) return;

            string jobTitle = selectedJob.JobTitle != null ? selectedJob.JobTitle : "[Без названия]";

            var result = XtraMessageBox.Show(string.Format("Вы уверены, что хотите удалить должность '{0}' (ID: {1})?", 
                                                jobTitle,
                                                selectedJob.JobId),
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
                        var apiUrl = string.Format("{0}/Jobs/{1}", _baseUrl, selectedJob.JobId);
                        Log.Debug("Deleting job from: {0}", apiUrl);
                        var response = await client.DeleteAsync(apiUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            Log.Information("Job deleted successfully: ID {JobId}", selectedJob.JobId);
                            XtraMessageBox.Show("Должность успешно удалена.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            await LoadJobsAsync();
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
                                XtraMessageBox.Show(string.Format("Не удалось удалить должность: {0}\n{1}", response.ReasonPhrase, error), "Ошибка удаления", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    Log.Error(ex, "Exception deleting job");
                    XtraMessageBox.Show(string.Format("Произошла ошибка при удалении должности: {0}", ex.Message), "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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