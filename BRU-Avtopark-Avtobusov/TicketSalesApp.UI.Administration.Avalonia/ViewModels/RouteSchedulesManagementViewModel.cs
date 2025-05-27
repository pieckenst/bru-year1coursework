using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using ReactiveUI;
using TicketSalesApp.Core.Models;
using System.Linq;
using Serilog;
using TicketSalesApp.UI.Administration.Avalonia.Services;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using System.Text;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace TicketSalesApp.UI.Administration.Avalonia.ViewModels
{
    public partial class RouteSchedulesManagementViewModel : ReactiveObject
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

        private Marshut? _selectedRoute;
        public Marshut? SelectedRoute
        {
            get => _selectedRoute;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedRoute, value);
                if (value != null)
                    LoadSchedules().ConfigureAwait(false);
            }
        }

        private DateTimeOffset _selectedDate = DateTimeOffset.Now;
        public DateTimeOffset SelectedDate
        {
            get => _selectedDate;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedDate, value);
                if (SelectedRoute != null)
                    LoadSchedules().ConfigureAwait(false);
            }
        }

        private ObservableCollection<RouteSchedules> _schedules = new();
        public ObservableCollection<RouteSchedules> Schedules
        {
            get => _schedules;
            set => this.RaiseAndSetIfChanged(ref _schedules, value);
        }

        private RouteSchedules? _selectedSchedule;
        public RouteSchedules? SelectedSchedule
        {
            get => _selectedSchedule;
            set => this.RaiseAndSetIfChanged(ref _selectedSchedule, value);
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

        public RouteSchedulesManagementViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            _baseUrl = "http://localhost:5000/api";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };

            // Subscribe to auth token changes
            ApiClientService.Instance.OnAuthTokenChanged += (sender, token) =>
            {
                var oldClient = _httpClient;
                _httpClient = ApiClientService.Instance.CreateClient();
                oldClient.Dispose();
                LoadData().ConfigureAwait(false);
            };

            LoadData().ConfigureAwait(false);
        }

        private async Task LoadData()
        {
            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var response = await _httpClient.GetAsync($"{_baseUrl}/Routes");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var routes = JsonSerializer.Deserialize<List<Marshut>>(jsonString, _jsonOptions);
                    if (routes != null)
                    {
                        Routes = new ObservableCollection<Marshut>(routes);
                        await LoadSchedules();
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to load routes: {error}");
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

        private async Task LoadSchedules()
        {
            if (SelectedRoute == null) return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var response = await _httpClient.GetAsync(
                    $"{_baseUrl}/RouteSchedules/search?routeId={SelectedRoute.RouteId}&date={SelectedDate.Date:yyyy-MM-dd}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var schedules = JsonSerializer.Deserialize<List<RouteSchedules>>(jsonString, _jsonOptions);
                    if (schedules != null)
                    {
                        Schedules = new ObservableCollection<RouteSchedules>(
                            schedules.OrderBy(s => s.DepartureTime));
                        
                        Log.Information("Successfully loaded {Count} schedules for route {RouteId} with stops", 
                            schedules.Count, SelectedRoute.RouteId);
                        
                        foreach (var schedule in schedules)
                        {
                            Log.Debug("Schedule {Id} has {StopCount} stops: {Stops}", 
                                schedule.RouteScheduleId, 
                                schedule.RouteStops?.Length ?? 0,
                                string.Join(", ", schedule.RouteStops ?? Array.Empty<string>()));
                        }
                    }
                    else
                    {
                        Schedules.Clear();
                        Log.Warning("No schedules found for route {RouteId}", SelectedRoute.RouteId);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to load schedules: {error}");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error loading schedules: {ex.Message}";
                Log.Error(ex, "Error loading schedules");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Add()
        {
            if (SelectedRoute == null)
            {
                ErrorMessage = "Please select a route first";
                HasError = true;
                return;
            }

            try
            {
                var dialog = new Window
                {
                    Title = "Добавить расписание",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    Margin = new Thickness(15)
                };

                // Left column controls
                var departureTimePicker = new TimePicker { Margin = new Thickness(0, 0, 10, 5) };
                var arrivalTimePicker = new TimePicker { Margin = new Thickness(0, 0, 10, 5) };
                var priceBox = new NumericUpDown { Minimum = 0, Value = 0.75M, Increment = 0.25M, Margin = new Thickness(0, 0, 10, 5) };
                var seatsBox = new NumericUpDown { Minimum = 0, Value = 42, Increment = 1, Margin = new Thickness(0, 0, 10, 5) };
                var isActiveCheckBox = new CheckBox { Content = "Активно", IsChecked = true, Margin = new Thickness(0, 0, 10, 5) };
                var isRecurringCheckBox = new CheckBox { Content = "Повторяющееся", IsChecked = true, Margin = new Thickness(0, 0, 10, 5) };

                // Right column - Route Stops
                var routeStopsPanel = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,*"),
                    Margin = new Thickness(10, 0, 0, 0)
                };

                var routeStopsLabel = new TextBlock
                {
                    Text = "Остановки маршрута",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                // Get route configuration from DbInitializer
                var routeConfig = GetRouteConfiguration(SelectedRoute);
                var routeStops = routeConfig?.stops ?? 
                    SelectedRoute.StartPoint.Split(',')
                        .Concat(SelectedRoute.EndPoint.Split(','))
                        .Distinct()
                        .ToArray();

                var routeStopsListBox = new ListBox
                {
                    ItemsSource = routeStops,
                    SelectionMode = SelectionMode.Multiple,
                    SelectedItems = new ObservableCollection<string>(routeStops), // Pre-select all stops
                    Margin = new Thickness(0, 0, 0, 10)
                };

                Grid.SetRow(routeStopsLabel, 0);
                Grid.SetRow(routeStopsListBox, 1);
                routeStopsPanel.Children.Add(routeStopsLabel);
                routeStopsPanel.Children.Add(routeStopsListBox);

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var addButton = new Button { Content = "Добавить", Margin = new Thickness(0, 0, 10, 0) };
                var cancelButton = new Button { Content = "Отмена" };

                // Add left column controls
                var departureLabel = new TextBlock { Text = "Время отправления:", Margin = new Thickness(0, 0, 0, 5) };
                Grid.SetRow(departureLabel, 0);
                Grid.SetColumn(departureLabel, 0);
                grid.Children.Add(departureLabel);

                Grid.SetRow(departureTimePicker, 1);
                Grid.SetColumn(departureTimePicker, 0);
                grid.Children.Add(departureTimePicker);

                var arrivalLabel = new TextBlock { Text = "Время прибытия:", Margin = new Thickness(0, 10, 0, 5) };
                Grid.SetRow(arrivalLabel, 2);
                Grid.SetColumn(arrivalLabel, 0);
                grid.Children.Add(arrivalLabel);

                Grid.SetRow(arrivalTimePicker, 3);
                Grid.SetColumn(arrivalTimePicker, 0);
                grid.Children.Add(arrivalTimePicker);

                var priceLabel = new TextBlock { Text = "Цена:", Margin = new Thickness(0, 10, 0, 5) };
                Grid.SetRow(priceLabel, 4);
                Grid.SetColumn(priceLabel, 0);
                grid.Children.Add(priceLabel);

                Grid.SetRow(priceBox, 5);
                Grid.SetColumn(priceBox, 0);
                grid.Children.Add(priceBox);

                var seatsLabel = new TextBlock { Text = "Количество мест:", Margin = new Thickness(0, 10, 0, 5) };
                Grid.SetRow(seatsLabel, 6);
                Grid.SetColumn(seatsLabel, 0);
                grid.Children.Add(seatsLabel);

                Grid.SetRow(seatsBox, 7);
                Grid.SetColumn(seatsBox, 0);
                grid.Children.Add(seatsBox);
                Grid.SetRow(grid.Children[grid.Children.Count - 1], 7);

                Grid.SetRow(isActiveCheckBox, 8);
                Grid.SetColumn(isActiveCheckBox, 0);
                grid.Children.Add(isActiveCheckBox);

                Grid.SetRow(isRecurringCheckBox, 9);
                Grid.SetColumn(isRecurringCheckBox, 0);
                grid.Children.Add(isRecurringCheckBox);

                // Add right column - Route Stops
                Grid.SetColumn(routeStopsPanel, 1);
                Grid.SetRowSpan(routeStopsPanel, 8);
                grid.Children.Add(routeStopsPanel);

                // Add buttons
                Grid.SetRow(buttonsPanel, 9);
                Grid.SetColumnSpan(buttonsPanel, 2);
                buttonsPanel.Children.Add(addButton);
                buttonsPanel.Children.Add(cancelButton);
                grid.Children.Add(buttonsPanel);

                dialog.Content = grid;

                cancelButton.Click += (s, e) => dialog.Close();
                addButton.Click += async (s, e) =>
                {
                    var selectedStops = routeStopsListBox.SelectedItems?.Cast<string>().ToArray() ?? Array.Empty<string>();
                    if (selectedStops.Length < 2)
                    {
                        ErrorMessage = "Выберите как минимум две остановки";
                        HasError = true;
                        return;
                    }

                    // Calculate estimated times and distances
                    var estimatedTimes = new string[selectedStops.Length];
                    var stopDistances = new double[selectedStops.Length];
                    var departureTime = SelectedDate.Date.Add(departureTimePicker.SelectedTime ?? TimeSpan.Zero);
                    var arrivalTime = SelectedDate.Date.Add(arrivalTimePicker.SelectedTime ?? TimeSpan.Zero);
                    var totalMinutes = (arrivalTime - departureTime).TotalMinutes;
                    var minutesPerStop = totalMinutes / (selectedStops.Length - 1);

                    for (int i = 0; i < selectedStops.Length; i++)
                    {
                        estimatedTimes[i] = departureTime.AddMinutes(i * minutesPerStop).ToString("HH:mm");
                        stopDistances[i] = Math.Round(i * (6.0 / (selectedStops.Length - 1)), 2); // Assuming average route length of 6km
                    }

                    var schedule = new RouteSchedules
                    {
                        RouteId = SelectedRoute.RouteId,
                        StartPoint = selectedStops.First(),
                        EndPoint = selectedStops.Last(),
                        RouteStops = selectedStops.Length > 0 ? selectedStops : new[] { "Могилев", "Минск" },
                        DepartureTime = departureTime,
                        ArrivalTime = arrivalTime,
                        Price = (double)priceBox.Value,
                        AvailableSeats = (int)seatsBox.Value,
                        IsActive = isActiveCheckBox.IsChecked ?? true,
                        IsRecurring = isRecurringCheckBox.IsChecked ?? true,
                        DaysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" },
                        BusTypes = new[] { "МАЗ-103", "МАЗ-107" },
                        ValidFrom = SelectedDate.Date,
                        ValidUntil = SelectedDate.Date.AddMonths(3),
                        StopDurationMinutes = 5,
                        EstimatedStopTimes = estimatedTimes.Length > 0 ? estimatedTimes : new[] { "08:00", "12:00" },
                        StopDistances = stopDistances.Length > 0 ? stopDistances : new[] { 0.0, 6.0 },
                        Notes = $"Маршрут {selectedStops.First()} - {selectedStops.Last()}",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        UpdatedBy = "Admin"
                    };

                    try
                    {
                        var json = JsonSerializer.Serialize(schedule, _jsonOptions);
                        Log.Information("Sending route schedule data to API: {Json}", json);
                        
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PostAsync($"{_baseUrl}/RouteSchedules", content);
                        if (response.IsSuccessStatusCode)
                        {
                            Log.Information("Successfully created route schedule with {Stops} stops from {Start} to {End}", 
                                selectedStops.Length, schedule.StartPoint, schedule.EndPoint);
                            await LoadSchedules();
                            dialog.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            ErrorMessage = $"Failed to add schedule: {error}";
                            HasError = true;
                            Log.Error("Failed to add schedule: {Error}", error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Error adding schedule: {ex.Message}";
                        HasError = true;
                        Log.Error(ex, "Error adding schedule");
                    }
                };

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error showing add dialog: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error showing add dialog");
            }
        }

        private (string start, string end, string[] stops)? GetRouteConfiguration(Marshut route)
        {
            // Define route configurations based on DbInitializer
            var routeConfigs = new Dictionary<(string start, string end), string[]>
            {
                {("Вейнянка", "Фатина"), new[] {"Вейнянка", "Площадь Орджоникидзе", "Областная больница", "Фатина"}},
                {("Малая Боровка", "Солтановка"), new[] {"Малая Боровка", "Машековка", "Центр", "Солтановка"}},
                {("Железнодорожный вокзал", "Спутник"), new[] {"Вокзал", "Площадь Ленина", "Универмаг", "Спутник"}},
                {("Мясокомбинат", "Заводская"), new[] {"Мясокомбинат", "Димитрова", "Юбилейный", "Заводская"}},
                {("Броды", "Казимировка"), new[] {"Броды", "Центр", "Площадь Славы", "Казимировка"}},
                {("Гребеневский рынок", "Холмы"), new[] {"Гребеневский рынок", "Площадь Орджоникидзе", "Мир", "Холмы"}},
                {("Автовокзал", "Полыковичи"), new[] {"Автовокзал", "Площадь Ленина", "Димитрова", "Полыковичи"}},
                {("Центр", "Сидоровичи"), new[] {"Центр", "Площадь Славы", "Заднепровье", "Сидоровичи"}},
                {("Площадь Славы", "Буйничи"), new[] {"Площадь Славы", "Областная больница", "Зоосад", "Буйничи"}},
                {("Заднепровье", "Химволокно"), new[] {"Заднепровье", "Центр", "Юбилейный", "Химволокно"}},
                {("Вокзал", "Соломинка"), new[] {"Вокзал", "Центр", "Димитрова", "Соломинка"}},
                {("Площадь Ленина", "Чаусы"), new[] {"Площадь Ленина", "Центр", "Заднепровье", "Чаусы"}},
                {("Могилев-2", "Дашковка"), new[] {"Могилев-2", "Центр", "Юбилейный", "Дашковка"}},
                {("Кожзавод", "Сухари"), new[] {"Кожзавод", "Центр", "Площадь Славы", "Сухари"}},
                {("Гребеневский рынок", "Любуж"), new[] {"Гребеневский рынок", "Центр", "Заднепровье", "Любуж"}}
            };

            var key = routeConfigs.Keys.FirstOrDefault(k => 
                k.start == route.StartPoint && k.end == route.EndPoint);

            if (key != default)
            {
                return (key.start, key.end, routeConfigs[key]);
            }

            return null;
        }

        [RelayCommand]
        private async Task Edit(RouteSchedules? schedule)
        {
            if (schedule == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Редактировать расписание",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto"),
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    Margin = new Thickness(15)
                };

                // Left column controls
                var departureTimePicker = new TimePicker { Margin = new Thickness(0, 0, 10, 5) };
                var arrivalTimePicker = new TimePicker { Margin = new Thickness(0, 0, 10, 5) };
                var priceBox = new NumericUpDown { Minimum = 0, Value = 0.75M, Increment = 0.25M, Margin = new Thickness(0, 0, 10, 5) };
                var seatsBox = new NumericUpDown { Minimum = 0, Value = 42, Increment = 1, Margin = new Thickness(0, 0, 10, 5) };
                var isActiveCheckBox = new CheckBox { Content = "Активно", IsChecked = true, Margin = new Thickness(0, 0, 10, 5) };
                var isRecurringCheckBox = new CheckBox { Content = "Повторяющееся", IsChecked = true, Margin = new Thickness(0, 0, 10, 5) };

                // Right column - Route Stops
                var routeStopsPanel = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,*"),
                    Margin = new Thickness(10, 0, 0, 0)
                };

                var routeStopsLabel = new TextBlock
                {
                    Text = "Остановки маршрута",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                // Get route configuration from DbInitializer
                var routeConfig = GetRouteConfiguration(SelectedRoute);
                var routeStops = routeConfig?.stops ?? 
                    SelectedRoute.StartPoint.Split(',')
                        .Concat(SelectedRoute.EndPoint.Split(','))
                        .Distinct()
                        .ToArray();

                var routeStopsListBox = new ListBox
                {
                    ItemsSource = routeStops,
                    SelectionMode = SelectionMode.Multiple,
                    SelectedItems = new ObservableCollection<string>(routeStops), // Pre-select all stops
                    Margin = new Thickness(0, 0, 0, 10)
                };

                Grid.SetRow(routeStopsLabel, 0);
                Grid.SetRow(routeStopsListBox, 1);
                routeStopsPanel.Children.Add(routeStopsLabel);
                routeStopsPanel.Children.Add(routeStopsListBox);

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var updateButton = new Button { Content = "Обновить", Margin = new Thickness(0, 0, 10, 0) };
                var cancelButton = new Button { Content = "Отмена" };

                // Add left column controls
                var departureLabel = new TextBlock { Text = "Время отправления:", Margin = new Thickness(0, 0, 0, 5) };
                Grid.SetRow(departureLabel, 0);
                Grid.SetColumn(departureLabel, 0);
                grid.Children.Add(departureLabel);

                Grid.SetRow(departureTimePicker, 1);
                Grid.SetColumn(departureTimePicker, 0);
                grid.Children.Add(departureTimePicker);

                var arrivalLabel = new TextBlock { Text = "Время прибытия:", Margin = new Thickness(0, 10, 0, 5) };
                Grid.SetRow(arrivalLabel, 2);
                Grid.SetColumn(arrivalLabel, 0);
                grid.Children.Add(arrivalLabel);

                Grid.SetRow(arrivalTimePicker, 3);
                Grid.SetColumn(arrivalTimePicker, 0);
                grid.Children.Add(arrivalTimePicker);

                var priceLabel = new TextBlock { Text = "Цена:", Margin = new Thickness(0, 10, 0, 5) };
                Grid.SetRow(priceLabel, 4);
                Grid.SetColumn(priceLabel, 0);
                grid.Children.Add(priceLabel);

                Grid.SetRow(priceBox, 5);
                Grid.SetColumn(priceBox, 0);
                grid.Children.Add(priceBox);

                var seatsLabel = new TextBlock { Text = "Количество мест:", Margin = new Thickness(0, 10, 0, 5) };
                Grid.SetRow(seatsLabel, 6);
                Grid.SetColumn(seatsLabel, 0);
                grid.Children.Add(seatsLabel);

                Grid.SetRow(seatsBox, 7);
                Grid.SetColumn(seatsBox, 0);
                grid.Children.Add(seatsBox);
                Grid.SetRow(grid.Children[grid.Children.Count - 1], 7);

                Grid.SetRow(isActiveCheckBox, 8);
                Grid.SetColumn(isActiveCheckBox, 0);
                grid.Children.Add(isActiveCheckBox);

                Grid.SetRow(isRecurringCheckBox, 9);
                Grid.SetColumn(isRecurringCheckBox, 0);
                grid.Children.Add(isRecurringCheckBox);

                // Add right column - Route Stops
                Grid.SetColumn(routeStopsPanel, 1);
                Grid.SetRowSpan(routeStopsPanel, 8);
                grid.Children.Add(routeStopsPanel);

                // Add buttons
                Grid.SetRow(buttonsPanel, 9);
                Grid.SetColumnSpan(buttonsPanel, 2);
                buttonsPanel.Children.Add(updateButton);
                buttonsPanel.Children.Add(cancelButton);
                grid.Children.Add(buttonsPanel);

                dialog.Content = grid;

                cancelButton.Click += (s, e) => dialog.Close();
                updateButton.Click += async (s, e) =>
                {
                    var selectedStops = routeStopsListBox.SelectedItems?.Cast<string>().ToArray() ?? Array.Empty<string>();
                    if (selectedStops.Length < 2)
                    {
                        ErrorMessage = "Выберите как минимум две остановки";
                        HasError = true;
                        return;
                    }

                    var updatedSchedule = new RouteSchedules
                    {
                        RouteScheduleId = schedule.RouteScheduleId,
                        RouteId = schedule.RouteId,
                        StartPoint = schedule.StartPoint,
                        EndPoint = schedule.EndPoint,
                        RouteStops = selectedStops,
                        DepartureTime = schedule.DepartureTime.Date.Add(departureTimePicker.SelectedTime ?? TimeSpan.Zero),
                        ArrivalTime = schedule.ArrivalTime.Date.Add(arrivalTimePicker.SelectedTime ?? TimeSpan.Zero),
                        Price = (double)priceBox.Value,
                        AvailableSeats = (int)seatsBox.Value,
                        IsActive = isActiveCheckBox.IsChecked ?? true,
                        IsRecurring = isRecurringCheckBox.IsChecked ?? true,
                        DaysOfWeek = schedule.DaysOfWeek,
                        BusTypes = schedule.BusTypes,
                        ValidFrom = schedule.ValidFrom,
                        ValidUntil = schedule.ValidUntil,
                        UpdatedAt = DateTime.Now,
                        UpdatedBy = "Admin"
                    };

                    try
                    {
                        var json = JsonSerializer.Serialize(updatedSchedule, _jsonOptions);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PutAsync(
                            $"{_baseUrl}/RouteSchedules/{schedule.RouteScheduleId}", content);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadSchedules();
                            dialog.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            ErrorMessage = $"Failed to update schedule: {error}";
                            HasError = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Error updating schedule: {ex.Message}";
                        HasError = true;
                        Log.Error(ex, "Error updating schedule");
                    }
                };

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error showing edit dialog: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error showing edit dialog");
            }
        }

        [RelayCommand]
        private async Task Delete(RouteSchedules? schedule)
        {
            if (schedule == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Подтверждение удаления",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto"),
                    Margin = new Thickness(20)
                };

                var messageText = new TextBlock
                {
                    Text = $"Вы уверены, что хотите удалить расписание маршрута {schedule.StartPoint} - {schedule.EndPoint}?",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var deleteButton = new Button 
                { 
                    Content = "Удалить",
                    Background = new SolidColorBrush(Colors.Red),
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 0, 10, 0)
                };
                var cancelButton = new Button { Content = "Отмена" };

                Grid.SetRow(messageText, 0);
                Grid.SetRow(buttonsPanel, 1);

                buttonsPanel.Children.Add(deleteButton);
                buttonsPanel.Children.Add(cancelButton);

                grid.Children.Add(messageText);
                grid.Children.Add(buttonsPanel);

                dialog.Content = grid;

                cancelButton.Click += (s, e) => dialog.Close();
                deleteButton.Click += async (s, e) =>
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"{_baseUrl}/RouteSchedules/{schedule.RouteScheduleId}");
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadSchedules();
                            dialog.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            ErrorMessage = $"Failed to delete schedule: {error}";
                            HasError = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Error deleting schedule: {ex.Message}";
                        HasError = true;
                        Log.Error(ex, "Error deleting schedule");
                    }
                };

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error showing delete dialog: {ex.Message}";
                HasError = true;
                Log.Error(ex, "Error showing delete dialog");
            }
        }

        [RelayCommand]
        private Task Refresh()
        {
            return LoadSchedules();
        }
    }
} 