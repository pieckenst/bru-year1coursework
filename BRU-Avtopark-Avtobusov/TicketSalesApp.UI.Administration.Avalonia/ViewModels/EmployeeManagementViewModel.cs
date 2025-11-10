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
using TicketSalesApp.UI.Administration.Avalonia.Views.Dialogs;

namespace TicketSalesApp.UI.Administration.Avalonia.ViewModels
{
    public partial class EmployeeManagementViewModel : ReactiveObject
    {
        private HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        private ObservableCollection<Employee> _employees = new();
        public ObservableCollection<Employee> Employees
        {
            get => _employees;
            set => this.RaiseAndSetIfChanged(ref _employees, value);
        }

        private ObservableCollection<Job> _jobs = new();
        public ObservableCollection<Job> Jobs
        {
            get => _jobs;
            set => this.RaiseAndSetIfChanged(ref _jobs, value);
        }

        private Employee? _selectedEmployee;
        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set => this.RaiseAndSetIfChanged(ref _selectedEmployee, value);
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

        // HR Data Collections
        private ObservableCollection<Department> _departments = new();
        public ObservableCollection<Department> Departments
        {
            get => _departments;
            set => this.RaiseAndSetIfChanged(ref _departments, value);
        }

        private ObservableCollection<EmployeeDocument> _selectedEmployeeDocuments = new();
        public ObservableCollection<EmployeeDocument> SelectedEmployeeDocuments
        {
            get => _selectedEmployeeDocuments;
            set => this.RaiseAndSetIfChanged(ref _selectedEmployeeDocuments, value);
        }

        private ObservableCollection<EmployeeTraining> _selectedEmployeeTrainings = new();
        public ObservableCollection<EmployeeTraining> SelectedEmployeeTrainings
        {
            get => _selectedEmployeeTrainings;
            set => this.RaiseAndSetIfChanged(ref _selectedEmployeeTrainings, value);
        }

        private ObservableCollection<EmergencyContact> _selectedEmployeeContacts = new();
        public ObservableCollection<EmergencyContact> SelectedEmployeeContacts
        {
            get => _selectedEmployeeContacts;
            set => this.RaiseAndSetIfChanged(ref _selectedEmployeeContacts, value);
        }

        private ObservableCollection<VacationRequest> _selectedEmployeeVacations = new();
        public ObservableCollection<VacationRequest> SelectedEmployeeVacations
        {
            get => _selectedEmployeeVacations;
            set => this.RaiseAndSetIfChanged(ref _selectedEmployeeVacations, value);
        }

