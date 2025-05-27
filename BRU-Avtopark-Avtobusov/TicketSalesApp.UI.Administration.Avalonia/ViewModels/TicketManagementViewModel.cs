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
    public partial class TicketManagementViewModel : ReactiveObject
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        private ObservableCollection<Bilet> _tickets = new();
        public ObservableCollection<Bilet> Tickets
        {
            get => _tickets;
            set => this.RaiseAndSetIfChanged(ref _tickets, value);
        }

        private ObservableCollection<Marshut> _routes = new();
        public ObservableCollection<Marshut> Routes
        {
            get => _routes;
            set => this.RaiseAndSetIfChanged(ref _routes, value);
        }

        private Bilet? _selectedTicket;
        public Bilet? SelectedTicket
        {
            get => _selectedTicket;
            set => this.RaiseAndSetIfChanged(ref _selectedTicket, value);
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchText, value);
                OnSearchTextChanged(value);
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

        public TicketManagementViewModel()
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

        [RelayCommand]
        private async Task LoadData()
        {
            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var ticketsResponse = await _httpClient.GetAsync($"{_baseUrl}/Tickets");
                var routesResponse = await _httpClient.GetAsync($"{_baseUrl}/Routes");

                if (ticketsResponse.IsSuccessStatusCode)
                {
                    var jsonString = await ticketsResponse.Content.ReadAsStringAsync();
                    var tickets = JsonSerializer.Deserialize<List<Bilet>>(jsonString, _jsonOptions);

                    if (tickets != null)
                        Tickets = new ObservableCollection<Bilet>(tickets);
                }

                if (routesResponse.IsSuccessStatusCode)
                {
                    var jsonString = await routesResponse.Content.ReadAsStringAsync();
                    var routes = JsonSerializer.Deserialize<List<Marshut>>(jsonString, _jsonOptions);

                    if (routes != null)
                        Routes = new ObservableCollection<Marshut>(routes);
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error loading data: {ex.Message}";
                Log.Error(ex, "Error loading ticket data");
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
                var dialog = new Window
                {
                    Title = "Добавить билет",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var routeComboBox = new ComboBox
                {
                    PlaceholderText = "Выберите маршрут",
                    ItemsSource = Routes,
                    DisplayMemberBinding = new global::Avalonia.Data.Binding("StartPoint")
                };

                var priceBox = new TextBox { Watermark = "Цена билета" };

                var addButton = new Button
                {
                    Content = "Добавить",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                grid.Children.Add(routeComboBox);
                Grid.SetRow(routeComboBox, 0);
                grid.Children.Add(priceBox);
                Grid.SetRow(priceBox, 1);
                grid.Children.Add(addButton);
                Grid.SetRow(addButton, 2);

                dialog.Content = grid;

                addButton.Click += async (s, e) =>
                {
                    if (routeComboBox.SelectedItem is not Marshut selectedRoute)
                    {
                        ErrorMessage = "Пожалуйста, выберите маршрут";
                        return;
                    }

                    if (!decimal.TryParse(priceBox.Text, out decimal price))
                    {
                        ErrorMessage = "Пожалуйста, введите корректную цену";
                        return;
                    }

                    var newTicket = new
                    {
                        RouteId = selectedRoute.RouteId,
                        TicketPrice = price
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(newTicket),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PostAsync($"{_baseUrl}/Tickets", content);
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        ErrorMessage = $"Failed to add ticket: {error}";
                    }
                };

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                }
                else
                {
                    Log.Error("Could not find main window for dialog");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error adding ticket: {ex.Message}";
                Log.Error(ex, "Error adding ticket");
            }
        }

        [RelayCommand]
        private async Task Edit()
        {
            if (SelectedTicket == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Редактировать билет",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var routeComboBox = new ComboBox
                {
                    PlaceholderText = "Выберите маршрут",
                    ItemsSource = Routes,
                    DisplayMemberBinding = new global::Avalonia.Data.Binding("StartPoint"),
                    SelectedItem = Routes.FirstOrDefault(r => r.RouteId == SelectedTicket.RouteId)
                };

                var priceBox = new TextBox
                {
                    Text = SelectedTicket.TicketPrice.ToString(),
                    Watermark = "Цена билета"
                };

                var updateButton = new Button
                {
                    Content = "Обновить",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                grid.Children.Add(routeComboBox);
                Grid.SetRow(routeComboBox, 0);
                grid.Children.Add(priceBox);
                Grid.SetRow(priceBox, 1);
                grid.Children.Add(updateButton);
                Grid.SetRow(updateButton, 2);

                dialog.Content = grid;

                updateButton.Click += async (s, e) =>
                {
                    if (routeComboBox.SelectedItem is not Marshut selectedRoute)
                    {
                        ErrorMessage = "Пожалуйста, выберите маршрут";
                        return;
                    }

                    if (!decimal.TryParse(priceBox.Text, out decimal price))
                    {
                        ErrorMessage = "Пожалуйста, введите корректную цену";
                        return;
                    }

                    var updatedTicket = new
                    {
                        RouteId = selectedRoute.RouteId,
                        TicketPrice = price
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(updatedTicket),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PutAsync(
                        $"{_baseUrl}/Tickets/{SelectedTicket.TicketId}",
                        content);

                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        ErrorMessage = $"Failed to update ticket: {error}";
                    }
                };

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                }
                else
                {
                    Log.Error("Could not find main window for dialog");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error updating ticket: {ex.Message}";
                Log.Error(ex, "Error updating ticket");
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedTicket == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Подтверждение удаления",
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
                    Text = "Вы уверены, что хотите удалить этот билет?",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10
                };

                var yesButton = new Button { Content = "Да" };
                var noButton = new Button { Content = "Нет" };

                buttonsPanel.Children.Add(yesButton);
                buttonsPanel.Children.Add(noButton);

                grid.Children.Add(textBlock);
                Grid.SetRow(textBlock, 0);
                grid.Children.Add(buttonsPanel);
                Grid.SetRow(buttonsPanel, 1);

                dialog.Content = grid;

                yesButton.Click += async (s, e) =>
                {
                    var response = await _httpClient.DeleteAsync($"{_baseUrl}/Tickets/{SelectedTicket.TicketId}");
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        ErrorMessage = $"Failed to delete ticket: {error}";
                    }
                };

                noButton.Click += (s, e) => dialog.Close();

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                }
                else
                {
                    Log.Error("Could not find main window for dialog");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error deleting ticket: {ex.Message}";
                Log.Error(ex, "Error deleting ticket");
            }
        }

        private void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                LoadData().ConfigureAwait(false);
                return;
            }

            var filteredTickets = Tickets.Where(t =>
                t.Marshut.StartPoint.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                t.Marshut.EndPoint.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                t.TicketPrice.ToString().Contains(value, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            Tickets = new ObservableCollection<Bilet>(filteredTickets);
        }
    }
} 