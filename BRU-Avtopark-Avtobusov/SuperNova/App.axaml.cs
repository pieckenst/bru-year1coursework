using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Styling;
using Classic.Avalonia.Theme;
using Classic.CommonControls.Dialogs;
using PleasantUI.Controls;
using R3;
using Serilog;
using SuperNova.Forms.ViewModels;
using SuperNova.Forms.Views;
using SuperNova.Runtime.Interpreter;
using SuperNova.VisualDesigner;
using SuperNova.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AvaLogEventLevel = Avalonia.Logging.LogEventLevel;
using AvaILogSink = Avalonia.Logging.ILogSink;
using System.Linq;
using Serilog.Events;
using Serilog.Context;

namespace SuperNova;

public partial class App : Application
{
    public override void Initialize()
    {
        // Initialize aggressive stack overflow protection FIRST - before anything else
        try
        {
            SuperNova.Converters.CultureInfoProtection.Initialize();
            SuperNova.Converters.StringFormatInterceptor.Initialize();
            SuperNova.Converters.GlobalTypeDescriptorHook.InstallHook();
            SuperNova.Converters.AvaloniaTypeConverterInterceptor.Initialize();
            SuperNova.Converters.AvaloniaBindingInterceptor.Initialize();

            // CRITICAL: Initialize property change protection
            PropertyChangeProtection.Initialize();

            // CRITICAL: Initialize global DateTime interceptor
            SuperNova.Converters.GlobalDateTimeInterceptor.Initialize();

            System.Diagnostics.Debug.WriteLine("Aggressive stack overflow protection initialized");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize aggressive protection: {ex.Message}");
        }

        // CRITICAL FIX: Use InvariantCulture directly to avoid any custom culture issues
        try
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            System.Diagnostics.Debug.WriteLine("Culture initialized with InvariantCulture");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize culture: {ex.Message}");
            // Continue without culture setup if it fails
        }

        // Force light theme - override system theme preference  - can be disabled with commenting below line out without removing it
        //RequestedThemeVariant = ThemeVariant.Light;

        // Add global exception handling for DateTime binding issues
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is StackOverflowException)
            {
                Log.Fatal("Stack overflow exception caught in global handler");
                // Don't try to handle stack overflow - just log and exit gracefully
                Environment.Exit(1);
            }
        };

        // Initialize TypeConverter protection system
        try
        {
            // Pre-cache common type converters to prevent runtime recursion
            SuperNova.Converters.TypeConverterProtection.SafeGetConverter(typeof(string));
            SuperNova.Converters.TypeConverterProtection.SafeGetConverter(typeof(int));
            SuperNova.Converters.TypeConverterProtection.SafeGetConverter(typeof(bool));
            SuperNova.Converters.TypeConverterProtection.SafeGetConverter(typeof(DateTime));
            SuperNova.Converters.TypeConverterProtection.SafeGetConverter(typeof(DateTimeOffset));
            SuperNova.Converters.TypeConverterProtection.SafeGetConverter(typeof(double));
            SuperNova.Converters.TypeConverterProtection.SafeGetConverter(typeof(decimal));
            Log.Information("TypeConverter protection system initialized");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to initialize TypeConverter protection system");
        }

        // Initialize string formatting protection
        try
        {
            // CRITICAL FIX: Use InvariantCulture directly
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            Log.Information("String formatting protection initialized with InvariantCulture");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to initialize string formatting protection");
            // Continue without thread culture setup if it fails
        }

        // Set up global exception handlers
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Log.Fatal(exception, "Unhandled AppDomain exception: {ExceptionMessage}", exception?.Message);
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Error(args.Exception, "Unobserved Task exception: {ExceptionMessage}", args.Exception.Message);
            args.SetObserved();
        };

        // Configure comprehensive logging
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Debug)  // Log HTTP client activity
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SuperNova")
            .Enrich.WithProperty("ProcessId", System.Diagnostics.Process.GetCurrentProcess().Id)
            .WriteTo.Debug(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                "logs/app_.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 10485760,  // 10MB
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .WriteTo.File(
                "logs/errors_.log",
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 31);

        var logger = loggerConfig.CreateLogger();
        Log.Logger = logger;

        // Add Avalonia logging sink with enhanced context
        Avalonia.Logging.Logger.Sink = new AvaloniaLogSink(logger);

        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            Log.Information("Application framework initialization starting");

            // Ensure light theme is always used regardless of system settings - can be disabled with commenting below line out without removing it
            //RequestedThemeVariant = ThemeVariant.Light;
            //Log.Information("Forced light theme variant");

            var rootViewModel = new DISetup().Root;
            Static.RootViewModel = rootViewModel;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);

                try
                {
                    Log.Debug("Creating splash screen");
                    // Create and show the splash screen
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

                    // Create the authentication window
                    Log.Debug("Creating authentication window");
                    var authWindow = new AuthWindow
                    {
                        DataContext = new AuthViewModel()
                    };

                    var authResult = await ShowAuthWindowAsync(authWindow);
                    if (!authResult)
                    {
                        Log.Information("Authentication failed or cancelled");
                        desktop.Shutdown();
                        return;
                    }

                    Log.Information("Authentication successful, initializing main window");

                    // Initialize main window after successful authentication
                    if (Static.ForceSingleView)
                    {
                        Static.SingleView = true;
                        Static.MainView = new MainView
                        {
                            DataContext = rootViewModel
                        };

                        desktop.MainWindow = new PleasantWindow()
                        {
                            Content = Static.MainView
                        };

                        rootViewModel.ObservePropertyChanged(x => x.Title)
                            .Subscribe(title => desktop.MainWindow.Title = title);

#if DEBUG
                        desktop.MainWindow.AttachDevTools();
#endif

                        Static.MainView.WindowInitialized();
                    }
                    else
                    {
                        var mainWindow = new MainWindow
                        {
                            DataContext = rootViewModel
                        };
                        desktop.MainWindow = mainWindow;
                        Static.SingleView = false;
                        Static.MainView = mainWindow.MainViewControl;
                    }

                    Log.Information("Main window initialized");
                    desktop.MainWindow?.Show();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Fatal error during application initialization");
                    desktop.Shutdown();
                }
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                Static.SingleView = true;
                singleViewPlatform.MainView = Static.MainView = new MainView
                {
                    DataContext = rootViewModel
                };
                Static.MainView.WindowInitialized();
            }

            base.OnFrameworkInitializationCompleted();
            Log.Information("Application framework initialization completed");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal error in OnFrameworkInitializationCompleted");
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }

    private async Task<bool> ShowAuthWindowAsync(AuthWindow authWindow)
    {
        try
        {
            var tcs = new TaskCompletionSource<bool>();

            authWindow.Closed += (s, e) =>
            {
                var result = authWindow.DataContext is AuthViewModel vm && vm.IsAuthenticated;
                Log.Debug("Auth window closed with result: {AuthResult}", result);
                tcs.SetResult(result);
            };

            authWindow.Show();
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in ShowAuthWindowAsync");
            return false;
        }
    }
}

