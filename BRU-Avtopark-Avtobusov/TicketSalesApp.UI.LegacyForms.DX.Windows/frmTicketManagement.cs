using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using DevExpress.XtraGrid.Views.Base;
using Serilog;
using System.Text.Json.Serialization;
using TicketSalesApp.Core.Models;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraEditors.Controls;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    public partial class frmTicketManagement : DevExpress.XtraEditors.XtraForm
    {
        private readonly ApiClientService _apiClientService;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions 
        { 
             PropertyNameCaseInsensitive = true,
             ReferenceHandler = ReferenceHandler.Preserve
        };
        private readonly string _baseUrl = "http://localhost:5000";
        private List<Bilet> _allTicketsData = new List<Bilet>();
        private List<RouteSchedules> _routeSchedules = new List<RouteSchedules>();

        public frmTicketManagement()
        {
            InitializeComponent();

            _apiClientService = ApiClientService.Instance;

            // Set default dates
            dateFromFilter.DateTime = DateTime.Now.Date;
            dateToFilter.DateTime = DateTime.Now.Date.AddDays(7);

            // Configure GridView
            ConfigureGridView();

            // Bind events
            this.Load += frmTicketManagement_Load;
            gridViewTickets.CustomColumnDisplayText += gridViewTickets_CustomColumnDisplayText;
            gridViewTickets.FocusedRowChanged += (s, e) => UpdateButtonStates();

            // Handle auth token changes
            _apiClientService.OnAuthTokenChanged += async (s, token) => {
                await LoadTicketsAsync();
            }; 
            
            UpdateButtonStates();
        }

        private void ConfigureGridView()
        {
            gridViewTickets.OptionsBehavior.Editable = false;
            gridViewTickets.OptionsBehavior.ReadOnly = true;
            gridViewTickets.OptionsView.ShowGroupPanel = false;
            gridViewTickets.OptionsFind.AlwaysVisible = true;

            // Configure lookup editor for route filter
            var lookUpEdit = new RepositoryItemLookUpEdit();
            lookUpEdit.DataSource = _routeSchedules;
            lookUpEdit.DisplayMember = "StartPoint";
            lookUpEdit.ValueMember = "RouteScheduleId";
            lookUpEdit.BestFitMode = BestFitMode.BestFitResizePopup;
            lookUpEdit.SearchMode = SearchMode.AutoComplete;
            lueRouteScheduleFilter.Properties.DataSource = null;
            lueRouteScheduleFilter.Properties.DisplayMember = "StartPoint";
            lueRouteScheduleFilter.Properties.ValueMember = "RouteScheduleId";
            lueRouteScheduleFilter.Properties.NullText = "[Все рейсы]";
        }

        private async void frmTicketManagement_Load(object sender, EventArgs e)
        {
            Log.Information("Loading route schedules for Ticket Management filter...");
            await LoadRouteSchedulesAsync();
            Log.Information("Loading initial tickets data...");
            await LoadTicketsAsync();
        }

        private async Task LoadRouteSchedulesAsync()
        {
            try
            {
                var client = _apiClientService.CreateClient();
                var apiUrl = $"{_baseUrl}/api/routeschedules";
                Log.Information("Fetching route schedule data from: {ApiUrl}", apiUrl);
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    _routeSchedules = await response.Content.ReadFromJsonAsync<List<RouteSchedules>>(_jsonOptions) 
                                    ?? new List<RouteSchedules>();

                    var dataSource = new List<RouteSchedules>(_routeSchedules);
                    dataSource.Insert(0, new RouteSchedules { RouteScheduleId = -1, StartPoint = "[Все рейсы]" });
                    
                    lueRouteScheduleFilter.Properties.DataSource = dataSource;
                    lueRouteScheduleFilter.EditValue = -1;
                    
                    Log.Information("Successfully loaded {Count} route schedules for lookup.", _routeSchedules.Count);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load route schedule data. Status: {StatusCode}, Content: {ErrorContent}",
                                     response.StatusCode, errorContent);
                    lueRouteScheduleFilter.Properties.DataSource = null;
                    lueRouteScheduleFilter.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching route schedule data");
                lueRouteScheduleFilter.Properties.DataSource = null;
                lueRouteScheduleFilter.Enabled = false;
            }
        }

        private async Task LoadTicketsAsync()
        {
            Log.Information("Loading all tickets data...");
            try
            {
                Cursor = Cursors.WaitCursor;
                _allTicketsData.Clear();
                gridControlTickets.DataSource = null;

                var client = _apiClientService.CreateClient();
                var apiUrl = $"{_baseUrl}/api/tickets";
                Log.Information("Fetching all tickets from: {ApiUrl}", apiUrl);

                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    _allTicketsData = await response.Content.ReadFromJsonAsync<List<Bilet>>(_jsonOptions)
                                ?? new List<Bilet>();
                    Log.Information("Successfully loaded {Count} total tickets.", _allTicketsData.Count);

                    ApplyFiltersAndBindData();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load ALL tickets. Status: {StatusCode}, Content: {ErrorContent}",
                                     response.StatusCode, errorContent);
                    XtraMessageBox.Show($"Ошибка загрузки билетов: {response.ReasonPhrase}\n{errorContent}", "Ошибка API",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    gridControlTickets.DataSource = null;
                    UpdateButtonStates();
                }
            }
            catch (HttpRequestException httpEx)
            {
                Log.Error(httpEx, "Network error loading all tickets");
                XtraMessageBox.Show($"Сетевая ошибка при загрузке билетов: {httpEx.Message}", "Ошибка сети",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                gridControlTickets.DataSource = null;
                UpdateButtonStates();
            }
            catch (JsonException jsonEx)
            {
                Log.Error(jsonEx, "Error deserializing all tickets");
                XtraMessageBox.Show($"Ошибка обработки данных билетов: {jsonEx.Message}", "Ошибка данных",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                gridControlTickets.DataSource = null;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Generic error loading all tickets");
                XtraMessageBox.Show($"Произошла ошибка при загрузке билетов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                gridControlTickets.DataSource = null;
                UpdateButtonStates();
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ApplyFiltersAndBindData()
        {
            Log.Information("Applying client-side filters for tickets...");
            try
            {
                Cursor = Cursors.WaitCursor;
                gridControlTickets.DataSource = null;

                var routeScheduleId = lueRouteScheduleFilter.EditValue as long?;
                if (routeScheduleId == -1) routeScheduleId = null;

                IEnumerable<Bilet> filteredData = _allTicketsData;

                if (routeScheduleId.HasValue)
                {
                    filteredData = filteredData.Where(t => t.RouteId == routeScheduleId.Value);
                    Log.Information("Applying filter for Route ID: {RouteId}", routeScheduleId.Value);
                }

                var finalFilteredList = filteredData.ToList();
                gridControlTickets.DataSource = finalFilteredList;
                Log.Information("Client-side filtering applied. Displaying {Count} tickets.", finalFilteredList.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error applying client-side filters for tickets");
                XtraMessageBox.Show($"Произошла ошибка при применении фильтров: {ex.Message}", "Ошибка Фильтрации",
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = Cursors.Default;
                UpdateButtonStates();
                gridViewTickets.BestFitColumns();
            }
        }
        
        private void UpdateButtonStates()
        {
            var selectedTicket = gridViewTickets.GetFocusedRow() as Bilet;
            bool isSelected = selectedTicket != null;
            
            btnViewDetails.Enabled = isSelected;
            btnCancelTicket.Enabled = isSelected && !selectedTicket.Sales.Any();
        }

        private void gridViewTickets_CustomColumnDisplayText(object sender, CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == nameof(Bilet.TicketPrice) && e.Value is decimal price)
            {
                e.DisplayText = string.Format("{0:C}", price);
            }
        }

        private async void btnApplyFilter_Click(object sender, EventArgs e)
        {
            Log.Information("Apply Filter button clicked for tickets.");
            ApplyFiltersAndBindData();
        }

        private void btnViewDetails_Click(object sender, EventArgs e)
        {
            var selectedTicket = gridViewTickets.GetFocusedRow() as Bilet;
            if (selectedTicket == null) return;
            
            Log.Information("Viewing details for Ticket ID: {TicketId}", selectedTicket.TicketId);
            
            var details = new StringBuilder();
            details.AppendLine($"Билет ID: {selectedTicket.TicketId}");
            details.AppendLine($"Маршрут ID: {selectedTicket.RouteId}");
            if (selectedTicket.Marshut != null)
            {
                details.AppendLine($"Начальный пункт: {selectedTicket.Marshut.StartPoint}");
                details.AppendLine($"Конечный пункт: {selectedTicket.Marshut.EndPoint}");
            }
            details.AppendLine($"Цена билета: {selectedTicket.TicketPrice:C}");
            details.AppendLine($"Продан: {(selectedTicket.Sales.Any() ? "Да" : "Нет")}");
            
            XtraMessageBox.Show(details.ToString(), "Детали Билета", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void btnCancelTicket_Click(object sender, EventArgs e)
        {
            var selectedTicket = gridViewTickets.GetFocusedRow() as Bilet;
            if (selectedTicket == null || selectedTicket.Sales.Any()) return;
            
            Log.Warning("Attempting to cancel Ticket ID: {TicketId}", selectedTicket.TicketId);
            
            var confirmResult = XtraMessageBox.Show(
                $"Вы уверены, что хотите отменить билет #{selectedTicket.TicketId}?", 
                "Подтверждение отмены", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);
                
            if (confirmResult == DialogResult.Yes)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    btnCancelTicket.Enabled = false;
                    
                    var client = _apiClientService.CreateClient();
                    var apiUrl = $"{_baseUrl}/api/tickets/{selectedTicket.TicketId}";
                    
                    Log.Information("Calling DELETE {ApiUrl}", apiUrl);
                    var response = await client.DeleteAsync(apiUrl);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Ticket {TicketId} successfully cancelled.", selectedTicket.TicketId);
                        XtraMessageBox.Show("Билет успешно отменен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await LoadTicketsAsync();
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to cancel ticket {TicketId}. Status: {StatusCode}, Content: {ErrorContent}",
                                        selectedTicket.TicketId, response.StatusCode, errorContent);
                        XtraMessageBox.Show($"Ошибка при отмене билета: {response.ReasonPhrase}\n{errorContent}", "Ошибка API",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error cancelling ticket {TicketId}.", selectedTicket.TicketId);
                    XtraMessageBox.Show($"Произошла ошибка при отмене билета: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                    UpdateButtonStates();
                }
            }
            else
            {
                Log.Information("Cancellation aborted for Ticket ID: {TicketId}", selectedTicket.TicketId);
            }
        }
        
        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            Log.Information("Refresh button clicked for tickets.");
            await LoadTicketsAsync();
        }
    }
} 