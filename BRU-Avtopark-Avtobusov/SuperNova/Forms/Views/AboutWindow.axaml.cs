using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Classic.CommonControls.Dialogs;
using PleasantUI.Controls;
using SuperNova.Forms.Services;

namespace SuperNova.Forms.Views;

public partial class AboutWindow : PleasantWindow
{
    private readonly ItemsControl technologyList;
    private readonly TextBlock runtimeVersion;
    private readonly TextBlock osVersion;
    private readonly TextBlock avaloniaVersion;

    public AboutWindow()
    {
        InitializeComponent();

        technologyList = this.FindControl<ItemsControl>("TechnologyList");
        runtimeVersion = this.FindControl<TextBlock>("RuntimeVersion");
        osVersion = this.FindControl<TextBlock>("OsVersion");
        avaloniaVersion = this.FindControl<TextBlock>("AvaloniaVersion");

        InitializeSystemInfo();
        InitializeTechnologyList();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeSystemInfo()
    {
        runtimeVersion.Text = $".NET Runtime: {RuntimeInformation.FrameworkDescription}";
        osVersion.Text = $"ОС: {RuntimeInformation.OSDescription}";
        avaloniaVersion.Text = $"Avalonia UI: {typeof(Application).Assembly.GetName().Version}";
    }

    private void InitializeTechnologyList()
    {
        var technologies = new List<string>
        {
            "Avalonia UI - Кроссплатформенный UI фреймворк",
            "C# 12.0 - Язык программирования",
            ".NET 9.0 - Платформа разработки",
            "MVVM Architecture - Архитектурный паттерн",
            "Entity Framework Core - ORM для работы с БД",
            "Microsoft SQL Server - СУБД",
            "Git - Система контроля версий",
            "Visual Studio 2022 - IDE"
        };

        technologyList.ItemsSource = technologies;
    }

    private async void OnSystemInfo(object sender, RoutedEventArgs e)
    {
        var info = new StringBuilder();
        info.AppendLine("Системная информация:");
        info.AppendLine($"Время: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        info.AppendLine($"Процессор: {RuntimeInformation.ProcessArchitecture}");
        info.AppendLine($"ОС: {RuntimeInformation.OSDescription}");
        info.AppendLine($"Платформа: {RuntimeInformation.OSArchitecture}");
        info.AppendLine($".NET Runtime: {RuntimeInformation.FrameworkDescription}");
        info.AppendLine($"Avalonia UI: {typeof(Application).Assembly.GetName().Version}");

        try
        {
            // Get current user info using ApiClientService
            var currentUser = await GetCurrentUserAsync();
            if (currentUser != null)
            {
                var userJson = currentUser.RootElement;
                info.AppendLine("\nТекущий пользователь:");
                info.AppendLine($"Логин: {userJson.GetProperty("login").GetString()}");
                info.AppendLine($"Роль: {(userJson.GetProperty("role").GetInt32() == 1 ? "Администратор" : "Пользователь")}");
                info.AppendLine($"Активен: {(userJson.GetProperty("isActive").GetBoolean() ? "Да" : "Нет")}");
                
                if (userJson.TryGetProperty("isWindowsAuth", out var isWindowsAuth) && isWindowsAuth.GetBoolean())
                {
                    info.AppendLine("Тип входа: Windows аутентификация");
                    if (userJson.TryGetProperty("windowsIdentity", out var windowsIdentity) && 
                        windowsIdentity.ValueKind != System.Text.Json.JsonValueKind.Null)
                    {
                        info.AppendLine($"Windows ID: {windowsIdentity.GetString()}");
                    }
                }
                else
                {
                    info.AppendLine("Тип входа: Стандартная аутентификация");
                }
            }

            // Get API stats
            var apiStats = await GetApiStatsAsync();
            if (apiStats != null)
            {
                var statsJson = apiStats.RootElement;
                info.AppendLine("\nСтатистика сервера:");
                info.AppendLine($"Сервер: {statsJson.GetProperty("machineName").GetString()}");
                var uptime = DateTime.UtcNow - statsJson.GetProperty("processStartTime").GetDateTime();
                info.AppendLine($"Время работы: {uptime.Days}д {uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}");
                info.AppendLine($"Использование памяти: {statsJson.GetProperty("memoryUsageMB").GetInt64()} МБ");
                info.AppendLine($"Потоков: {statsJson.GetProperty("threadCount").GetInt32()}");
                info.AppendLine($"Процессоры: {statsJson.GetProperty("processorCount").GetInt32()}");
                info.AppendLine($"64-битный процесс: {(statsJson.GetProperty("is64BitProcess").GetBoolean() ? "Да" : "Нет")}");
            }
        }
        catch (Exception ex)
        {
            info.AppendLine("\nНе удалось получить информацию о пользователе или статистику сервера");
            info.AppendLine($"Ошибка: {ex.Message}");
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            await MessageBox.ShowDialog(
                this,
                info.ToString(),
                "Системная информация",
                MessageBoxButtons.Ok,
                MessageBoxIcon.Information);
        }
    }

    private async void OnCopyInfo(object sender, RoutedEventArgs e)
    {
        var info = new StringBuilder();
        info.AppendLine("Автопарк автобусов - Система управления автобусным парком");
        info.AppendLine("Версия: 0.5A");
        info.AppendLine("Разработчик: Савич Андрей Олегович");
        info.AppendLine("Группа: АСОИСЗ-241");
        info.AppendLine("Год: 2025");
        info.AppendLine();
        info.AppendLine("Системная информация:");
        info.AppendLine($"ОС: {RuntimeInformation.OSDescription}");
        info.AppendLine($".NET Runtime: {RuntimeInformation.FrameworkDescription}");
        info.AppendLine($"Avalonia UI: {typeof(Application).Assembly.GetName().Version}");

         try
        {
            // Get current user info using ApiClientService
            var currentUser = await GetCurrentUserAsync();
            if (currentUser != null)
            {
                var userJson = currentUser.RootElement;
                info.AppendLine("\nТекущий пользователь:");
                info.AppendLine($"Логин: {userJson.GetProperty("login").GetString()}");
                info.AppendLine($"Роль: {(userJson.GetProperty("role").GetInt32() == 1 ? "Администратор" : "Пользователь")}");
                info.AppendLine($"Активен: {(userJson.GetProperty("isActive").GetBoolean() ? "Да" : "Нет")}");
                
                if (userJson.TryGetProperty("isWindowsAuth", out var isWindowsAuth) && isWindowsAuth.GetBoolean())
                {
                    info.AppendLine("Тип входа: Windows аутентификация");
                    if (userJson.TryGetProperty("windowsIdentity", out var windowsIdentity) && 
                        windowsIdentity.ValueKind != System.Text.Json.JsonValueKind.Null)
                    {
                        info.AppendLine($"Windows ID: {windowsIdentity.GetString()}");
                    }
                }
                else
                {
                    info.AppendLine("Тип входа: Стандартная аутентификация");
                }
            }

            // Get API stats
            var apiStats = await GetApiStatsAsync();
            if (apiStats != null)
            {
                var statsJson = apiStats.RootElement;
                info.AppendLine("\nСтатистика сервера:");
                info.AppendLine($"Сервер: {statsJson.GetProperty("machineName").GetString()}");
                var uptime = DateTime.UtcNow - statsJson.GetProperty("processStartTime").GetDateTime();
                info.AppendLine($"Время работы: {uptime.Days}д {uptime.Hours:00}:{uptime.Minutes:00}:{uptime.Seconds:00}");
                info.AppendLine($"Использование памяти: {statsJson.GetProperty("memoryUsageMB").GetInt64()} МБ");
                info.AppendLine($"Потоков: {statsJson.GetProperty("threadCount").GetInt32()}");
                info.AppendLine($"Процессоры: {statsJson.GetProperty("processorCount").GetInt32()}");
                info.AppendLine($"64-битный процесс: {(statsJson.GetProperty("is64BitProcess").GetBoolean() ? "Да" : "Нет")}");
            }
        }
        catch (Exception ex)
        {
            info.AppendLine("\nНе удалось получить информацию о пользователе или статистику сервера");
            info.AppendLine($"Ошибка: {ex.Message}");
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(info.ToString());
            
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                await MessageBox.ShowDialog(
                    this,
                    "Информация скопирована в буфер обмена",
                    "Копирование",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Information);
            }
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async Task<JsonDocument?> GetCurrentUserAsync()
    {
        try
        {
            using var client = ApiClientService.Instance.CreateClient();
            var response = await client.GetAsync("users/current");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting current user: {ex.Message}");
            return null;
        }
    }

    private async Task<JsonDocument?> GetApiStatsAsync()
    {
        try
        {
            using var client = ApiClientService.Instance.CreateClient();
            var response = await client.GetAsync("users/api-stats");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting API stats: {ex.Message}");
            return null;
        }
    }
} 