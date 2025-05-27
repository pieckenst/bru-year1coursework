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
    public partial class BusManagementViewModel : ReactiveObject
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        private ObservableCollection<Avtobus> _buses = new();
        public ObservableCollection<Avtobus> Buses
        {
            get => _buses;
            set => this.RaiseAndSetIfChanged(ref _buses, value);
        }

        private Avtobus? _selectedBus;
        public Avtobus? SelectedBus
        {
            get => _selectedBus;
            set => this.RaiseAndSetIfChanged(ref _selectedBus, value);
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

        public BusManagementViewModel()
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
                // Create a new client with the updated token
                _httpClient.Dispose();
                _httpClient = ApiClientService.Instance.CreateClient();
                // Reload data with the new token
                LoadData().ConfigureAwait(false);
            };

            LoadData().ConfigureAwait(false);
        }

        private async Task LoadData()
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/Buses");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var loadedBuses = JsonSerializer.Deserialize<List<Avtobus>>(jsonString, _jsonOptions);
                    if (loadedBuses != null)
                    {
                        Buses = new ObservableCollection<Avtobus>(loadedBuses);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load buses. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, error);
                    throw new Exception($"Failed to load buses. Status: {response.StatusCode}, Error: {error}");
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

        [RelayCommand]
        private async Task Add()
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Добавить автобус",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var modelBox = new TextBox { Watermark = "Модель автобуса" };

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var addButton = new Button { Content = "Добавить", Margin = new Thickness(0, 0, 10, 0) };
                var cancelButton = new Button { Content = "Отмена" };

                Grid.SetRow(modelBox, 0);
                Grid.SetRow(buttonsPanel, 1);

                buttonsPanel.Children.Add(addButton);
                buttonsPanel.Children.Add(cancelButton);

                grid.Children.Add(modelBox);
                grid.Children.Add(buttonsPanel);

                dialog.Content = grid;

                cancelButton.Click += (s, e) => dialog.Close();
                addButton.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(modelBox.Text))
                    {
                        ErrorMessage = "Модель автобуса обязательна для заполнения";
                        return;
                    }

                    var newBus = new Avtobus
                    {
                        Model = modelBox.Text
                    };

                    try 
                    {
                        var json = JsonSerializer.Serialize(newBus, _jsonOptions);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PostAsync($"{_baseUrl}/Buses", content);
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadData();
                            dialog.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            ErrorMessage = $"Не удалось добавить автобус: {error}";
                            Log.Error("Failed to add bus. Status: {StatusCode}, Error: {Error}", 
                                response.StatusCode, error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Error adding bus: {ex.Message}";
                        Log.Error(ex, "Error adding bus");
                    }
                };

                // Получаем главное окно для диалога
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
                ErrorMessage = $"Ошибка при добавлении автобуса: {ex.Message}";
                Log.Error(ex, "Error adding bus");
            }
        }

        [RelayCommand]
        private async Task Edit()
        {
            if (SelectedBus == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Редактировать автобус",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var modelBox = new TextBox { Text = SelectedBus.Model, Watermark = "Модель автобуса" };

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var updateButton = new Button { Content = "Обновить", Margin = new Thickness(0, 0, 10, 0) };
                var cancelButton = new Button { Content = "Отмена" };

                Grid.SetRow(modelBox, 0);
                Grid.SetRow(buttonsPanel, 1);

                buttonsPanel.Children.Add(updateButton);
                buttonsPanel.Children.Add(cancelButton);

                grid.Children.Add(modelBox);
                grid.Children.Add(buttonsPanel);

                dialog.Content = grid;

                cancelButton.Click += (s, e) => dialog.Close();
                updateButton.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(modelBox.Text))
                    {
                        ErrorMessage = "Модель автобуса обязательна для заполнения";
                        return;
                    }

                    var updatedBus = new Avtobus
                    {
                        BusId = SelectedBus.BusId,
                        Model = modelBox.Text
                    };

                    try
                    {
                        var json = JsonSerializer.Serialize(updatedBus, _jsonOptions);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PutAsync($"{_baseUrl}/Buses/{SelectedBus.BusId}", content);
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadData();
                            dialog.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            ErrorMessage = $"Не удалось обновить автобус: {error}";
                            Log.Error("Failed to update bus. Status: {StatusCode}, Error: {Error}", 
                                response.StatusCode, error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Ошибка при обновлении автобуса: {ex.Message}";
                        Log.Error(ex, "Error updating bus");
                    }
                };

                // Получаем главное окно для диалога
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
                ErrorMessage = $"Ошибка при обновлении автобуса: {ex.Message}";
                Log.Error(ex, "Error updating bus");
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedBus == null) return;

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
                    Margin = new Thickness(10)
                };

                var messageText = new TextBlock
                {
                    Text = $"Вы уверены, что хотите удалить автобус {SelectedBus.Model}?",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
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
                        var response = await _httpClient.DeleteAsync($"{_baseUrl}/Buses/{SelectedBus.BusId}");
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadData();
                            dialog.Close();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            ErrorMessage = $"Не удалось удалить автобус: {error}";
                            Log.Error("Failed to delete bus. Status: {StatusCode}, Error: {Error}", 
                                response.StatusCode, error);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = $"Ошибка при удалении автобуса: {ex.Message}";
                        Log.Error(ex, "Error deleting bus");
                    }
                };

                // Получаем главное окно для диалога
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
                ErrorMessage = $"Ошибка при удалении автобуса: {ex.Message}";
                Log.Error(ex, "Error deleting bus");
            }
        }

        private void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                LoadData().ConfigureAwait(false);
                return;
            }

            var filteredBuses = Buses.Where(b => 
                b.Model.Contains(value, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            Buses = new ObservableCollection<Avtobus>(filteredBuses);
        }
    }
} 