using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using TicketSalesApp.UI.Administration.Avalonia.ViewModels;
using TicketSalesApp.UI.Administration.Avalonia.Views;
using System;
using System.Threading.Tasks;
using Serilog;

namespace TicketSalesApp.UI.Administration.Avalonia;

public partial class App : Application
{
    private Window? _mainWindow;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
#if DEBUG
                Log.Debug("Showing under construction window for debug build");
                var underConstructionWindow = new UnderConstructionWindow();
                
                // Set up handler for when under construction window closes
                underConstructionWindow.Closed += async (s, e) =>
                {
                    Log.Debug("Under construction window closed, showing splash screen");
                    await ShowSplashScreenAndInitialize(desktop);
                };
                
                underConstructionWindow.Show();
#else
                Log.Debug("Creating splash screen");
                // Create and show the splash screen directly in release mode
                await ShowSplashScreenAndInitialize(desktop);
#endif

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during application initialization");
                desktop.Shutdown();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task ShowSplashScreenAndInitialize(IClassicDesktopStyleApplicationLifetime desktop)
    {

        try
        {
            var splashScreen = new SplashScreen();
            splashScreen.Show();

            // Initial delay to show splash screen
            await Task.Delay(1000);

            // Check server availability
            Log.Debug("Checking server availability");
            if (!await splashScreen.CheckServerAvailability())
            {
                Log.Error("Server not available");
                // Wait for 5 seconds to show the error message
                await Task.Delay(5000);
                desktop.Shutdown();
                return;
            }

            // Additional initialization delay
            await Task.Delay(3000);

            // Close the splash screen
            splashScreen.Close();
            Log.Debug("Splash screen closed");

            // Create and show auth window
            var authWindow = new AuthWindow
            {
                DataContext = new AuthViewModel()
            };

            // Handle authentication result
            authWindow.Closed += (s, e) =>
            {
                if (authWindow.DataContext is AuthViewModel vm && vm.IsAuthenticated)
                {
                    // Create main window
                    _mainWindow = new MainWindow();
                    var mainViewModel = new MainWindowViewModel();
                    _mainWindow.DataContext = mainViewModel;

                    desktop.MainWindow = _mainWindow;
                    _mainWindow.Show();
                }
                else
                {
                    Log.Information("Authentication failed or cancelled. Shutting down...");
                    desktop.Shutdown();
                }
            };

            // Show auth window
            authWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during application initialization");
            desktop.Shutdown();
        }
    }
    
        
    
       

        
    

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}