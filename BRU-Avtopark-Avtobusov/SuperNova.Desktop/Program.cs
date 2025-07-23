using Avalonia;
using System;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using Avalonia.ReactiveUI;
using Avalonia.Controls;
using Classic.CommonControls;
using Avalonia.Media.Fonts;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SuperNova.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // CRITICAL: Initialize stack overflow protection BEFORE anything else
        try
        {
            Console.WriteLine("[INIT] Starting stack overflow protection initialization...");
            InitializeStackOverflowProtection();
            Console.WriteLine("[INIT] Stack overflow protection initialized successfully");
        }
        catch (Exception protectionEx)
        {
            Console.WriteLine($"[ERROR] Failed to initialize stack overflow protection: {protectionEx.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {protectionEx.StackTrace}");
            // Continue anyway - better to try running than fail completely
        }

        try
        {
            Console.WriteLine("[INIT] Initializing Serilog logger...");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose() // МАКСИМАЛЬНЫЙ уровень логирования
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/app-.log",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Verbose, // Все в файл
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/debug-.log", // Отдельный файл для дебага
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("=== APPLICATION STARTUP INITIATED ===");
            Log.Information("Serilog logger initialized successfully");
            Log.Information("Starting application with comprehensive stack overflow protection...");

            Log.Debug("Fixing current working directory...");
            FixCurrentWorkingDictionary();
            Log.Debug("Working directory fixed successfully");

            Log.Information("Initiating safe Avalonia startup...");
            // Wrap Avalonia startup in additional protection
            return SafeStartAvalonia(args);
        }
        catch (StackOverflowException soEx)
        {
            Console.WriteLine("=== CRITICAL: STACK OVERFLOW DETECTED IN MAIN APPLICATION STARTUP! ===");
            Console.WriteLine($"Stack overflow message: {soEx.Message}");
            Console.WriteLine($"Stack overflow source: {soEx.Source}");
            Console.WriteLine("This indicates a recursive loop in the application initialization");

            try
            {
                Log.Fatal("=== CRITICAL STACK OVERFLOW IN MAIN STARTUP ===");
                Log.Fatal("Stack overflow exception in main application startup");
                Log.Fatal("Stack overflow message: {Message}", soEx.Message);
                Log.Fatal("Stack overflow source: {Source}", soEx.Source);
                Log.Fatal("This indicates a recursive loop in the application initialization");
            }
            catch
            {
                Console.WriteLine("Failed to log stack overflow to file");
            }
            return 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== CRITICAL: UNHANDLED EXCEPTION IN MAIN STARTUP ===");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            Console.WriteLine($"Exception message: {ex.Message}");
            Console.WriteLine($"Exception source: {ex.Source}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");

            try
            {
                Log.Fatal("=== CRITICAL UNHANDLED EXCEPTION IN MAIN STARTUP ===");
                Log.Fatal(ex, "Application terminated unexpectedly");
                Log.Fatal("Exception type: {ExceptionType}", ex.GetType().Name);
                Log.Fatal("Exception message: {Message}", ex.Message);
                Log.Fatal("Exception source: {Source}", ex.Source);
                Log.Fatal("Stack trace: {StackTrace}", ex.StackTrace);
            }
            catch
            {
                Console.WriteLine($"Failed to log exception to file: {ex.Message}");
            }
            return 1;
        }
        finally
        {
            Console.WriteLine("=== APPLICATION SHUTDOWN INITIATED ===");
            try
            {
                Log.Information("=== APPLICATION SHUTDOWN INITIATED ===");
                Log.Information("Closing and flushing logs...");
                Log.CloseAndFlush();
                Console.WriteLine("Logs closed and flushed successfully");
            }
            catch (Exception finalEx)
            {
                Console.WriteLine($"Failed to close logs: {finalEx.Message}");
            }
        }
    }

    /// <summary>
    /// Initialize all stack overflow protection systems
    /// </summary>
    private static void InitializeStackOverflowProtection()
    {
        try
        {
            Console.WriteLine("[PROTECTION] Initializing CultureInfoProtection...");
            SuperNova.Converters.CultureInfoProtection.Initialize();
            Console.WriteLine("[PROTECTION] CultureInfoProtection initialized successfully");

            Console.WriteLine("[PROTECTION] Initializing StringFormatInterceptor...");
            SuperNova.Converters.StringFormatInterceptor.Initialize();
            Console.WriteLine("[PROTECTION] StringFormatInterceptor initialized successfully");

            Console.WriteLine("[PROTECTION] Installing GlobalTypeDescriptorHook...");
            SuperNova.Converters.GlobalTypeDescriptorHook.InstallHook();
            Console.WriteLine("[PROTECTION] GlobalTypeDescriptorHook installed successfully");

            Console.WriteLine("[PROTECTION] Initializing AvaloniaTypeConverterInterceptor...");
            SuperNova.Converters.AvaloniaTypeConverterInterceptor.Initialize();
            Console.WriteLine("[PROTECTION] AvaloniaTypeConverterInterceptor initialized successfully");

            Console.WriteLine("[PROTECTION] Initializing AvaloniaBindingInterceptor...");
            SuperNova.Converters.AvaloniaBindingInterceptor.Initialize();
            Console.WriteLine("[PROTECTION] AvaloniaBindingInterceptor initialized successfully");

            Console.WriteLine("[PROTECTION] Initializing GlobalDateTimeInterceptor...");
            SuperNova.Converters.GlobalDateTimeInterceptor.Initialize();
            Console.WriteLine("[PROTECTION] GlobalDateTimeInterceptor initialized successfully");

            Console.WriteLine("[PROTECTION] All stack overflow protection systems initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Stack overflow protection initialization failed: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Safely start Avalonia with additional protection
    /// </summary>
    private static int SafeStartAvalonia(string[] args)
    {
        try
        {
            Log.Information("=== AVALONIA STARTUP PHASE ===");
            Log.Debug("Building Avalonia application...");

            var appBuilder = BuildAvaloniaApp();

            Log.Debug("Avalonia app builder created successfully");

            // Add additional safety checks before starting
            if (appBuilder == null)
            {
                Log.Error("CRITICAL: Failed to build Avalonia app - appBuilder is null");
                Console.WriteLine("[ERROR] CRITICAL: Failed to build Avalonia app - appBuilder is null");
                return 3;
            }

            Log.Information("Starting Avalonia with ClassicDesktopLifetime...");
            Log.Debug("Arguments: {Args}", string.Join(" ", args));
            Log.Debug("Shutdown mode: OnExplicitShutdown");

            Console.WriteLine("[AVALONIA] Starting Avalonia application...");
            var result = appBuilder.StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);

            Log.Information("Avalonia application completed with exit code: {ExitCode}", result);
            Console.WriteLine($"[AVALONIA] Application completed with exit code: {result}");

            return result;
        }
        catch (StackOverflowException soEx)
        {
            Log.Fatal("CRITICAL: Stack overflow exception in Avalonia startup");
            Log.Fatal("Stack overflow details: {Message}", soEx.Message);
            Console.WriteLine("[FATAL] CRITICAL: Stack overflow exception in Avalonia startup");
            Console.WriteLine($"[FATAL] Stack overflow details: {soEx.Message}");
            return 4;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "CRITICAL: Unhandled exception in Avalonia startup");
            Log.Fatal("Exception type: {ExceptionType}", ex.GetType().Name);
            Log.Fatal("Exception message: {Message}", ex.Message);
            Log.Fatal("Stack trace: {StackTrace}", ex.StackTrace);
            Console.WriteLine($"[FATAL] CRITICAL: Unhandled exception in Avalonia startup: {ex.GetType().Name}");
            Console.WriteLine($"[FATAL] Exception message: {ex.Message}");
            Console.WriteLine($"[FATAL] Stack trace: {ex.StackTrace}");
            ForceLogDump();
            return 5;
        } }

    /// <summary>
    /// Force log dump for diagnostics before fatal crash.
    /// </summary>
    private static void ForceLogDump()
    {
        try
        {
            Log.Information("[ForceLogDump] Forcing log flush and diagnostic dump before fatal error.");
            Console.WriteLine("[ForceLogDump] Flushing logs and dumping diagnostics...");

            // Serilog: flush and close all log sinks
            Serilog.Log.CloseAndFlush();
            Log.Information("[ForceLogDump] Serilog CloseAndFlush called.");
            Console.WriteLine("[ForceLogDump] Serilog CloseAndFlush called.");

            // Log file locations (if known)
            // NOTE: If using File sink, you may have a static path or configuration
            var logFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "logs", "log.txt"); // Adjust as needed
            Log.Information($"[ForceLogDump] Log file location: {logFilePath}");
            Console.WriteLine($"[ForceLogDump] Log file location: {logFilePath}");

            // Optionally dump last N lines of the log file
            try
            {
                if (System.IO.File.Exists(logFilePath))
                {
                    var logLines = new List<string>(System.IO.File.ReadLines(logFilePath));
                    var lastLines = logLines.Count > 20 ? logLines.GetRange(logLines.Count - 20, 20) : logLines;
                    Console.WriteLine("[ForceLogDump] Last 20 log lines:");
                    foreach (var line in lastLines)
                    {
                        Console.WriteLine(line);
                    }
                }
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"[ForceLogDump] Could not dump log file tail: {logEx.Message}");
            }

            // Additional diagnostics: time, directory, process/thread info
            Console.WriteLine($"[ForceLogDump] Current time: {DateTime.Now:O}");
            Console.WriteLine($"[ForceLogDump] Current directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"[ForceLogDump] Process ID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
            Console.WriteLine($"[ForceLogDump] Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            // Optionally dump stack traces of all threads (advanced)
            // ...
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ForceLogDump] Error during forced log dump: {ex.Message}");
        }
    }

    private static void FixCurrentWorkingDictionary()
    {
        try
        {
            Log.Debug("Fixing current working directory...");
            Console.WriteLine("[WORKDIR] Fixing current working directory...");

            var processPath = Environment.ProcessPath;
            Log.Debug("Process path: {ProcessPath}", processPath);
            Console.WriteLine($"[WORKDIR] Process path: {processPath}");

            if (Path.GetDirectoryName(processPath) is { } dir)
            {
                Log.Debug("Setting current directory to: {Directory}", dir);
                Console.WriteLine($"[WORKDIR] Setting current directory to: {dir}");

                Environment.CurrentDirectory = dir;

                Log.Debug("Current directory set successfully to: {CurrentDirectory}", Environment.CurrentDirectory);
                Console.WriteLine($"[WORKDIR] Current directory set successfully to: {Environment.CurrentDirectory}");
            }
            else
            {
                Log.Warning("Could not determine process directory");
                Console.WriteLine("[WORKDIR] Warning: Could not determine process directory");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to fix working directory");
            Console.WriteLine($"[WORKDIR] Warning: Failed to fix working directory: {ex.Message}");
            Console.WriteLine($"[WORKDIR] Stack trace: {ex.StackTrace}");
            // Continue anyway
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        try
        {
            Log.Debug("=== BUILDING AVALONIA APP ===");
            Console.WriteLine("[BUILD] Starting Avalonia app builder configuration...");

            Log.Debug("Configuring base AppBuilder...");
            var builder = AppBuilder.Configure<App>();
            Log.Debug("AppBuilder.Configure<App>() completed");
            Console.WriteLine("[BUILD] AppBuilder.Configure<App>() completed");

            Log.Debug("Adding UsePlatformDetect()...");
            builder = builder.UsePlatformDetect();
            Log.Debug("UsePlatformDetect() completed");
            Console.WriteLine("[BUILD] UsePlatformDetect() completed");

            Log.Debug("Adding UseMessageBoxSounds()...");
            builder = builder.UseMessageBoxSounds();
            Log.Debug("UseMessageBoxSounds() completed");
            Console.WriteLine("[BUILD] UseMessageBoxSounds() completed");

            Log.Debug("Adding LogToTrace()...");
            builder = builder.LogToTrace();
            Log.Debug("LogToTrace() completed");
            Console.WriteLine("[BUILD] LogToTrace() completed");

            // Safely configure fonts with error handling
            try
            {
                Log.Debug("Configuring fonts...");
                Console.WriteLine("[BUILD] Configuring fonts...");

                builder = builder.ConfigureFonts(manager =>
                {
                    try
                    {
                        Log.Debug("Adding embedded font collection...");
                        manager.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:App", UriKind.Absolute),
                            new Uri("avares://AvaloniaVisualBasic/Resources", UriKind.Absolute)));
                        Log.Debug("Embedded font collection added successfully");
                        Console.WriteLine("[BUILD] Embedded font collection added successfully");
                    }
                    catch (Exception fontEx)
                    {
                        Log.Warning(fontEx, "Failed to configure embedded fonts");
                        Console.WriteLine($"[BUILD] Warning: Failed to configure fonts: {fontEx.Message}");
                        // Continue without custom fonts
                    }
                });

                Log.Debug("Font configuration completed");
                Console.WriteLine("[BUILD] Font configuration completed");
            }
            catch (Exception configEx)
            {
                Log.Warning(configEx, "Failed to configure font manager");
                Console.WriteLine($"[BUILD] Warning: Failed to configure font manager: {configEx.Message}");
                // Continue with basic configuration
            }

            Log.Information("Avalonia app builder configured successfully");
            Console.WriteLine("[BUILD] Avalonia app builder configured successfully");
            return builder;
        }
        catch (StackOverflowException soEx)
        {
            Log.Fatal("CRITICAL: Stack overflow in BuildAvaloniaApp");
            Console.WriteLine("[FATAL] CRITICAL: Stack overflow in BuildAvaloniaApp");
            Console.WriteLine($"[FATAL] Stack overflow details: {soEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "CRITICAL: Error building Avalonia app");
            Console.WriteLine($"[FATAL] CRITICAL: Error building Avalonia app: {ex.Message}");
            Console.WriteLine($"[FATAL] Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}
