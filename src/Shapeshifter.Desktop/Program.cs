using Avalonia;
using Serilog;
using Shapeshifter.Core.Settings;

namespace Shapeshifter.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var paths = AppPaths.CreateDefault();
        paths.EnsureCreated();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information, formatProvider: System.Globalization.CultureInfo.InvariantCulture)
            .WriteTo.File(
                Path.Combine(paths.LogDirectory, "shapeshifter-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                formatProvider: System.Globalization.CultureInfo.InvariantCulture)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        // Started from a download or anywhere else: install for this user and start the installed copy instead.
        var installation = Services.Installation.OnStartup(args, paths);
        if (installation.Exit)
        {
            Log.CloseAndFlush();
            return 0;
        }

        args = installation.Arguments;
        App.Installed = installation;

        using var instance = SingleInstance.TryAcquire(paths.InstanceLockFile);
        if (instance is null)
        {
            Log.Information("Shapeshifter is already running");
            Console.Error.WriteLine("Shapeshifter is already running.");
            Log.CloseAndFlush();
            return 1;
        }

        try
        {
            Log.Information("Starting Shapeshifter {Version} on {Platform} (.NET {Runtime})",
                typeof(Program).Assembly.GetName().Version, Platform.PlatformServices.PlatformName, Environment.Version);
            App.Paths = paths;
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnExplicitShutdown);
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Shapeshifter crashed");
            return 2;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    /// <summary>Used by the Avalonia previewer as well.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new MacOSPlatformOptions { ShowInDock = false })
            .With(new X11PlatformOptions { WmClass = "Shapeshifter" })
            .LogToTrace();
}
