using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ReactiveUI;
using TicketSalesApp.Core.Models;
using System.Linq;
using Serilog;
using TicketSalesApp.UI.Avalonia.Services;
using System.Collections.Generic;
using System.Text.Json;

namespace TicketSalesApp.UI.Avalonia.ViewModels
{
    public class SearchResultsViewModel : ReactiveObject
    {
        private readonly ApiClientService _apiClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private string _lastSearchQuery = string.Empty;

        private ObservableCollection<RouteSchedules> _scheduleResults = new();
        public ObservableCollection<RouteSchedules> ScheduleResults
        {
            get => _scheduleResults;
            set => this.RaiseAndSetIfChanged(ref _scheduleResults, value);
        }

        private ObservableCollection<Bilet> _ticketResults = new();
        public ObservableCollection<Bilet> TicketResults
        {
            get => _ticketResults;
            set => this.RaiseAndSetIfChanged(ref _ticketResults, value);
        }

        private ObservableCollection<Prodazha> _myTicketResults = new();
        public ObservableCollection<Prodazha> MyTicketResults
        {
            get => _myTicketResults;
            set => this.RaiseAndSetIfChanged(ref _myTicketResults, value);
        }

        private string _searchSummary = string.Empty;
        public string SearchSummary
        {
            get => _searchSummary;
            set => this.RaiseAndSetIfChanged(ref _searchSummary, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        public SearchResultsViewModel()
        {
            _apiClient = ApiClientService.Instance;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };
        }

        public async Task UpdateResults(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 3)
                return;

            _lastSearchQuery = searchQuery;
            IsLoading = true;

            try
            {
                await Task.WhenAll(
                    SearchSchedules(searchQuery),
                    SearchTickets(searchQuery),
                    SearchMyTickets(searchQuery)
                );

                UpdateSearchSummary();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during search");
                SearchSummary = $"Ошибка поиска: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SearchSchedules(string query)
        {
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync($"RouteSchedules/search?startPoint={Uri.EscapeDataString(query)}&endPoint={Uri.EscapeDataString(query)}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var schedules = JsonSerializer.Deserialize<List<RouteSchedules>>(content, _jsonOptions);
                    
                    if (schedules != null)
                    {
                        var filteredSchedules = schedules.Where(s =>
                            (s.StartPoint?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (s.EndPoint?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (s.RouteStops != null && s.RouteStops.Any(stop => 
                                stop.Contains(query, StringComparison.OrdinalIgnoreCase)))
                        ).ToList();

                        ScheduleResults = new ObservableCollection<RouteSchedules>(filteredSchedules);
                    }
                }
                else
                {
                    Log.Error("Failed to fetch schedules. Status code: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch schedules. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching schedules");
                throw;
            }
        }

        private async Task SearchTickets(string query)
        {
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync("Tickets/available");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var tickets = JsonSerializer.Deserialize<List<Bilet>>(content, _jsonOptions);
                    
                    if (tickets != null)
                    {
                        var filteredTickets = tickets.Where(t =>
                            t.Marshut != null && (
                                t.Marshut.StartPoint.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                t.Marshut.EndPoint.Contains(query, StringComparison.OrdinalIgnoreCase))
                        ).ToList();

                        TicketResults = new ObservableCollection<Bilet>(filteredTickets);
                    }
                }
                else
                {
                    Log.Error("Failed to fetch tickets. Status code: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch tickets. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching tickets");
                throw;
            }
        }

        private async Task SearchMyTickets(string query)
        {
            try
            {
                using var client = _apiClient.CreateClient();
                var response = await client.GetAsync("TicketSales");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var myTickets = JsonSerializer.Deserialize<List<Prodazha>>(content, _jsonOptions);
                    
                    if (myTickets != null)
                    {
                        var filteredMyTickets = myTickets.Where(t =>
                            t.Bilet?.Marshut != null && (
                                t.Bilet.Marshut.StartPoint.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                t.Bilet.Marshut.EndPoint.Contains(query, StringComparison.OrdinalIgnoreCase))
                        ).ToList();

                        MyTicketResults = new ObservableCollection<Prodazha>(filteredMyTickets);
                    }
                }
                else
                {
                    Log.Error("Failed to fetch my tickets. Status code: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch my tickets. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error searching my tickets");
                throw;
            }
        }

        private void UpdateSearchSummary()
        {
            SearchSummary = $"Найдено по запросу \"{_lastSearchQuery}\": " +
                          $"{ScheduleResults.Count} расписаний, " +
                          $"{TicketResults.Count} билетов, " +
                          $"{MyTicketResults.Count} моих билетов";
        }

        public void ClearResults()
        {
            ScheduleResults.Clear();
            TicketResults.Clear();
            MyTicketResults.Clear();
            SearchSummary = string.Empty;
            _lastSearchQuery = string.Empty;
        }
    }
} 