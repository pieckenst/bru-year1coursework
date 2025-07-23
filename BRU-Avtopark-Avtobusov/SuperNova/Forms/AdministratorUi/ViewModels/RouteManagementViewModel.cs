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
using SuperNova.Forms.Services;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace SuperNova.Forms.AdministratorUi.ViewModels
{
    public partial class RouteManagementViewModel : ReactiveObject
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        private ObservableCollection<Marshut> _routes = new();
        public ObservableCollection<Marshut> Routes
        {
            get => _routes;
            set => this.RaiseAndSetIfChanged(ref _routes, value);
        }

        private ObservableCollection<Avtobus> _buses = new();
        public ObservableCollection<Avtobus> Buses
        {
            get => _buses;
            set => this.RaiseAndSetIfChanged(ref _buses, value);
        }

        private ObservableCollection<Employee> _drivers = new();
        public ObservableCollection<Employee> Drivers
        {
            get => _drivers;
            set => this.RaiseAndSetIfChanged(ref _drivers, value);
        }

        private Marshut? _selectedRoute;
        public Marshut? SelectedRoute
        {
            get => _selectedRoute;
            set => this.RaiseAndSetIfChanged(ref _selectedRoute, value);
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

        public RouteManagementViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            _baseUrl = "http://localhost:5000/api";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                MaxDepth = 64,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Skip,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
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

                // Load routes
                var routesResponse = await _httpClient.GetAsync($"{_baseUrl}/Routes");
                if (routesResponse.IsSuccessStatusCode)
                {
                    var jsonString = await routesResponse.Content.ReadAsStringAsync();
                    var loadedRoutes = JsonSerializer.Deserialize<List<Marshut>>(jsonString, _jsonOptions);
                    if (loadedRoutes != null)
                    {
                        Routes = new ObservableCollection<Marshut>(loadedRoutes);
                    }
                }

                // Load buses
                var busesResponse = await _httpClient.GetAsync($"{_baseUrl}/Buses");
                if (busesResponse.IsSuccessStatusCode)
                {
                    var jsonString = await busesResponse.Content.ReadAsStringAsync();
                    var loadedBuses = JsonSerializer.Deserialize<List<Avtobus>>(jsonString, _jsonOptions);
                    if (loadedBuses != null)
                    {
                        Buses = new ObservableCollection<Avtobus>(loadedBuses);
                    }
                }

                // Load drivers (employees)
                var driversResponse = await _httpClient.GetAsync($"{_baseUrl}/Employees");
                if (driversResponse.IsSuccessStatusCode)
                {
                    var jsonString = await driversResponse.Content.ReadAsStringAsync();
                    var loadedDrivers = JsonSerializer.Deserialize<List<Employee>>(jsonString, _jsonOptions);
                    if (loadedDrivers != null)
                    {
                        Drivers = new ObservableCollection<Employee>(loadedDrivers);
                    }
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error loading data: {ex.Message}";
                Log.Error(ex, "Error loading routes data");
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
                    Title = "Добавить маршрут",
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var startPointBox = new TextBox { Watermark = "Начальная точка" };
                var endPointBox = new TextBox { Watermark = "Конечная точка" };
                var travelTimeBox = new TextBox { Watermark = "Время в пути" };

                var busComboBox = new ComboBox
                {
                    PlaceholderText = "Выберите автобус",
                    ItemsSource = Buses,
                    DisplayMemberBinding = new global::Avalonia.Data.Binding("Model")
                };

                var driverComboBox = new ComboBox
                {
                    PlaceholderText = "Выберите водителя",
                    ItemsSource = Drivers,
                    DisplayMemberBinding = new global::Avalonia.Data.Binding("Surname")
                };

                var addButton = new Button
                {
                    Content = "Добавить",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                grid.Children.Add(startPointBox);
                Grid.SetRow(startPointBox, 0);
                grid.Children.Add(endPointBox);
                Grid.SetRow(endPointBox, 1);
                grid.Children.Add(travelTimeBox);
                Grid.SetRow(travelTimeBox, 2);
                grid.Children.Add(busComboBox);
                Grid.SetRow(busComboBox, 3);
                grid.Children.Add(driverComboBox);
                Grid.SetRow(driverComboBox, 4);
                grid.Children.Add(addButton);
                Grid.SetRow(addButton, 5);

                dialog.Content = grid;

                addButton.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(startPointBox.Text) ||
                        string.IsNullOrWhiteSpace(endPointBox.Text) ||
                        string.IsNullOrWhiteSpace(travelTimeBox.Text) ||
                        busComboBox.SelectedItem == null ||
                        driverComboBox.SelectedItem == null)
                    {
                        var errorDialog = MessageBoxManager
                            .GetMessageBoxStandard(
                                "Ошибка",
                                "Все поля обязательны для заполнения",
                                ButtonEnum.Ok,
                                Icon.Error);

                        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
                            ? lifetime.MainWindow
                            : null;

                        if (mainWindow != null)
                        {
                            await errorDialog.ShowAsync();
                        }
                        return;
                    }

                    var selectedBus = busComboBox.SelectedItem as Avtobus;
                    var selectedDriver = driverComboBox.SelectedItem as Employee;

                    var newRoute = new
                    {
                        StartPoint = startPointBox.Text,
                        EndPoint = endPointBox.Text,
                        TravelTime = travelTimeBox.Text,
                        BusId = selectedBus!.BusId,
                        DriverId = selectedDriver!.EmpId
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(newRoute),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PostAsync($"{_baseUrl}/Routes", content);
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        var errorDialog = MessageBoxManager
                            .GetMessageBoxStandard(
                                "Ошибка",
                                $"Не удалось добавить маршрут: {error}",
                                ButtonEnum.Ok,
                                Icon.Error);

                        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app
                            ? app.MainWindow
                            : null;

                        if (mainWindow != null)
                        {
                            await errorDialog.ShowAsync();
                        }
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
                ErrorMessage = $"Error adding route: {ex.Message}";
                Log.Error(ex, "Error adding route");
            }
        }

        [RelayCommand]
        private async Task Edit()
        {
            if (SelectedRoute == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Редактировать маршрут",
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var startPointBox = new TextBox 
                { 
                    Text = SelectedRoute.StartPoint,
                    Watermark = "Начальная точка" 
                };
                var endPointBox = new TextBox 
                { 
                    Text = SelectedRoute.EndPoint,
                    Watermark = "Конечная точка" 
                };
                var travelTimeBox = new TextBox 
                { 
                    Text = SelectedRoute.TravelTime,
                    Watermark = "Время в пути" 
                };

                var busComboBox = new ComboBox
                {
                    PlaceholderText = "Выберите автобус",
                    ItemsSource = Buses,
                    DisplayMemberBinding = new global::Avalonia.Data.Binding("Model"),
                    SelectedItem = Buses.FirstOrDefault(b => b.BusId == SelectedRoute.BusId)
                };

                var driverComboBox = new ComboBox
                {
                    PlaceholderText = "Выберите водителя",
                    ItemsSource = Drivers,
                    DisplayMemberBinding = new global::Avalonia.Data.Binding("Surname"),
                    SelectedItem = Drivers.FirstOrDefault(d => d.EmpId == SelectedRoute.DriverId)
                };

                var updateButton = new Button
                {
                    Content = "Обновить",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                grid.Children.Add(startPointBox);
                Grid.SetRow(startPointBox, 0);
                grid.Children.Add(endPointBox);
                Grid.SetRow(endPointBox, 1);
                grid.Children.Add(travelTimeBox);
                Grid.SetRow(travelTimeBox, 2);
                grid.Children.Add(busComboBox);
                Grid.SetRow(busComboBox, 3);
                grid.Children.Add(driverComboBox);
                Grid.SetRow(driverComboBox, 4);
                grid.Children.Add(updateButton);
                Grid.SetRow(updateButton, 5);

                dialog.Content = grid;

                updateButton.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(startPointBox.Text) ||
                        string.IsNullOrWhiteSpace(endPointBox.Text) ||
                        string.IsNullOrWhiteSpace(travelTimeBox.Text) ||
                        busComboBox.SelectedItem == null ||
                        driverComboBox.SelectedItem == null)
                    {
                        var errorDialog = MessageBoxManager
                            .GetMessageBoxStandard(
                                "Ошибка",
                                "Все поля обязательны для заполнения",
                                ButtonEnum.Ok,
                                Icon.Error);

                        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
                            ? lifetime.MainWindow
                            : null;

                        if (mainWindow != null)
                        {
                            await errorDialog.ShowAsync();
                        }
                        return;
                    }

                    var selectedBus = busComboBox.SelectedItem as Avtobus;
                    var selectedDriver = driverComboBox.SelectedItem as Employee;

                    var updatedRoute = new
                    {
                        StartPoint = startPointBox.Text,
                        EndPoint = endPointBox.Text,
                        TravelTime = travelTimeBox.Text,
                        BusId = selectedBus!.BusId,
                        DriverId = selectedDriver!.EmpId
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(updatedRoute),
                        Encoding.UTF8,
                        "application/json");

                    var response = await _httpClient.PutAsync($"{_baseUrl}/Routes/{SelectedRoute.RouteId}", content);
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        var errorDialog = MessageBoxManager
                            .GetMessageBoxStandard(
                                "Ошибка",
                                $"Не удалось обновить маршрут: {error}",
                                ButtonEnum.Ok,
                                Icon.Error);

                        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app
                            ? app.MainWindow
                            : null;

                        if (mainWindow != null)
                        {
                            await errorDialog.ShowAsync();
                        }
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
                ErrorMessage = $"Error updating route: {ex.Message}";
                Log.Error(ex, "Error updating route");
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedRoute == null) return;

            try
            {
                var confirmDialog = MessageBoxManager
                    .GetMessageBoxStandard(
                        "Подтверждение",
                        $"Вы уверены, что хотите удалить маршрут {SelectedRoute.StartPoint} - {SelectedRoute.EndPoint}?",
                        ButtonEnum.YesNo,
                        Icon.Question);

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    var result = await confirmDialog.ShowAsync();
                    if (result == ButtonResult.Yes)
                    {
                        var response = await _httpClient.DeleteAsync($"{_baseUrl}/Routes/{SelectedRoute.RouteId}");
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadData();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            var errorDialog = MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Ошибка",
                                    $"Не удалось удалить маршрут: {error}",
                                    ButtonEnum.Ok,
                                    Icon.Error);

                            await errorDialog.ShowAsync();
                        }
                    }
                }
                else
                {
                    Log.Error("Could not find main window for dialog");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error deleting route: {ex.Message}";
                Log.Error(ex, "Error deleting route");
            }
        }

        private void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                LoadData().ConfigureAwait(false);
                return;
            }

            var filteredRoutes = Routes.Where(r => 
                r.StartPoint.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                r.EndPoint.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                r.Avtobus.Model.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                r.Employee.Surname.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                r.Employee.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            Routes = new ObservableCollection<Marshut>(filteredRoutes);
        }
    }
} 