using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Classic.CommonControls;
using Serilog;
using Serilog.Events;
using System;
using System.Threading.Tasks;

namespace TicketSalesApp.UI.Administration.Avalonia;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.Debug()
                .WriteTo.File("logs/app-.log", 
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information)
                .CreateLogger();

            Log.Information("Starting application...");

            return BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args,ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseMessageBoxSounds()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