        // View state
        private string _currentView = "list";
        public string CurrentView
        {
            get => _currentView;
            set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        private string _detailSection = "info";
        public string DetailSection
        {
            get => _detailSection;
            set => this.RaiseAndSetIfChanged(ref _detailSection, value);
        }

        public EmployeeManagementViewModel()
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

            // Subscribe to selection changes
            this.WhenAnyValue(x => x.SelectedEmployee)
                .Subscribe(async employee =>
                {
                    if (employee != null)
                    {
                        await LoadEmployeeDetails(employee.EmpId);
                    }
                });

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

                // Load jobs first
                var jobsResponse = await _httpClient.GetAsync($"{_baseUrl}/Jobs");
                if (jobsResponse.IsSuccessStatusCode)
                {
                    var jsonString = await jobsResponse.Content.ReadAsStringAsync();
                    var loadedJobs = JsonSerializer.Deserialize<List<Job>>(jsonString, _jsonOptions);
                    if (loadedJobs != null)
                    {
                        Jobs = new ObservableCollection<Job>(loadedJobs);
                    }
                }

                // Load departments
                var deptResponse = await _httpClient.GetAsync($"{_baseUrl}/Departments");
                if (deptResponse.IsSuccessStatusCode)
                {
                    var jsonString = await deptResponse.Content.ReadAsStringAsync();
                    var loadedDepts = JsonSerializer.Deserialize<List<Department>>(jsonString, _jsonOptions);
                    if (loadedDepts != null)
                    {
                        Departments = new ObservableCollection<Department>(loadedDepts);
                    }
                }

                // Then load employees
                var response = await _httpClient.GetAsync($"{_baseUrl}/Employees");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var loadedEmployees = JsonSerializer.Deserialize<List<Employee>>(jsonString, _jsonOptions);
                    if (loadedEmployees != null)
                    {
                        Employees = new ObservableCollection<Employee>(loadedEmployees);
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Log.Error("Failed to load employees. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, error);
                    throw new Exception($"Failed to load employees. Status: {response.StatusCode}, Error: {error}");
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
                var dialog = new EmployeeEditDialog(null, Jobs, Departments);

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);

                    if (dialog.IsSaved && dialog.Employee != null)
                    {
                        var json = JsonSerializer.Serialize(dialog.Employee, _jsonOptions);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PostAsync($"{_baseUrl}/Employees", content);
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadData();
                        }
                        else
                        {
                            ErrorMessage = $"Failed to add employee. Status: {response.StatusCode}";
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
                ErrorMessage = $"Error adding employee: {ex.Message}";
                Log.Error(ex, "Error adding employee");
            }
        }

        [RelayCommand]
        private async Task Edit()
        {
            if (SelectedEmployee == null) return;

            try
            {
                var dialog = new EmployeeEditDialog(SelectedEmployee, Jobs, Departments);

                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    ? desktop.MainWindow
                    : null;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);

                    if (dialog.IsSaved && dialog.Employee != null)
                    {
                        var json = JsonSerializer.Serialize(dialog.Employee, _jsonOptions);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _httpClient.PutAsync($"{_baseUrl}/Employees/{SelectedEmployee.EmpId}", content);
                        if (response.IsSuccessStatusCode)
                        {
                            await LoadData();
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            ErrorMessage = $"Failed to update employee: {error}";
                            Log.Error("Failed to update employee. Status: {StatusCode}, Error: {Error}", 
                                response.StatusCode, error);
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
                ErrorMessage = $"Error editing employee: {ex.Message}";
                Log.Error(ex, "Error editing employee");
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedEmployee == null) return;

            try
            {
                var dialog = new Window
                {
                    Title = "Confirm Delete",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var grid = new Grid
                {
                    RowDefinitions = new RowDefinitions("*,Auto"),
                    Margin = new Thickness(10)
                };

                var messageText = new TextBlock
                {
                    Text = $"Are you sure you want to delete employee {SelectedEmployee.Surname} {SelectedEmployee.Name}?",
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10
                };

                var yesButton = new Button { Content = "Yes" };
                var noButton = new Button { Content = "No" };

                buttonPanel.Children.Add(yesButton);
                buttonPanel.Children.Add(noButton);

                grid.Children.Add(messageText);
                Grid.SetRow(messageText, 0);
                grid.Children.Add(buttonPanel);
                Grid.SetRow(buttonPanel, 1);

                dialog.Content = grid;

                yesButton.Click += async (s, e) =>
                {
                    var response = await _httpClient.DeleteAsync($"{_baseUrl}/Employees/{SelectedEmployee.EmpId}");
                    if (response.IsSuccessStatusCode)
                    {
                        await LoadData();
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        ErrorMessage = $"Failed to delete employee: {error}";
                    }
                };

                noButton.Click += (s, e) => dialog.Close();

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
                ErrorMessage = $"Error deleting employee: {ex.Message}";
                Log.Error(ex, "Error deleting employee");
            }
        }

        private void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                LoadData().ConfigureAwait(false);
                return;
            }

            var filteredEmployees = Employees.Where(e => 
                e.Surname.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                e.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                e.Patronym.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                e.Job?.JobTitle.Contains(value, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();

            Employees = new ObservableCollection<Employee>(filteredEmployees);
        }

        private async Task LoadEmployeeDetails(long employeeId)
        {
            try
            {
                // Load documents
                var docsResponse = await _httpClient.GetAsync($"{_baseUrl}/Employees/{employeeId}/documents");
                if (docsResponse.IsSuccessStatusCode)
                {
                    var jsonString = await docsResponse.Content.ReadAsStringAsync();
                    var docs = JsonSerializer.Deserialize<List<EmployeeDocument>>(jsonString, _jsonOptions);
                    SelectedEmployeeDocuments = new ObservableCollection<EmployeeDocument>(docs ?? new List<EmployeeDocument>());
                }

                // Load trainings
                var trainingsResponse = await _httpClient.GetAsync($"{_baseUrl}/Employees/{employeeId}/trainings");
                if (trainingsResponse.IsSuccessStatusCode)
                {
                    var jsonString = await trainingsResponse.Content.ReadAsStringAsync();
                    var trainings = JsonSerializer.Deserialize<List<EmployeeTraining>>(jsonString, _jsonOptions);
                    SelectedEmployeeTrainings = new ObservableCollection<EmployeeTraining>(trainings ?? new List<EmployeeTraining>());
                }

                // Load emergency contacts
                var contactsResponse = await _httpClient.GetAsync($"{_baseUrl}/Employees/{employeeId}/emergency-contacts");
                if (contactsResponse.IsSuccessStatusCode)
                {
                    var jsonString = await contactsResponse.Content.ReadAsStringAsync();
                    var contacts = JsonSerializer.Deserialize<List<EmergencyContact>>(jsonString, _jsonOptions);
                    SelectedEmployeeContacts = new ObservableCollection<EmergencyContact>(contacts ?? new List<EmergencyContact>());
                }

                // Load vacation requests
                var vacationsResponse = await _httpClient.GetAsync($"{_baseUrl}/Employees/{employeeId}/vacation-requests");
                if (vacationsResponse.IsSuccessStatusCode)
                {
                    var jsonString = await vacationsResponse.Content.ReadAsStringAsync();
                    var vacations = JsonSerializer.Deserialize<List<VacationRequest>>(jsonString, _jsonOptions);
                    SelectedEmployeeVacations = new ObservableCollection<VacationRequest>(vacations ?? new List<VacationRequest>());
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading employee details for {EmployeeId}", employeeId);
            }
        }

        [RelayCommand]
        private void ShowDetail()
        {
            if (SelectedEmployee != null)
            {
                CurrentView = "detail";
            }
        }

        [RelayCommand]
        private void ShowList()
        {
            CurrentView = "list";
        }

        [RelayCommand]
        private void ShowSection(string section)
        {
            DetailSection = section;
        }

        [RelayCommand]
        private async Task AddDocument()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            var dialog = new Window
            {
                Title = "Добавить документ",
                Width = 520,
                Height = 520,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var typeBox = new TextBox { Watermark = "Тип документа (паспорт, ВУ и т.д.)" };
            var numberBox = new TextBox { Watermark = "Номер документа" };
            var issueDatePicker = new DatePicker();
            var expiryDatePicker = new DatePicker();
            var issuedByBox = new TextBox { Watermark = "Кем выдан" };
            var filePathBox = new TextBox { Watermark = "Путь к файлу / ссылка" };
            var notesBox = new TextBox { Watermark = "Примечания", AcceptsReturn = true, MinHeight = 80, TextWrapping = TextWrapping.Wrap };

            var saveButton = new Button { Content = "Сохранить", HorizontalAlignment = HorizontalAlignment.Right };
            var cancelButton = new Button { Content = "Отмена", HorizontalAlignment = HorizontalAlignment.Left };

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Children = { cancelButton, saveButton }
            };

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = "Тип документа*" },
                    typeBox,
                    new TextBlock { Text = "Номер документа*" },
                    numberBox,
                    new TextBlock { Text = "Дата выдачи*" },
                    issueDatePicker,
                    new TextBlock { Text = "Действителен до" },
                    expiryDatePicker,
                    new TextBlock { Text = "Кем выдан" },
                    issuedByBox,
                    new TextBlock { Text = "Файл / ссылка" },
                    filePathBox,
                    new TextBlock { Text = "Примечания" },
                    notesBox,
                    buttonsPanel
                }
            };

            dialog.Content = new ScrollViewer { Content = contentPanel };

            cancelButton.Click += (_, _) => dialog.Close();

            saveButton.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(typeBox.Text) || string.IsNullOrWhiteSpace(numberBox.Text) || !issueDatePicker.SelectedDate.HasValue)
                {
                    HasError = true;
                    ErrorMessage = "Заполните тип, номер и дату выдачи";
                    return;
                }

