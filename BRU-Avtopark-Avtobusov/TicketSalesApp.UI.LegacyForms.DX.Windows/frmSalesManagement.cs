using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using DevExpress.Data;
using DevExpress.XtraPrinting;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web; // For HttpUtility
using DevExpress.XtraEditors.Controls;
using DevExpress.Utils;
using System.Drawing; // Added for Point, Size etc.
using DevExpress.XtraLayout; // Added for dynamic layout (optional but good)
using Serilog; // Added for Serilog

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    // ViewModel for sales data retrieved from API
    public class SaleViewModel
    {
        public int SaleId { get; set; }
        public DateTime SaleDate { get; set; }
        public int RouteScheduleId { get; set; }
        public string RouteDescription { get; set; } = string.Empty; // Route details fetched separately or included by API
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public int SeatNumber { get; set; }
        public double TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // ViewModel for RouteSchedule lookup (used in Add/Edit form)
    public class RouteScheduleLookupViewModel
    {
        public int RouteScheduleId { get; set; }
        public string DisplayName { get; set; } = string.Empty; // e.g., "Start - End (Departs: HH:mm)"
        public double Price { get; set; } // To auto-fill price when selected
    }

    public partial class frmSalesManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClientService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
             PropertyNameCaseInsensitive = true, 
             ReferenceHandler = ReferenceHandler.Preserve // Keep this handler
        };
        private readonly string _baseUrl = "http://localhost:5000"; // Base API URL
        private ObservableCollection<RouteScheduleLookupViewModel> _availableRouteSchedules = new ObservableCollection<RouteScheduleLookupViewModel>(); // Cache for edit form
        private List<SaleViewModel> _allSalesData = new List<SaleViewModel>(); // Added to store all sales data

        public frmSalesManagement()
        {
            InitializeComponent();

            _apiClientService = ApiClientService.Instance;

            // Set default filter dates (last 30 days)
            dateFromFilter.DateTime = DateTime.Now.Date.AddDays(-30);
            dateToFilter.DateTime = DateTime.Now.Date;

            // Configure grid view
            gridViewSales.OptionsBehavior.Editable = false;
            gridViewSales.OptionsBehavior.ReadOnly = true;
            gridViewSales.OptionsDetail.EnableMasterViewMode = false;
            gridViewSales.OptionsFind.AlwaysVisible = true; // Enable search
            gridViewSales.OptionsView.ShowGroupPanel = false;
            gridViewSales.DoubleClick += gridViewSales_DoubleClick;

            // Bind events
            this.Load += frmSalesManagement_Load;
            gridViewSales.CustomColumnDisplayText += gridViewSales_CustomColumnDisplayText;
            gridViewSales.FocusedRowChanged += (s, e) => UpdateButtonStates(); // Update buttons on selection change

            // Handle auth token changes
            _apiClientService.OnAuthTokenChanged += async (s, token) => {
                 await RefreshDataAsync(); // Reload all data on token change
            };
            
            UpdateButtonStates(); // Initial state
        }

        private async void frmSalesManagement_Load(object sender, EventArgs e)
        {
             Log.Information("Loading prerequisites for Sales Management...");
            await LoadPrerequisitesAsync(); // Load routes needed for editing first
             Log.Information("Loading initial sales data...");
            await RefreshDataAsync(); // Load sales and apply initial filters
        }

        private async Task LoadPrerequisitesAsync()
        {
             try
             {
                 var client = _apiClientService.CreateClient();
                 var apiUrl = $"{_baseUrl}/api/routeschedules";
                  Log.Information("Fetching route schedule lookup data from: {ApiUrl}", apiUrl);
                 var response = await client.GetAsync(apiUrl);

                 if (response.IsSuccessStatusCode)
                 {
                     var schedules = await response.Content.ReadFromJsonAsync<List<RouteScheduleLookupViewModel>>(_jsonOptions)
                                             ?? new List<RouteScheduleLookupViewModel>();
                     _availableRouteSchedules = new ObservableCollection<RouteScheduleLookupViewModel>(schedules);
                      Log.Information("Successfully loaded {Count} route schedules for lookup.", _availableRouteSchedules.Count);
                 }
                 else
                 {
                     var errorContent = await response.Content.ReadAsStringAsync();
                      Log.Error("Failed to load route schedule lookup data. Status: {StatusCode}, Content: {ErrorContent}",
                                      response.StatusCode, errorContent);
                      _availableRouteSchedules = new ObservableCollection<RouteScheduleLookupViewModel>(); 
                 }
             }
             catch (Exception ex)
             {
                  Log.Error(ex, "Error fetching route schedule lookup data.");
                  _availableRouteSchedules = new ObservableCollection<RouteScheduleLookupViewModel>(); 
             }
        }

        private void gridViewSales_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == nameof(SaleViewModel.TotalAmount) && e.Value is double amount)
            {
                e.DisplayText = string.Format("{0:C}", amount); // Format as currency
            }
            else if (e.Value is DateTime dt)
            {
                 if (e.Column.FieldName == nameof(SaleViewModel.SaleDate))
                 {
                      e.DisplayText = dt.ToString("dd.MM.yyyy HH:mm");
                 }
                 else if (e.Column.FieldName == nameof(SaleViewModel.DepartureTime) || e.Column.FieldName == nameof(SaleViewModel.ArrivalTime))
                 {
                      // Use a consistent format, perhaps just Time if date is implicit or redundant
                      e.DisplayText = dt.ToString("dd.MM HH:mm");
                 }
            }
        }

        private void gridViewSales_DoubleClick(object sender, EventArgs e)
        {
             var view = sender as GridView;
             if (view == null) return;
             var pt = view.GridControl.PointToClient(Control.MousePosition);
             var hitInfo = view.CalcHitInfo(pt);
             if (hitInfo.InRow)
             {
                 btnEdit_Click(sender, e);
             }
        }

        private async Task RefreshDataAsync()
        {
             Log.Information("Refreshing ALL sales data from API...");
            try
            {
                Cursor = Cursors.WaitCursor;
                gridControlSales.Enabled = false;
                _allSalesData = new List<SaleViewModel>(); // Clear previous full data
                salesBindingSource.DataSource = null; // Clear display

                // Fetch ALL sales data
                var client = _apiClientService.CreateClient();
                var apiUrl = $"{_baseUrl}/api/sales";
                 Log.Information("Fetching all sales from: {ApiUrl}", apiUrl);

                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Store all data locally
                    _allSalesData = await response.Content.ReadFromJsonAsync<List<SaleViewModel>>(_jsonOptions)
                                ?? new List<SaleViewModel>();
                     Log.Information("Successfully loaded {Count} total sales.", _allSalesData.Count);
                     
                     // Apply initial/current filters and bind
                     ApplyFiltersAndBindData(); 
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                     Log.Error("Failed to load ALL sales. Status: {StatusCode}, Content: {ErrorContent}",
                                     response.StatusCode, errorContent);
                    XtraMessageBox.Show($"Ошибка загрузки продаж: {response.ReasonPhrase}\n{errorContent}", "Ошибка API",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    salesBindingSource.DataSource = null; // Clear grid if loading failed
                    UpdateButtonStates(); // Update button states based on empty grid
                }
            }
            catch (HttpRequestException httpEx)
            {
                  Log.Error(httpEx, "Network error refreshing all sales data.");
                 XtraMessageBox.Show($"Сетевая ошибка при загрузке продаж: {httpEx.Message}", "Ошибка сети",
                     MessageBoxButtons.OK, MessageBoxIcon.Error);
                 salesBindingSource.DataSource = null;
                 UpdateButtonStates(); 
            }
            catch (JsonException jsonEx)
            {
                  Log.Error(jsonEx, "Error deserializing all sales data.");
                 XtraMessageBox.Show($"Ошибка обработки данных продаж: {jsonEx.Message}", "Ошибка данных",
                     MessageBoxButtons.OK, MessageBoxIcon.Error);
                 salesBindingSource.DataSource = null;
                 UpdateButtonStates(); 
            }
            catch (Exception ex)
            {
                 Log.Error(ex, "Generic error refreshing all sales data.");
                XtraMessageBox.Show($"Произошла ошибка при загрузке продаж: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                 salesBindingSource.DataSource = null;
                 UpdateButtonStates(); 
            }
            finally
            {
                 // ApplyFiltersAndBindData handles enabling grid, cursor, and button states
            }
        }

        // New method to apply filters and update the grid
        private void ApplyFiltersAndBindData()
        {
            Log.Information("Applying client-side filters for sales...");
            try
            {
                Cursor = Cursors.WaitCursor;
                gridControlSales.Enabled = false;
                salesBindingSource.DataSource = null; // Clear previous filtered data

                DateTime fromDate = dateFromFilter.DateTime.Date;
                DateTime toDate = dateToFilter.DateTime.Date.AddDays(1).AddTicks(-1); // Include end date

                // Filter the local _allSalesData list
                var filteredData = _allSalesData
                    .Where(s => s.SaleDate >= fromDate && s.SaleDate <= toDate)
                    .ToList(); // Filter by date

                // Set filtered data source
                salesBindingSource.DataSource = filteredData;
                Log.Information("Client-side filtering applied. Displaying {Count} sales.", filteredData.Count);
            }
            catch (Exception ex)
            {
                 Log.Error(ex, "Error applying client-side filters for sales.");
                 XtraMessageBox.Show($"Произошла ошибка при применении фильтров: {ex.Message}", "Ошибка Фильтрации",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                gridControlSales.Enabled = true;
                Cursor = Cursors.Default;
                UpdateButtonStates(); // Update button states based on selection in filtered view
                // gridViewSales.BestFitColumns(); // Optional: might be slow with large datasets
            }
        }

        private void UpdateButtonStates()
        {
             bool isRowSelected = gridViewSales.GetSelectedRows().Length > 0;
             btnEdit.Enabled = isRowSelected;
             btnDelete.Enabled = isRowSelected;
             // Add/Refresh/Export are always enabled (or based on other conditions)
             btnAdd.Enabled = true;
             btnRefresh.Enabled = true;
             btnExport.Enabled = true;
             btnApplyFilter.Enabled = true;
        }

        private async void btnAdd_Click(object sender, EventArgs e)
        {
             Log.Information("Add button clicked.");
            ShowEditSaleForm(null); // Pass null for Add mode
        }

        private async void btnEdit_Click(object sender, EventArgs e)
        {
             Log.Information("Edit button clicked.");
            var selectedRows = gridViewSales.GetSelectedRows();
            if (selectedRows.Length == 0)
            {
                // Should not happen if button state is correct, but good practice
                return;
            }

            var saleViewModel = gridViewSales.GetRow(selectedRows[0]) as SaleViewModel;
            if (saleViewModel == null)
            {
                Log.Warning("Could not get SaleViewModel for selected row.");
                return;
            }

            Log.Information("Editing Sale ID: {SaleId}", saleViewModel.SaleId);
            ShowEditSaleForm(saleViewModel); // Pass the selected sale for Edit mode
        }

        // Method to dynamically create and show the Add/Edit Sale form
        private void ShowEditSaleForm(SaleViewModel? saleToEdit)
        {
            bool isAdding = saleToEdit == null;
            var formTitle = isAdding ? "Добавить Продажу" : "Редактировать Продажу";
             Log.Information("Showing form: {FormTitle}", formTitle);

             if (!isAdding && saleToEdit == null)
             {
                   Log.Error("Edit mode requested but saleToEdit is null.");
                  XtraMessageBox.Show("Ошибка: Не удалось загрузить данные для редактирования.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                  return;
             }

             if (_availableRouteSchedules == null || !_availableRouteSchedules.Any())
             {
                   Log.Warning("Cannot {Action} sale because route schedule lookup list is empty.", isAdding ? "add" : "edit");
                  XtraMessageBox.Show("Не удалось загрузить список доступных рейсов. Добавление/редактирование продажи невозможно.", "Ошибка данных", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                  return;
             }

            using (var form = new XtraForm())
            {
                form.Text = formTitle;
                form.Width = 550;
                form.Height = 400; // Adjust as needed
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var panel = new PanelControl { Dock = DockStyle.Fill, Padding = new Padding(15) };
                form.Controls.Add(panel);

                int yPos = 20;
                int labelWidth = 150;
                int controlWidth = 300;
                int spacing = 35;

                // Route Schedule Selection
                var routeLabel = new LabelControl { Text = "Рейс:", Location = new Point(15, yPos), Width = labelWidth };
                var routeCombo = new LookUpEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    Properties = {
                        DataSource = _availableRouteSchedules,
                        DisplayMember = "DisplayName",
                        ValueMember = "RouteScheduleId",
                        NullText = "[Выберите рейс]",
                        ShowHeader = false,
                        SearchMode = SearchMode.AutoComplete,
                        AutoSearchColumnIndex = 0 // Search by DisplayName
                    }
                };
                routeCombo.Properties.Columns.Add(new LookUpColumnInfo("DisplayName", "Рейс"));
                panel.Controls.Add(routeLabel);
                panel.Controls.Add(routeCombo);
                yPos += spacing;

                // Seat Number
                var seatLabel = new LabelControl { Text = "Место:", Location = new Point(15, yPos), Width = labelWidth };
                var seatEdit = new SpinEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    Properties = { Mask = { EditMask = "d" }, MinValue = 1, MaxValue = 100 } // Assuming max 100 seats
                };
                panel.Controls.Add(seatLabel);
                panel.Controls.Add(seatEdit);
                yPos += spacing;

                 // Total Amount (auto-filled or manual)
                var amountLabel = new LabelControl { Text = "Сумма:", Location = new Point(15, yPos), Width = labelWidth };
                var amountEdit = new SpinEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth,
                    Properties = { Mask = { EditMask = "c" }, Increment = 50m, MinValue = 0 }
                };
                 // Update amount when route changes
                routeCombo.EditValueChanged += (s, e) => {
                    var selectedRouteId = (int?)routeCombo.EditValue;
                    var selectedRoute = _availableRouteSchedules.FirstOrDefault(r => r.RouteScheduleId == selectedRouteId);
                    if (selectedRoute != null)
                    {
                        amountEdit.Value = (decimal)selectedRoute.Price;
                    }
                };
                panel.Controls.Add(amountLabel);
                panel.Controls.Add(amountEdit);
                yPos += spacing;

                // Payment Method
                var paymentLabel = new LabelControl { Text = "Метод оплаты:", Location = new Point(15, yPos), Width = labelWidth };
                var paymentCombo = new ComboBoxEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth
                };
                paymentCombo.Properties.Items.AddRange(new[] { "Наличные", "Карта", "Онлайн" }); // Example methods
                panel.Controls.Add(paymentLabel);
                panel.Controls.Add(paymentCombo);
                yPos += spacing;

                // Status
                var statusLabel = new LabelControl { Text = "Статус:", Location = new Point(15, yPos), Width = labelWidth };
                var statusCombo = new ComboBoxEdit
                {
                    Location = new Point(labelWidth + 20, yPos),
                    Width = controlWidth
                };
                statusCombo.Properties.Items.AddRange(new[] { "Оплачено", "Забронировано", "Отменено" }); // Example statuses
                panel.Controls.Add(statusLabel);
                panel.Controls.Add(statusCombo);
                yPos += spacing + 20;


                // Populate controls if editing
                if (!isAdding)
                {
                     routeCombo.EditValue = saleToEdit!.RouteScheduleId;
                     seatEdit.Value = saleToEdit.SeatNumber;
                     amountEdit.Value = (decimal)saleToEdit.TotalAmount;
                     paymentCombo.SelectedItem = saleToEdit.PaymentMethod;
                     statusCombo.SelectedItem = saleToEdit.Status;
                } else {
                    // Default values for adding
                    seatEdit.Value = 1;
                    amountEdit.Value = 0m;
                    paymentCombo.SelectedIndex = 0; // Default to first item
                    statusCombo.SelectedIndex = 0; // Default to first item
                }


                // Buttons
                var btnCancel = new SimpleButton { Text = "Отмена", Location = new Point(form.Width / 2 - 110, yPos), Width = 100, DialogResult = DialogResult.Cancel };
                var btnSave = new SimpleButton { Text = isAdding ? "Добавить" : "Сохранить", Location = new Point(form.Width / 2 + 10, yPos), Width = 100 };
                panel.Controls.Add(btnCancel);
                panel.Controls.Add(btnSave);
                form.CancelButton = btnCancel;

                // Save button click handler
                btnSave.Click += async (s, args) =>
                {
                     Log.Information("Save button clicked in {Mode} mode.", isAdding ? "Add" : "Edit");
                    // Validation
                    if (routeCombo.EditValue == null) { XtraMessageBox.Show("Выберите рейс.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (string.IsNullOrEmpty(paymentCombo.Text)) { XtraMessageBox.Show("Выберите метод оплаты.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                     if (string.IsNullOrEmpty(statusCombo.Text)) { XtraMessageBox.Show("Выберите статус.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }


                    // Prepare data
                    var saleData = new SaleViewModel
                    {
                         SaleId = isAdding ? 0 : saleToEdit!.SaleId,
                         SaleDate = DateTime.Now, // API should set this ideally, or use a date picker if needed
                         RouteScheduleId = (int)routeCombo.EditValue,
                         SeatNumber = (int)seatEdit.Value,
                         TotalAmount = (double)amountEdit.Value,
                         PaymentMethod = paymentCombo.Text,
                         Status = statusCombo.Text
                         // RouteDescription, Departure/Arrival Times will be set by API/backend based on RouteScheduleId
                    };

                    try
                    {
                        Cursor = Cursors.WaitCursor;
                        btnSave.Enabled = false;
                        btnCancel.Enabled = false;

                        var client = _apiClientService.CreateClient();
                        HttpResponseMessage response;

                        if (isAdding)
                        {
                             Log.Information("Calling POST /api/sales");
                            response = await client.PostAsJsonAsync($"{_baseUrl}/api/sales", saleData, _jsonOptions);
                        }
                        else
                        {
                             Log.Information("Calling PUT /api/sales/{SaleId}", saleData.SaleId);
                            response = await client.PutAsJsonAsync($"{_baseUrl}/api/sales/{saleData.SaleId}", saleData, _jsonOptions);
                        }

                        if (response.IsSuccessStatusCode)
                        {
                             Log.Information("Sale {Action} successfully.", isAdding ? "added" : "updated");
                            await RefreshDataAsync();
                            form.DialogResult = DialogResult.OK;
                            form.Close();
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                             Log.Error("Failed to {Action} sale. Status: {StatusCode}, Content: {ErrorContent}",
                                             isAdding ? "add" : "update", response.StatusCode, errorContent);
                            XtraMessageBox.Show($"Ошибка при {(isAdding ? "добавлении" : "обновлении")} продажи: {response.ReasonPhrase}\n{errorContent}",
                                "Ошибка API", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                     catch (Exception ex)
                    {
                          Log.Error(ex, "Error saving sale.");
                        XtraMessageBox.Show($"Произошла системная ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                     finally
                    {
                         Cursor = Cursors.Default;
                         btnSave.Enabled = true;
                         btnCancel.Enabled = true;
                    }
                };

                form.ShowDialog(this);
            }
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
             Log.Information("Delete button clicked.");
             var selectedRows = gridViewSales.GetSelectedRows();
            if (selectedRows.Length == 0) return; // Should be disabled if no selection

            try
            {
                var saleViewModel = gridViewSales.GetRow(selectedRows[0]) as SaleViewModel;
                if (saleViewModel == null) {
                     Log.Warning("Could not get SaleViewModel for deletion.");
                     return;
                }

                var result = XtraMessageBox.Show(
                    $"Вы уверены, что хотите удалить продажу #{saleViewModel.SaleId} от {saleViewModel.SaleDate:dd.MM.yyyy HH:mm}?",
                    "Подтверждение удаления",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Cursor = Cursors.WaitCursor;
                    try
                    {
                        var client = _apiClientService.CreateClient();
                        var apiUrl = $"{_baseUrl}/api/sales/{saleViewModel.SaleId}";
                         Log.Information("Attempting to delete sale: {ApiUrl}", apiUrl);

                        var response = await client.DeleteAsync(apiUrl);

                        if (response.IsSuccessStatusCode)
                        {
                             Log.Information("Sale {SaleId} deleted successfully.", saleViewModel.SaleId);
                            XtraMessageBox.Show("Продажа удалена успешно.", "Успех",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            await RefreshDataAsync(); // Refresh grid
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                             Log.Error("Failed to delete sale {SaleId}. Status: {StatusCode}, Content: {ErrorContent}",
                                            saleViewModel.SaleId, response.StatusCode, errorContent);
                            XtraMessageBox.Show($"Ошибка при удалении продажи: {response.ReasonPhrase}\n{errorContent}", "Ошибка API",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    finally
                    {
                         Cursor = Cursors.Default;
                    }
                }
                else
                {
                     Log.Information("Deletion cancelled for Sale ID: {SaleId}", saleViewModel.SaleId);
                }
            }
            catch (Exception ex)
            {
                 Log.Error(ex, "Error during delete process.");
                XtraMessageBox.Show($"Произошла системная ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                 Cursor = Cursors.Default; // Ensure cursor resets on exception
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
             Log.Information("Refresh button clicked for sales.");
            // Reload all data from the API and re-apply filters
            await RefreshDataAsync(); 
        }

        private void btnApplyFilter_Click(object sender, EventArgs e) // No async void needed
        {
             Log.Information("Apply Filter button clicked for sales.");
            // Apply filters to the locally stored data
            ApplyFiltersAndBindData();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
             Log.Information("Export button clicked.");
            try
            {
                if (gridViewSales.RowCount == 0)
                {
                    XtraMessageBox.Show("Нет данных для экспорта.", "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv", // Simplified options
                    FilterIndex = 1,
                    RestoreDirectory = true,
                    FileName = $"Sales_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog(this) == DialogResult.OK)
                {
                    Cursor = Cursors.WaitCursor;
                    string fileExt = System.IO.Path.GetExtension(saveDialog.FileName).ToLowerInvariant();
                    var exportOptions = new XlsxExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG }; // Default Excel WYSIWYG

                    if (fileExt == ".xlsx")
                    {
                        gridControlSales.ExportToXlsx(saveDialog.FileName, exportOptions);
                    }
                    else if (fileExt == ".csv")
                    {
                         var csvOptions = new CsvExportOptionsEx { ExportType = DevExpress.Export.ExportType.WYSIWYG };
                        gridControlSales.ExportToCsv(saveDialog.FileName, csvOptions);
                    }
                     // No need for 'else' as filter limits choices

                     Log.Information("Data exported successfully to {FileName}", saveDialog.FileName);
                    XtraMessageBox.Show("Данные успешно экспортированы.", "Экспорт завершен",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (XtraMessageBox.Show("Открыть экспортированный файл?", "Открыть файл",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        try
                        {
                            var processStartInfo = new System.Diagnostics.ProcessStartInfo(saveDialog.FileName) { UseShellExecute = true };
                            System.Diagnostics.Process.Start(processStartInfo);
                        }
                        catch (Exception exOpen)
                        {
                             Log.Error(exOpen, "Failed to open exported file {FileName}", saveDialog.FileName);
                             XtraMessageBox.Show($"Не удалось открыть файл: {exOpen.Message}", "Ошибка открытия файла",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 Log.Error(ex, "Error exporting data.");
                XtraMessageBox.Show($"Ошибка при экспорте данных: {ex.Message}", "Ошибка экспорта",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
             finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
} 