using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;
using TicketSalesApp.UI.Avalonia.Views;
using TicketSalesApp.UI.Avalonia.ViewModels;
using Serilog;
using System;


namespace TicketSalesApp.UI.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
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

                try
                {
                    // Create the authentication window
                    var authWindow = new AuthWindow
                    {
                        DataContext = new AuthViewModel()
                    };

                    // Handle the closing event of the authentication window
                    authWindow.Closed += (sender, e) =>
                    {
                        // If authentication was successful, open the main window
                        if (authWindow.DataContext is AuthViewModel authViewModel && authViewModel.IsAuthenticated)
                        {
                            var mainViewModel = new MainWindowViewModel();
                            desktop.MainWindow = new MainWindow
                            {
                                DataContext = mainViewModel
                            };
                            desktop.MainWindow.Show();
                        }
                        else
                        {
                            // Close the application if authentication failed
                            desktop.Shutdown();
                            // temporary debug hack below to test app while only admin module is working
                            //var mainViewModel = new MainWindowViewModel();
                            //desktop.MainWindow = new MainWindow
                            //{
                                //DataContext = mainViewModel
                            //};
                            //desktop.MainWindow.Show();
                        }
                    };

                    // Show the authentication window
                    authWindow.Show();
                }
                catch (Exception ex)
                {
                    // Log the exception
                    Log.Error(ex, "An error occurred during application initialization.");
                    desktop.Shutdown(); // Close the application on error
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