                var newDocument = new EmployeeDocument
                {
                    DocumentType = typeBox.Text!,
                    DocumentNumber = numberBox.Text!,
                    IssueDate = issueDatePicker.SelectedDate!.Value.DateTime,
                    ExpiryDate = expiryDatePicker.SelectedDate?.DateTime,
                    IssuedBy = issuedByBox.Text,
                    FilePath = filePathBox.Text,
                    Notes = notesBox.Text
                };

                try
                {
                    var json = JsonSerializer.Serialize(newDocument, _jsonOptions);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/Employees/{SelectedEmployee.EmpId}/documents", content);

                    if (response.IsSuccessStatusCode)
                    {
                        HasError = false;
                        ErrorMessage = string.Empty;
                        await LoadEmployeeDetails(SelectedEmployee.EmpId);
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        HasError = true;
                        ErrorMessage = $"Не удалось добавить документ: {error}";
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Ошибка при добавлении документа: {ex.Message}";
                    Log.Error(ex, "Error adding employee document");
                }
            };

            var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (owner != null)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                Log.Error("Could not find main window for AddDocument dialog");
            }
        }

        [RelayCommand]
        private async Task AddTraining()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            var dialog = new Window
            {
                Title = "Добавить обучение",
                Width = 540,
                Height = 560,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var nameBox = new TextBox { Watermark = "Название курса / сертификата" };
            var organizationBox = new TextBox { Watermark = "Организация" };
            var descriptionBox = new TextBox { Watermark = "Описание", AcceptsReturn = true, MinHeight = 80, TextWrapping = TextWrapping.Wrap };
            var completionPicker = new DatePicker();
            var expiryPicker = new DatePicker();
            var certificateBox = new TextBox { Watermark = "Номер сертификата" };
            var filePathBox = new TextBox { Watermark = "Файл / ссылка" };
            var isMandatoryCheck = new CheckBox { Content = "Обязательное обучение" };
            var notesBox = new TextBox { Watermark = "Примечания", AcceptsReturn = true, MinHeight = 70, TextWrapping = TextWrapping.Wrap };

            var saveButton = new Button { Content = "Сохранить" };
            var cancelButton = new Button { Content = "Отмена" };

            cancelButton.Click += (_, _) => dialog.Close();

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = "Название обучения*" },
                    nameBox,
                    new TextBlock { Text = "Организация" },
                    organizationBox,
                    new TextBlock { Text = "Описание" },
                    descriptionBox,
                    new TextBlock { Text = "Дата прохождения*" },
                    completionPicker,
                    new TextBlock { Text = "Действительно до" },
                    expiryPicker,
                    new TextBlock { Text = "Номер сертификата" },
                    certificateBox,
                    new TextBlock { Text = "Файл / ссылка" },
                    filePathBox,
                    isMandatoryCheck,
                    new TextBlock { Text = "Примечания" },
                    notesBox,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancelButton, saveButton }
                    }
                }
            };

            dialog.Content = new ScrollViewer { Content = contentPanel };

            saveButton.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || !completionPicker.SelectedDate.HasValue)
                {
                    HasError = true;
                    ErrorMessage = "Укажите название обучения и дату прохождения";
                    return;
                }

                var newTraining = new EmployeeTraining
                {
                    TrainingName = nameBox.Text!,
                    Description = descriptionBox.Text,
                    CompletionDate = completionPicker.SelectedDate!.Value.DateTime,
                    ExpiryDate = expiryPicker.SelectedDate?.DateTime,
                    CertificateNumber = certificateBox.Text,
                    IssuingOrganization = organizationBox.Text,
                    IsMandatory = isMandatoryCheck.IsChecked ?? false,
                    FilePath = filePathBox.Text,
                    Notes = notesBox.Text
                };

                try
                {
                    var json = JsonSerializer.Serialize(newTraining, _jsonOptions);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/Employees/{SelectedEmployee.EmpId}/trainings", content);

                    if (response.IsSuccessStatusCode)
                    {
                        HasError = false;
                        ErrorMessage = string.Empty;
                        await LoadEmployeeDetails(SelectedEmployee.EmpId);
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        HasError = true;
                        ErrorMessage = $"Не удалось добавить обучение: {error}";
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Ошибка при добавлении обучения: {ex.Message}";
                    Log.Error(ex, "Error adding employee training");
                }
            };

            var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (owner != null)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                Log.Error("Could not find main window for AddTraining dialog");
            }
        }

        [RelayCommand]
        private async Task AddContact()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            var dialog = new Window
            {
                Title = "Новый контакт для экстренной связи",
                Width = 500,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var nameBox = new TextBox { Watermark = "ФИО" };
            var relationshipBox = new TextBox { Watermark = "Кем приходится" };
            var phoneBox = new TextBox { Watermark = "+7 (___) ___-__-__" };
            var altPhoneBox = new TextBox { Watermark = "Доп. телефон" };
            var addressBox = new TextBox { Watermark = "Адрес", AcceptsReturn = true, MinHeight = 60, TextWrapping = TextWrapping.Wrap };
            var isPrimaryCheck = new CheckBox { Content = "Основной контакт" };

            var saveButton = new Button { Content = "Сохранить" };
            var cancelButton = new Button { Content = "Отмена" };

            cancelButton.Click += (_, _) => dialog.Close();

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = "ФИО контакта*" },
                    nameBox,
                    new TextBlock { Text = "Отношение*" },
                    relationshipBox,
                    new TextBlock { Text = "Телефон*" },
                    phoneBox,
                    new TextBlock { Text = "Доп. телефон" },
                    altPhoneBox,
                    new TextBlock { Text = "Адрес" },
                    addressBox,
                    isPrimaryCheck,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancelButton, saveButton }
                    }
                }
            };

            dialog.Content = new ScrollViewer { Content = contentPanel };

            saveButton.Click += async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(relationshipBox.Text) || string.IsNullOrWhiteSpace(phoneBox.Text))
                {
                    HasError = true;
                    ErrorMessage = "Заполните ФИО, отношение и телефон";
                    return;
                }

                var newContact = new EmergencyContact
                {
                    ContactName = nameBox.Text!,
                    Relationship = relationshipBox.Text!,
                    PhoneNumber = phoneBox.Text!,
                    AlternatePhoneNumber = altPhoneBox.Text,
                    Address = addressBox.Text,
                    IsPrimary = isPrimaryCheck.IsChecked ?? false
                };

                try
                {
                    var json = JsonSerializer.Serialize(newContact, _jsonOptions);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/Employees/{SelectedEmployee.EmpId}/emergency-contacts", content);

                    if (response.IsSuccessStatusCode)
                    {
                        HasError = false;
                        ErrorMessage = string.Empty;
                        await LoadEmployeeDetails(SelectedEmployee.EmpId);
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        HasError = true;
                        ErrorMessage = $"Не удалось добавить контакт: {error}";
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Ошибка при добавлении контакта: {ex.Message}";
                    Log.Error(ex, "Error adding emergency contact");
                }
            };

            var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (owner != null)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                Log.Error("Could not find main window for AddContact dialog");
            }
        }

        [RelayCommand]
        private async Task AddVacation()
        {
            if (SelectedEmployee == null)
            {
                return;
            }

            var dialog = new Window
            {
                Title = "Новая заявка на отпуск",
                Width = 520,
                Height = 480,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var startDatePicker = new DatePicker();
            var endDatePicker = new DatePicker();
            var typeBox = new TextBox { Watermark = "Тип отпуска (ежегодный, больничный и т.п.)" };
            var reasonBox = new TextBox { Watermark = "Комментарий", AcceptsReturn = true, MinHeight = 100, TextWrapping = TextWrapping.Wrap };

            var saveButton = new Button { Content = "Сохранить" };
            var cancelButton = new Button { Content = "Отмена" };

            cancelButton.Click += (_, _) => dialog.Close();

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = "Дата начала*" },
                    startDatePicker,
                    new TextBlock { Text = "Дата окончания*" },
                    endDatePicker,
                    new TextBlock { Text = "Тип отпуска*" },
                    typeBox,
                    new TextBlock { Text = "Комментарий" },
                    reasonBox,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancelButton, saveButton }
                    }
                }
            };

            dialog.Content = new ScrollViewer { Content = contentPanel };

            saveButton.Click += async (_, _) =>
            {
                if (!startDatePicker.SelectedDate.HasValue || !endDatePicker.SelectedDate.HasValue || string.IsNullOrWhiteSpace(typeBox.Text))
                {
                    HasError = true;
                    ErrorMessage = "Укажите даты и тип отпуска";
                    return;
                }

                var start = startDatePicker.SelectedDate!.Value.DateTime;
                var end = endDatePicker.SelectedDate!.Value.DateTime;

                if (end < start)
                {
                    HasError = true;
                    ErrorMessage = "Дата окончания не может быть раньше даты начала";
                    return;
                }

                var newRequest = new VacationRequest
                {
                    StartDate = start,
                    EndDate = end,
                    VacationType = typeBox.Text!,
                    Reason = reasonBox.Text,
                    Status = "Pending"
                };

                try
                {
                    var json = JsonSerializer.Serialize(newRequest, _jsonOptions);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{_baseUrl}/Employees/{SelectedEmployee.EmpId}/vacation-requests", content);

                    if (response.IsSuccessStatusCode)
                    {
                        HasError = false;
                        ErrorMessage = string.Empty;
                        await LoadEmployeeDetails(SelectedEmployee.EmpId);
                        dialog.Close();
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        HasError = true;
                        ErrorMessage = $"Не удалось создать заявку: {error}";
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Ошибка при создании заявки: {ex.Message}";
                    Log.Error(ex, "Error creating vacation request");
                }
            };

            var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (owner != null)
            {
                await dialog.ShowDialog(owner);
            }
            else
            {
                Log.Error("Could not find main window for AddVacation dialog");
            }
        }
    }
} 