using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Serilog;

namespace TicketSalesApp.UI.Avalonia
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting application...");
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args,ShutdownMode.OnExplicitShutdown);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed.");
                throw; // Re-throw the exception after logging it
            }
            finally
            {
                Log.CloseAndFlush(); // Ensure to flush and close the log
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}
