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
    public partial class MyTicketsViewModel : ReactiveObject
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private User _cachedCurrentUser;

        private ObservableCollection<Prodazha> _purchasedTickets = new();
        public ObservableCollection<Prodazha> PurchasedTickets
        {
            get => _purchasedTickets;
            set => this.RaiseAndSetIfChanged(ref _purchasedTickets, value);
        }

        private ObservableCollection<string> _sortOptions = new()
        {
            "По дате (сначала новые)",
            "По дате (сначала старые)",
            "По цене (по возрастанию)",
            "По цене (по убыванию)"
        };
        public ObservableCollection<string> SortOptions
        {
            get => _sortOptions;
            set => this.RaiseAndSetIfChanged(ref _sortOptions, value);
        }

        private ObservableCollection<string> _filterOptions = new()
        {
            "Все билеты",
            "Предстоящие поездки",
            "Прошедшие поездки"
        };
        public ObservableCollection<string> FilterOptions
        {
            get => _filterOptions;
            set => this.RaiseAndSetIfChanged(ref _filterOptions, value);
        }

        private string _selectedSortOption;
        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSortOption, value);
                ApplySortingAndFiltering();
            }
        }

        private string _selectedFilterOption;
        public string SelectedFilterOption
        {
            get => _selectedFilterOption;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFilterOption, value);
                ApplySortingAndFiltering();
            }
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

        private Prodazha _selectedTicket;
        public Prodazha SelectedTicket
        {
            get => _selectedTicket;
            set => this.RaiseAndSetIfChanged(ref _selectedTicket, value);
        }

        public MyTicketsViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            _baseUrl = "http://localhost:5000/api";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };

            SelectedSortOption = SortOptions[0];
            SelectedFilterOption = FilterOptions[0];

            LoadData().ConfigureAwait(false);
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

        [RelayCommand]
        private async Task LoadData()
        {
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

                // Get sales filtered by current user
                var response = await _httpClient.GetAsync($"{_baseUrl}/TicketSales/search?soldToUser={currentUser.Login}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var sales = JsonSerializer.Deserialize<List<Prodazha>>(jsonString, _jsonOptions);

                    if (sales != null)
                    {
                        PurchasedTickets = new ObservableCollection<Prodazha>(sales);
                        Log.Information("Successfully loaded {Count} tickets for user {User}", sales.Count, currentUser.Login);
                        ApplySortingAndFiltering();
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleAuthenticationError(response);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to load tickets: {error}";
                    HasError = true;
                    Log.Error("Failed to load tickets: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error loading data: {ex.Message}";
                Log.Error(ex, "Error loading data");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ApplySortingAndFiltering()
        {
            try
            {
                if (PurchasedTickets == null || !PurchasedTickets.Any()) return;

                // Create a new list from the original data
                var filteredList = new List<Prodazha>(PurchasedTickets);

                // Apply filtering
                switch (SelectedFilterOption)
                {
                    case "Предстоящие поездки":
                        filteredList = filteredList.Where(t => t.SaleDate > DateTime.Now).ToList();
                        break;
                    case "Прошедшие поездки":
                        filteredList = filteredList.Where(t => t.SaleDate <= DateTime.Now).ToList();
                        break;
                }

                // Apply sorting
                switch (SelectedSortOption)
                {
                    case "По дате (сначала новые)":
                        filteredList = filteredList.OrderByDescending(t => t.SaleDate).ToList();
                        break;
                    case "По дате (сначала старые)":
                        filteredList = filteredList.OrderBy(t => t.SaleDate).ToList();
                        break;
                    case "По цене (по возрастанию)":
                        filteredList = filteredList.OrderBy(t => t.Bilet.TicketPrice).ToList();
                        break;
                    case "По цене (по убыванию)":
                        filteredList = filteredList.OrderByDescending(t => t.Bilet.TicketPrice).ToList();
                        break;
                }

                // Update the collection with filtered and sorted data
                PurchasedTickets = new ObservableCollection<Prodazha>(filteredList);
                
                Log.Information("Applied filter: {Filter}, sort: {Sort}. Showing {Count} tickets", 
                    SelectedFilterOption, SelectedSortOption, PurchasedTickets.Count);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error applying filters: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error applying filters and sorting");
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            await LoadData();
        }

        [RelayCommand]
        private async Task PrintTicket(Prodazha sale)
        {
            if (sale == null) return;

            if (!await EnsureAuthenticatedAsync())
            {
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                var response = await _httpClient.GetAsync($"{_baseUrl}/TicketSales/{sale.SaleId}/print");
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        await HandleAuthenticationError(response);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        ErrorMessage = $"Failed to print ticket: {error}";
                        Log.Error("Failed to print ticket: {Error}", error);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error printing ticket: {ex.Message}";
                Log.Error(ex, "Error printing ticket");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RefundTicket(Prodazha sale)
        {
            if (sale == null) return;

            if (!await EnsureAuthenticatedAsync())
            {
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                var response = await _httpClient.DeleteAsync($"{_baseUrl}/TicketSales/{sale.SaleId}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadData();
                    Log.Information("Successfully refunded ticket {TicketId}", sale.Bilet.TicketId);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await HandleAuthenticationError(response);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "This ticket sale no longer exists";
                    HasError = true;
                    await LoadData(); // Refresh to get current state
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to refund ticket: {error}";
                    HasError = true;
                    Log.Error("Failed to refund ticket: {Error}", error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error refunding ticket: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error refunding ticket");
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