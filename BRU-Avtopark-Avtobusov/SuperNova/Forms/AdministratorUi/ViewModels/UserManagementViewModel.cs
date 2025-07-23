using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Input;
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
using Microsoft.Extensions.Logging;
using Serilog;
using SuperNova.Forms.Services;
using System.Collections.Generic;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;

namespace SuperNova.Forms.AdministratorUi.ViewModels
{
    public partial class UserManagementViewModel : ReactiveObject
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        private ObservableCollection<User> _users = new();
        public ObservableCollection<User> Users
        {
            get => _users;
            set => this.RaiseAndSetIfChanged(ref _users, value);
        }

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set => this.RaiseAndSetIfChanged(ref _selectedUser, value);
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

        public UserManagementViewModel()
        {
            _httpClient = ApiClientService.Instance.CreateClient();
            _baseUrl = "http://localhost:5000/api";
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                MaxDepth = 64,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
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

        [RelayCommand]
        private async Task LoadData()
        {
            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var response = await _httpClient.GetAsync($"{_baseUrl}/Users");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                    };
                    var loadedUsers = JsonSerializer.Deserialize<List<User>>(jsonString, options);
                    if (loadedUsers != null)
                    {
                        Users = new ObservableCollection<User>(loadedUsers);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load users. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, error);
                    throw new Exception($"Failed to load users. Status: {response.StatusCode}, Error: {error}");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error loading users: {ex.Message}";
                Log.Error(ex, "Error loading users");
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
                    Title = "Add User",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var loginBox = new TextBox { Watermark = "Login" };
                var passwordBox = new TextBox { Watermark = "Password", PasswordChar = '*' };
                var roleComboBox = new ComboBox();
                var roles = new[] { "User", "Admin" };
                foreach (var role in roles)
                {
                    roleComboBox.Items.Add(role);
                }
                roleComboBox.SelectedIndex = 0;

                var addButton = new Button
                {
                    Content = "Add",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                grid.Children.Add(loginBox);
                Grid.SetRow(loginBox, 0);
                grid.Children.Add(passwordBox);
                Grid.SetRow(passwordBox, 1);
                grid.Children.Add(roleComboBox);
                Grid.SetRow(roleComboBox, 2);
                grid.Children.Add(addButton);
                Grid.SetRow(addButton, 3);

                dialog.Content = grid;

                addButton.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(loginBox.Text) || string.IsNullOrWhiteSpace(passwordBox.Text))
                    {
                        ErrorMessage = "Login and password are required";
                        return;
                    }

                    var newUser = new
                    {
                        Login = loginBox.Text,
                        Password = passwordBox.Text,
                        Role = roleComboBox.SelectedIndex
                    };

                    var json = JsonSerializer.Serialize(newUser);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"{_baseUrl}/Users", content);
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        ErrorMessage = $"Failed to add user: {error}";
                    }
                };

                // Get the main window as owner
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
                ErrorMessage = $"Error adding user: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error adding user: {ex}");
            }
        }

        [RelayCommand]
        private async Task Edit()
        {
            if (SelectedUser == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Редактировать пользователя",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
                    Margin = new Thickness(10)
                };

                var loginBox = new TextBox 
                { 
                    Text = SelectedUser.Login,
                    Watermark = "Логин" 
                };
                var passwordBox = new TextBox 
                { 
                    Watermark = "Новый пароль (оставьте пустым, чтобы не менять)",
                    PasswordChar = '*' 
                };
                var roleComboBox = new ComboBox();
                var roles = new[] { "Пользователь", "Администратор" };
                foreach (var role in roles)
                {
                    roleComboBox.Items.Add(role);
                }
                roleComboBox.SelectedIndex = SelectedUser.Role;

                var updateButton = new Button
                {
                    Content = "Обновить",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                grid.Children.Add(loginBox);
                Grid.SetRow(loginBox, 0);
                grid.Children.Add(passwordBox);
                Grid.SetRow(passwordBox, 1);
                grid.Children.Add(roleComboBox);
                Grid.SetRow(roleComboBox, 2);
                grid.Children.Add(updateButton);
                Grid.SetRow(updateButton, 3);

                dialog.Content = grid;

                updateButton.Click += async (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(loginBox.Text))
                    {
                        var errorDialog = MessageBoxManager
                            .GetMessageBoxStandard(
                                "Ошибка",
                                "Логин обязателен",
                                ButtonEnum.Ok,
                                Icon.Error);

                        // Get the main window for error dialog
                        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
                            ? lifetime.MainWindow
                            : null;

                        if (mainWindow != null)
                        {
                            await errorDialog.ShowAsync();
                        }
                        return;
                    }

                    var updateUser = new
                    {
                        Login = loginBox.Text,
                        Password = string.IsNullOrWhiteSpace(passwordBox.Text) ? null : passwordBox.Text,
                        Role = roleComboBox.SelectedIndex
                    };

                    var json = JsonSerializer.Serialize(updateUser);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PutAsync($"{_baseUrl}/Users/{SelectedUser.UserId}", content);
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            var errorDialog = MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Ошибка",
                                    error,
                                    ButtonEnum.Ok,
                                    Icon.Error);

                            // Get the main window for error dialog
                            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app
                                ? app.MainWindow
                                : null;

                            if (mainWindow != null)
                            {
                                await errorDialog.ShowAsync();
                            }
                        }
                        else
                        {
                            ErrorMessage = $"Failed to update user: {error}";
                        }
                    }
                };

                // Get the main window as owner for edit dialog
                var ownerWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (ownerWindow != null)
                {
                    await dialog.ShowDialog(ownerWindow);
                }
                else
                {
                    Log.Error("Could not find main window for dialog");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error updating user: {ex.Message}";
                Log.Error(ex, "Error updating user");
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedUser == null) return;

            try
            {
                // Get current user ID from stored token
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(ApiClientService.Instance.AuthToken);
                var currentUserId = long.Parse(token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "0");

                // Prevent deleting yourself
                if (SelectedUser.UserId == currentUserId)
                {
                    var errorDialog = MessageBoxManager
                        .GetMessageBoxStandard(
                            "Ошибка",
                            "Вы не можете удалить свою собственную учетную запись.",
                            ButtonEnum.Ok,
                            Icon.Error);

                    // Get the main window as owner
                    var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                        ? desktop.MainWindow
                        : null;

                    if (mainWindow != null)
                    {
                        await errorDialog.ShowAsync();
                    }
                    return;
                }

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
                    Text = $"Вы уверены, что хотите удалить пользователя {SelectedUser.Login}?",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 20)
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10
                };

                var yesButton = new Button { Content = "Да" };
                var noButton = new Button { Content = "Нет" };

                buttonPanel.Children.Add(yesButton);
                buttonPanel.Children.Add(noButton);

                grid.Children.Add(messageText);
                Grid.SetRow(messageText, 0);
                grid.Children.Add(buttonPanel);
                Grid.SetRow(buttonPanel, 1);

                dialog.Content = grid;

                yesButton.Click += async (s, e) =>
                {
                    var response = await _httpClient.DeleteAsync($"{_baseUrl}/Users/{SelectedUser.UserId}");
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            var errorMessageBox = MessageBoxManager
                                .GetMessageBoxStandard(
                                    "Ошибка",
                                    error,
                                    ButtonEnum.Ok,
                                    Icon.Error);

                            // Get the main window as owner for error dialog
                            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
                                ? lifetime.MainWindow
                                : null;

                            if (mainWindow != null)
                            {
                                await errorMessageBox.ShowAsync();
                            }
                        }
                        else
                        {
                            ErrorMessage = $"Failed to delete user: {error}";
                        }
                    }
                };

                noButton.Click += (s, e) => dialog.Close();

                // Get the main window as owner for confirmation dialog
                var ownerWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime app
                    ? app.MainWindow
                    : null;

                if (ownerWindow != null)
                {
                    await dialog.ShowDialog(ownerWindow);
                }
                else
                {
                    Log.Error("Could not find main window for dialog");
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Error deleting user: {ex.Message}";
                Log.Error(ex, "Error deleting user");
            }
        }

        private void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                LoadData().ConfigureAwait(false);
                return;
            }

            var filteredUsers = Users.Where(u => 
                u.Login.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                u.UserId.ToString().Contains(value)
            ).ToList();

            Users = new ObservableCollection<User>(filteredUsers);
        }
    }
}