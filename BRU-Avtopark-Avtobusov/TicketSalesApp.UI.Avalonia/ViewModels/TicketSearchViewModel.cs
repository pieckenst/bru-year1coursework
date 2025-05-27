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
    public partial class TicketSearchViewModel : ReactiveObject
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private User _cachedCurrentUser;

        private ObservableCollection<string> _startPoints = new();
        public ObservableCollection<string> StartPoints
        {
            get => _startPoints;
            set => this.RaiseAndSetIfChanged(ref _startPoints, value);
        }

        private ObservableCollection<string> _endPoints = new();
        public ObservableCollection<string> EndPoints
        {
            get => _endPoints;
            set => this.RaiseAndSetIfChanged(ref _endPoints, value);
        }

        private string _selectedStartPoint;
        public string SelectedStartPoint
        {
            get => _selectedStartPoint;
            set => this.RaiseAndSetIfChanged(ref _selectedStartPoint, value);
        }

        private string _selectedEndPoint;
        public string SelectedEndPoint
        {
            get => _selectedEndPoint;
            set => this.RaiseAndSetIfChanged(ref _selectedEndPoint, value);
        }

        private DateTimeOffset _selectedDate = DateTimeOffset.Now;
        public DateTimeOffset SelectedDate
        {
            get => _selectedDate;
            set => this.RaiseAndSetIfChanged(ref _selectedDate, value);
        }

        private ObservableCollection<Bilet> _availableTickets = new();
        public ObservableCollection<Bilet> AvailableTickets
        {
            get => _availableTickets;
            set => this.RaiseAndSetIfChanged(ref _availableTickets, value);
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

        private Bilet _selectedTicket;
        public Bilet SelectedTicket
        {
            get => _selectedTicket;
            set => this.RaiseAndSetIfChanged(ref _selectedTicket, value);
        }

        public TicketSearchViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            _baseUrl = "http://localhost:5000/api";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };

            LoadRoutePoints().ConfigureAwait(false);
        }

        private async Task LoadRoutePoints()
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
                        StartPoints = new ObservableCollection<string>(
                            routes.Select(r => r.StartPoint).Distinct().OrderBy(p => p));
                        EndPoints = new ObservableCollection<string>(
                            routes.Select(r => r.EndPoint).Distinct().OrderBy(p => p));
                        Log.Information("Successfully loaded {StartPointCount} start points and {EndPointCount} end points",
                            StartPoints.Count, EndPoints.Count);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to load route points: {error}";
                    HasError = true;
                    Log.Error("Failed to load route points: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading route points: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error loading route points");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task HandleAuthenticationError(HttpResponseMessage response)
        {
            try
            {
                var error = await response.Content.ReadAsStringAsync();
                var errorDetails = JsonSerializer.Deserialize<Dictionary<string, string>>(error, _jsonOptions);
                
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Clear the token and notify the user
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

        [RelayCommand]
        private async Task Search()
        {
            if (!await EnsureAuthenticatedAsync())
            {
                return;
            }

            if (string.IsNullOrEmpty(SelectedStartPoint) || string.IsNullOrEmpty(SelectedEndPoint))
            {
                ErrorMessage = "Please select both start and end points";
                HasError = true;
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;
                HasError = false;

                // Create a list to store available tickets
                var availableTickets = new List<Bilet>();

                // First get available routes
                var routesResponse = await _httpClient.GetAsync(
                    $"{_baseUrl}/Routes/search?startPoint={SelectedStartPoint}&endPoint={SelectedEndPoint}");

                if (routesResponse.IsSuccessStatusCode)
                {
                    var routesJson = await routesResponse.Content.ReadAsStringAsync();
                    var routes = JsonSerializer.Deserialize<List<Marshut>>(routesJson, _jsonOptions);

                    if (routes != null && routes.Any())
                    {
                        // Then get available tickets for these routes
                        foreach (var route in routes)
                        {
                            var ticketsResponse = await _httpClient.GetAsync(
                                $"{_baseUrl}/Tickets/search?routeId={route.RouteId}&date={SelectedDate:yyyy-MM-dd}");

                            if (ticketsResponse.IsSuccessStatusCode)
                            {
                                var ticketsJson = await ticketsResponse.Content.ReadAsStringAsync();
                                var tickets = JsonSerializer.Deserialize<List<Bilet>>(ticketsJson, _jsonOptions);

                                if (tickets != null && tickets.Any())
                                {
                                    // Filter tickets for this route based on start and end points
                                    var routeTickets = tickets.Where(t => 
                                        t.Marshut.StartPoint == SelectedStartPoint && 
                                        t.Marshut.EndPoint == SelectedEndPoint).ToList();

                                    availableTickets.AddRange(routeTickets);
                                }
                            }
                            else
                            {
                                var error = await ticketsResponse.Content.ReadAsStringAsync();
                                Log.Warning("Failed to get tickets for route {RouteId}: {Error}", route.RouteId, error);
                            }
                        }

                        if (availableTickets.Any())
                        {
                            // Sort tickets by route and price
                            availableTickets = availableTickets
                                .OrderBy(t => t.Marshut.StartPoint)
                                .ThenBy(t => t.TicketPrice)
                                .ToList();
                            AvailableTickets = new ObservableCollection<Bilet>(availableTickets);
                            Log.Information("Successfully loaded {Count} available tickets", availableTickets.Count);
                        }
                        else
                        {
                            AvailableTickets.Clear();
                            ErrorMessage = "No tickets available for selected route and date";
                            HasError = true;
                            Log.Warning("No tickets found for {StartPoint} to {EndPoint} on {Date}", 
                                SelectedStartPoint, SelectedEndPoint, SelectedDate.Date);
                        }
                    }
                    else
                    {
                        AvailableTickets.Clear();
                        ErrorMessage = "No routes found for selected points";
                        HasError = true;
                        Log.Warning("No routes found for {StartPoint} to {EndPoint}", 
                            SelectedStartPoint, SelectedEndPoint);
                    }
                }
                else
                {
                    var error = await routesResponse.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to search routes: {error}";
                    HasError = true;
                    Log.Error("Failed to search routes: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error searching tickets: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error searching tickets");
            }
            finally
            {
                IsBusy = false;
            }
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

        private async Task<Bilet> CreateNewTicketAsync(Marshut route)
        {
            try
            {
                var newTicket = new
                {
                    RouteId = route.RouteId,
                    TicketPrice = 0.25m // Use default price or fallback to 100
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
                    Log.Information("Successfully created new ticket {TicketId} for route {RouteId}", 
                        createdTicket.TicketId, route.RouteId);
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
        private async Task BuyTicket(Bilet ticket)
        {
            if (ticket == null) return;

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;
                HasError = false;

                // First verify the ticket is still available
                var verifyResponse = await _httpClient.GetAsync($"{_baseUrl}/Tickets/{ticket.TicketId}");
                if (!verifyResponse.IsSuccessStatusCode)
                {
                    if (verifyResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Try to create a new ticket for this route
                        var newTicket = await CreateNewTicketAsync(ticket.Marshut);
                        if (newTicket != null)
                        {
                            ticket = newTicket; // Use the newly created ticket
                            Log.Information("Created new ticket {TicketId} as replacement", newTicket.TicketId);
                        }
                        else
                        {
                            ErrorMessage = "Unable to create a new ticket. Please try again later.";
                            HasError = true;
                            await Search(); // Refresh the list
                            return;
                        }
                    }
                    else
                    {
                        var error = await verifyResponse.Content.ReadAsStringAsync();
                        ErrorMessage = $"Failed to verify ticket availability: {error}";
                        HasError = true;
                        return;
                    }
                }

                var ticketJson = await verifyResponse.Content.ReadAsStringAsync();
                var currentTicket = JsonSerializer.Deserialize<Bilet>(ticketJson, _jsonOptions);

                if (currentTicket.Sales != null && currentTicket.Sales.Any())
                {
                    // Try to create a new ticket for this route
                    var newTicket = await CreateNewTicketAsync(ticket.Marshut);
                    if (newTicket != null)
                    {
                        currentTicket = newTicket; // Use the newly created ticket
                        Log.Information("Created new ticket {TicketId} as replacement for sold ticket", newTicket.TicketId);
                    }
                    else
                    {
                        ErrorMessage = "This ticket has already been sold and we couldn't create a new one. Please try again later.";
                        HasError = true;
                        await Search(); // Refresh to get updated availability
                        return;
                    }
                }

                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return;
                }

                var sale = new
                {
                    TicketId = currentTicket.TicketId, // Use the verified or newly created ticket ID
                    SaleDate = DateTime.Now,
                    TicketSoldToUser = currentUser.Login
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(sale, _jsonOptions),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/TicketSales", content);
                if (response.IsSuccessStatusCode)
                {
                    await Search(); // Refresh available tickets
                    Log.Information("Successfully purchased ticket {TicketId}", currentTicket.TicketId);
                    ErrorMessage = string.Empty;
                    HasError = false;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest && 
                        error.Contains("already sold"))
                    {
                        // One final attempt to create and buy a new ticket
                        var lastChanceTicket = await CreateNewTicketAsync(ticket.Marshut);
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
                                await Search();
                                Log.Information("Successfully purchased new ticket {TicketId} after final attempt", lastChanceTicket.TicketId);
                                ErrorMessage = string.Empty;
                                HasError = false;
                                return;
                            }
                        }

                        ErrorMessage = "This ticket has already been sold. Please choose another ticket.";
                        await Search(); // Refresh to get updated availability
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        await HandleAuthenticationError(response);
                    }
                    else
                    {
                        ErrorMessage = $"Failed to buy ticket: {error}";
                    }
                    
                    Log.Error("Failed to buy ticket: {Error}", error);
                    HasError = true;
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