using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using TicketSalesApp.Core.Models;
using Avalonia.Controls;
using System.Linq;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Serilog;
using TicketSalesApp.UI.Administration.Avalonia.Services;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;

namespace TicketSalesApp.UI.Administration.Avalonia.ViewModels
{
    public partial class SalesManagementViewModel : ReactiveObject
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        private ObservableCollection<Prodazha> _sales = new();
        public ObservableCollection<Prodazha> Sales
        {
            get => _sales;
            set => this.RaiseAndSetIfChanged(ref _sales, value);
        }

        private ObservableCollection<Bilet> _availableTickets = new();
        public ObservableCollection<Bilet> AvailableTickets
        {
            get => _availableTickets;
            set => this.RaiseAndSetIfChanged(ref _availableTickets, value);
        }

        private Prodazha? _selectedSale;
        public Prodazha? SelectedSale
        {
            get => _selectedSale;
            set => this.RaiseAndSetIfChanged(ref _selectedSale, value);
        }

        private DateTimeOffset _startDate = DateTimeOffset.Now.AddMonths(-1);
        public DateTimeOffset StartDate
        {
            get => _startDate;
            set
            {
                this.RaiseAndSetIfChanged(ref _startDate, value);
                LoadData().ConfigureAwait(false);
            }
        }

        private DateTimeOffset _endDate = DateTimeOffset.Now;
        public DateTimeOffset EndDate
        {
            get => _endDate;
            set
            {
                this.RaiseAndSetIfChanged(ref _endDate, value);
                LoadData().ConfigureAwait(false);
            }
        }

        private decimal _totalIncome;
        public decimal TotalIncome
        {
            get => _totalIncome;
            set => this.RaiseAndSetIfChanged(ref _totalIncome, value);
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

        public SalesManagementViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            _baseUrl = "http://localhost:5000/api";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };

            ApiClientService.Instance.OnAuthTokenChanged += (_, token) =>
            {
                _httpClient = ApiClientService.Instance.CreateClient();
                LoadData().ConfigureAwait(false);
            };