public class AvaloniaLogSink : AvaILogSink
{
    private readonly ILogger _logger;

    public AvaloniaLogSink(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsEnabled(AvaLogEventLevel level, string area)
    {
        // Log all areas at Warning level and above, plus specific debug areas
        if (level >= AvaLogEventLevel.Warning) return true;
        
        // Add specific debug areas you want to monitor
        return level >= AvaLogEventLevel.Debug && (
            area == "Binding" ||
            area == "DataValidation" ||
            area == "Visual" ||
            area == "Layout" ||
            area == "Control" ||
            area == "Data" ||
            area == "Animation"
        );
    }

    public void Log(AvaLogEventLevel level, string area, object? source, string messageTemplate)
    {
        if (!IsEnabled(level, area))
            return;

        var sourceInfo = source?.ToString() ?? "Unknown";
        var logLevel = MapLogLevel(level);
        
        using (LogContext.PushProperty("Area", area))
        using (LogContext.PushProperty("Source", sourceInfo))
        {
            _logger.Write(logLevel, "[{Area}] [{Source}] {Message}", area, sourceInfo, messageTemplate);
        }
    }

    public void Log(AvaLogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if (!IsEnabled(level, area))
            return;

        var sourceInfo = source?.ToString() ?? "Unknown";
        var logLevel = MapLogLevel(level);
        
        using (LogContext.PushProperty("Area", area))
        using (LogContext.PushProperty("Source", sourceInfo))
        {
            var allProperties = new object[] { area, sourceInfo }
                .Concat(propertyValues ?? Array.Empty<object>())
                .ToArray();
            
            _logger.Write(logLevel, "[{Area}] [{Source}] " + messageTemplate, allProperties);
        }
    }

    private static LogEventLevel MapLogLevel(AvaLogEventLevel level)
    {
        return level switch
        {
            AvaLogEventLevel.Verbose => LogEventLevel.Verbose,
            AvaLogEventLevel.Debug => LogEventLevel.Debug,
            AvaLogEventLevel.Information => LogEventLevel.Information,
            AvaLogEventLevel.Warning => LogEventLevel.Warning,
            AvaLogEventLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Information
        };
    }


}