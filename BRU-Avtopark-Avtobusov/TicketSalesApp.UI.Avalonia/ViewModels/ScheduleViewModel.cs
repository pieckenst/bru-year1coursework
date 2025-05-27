using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using ReactiveUI;
using TicketSalesApp.Core.Models;
using System.Linq;
using Serilog;
using TicketSalesApp.UI.Avalonia.Services;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace TicketSalesApp.UI.Avalonia.ViewModels
{
    public partial class ScheduleViewModel : ReactiveObject
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private User _cachedCurrentUser;

        private ObservableCollection<Marshut> _routes = new();
        public ObservableCollection<Marshut> Routes
        {
            get => _routes;
            set => this.RaiseAndSetIfChanged(ref _routes, value);
        }

        private Marshut? _selectedRoute;
        public Marshut? SelectedRoute
        {
            get => _selectedRoute;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedRoute, value);
                if (value != null)
                    ShowSchedule().ConfigureAwait(false);
            }
        }

        private DateTimeOffset _selectedDate = DateTimeOffset.Now;
        public DateTimeOffset SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (value == default)
                {
                    // If null or default, use current date
                    var now = DateTimeOffset.Now;
                    var today = new DateTimeOffset(now.Date, TimeZoneInfo.Local.GetUtcOffset(now.Date));
                    this.RaiseAndSetIfChanged(ref _selectedDate, today);
                }
                else
                {
                    // Ensure we only use the date part and preserve the local offset
                    var newDate = new DateTimeOffset(value.Date, TimeZoneInfo.Local.GetUtcOffset(value.Date));
                    this.RaiseAndSetIfChanged(ref _selectedDate, newDate);
                }

                if (SelectedRoute != null)
                {
                    ShowSchedule().ConfigureAwait(false);
                }
            }
        }

        private ObservableCollection<RouteSchedules> _scheduleItems = new();
        public ObservableCollection<RouteSchedules> ScheduleItems
        {
            get => _scheduleItems;
            set => this.RaiseAndSetIfChanged(ref _scheduleItems, value);
        }

        private RouteSchedules? _selectedScheduleItem;
        public RouteSchedules? SelectedScheduleItem
        {
            get => _selectedScheduleItem;
            set => this.RaiseAndSetIfChanged(ref _selectedScheduleItem, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => this.RaiseAndSetIfChanged(ref _hasError, value);
        }

        private bool _isMonthlyView;
        public bool IsMonthlyView
        {
            get => _isMonthlyView;
            set
            {
                this.RaiseAndSetIfChanged(ref _isMonthlyView, value);
                if (SelectedRoute != null)
                {
                    ShowSchedule().ConfigureAwait(false);
                }
            }
        }

        public ScheduleViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            _baseUrl = "http://localhost:5000/api";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };

            // Initialize empty collections
            Routes = new ObservableCollection<Marshut>();
            ScheduleItems = new ObservableCollection<RouteSchedules>();

            // Load initial data
            LoadRoutes().ConfigureAwait(false);

            // Subscribe to auth token changes
            ApiClientService.Instance.OnAuthTokenChanged += (sender, token) =>
            {
                var oldClient = _httpClient;
                _httpClient = ApiClientService.Instance.CreateClient();
                oldClient.Dispose();
                LoadRoutes().ConfigureAwait(false);
            };
        }

        private async Task LoadRoutes()
        {
            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;
                HasError = false;

                var response = await _httpClient.GetAsync($"{_baseUrl}/Routes");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var routes = JsonSerializer.Deserialize<List<Marshut>>(jsonString, _jsonOptions);

                    if (routes != null)
                    {
                        Routes = new ObservableCollection<Marshut>(routes);
                        Log.Information("Successfully loaded {Count} routes", routes.Count);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to load routes: {error}";
                    HasError = true;
                    Log.Error("Failed to load routes: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading routes: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error loading routes");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ShowSchedule()
        {
            if (SelectedRoute == null)
            {
                ErrorMessage = "Please select a route";
                HasError = true;
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;
                HasError = false;

                var selectedDate = SelectedDate.Date;
                Log.Information("Fetching schedules for route {RouteId} on {Date}", SelectedRoute.RouteId, selectedDate);

                // For monthly view, we'll fetch the entire month
                var startDate = IsMonthlyView ? new DateTime(selectedDate.Year, selectedDate.Month, 1) : selectedDate;
                var endDate = IsMonthlyView ? startDate.AddMonths(1).AddDays(-1) : selectedDate;

                var response = await _httpClient.GetAsync(
                    $"{_baseUrl}/RouteSchedules/search?routeId={SelectedRoute.RouteId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Log.Debug("Received response: {Response}", jsonString);
                    
                    var allSchedules = JsonSerializer.Deserialize<List<RouteSchedules>>(jsonString, _jsonOptions);

                    if (allSchedules != null && allSchedules.Any())
                    {
                        var filteredSchedules = allSchedules
                            .Where(s => s.IsActive &&
                                      ((IsMonthlyView && 
                                        s.DepartureTime.Date >= startDate && 
                                        s.DepartureTime.Date <= endDate) ||
                                       (!IsMonthlyView && s.DepartureTime.Date == selectedDate) ||
                                       (s.IsRecurring && s.DaysOfWeek.Contains(selectedDate.DayOfWeek.ToString()) &&
                                        s.ValidFrom.Date <= selectedDate &&
                                        (s.ValidUntil == null || s.ValidUntil.Value.Date >= selectedDate))))
                            .OrderBy(s => s.DepartureTime)
                            .ToList();

                        if (filteredSchedules.Any())
                        {
                            // Update departure times for recurring schedules
                            foreach (var schedule in filteredSchedules.Where(s => 
                                s.IsRecurring && s.DepartureTime.Date != selectedDate))
                            {
                                var targetDate = IsMonthlyView ? 
                                    GetNextValidDate(schedule, startDate, endDate) : 
                                    selectedDate;
                                
                                if (targetDate.HasValue)
                                {
                                    var newDepartureTime = targetDate.Value.Add(schedule.DepartureTime.TimeOfDay);
                                    var newArrivalTime = targetDate.Value.Add(schedule.ArrivalTime.TimeOfDay);
                                    schedule.DepartureTime = newDepartureTime;
                                    schedule.ArrivalTime = newArrivalTime;
                                }
                            }

                            ScheduleItems = new ObservableCollection<RouteSchedules>(filteredSchedules);
                            Log.Information("Successfully loaded {Count} schedule items for route {RouteId}", 
                                filteredSchedules.Count, SelectedRoute.RouteId);
                        }
                        else
                        {
                            ScheduleItems.Clear();
                            ErrorMessage = IsMonthlyView ? 
                                "No schedules found for selected month" : 
                                "No schedules found for selected date";
                            HasError = true;
                            Log.Warning("No schedules found for route {RouteId} on {Date}", 
                                SelectedRoute.RouteId, selectedDate.ToString("yyyy-MM-dd"));
                        }
                    }
                    else
                    {
                        ScheduleItems.Clear();
                        ErrorMessage = "No schedules found for selected route";
                        HasError = true;
                        Log.Warning("No schedules found for route {RouteId}", SelectedRoute.RouteId);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to load schedule: {error}";
                    HasError = true;
                    Log.Error("Failed to load schedule: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading schedule: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error loading schedule");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private DateTime? GetNextValidDate(RouteSchedules schedule, DateTime startDate, DateTime endDate)
        {
            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                if (schedule.DaysOfWeek.Contains(currentDate.DayOfWeek.ToString()) &&
                    schedule.ValidFrom.Date <= currentDate &&
                    (schedule.ValidUntil == null || schedule.ValidUntil.Value.Date >= currentDate))
                {
                    return currentDate;
                }
                currentDate = currentDate.AddDays(1);
            }
            return null;
        }

        private async Task<User> GetCurrentUserAsync()
        {
            if (_cachedCurrentUser != null)
                return _cachedCurrentUser;

            var response = await _httpClient.GetAsync($"{_baseUrl}/Users/current");
            if (!response.IsSuccessStatusCode)
            {
                await HandleAuthenticationError(response);
                return null;
            }

            var userJson = await response.Content.ReadAsStringAsync();
            _cachedCurrentUser = JsonSerializer.Deserialize<User>(userJson, _jsonOptions);
            return _cachedCurrentUser;
        }

        private async Task HandleAuthenticationError(HttpResponseMessage response)
        {
            try
            {
                var error = await response.Content.ReadAsStringAsync();
                var errorDetails = JsonSerializer.Deserialize<Dictionary<string, string>>(error, _jsonOptions);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    ApiClientService.Instance.AuthToken = null;
                    ErrorMessage = "Your session has expired. Please log in again.";
                    HasError = true;
                    Log.Warning("Authentication failed: {Error}", errorDetails?["message"] ?? "Unknown error");
                    return;
                }
                
                ErrorMessage = errorDetails?["message"] ?? "An error occurred during authentication";
                HasError = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error parsing authentication error response");
                ErrorMessage = "An unexpected error occurred";
                HasError = true;
            }
        }

        private async Task<bool> EnsureAuthenticatedAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Users/current");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                await HandleAuthenticationError(response);
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error checking authentication status");
                ErrorMessage = "Could not verify authentication status";
                HasError = true;
                return false;
            }
        }

        private async Task<Bilet> CreateNewTicketAsync(RouteSchedules schedule)
        {
            try
            {
                // First get the full route details
                var routeResponse = await _httpClient.GetAsync($"{_baseUrl}/Routes/{schedule.RouteId}");
                if (!routeResponse.IsSuccessStatusCode)
                {
                    Log.Error("Failed to get route details for ticket creation: {Error}", await routeResponse.Content.ReadAsStringAsync());
                    return null;
                }

                var routeJson = await routeResponse.Content.ReadAsStringAsync();
                var route = JsonSerializer.Deserialize<Marshut>(routeJson, _jsonOptions);

                if (route == null)
                {
                    Log.Error("Failed to deserialize route for ticket creation");
                    return null;
                }

                var newTicket = new
                {
                    RouteId = schedule.RouteId,
                    TicketPrice = schedule.Price,
                    DepartureTime = schedule.DepartureTime,
                    StartPoint = schedule.StartPoint,    // Use schedule's start point
                    EndPoint = schedule.EndPoint,        // Use schedule's end point
                    RouteStops = schedule.RouteStops     // Use schedule's route stops
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(newTicket, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/Tickets", content);
                if (response.IsSuccessStatusCode)
                {
                    var ticketJson = await response.Content.ReadAsStringAsync();
                    var createdTicket = JsonSerializer.Deserialize<Bilet>(ticketJson, _jsonOptions);
                    Log.Information("Successfully created new ticket {TicketId} for route {RouteId} from {Start} to {End}", 
                        createdTicket.TicketId, schedule.RouteId, schedule.StartPoint, schedule.EndPoint);
                    return createdTicket;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                Log.Error("Failed to create new ticket: {Error}", error);
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating new ticket");
                return null;
            }
        }

        [RelayCommand]
        private async Task BuyTicket(RouteSchedules? schedule)
        {
            if (schedule == null) return;

            if (!await EnsureAuthenticatedAsync())
            {
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;
                HasError = false;

                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return;
                }

                // First, get available tickets for this route and time
                var response = await _httpClient.GetAsync(
                    $"{_baseUrl}/Tickets/available?routeId={schedule.RouteId}&departureTime={schedule.DepartureTime:yyyy-MM-ddTHH:mm:ss}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var availableTickets = JsonSerializer.Deserialize<List<Bilet>>(jsonString, _jsonOptions);

                    Bilet? ticketToUse = null;

                    if (availableTickets == null || !availableTickets.Any())
                    {
                        // No available tickets, create a new one
                        ticketToUse = await CreateNewTicketAsync(schedule);
                        if (ticketToUse == null)
                        {
                            ErrorMessage = "Failed to create a new ticket. Please try again later.";
                            HasError = true;
                            return;
                        }
                    }
                    else
                    {
                        // Verify the first available ticket
                        var ticket = availableTickets.First();
                        var verifyResponse = await _httpClient.GetAsync($"{_baseUrl}/Tickets/{ticket.TicketId}");
                        
                        if (!verifyResponse.IsSuccessStatusCode)
                        {
                            // Ticket not found or error, create new one
                            ticketToUse = await CreateNewTicketAsync(schedule);
                            if (ticketToUse == null)
                            {
                                ErrorMessage = "Failed to create a new ticket. Please try again later.";
                                HasError = true;
                                return;
                            }
                        }
                        else
                        {
                            var ticketJson = await verifyResponse.Content.ReadAsStringAsync();
                            var currentTicket = JsonSerializer.Deserialize<Bilet>(ticketJson, _jsonOptions);

                            if (currentTicket.Sales != null && currentTicket.Sales.Any())
                            {
                                // Ticket is already sold, create new one
                                ticketToUse = await CreateNewTicketAsync(schedule);
                                if (ticketToUse == null)
                                {
                                    ErrorMessage = "Failed to create a new ticket. Please try again later.";
                                    HasError = true;
                                    return;
                                }
                            }
                            else
                            {
                                ticketToUse = currentTicket;
                            }
                        }
                    }

                    // Now try to create the sale
                    var sale = new
                    {
                        TicketId = ticketToUse.TicketId,
                        SaleDate = DateTime.Now,
                        TicketSoldToUser = currentUser.Login
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(sale, _jsonOptions),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    var saleResponse = await _httpClient.PostAsync($"{_baseUrl}/TicketSales", content);
                    if (saleResponse.IsSuccessStatusCode)
                    {
                        // Refresh schedule after successful purchase
                        await ShowSchedule();
                        Log.Information("Successfully purchased ticket {TicketId} for route {RouteId}", 
                            ticketToUse.TicketId, schedule.RouteId);
                        ErrorMessage = string.Empty;
                        HasError = false;
                    }
                    else
                    {
                        var error = await saleResponse.Content.ReadAsStringAsync();
                        
                        if (saleResponse.StatusCode == System.Net.HttpStatusCode.BadRequest && 
                            error.Contains("already sold"))
                        {
                            // One final attempt to create and buy a new ticket
                            var lastChanceTicket = await CreateNewTicketAsync(schedule);
                            if (lastChanceTicket != null)
                            {
                                var lastChanceSale = new
                                {
                                    TicketId = lastChanceTicket.TicketId,
                                    SaleDate = DateTime.Now,
                                    TicketSoldToUser = currentUser.Login
                                };

                                var lastChanceContent = new StringContent(
                                    JsonSerializer.Serialize(lastChanceSale, _jsonOptions),
                                    System.Text.Encoding.UTF8,
                                    "application/json");

                                var lastChanceResponse = await _httpClient.PostAsync($"{_baseUrl}/TicketSales", lastChanceContent);
                                if (lastChanceResponse.IsSuccessStatusCode)
                                {
                                    await ShowSchedule();
                                    Log.Information("Successfully purchased new ticket {TicketId} after final attempt", 
                                        lastChanceTicket.TicketId);
                                    ErrorMessage = string.Empty;
                                    HasError = false;
                                    return;
                                }
                            }

                            ErrorMessage = "Failed to purchase ticket. Please try again.";
                            HasError = true;
                        }
                        else if (saleResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        {
                            await HandleAuthenticationError(saleResponse);
                        }
                        else
                        {
                            ErrorMessage = $"Failed to buy ticket: {error}";
                            HasError = true;
                        }
                        
                        Log.Error("Failed to buy ticket: {Error}", error);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to check available tickets: {error}";
                    HasError = true;
                    Log.Error("Failed to check available tickets: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error buying ticket: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error buying ticket");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void ClearUserCache()
        {
            _cachedCurrentUser = null;
        }
    }
} 