            LoadData().ConfigureAwait(false);
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Users/current");
                if (response.IsSuccessStatusCode)
                {
                    var userJson = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<User>(userJson, _jsonOptions);
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting current user");
                return null;
            }
        }

        [RelayCommand]
        private async Task LoadData()
        {
            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                // First get all routes
                var routesResponse = await _httpClient.GetAsync($"{_baseUrl}/Routes");
                if (!routesResponse.IsSuccessStatusCode)
                {
                    var error = await routesResponse.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to load routes: {error}";
                    HasError = true;
                    Log.Error("Failed to load routes: {Error}", error);
                    return;
                }

                var routesJson = await routesResponse.Content.ReadAsStringAsync();
                var routes = JsonSerializer.Deserialize<List<Marshut>>(routesJson, _jsonOptions);

                if (routes == null || !routes.Any())
                {
                    ErrorMessage = "No routes available";
                    HasError = true;
                    return;
                }

                // Get sales for date range
                var salesResponse = await _httpClient.GetAsync(
                    $"{_baseUrl}/TicketSales/search?startDate={StartDate.Date:yyyy-MM-dd}&endDate={EndDate.Date:yyyy-MM-dd}");

                if (salesResponse.IsSuccessStatusCode)
                {
                    var jsonString = await salesResponse.Content.ReadAsStringAsync();
                    var sales = JsonSerializer.Deserialize<List<Prodazha>>(jsonString, _jsonOptions);

                    if (sales != null)
                    {
                        Sales = new ObservableCollection<Prodazha>(sales.OrderByDescending(s => s.SaleDate));
                        TotalIncome = sales.Sum(s => s.Bilet.TicketPrice);
                        Log.Information("Loaded {Count} sales with total income {Income}", sales.Count, TotalIncome);
                    }
                }
                else
                {
                    var error = await salesResponse.Content.ReadAsStringAsync();
                    ErrorMessage = $"Failed to load sales: {error}";
                    HasError = true;
                    Log.Error("Failed to load sales: {Error}", error);
                }

                // Get available tickets for each route
                var allAvailableTickets = new List<Bilet>();
                foreach (var route in routes)
                {
                    var ticketsResponse = await _httpClient.GetAsync($"{_baseUrl}/Tickets/route/{route.RouteId}");
                    if (ticketsResponse.IsSuccessStatusCode)
                    {
                        var ticketsJson = await ticketsResponse.Content.ReadAsStringAsync();
                        var routeTickets = JsonSerializer.Deserialize<List<Bilet>>(ticketsJson, _jsonOptions);

                        if (routeTickets != null)
                        {
                            // Add only unsold tickets
                            var availableRouteTickets = routeTickets.Where(t => t.Sales == null || !t.Sales.Any()).ToList();
                            allAvailableTickets.AddRange(availableRouteTickets);
                        }
                    }
                }

                // Order tickets by route
                var orderedTickets = allAvailableTickets
                    .OrderBy(t => t.Marshut.StartPoint)
                    .ThenBy(t => t.Marshut.EndPoint)
                    .ToList();

                AvailableTickets = new ObservableCollection<Bilet>(orderedTickets);
                Log.Information("Loaded {Count} available tickets across {RouteCount} routes", 
                    orderedTickets.Count, routes.Count);
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error loading data: {ex.Message}";
                Log.Error(ex, "Error loading sales data");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Add()
        {
            try
            {
                Log.Debug("Starting Add() method for new ticket sale");
                
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null)
                {
                    Log.Error("Failed to get current user information");
                    HasError = true;
                    ErrorMessage = "Failed to get current user information";
                    return;
                }

                // Get all available tickets
                var ticketsResponse = await _httpClient.GetAsync($"{_baseUrl}/Tickets/available");
                if (!ticketsResponse.IsSuccessStatusCode)
                {
                    Log.Error("Failed to get available tickets");
                    HasError = true;
                    ErrorMessage = "Failed to get available tickets";
                    return;
                }

                var ticketsJson = await ticketsResponse.Content.ReadAsStringAsync();
                var availableTickets = JsonSerializer.Deserialize<List<Bilet>>(ticketsJson, _jsonOptions);

                if (availableTickets == null || !availableTickets.Any())
                {
                    Log.Warning("No available tickets found");
                    HasError = true;
                    ErrorMessage = "No available tickets found";
                    return;
                }

                Log.Debug("Creating add sale dialog window");
                var dialog = new Window
                {
                    Title = "Добавить продажу",
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto"),
                    Margin = new Thickness(10)
                };

                Log.Debug("Setting up ticket selection combobox with {Count} available tickets", availableTickets.Count);
                var ticketComboBox = new ComboBox
                {
                    PlaceholderText = "Выберите маршрут",
                    ItemsSource = availableTickets.OrderBy(t => t.Marshut.StartPoint).ThenBy(t => t.Marshut.EndPoint),
                    Width = 400,
                    DisplayMemberBinding = new global::Avalonia.Data.Binding("Marshut.StartPoint")
                };

                var routeInfoTextBlock = new TextBlock
                {
                    Text = "",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 5)
                };

                ticketComboBox.SelectionChanged += (s, e) =>
                {
                    if (ticketComboBox.SelectedItem is Bilet selectedTicket)
                    {
                        Log.Debug("Ticket selected: Route {Start} - {End}, Price: {Price}", 
                            selectedTicket.Marshut.StartPoint,
                            selectedTicket.Marshut.EndPoint,
                            selectedTicket.TicketPrice);
                            
                        routeInfoTextBlock.Text = $"Маршрут: {selectedTicket.Marshut.StartPoint} - {selectedTicket.Marshut.EndPoint}\n" +
                                                $"Время в пути: {selectedTicket.Marshut.TravelTime}\n" +
                                                $"Цена: {selectedTicket.TicketPrice:F2}";
                    }
                    else
                    {
                        Log.Debug("No ticket selected in combobox");
                        routeInfoTextBlock.Text = "";
                    }
                };

                Log.Debug("Setting up date picker with default date {Date}", DateTimeOffset.Now);
                var datePicker = new DatePicker
                {
                    SelectedDate = DateTimeOffset.Now
                };

                Log.Debug("Setting up sale type textbox with default value");
                var saleTypeTextBox = new TextBox
                {
                    Watermark = "Покупатель",
                    Text = "ФИЗ.ПРОДАЖА",
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Log.Debug("Setting up phone textbox with user's phone: {Phone}", currentUser.PhoneNumber ?? "+375333000000");
                var phoneTextBox = new TextBox
                {
                    Watermark = "Телефон покупателя",
                    Text = currentUser.PhoneNumber ?? "+375333000000",
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var addButton = new Button
                {
                    Content = "Добавить",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Log.Debug("Adding controls to dialog grid");
                grid.Children.Add(ticketComboBox);
                Grid.SetRow(ticketComboBox, 0);
                grid.Children.Add(routeInfoTextBlock);
                Grid.SetRow(routeInfoTextBlock, 1);
                grid.Children.Add(datePicker);
                Grid.SetRow(datePicker, 2);
                grid.Children.Add(saleTypeTextBox);
                Grid.SetRow(saleTypeTextBox, 3);
                grid.Children.Add(phoneTextBox);
                Grid.SetRow(phoneTextBox, 4);
                grid.Children.Add(addButton);
                Grid.SetRow(addButton, 5);

                dialog.Content = grid;

                addButton.Click += async (s, e) =>
                {
                    Log.Debug("Add button clicked");
                    
                    if (ticketComboBox.SelectedItem is not Bilet selectedTicket)
                    {
                        Log.Warning("Add attempted without selecting a ticket");
                        ErrorMessage = "Пожалуйста, выберите билет";
                        return;
                    }

                    Log.Debug("Creating new sale for ticket {TicketId}", selectedTicket.TicketId);
                    var newSale = new
                    {
                        TicketId = selectedTicket.TicketId,
                        SaleDate = datePicker.SelectedDate?.DateTime ?? DateTimeOffset.Now.DateTime,
                        TicketSoldToUser = string.IsNullOrWhiteSpace(saleTypeTextBox.Text) ? "ФИЗ.ПРОДАЖА" : saleTypeTextBox.Text,
                        TicketSoldToUserPhone = string.IsNullOrWhiteSpace(phoneTextBox.Text) ? currentUser.PhoneNumber : phoneTextBox.Text
                    };

                    Log.Debug("Sending POST request to create sale: {@SaleData}", newSale);
                    var content = new StringContent(
                        JsonSerializer.Serialize(newSale, _jsonOptions),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PostAsync($"{_baseUrl}/TicketSales", content);
                    if (response.IsSuccessStatusCode)
                    {
                        Log.Information("Successfully created new ticket sale for ticket {TicketId}", selectedTicket.TicketId);
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Error("Failed to create ticket sale: {Error}", error);
                        ErrorMessage = $"Failed to add sale: {error}";
                        HasError = true;
                    }
                };

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    Log.Debug("Showing add sale dialog");
                    await dialog.ShowDialog(mainWindow);
                }
                else
                {
                    Log.Error("Could not find main window for dialog");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in Add() method");
                HasError = true;
                ErrorMessage = $"Error adding sale: {ex.Message}";
                Log.Error(ex, "Error adding sale");
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            try
            {
                if (SelectedSale == null)
                {
                    ErrorMessage = "Выберите продажу для возврата";
                    HasError = true;
                    return;
                }

                var dialog = new Window
                {
                    Title = "Подтверждение возврата",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var textBlock = new TextBlock
                {
                    Text = $"Вы уверены, что хотите вернуть билет?\n\nМаршрут: {SelectedSale.Bilet.Marshut.StartPoint} - {SelectedSale.Bilet.Marshut.EndPoint}\nЦена: {SelectedSale.Bilet.TicketPrice:C}",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10
                };

                var yesButton = new Button 
                { 
                    Content = "Да",
                    Background = new SolidColorBrush(Color.Parse("#e74c3c")),
                    Foreground = Brushes.White
                };
                
                var noButton = new Button 
                { 
                    Content = "Нет",
                    Background = new SolidColorBrush(Color.Parse("#7f8c8d")),
                    Foreground = Brushes.White
                };

                buttonsPanel.Children.Add(yesButton);
                buttonsPanel.Children.Add(noButton);

                grid.Children.Add(textBlock);
                Grid.SetRow(textBlock, 0);
                grid.Children.Add(buttonsPanel);
                Grid.SetRow(buttonsPanel, 1);

                dialog.Content = grid;

                var tcs = new TaskCompletionSource<bool>();

                yesButton.Click += async (s, e) =>
                {
                    try
                    {
                        var saleId = SelectedSale.SaleId; // Store ID before any potential changes
                        var response = await _httpClient.DeleteAsync($"{_baseUrl}/TicketSales/{saleId}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadData();
                            dialog.Close();
                            ErrorMessage = string.Empty;
                            HasError = false;
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            ErrorMessage = $"Ошибка возврата: {error}";
                            HasError = true;
                            Log.Error("Failed to delete sale: {Error}", error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Ошибка возврата: {ex.Message}";
                        HasError = true;
                        Log.Error(ex, "Error deleting sale");
                    }
                    finally
                    {
                        tcs.TrySetResult(true);
                    }
                };

                noButton.Click += (s, e) =>
                {
                    dialog.Close();
                    tcs.TrySetResult(false);
                };

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                    await tcs.Task; // Wait for dialog result
                }
                else
                {
                    Log.Error("Could not find main window for dialog");
                    ErrorMessage = "Системная ошибка: не найдено главное окно";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Ошибка возврата: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error in Delete method");
            }
        }
    }
} 