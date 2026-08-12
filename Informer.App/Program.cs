using Avalonia;
using Informer.Api.Endpoints;
using Informer.Api.Middleware;
using Informer.Core.Services;
using Informer.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Informer.App;

public static class Program
{
    /// <summary>
    /// Root DI container for the whole process. Avalonia's App.axaml.cs and every
    /// ViewModel resolve their dependencies (DbContext factory, NotificationBus, ...)
    /// from here, since Avalonia itself has no built-in DI/hosting model.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    private static WebApplication? _webApp;

    [STAThread]
    public static void Main(string[] args)
    {
        InstallCrashLogging();

        // TEMPORARY DIAGNOSTIC: measures each startup phase and prints to the Debug
        // output window (View -> Output -> Show output from: Debug in Visual Studio)
        // so we can see exactly where the 15-second startup delay is coming from.
        // Safe to remove once the slow phase is identified and fixed.
        var totalStopwatch = Stopwatch.StartNew();
        var phaseStopwatch = Stopwatch.StartNew();

        var webApp = BuildWebHost(args);
        Debug.WriteLine($"[STARTUP] BuildWebHost: {phaseStopwatch.ElapsedMilliseconds} ms");
        phaseStopwatch.Restart();

        _webApp = webApp;
        Services = webApp.Services;

        // Apply pending EF Core migrations / create the SQLite file on first run.
        using (var scope = webApp.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();
            db.Database.Migrate();
        }
        Debug.WriteLine($"[STARTUP] Database.Migrate: {phaseStopwatch.ElapsedMilliseconds} ms");
        phaseStopwatch.Restart();

        // Kestrel runs on its own async machinery in the background; it does not need
        // to own the thread that calls it, so we fire it and immediately move on to
        // start the Avalonia UI loop on this (the process main) thread.
        var kestrelTask = webApp.RunAsync();
        Debug.WriteLine($"[STARTUP] webApp.RunAsync (fire-and-forget call itself): {phaseStopwatch.ElapsedMilliseconds} ms");
        phaseStopwatch.Restart();

        var avaloniaApp = BuildAvaloniaApp();
        Debug.WriteLine($"[STARTUP] BuildAvaloniaApp: {phaseStopwatch.ElapsedMilliseconds} ms");
        phaseStopwatch.Restart();

        Debug.WriteLine($"[STARTUP] TOTAL before StartWithClassicDesktopLifetime: {totalStopwatch.ElapsedMilliseconds} ms");

        try
        {
            // Everything up to here should be fast. If the delay is actually INSIDE
            // StartWithClassicDesktopLifetime (Avalonia's own platform/tray init), the
            // "TOTAL before" line above will be small and the real 15s gap will be
            // between that line and the tray icon actually appearing on screen.
            avaloniaApp.StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // UI loop exited (tray "Exit" clicked) -> shut Kestrel down gracefully too.
            webApp.StopAsync().GetAwaiter().GetResult();
            kestrelTask.GetAwaiter().GetResult();
        }

        // Safety net: StartWithClassicDesktopLifetime() returning is SUPPOSED to mean the
        // whole process is done and .NET should exit naturally once every thread finishes.
        // In practice, tray apps can leave a background thread alive (a stray
        // DispatcherTimer, the native tray-icon handle, an ASP.NET Core internal worker
        // that didn't unwind in time) which silently keeps the process listed in Task
        // Manager forever, even though the window/tray icon are gone. Explicitly forcing
        // termination here guarantees the process always fully disappears on exit.
        Environment.Exit(0);
    }

    /// <summary>
    /// Diagnostic: writes any exception that would otherwise silently kill the whole
    /// process (unhandled exceptions on ANY thread, and unobserved exceptions from
    /// fire-and-forget async void methods / background Tasks) to crash.log next to the
    /// exe, with a timestamp and the full exception including inner exceptions and stack
    /// trace. Without this, an exception thrown from an async void event handler (used
    /// throughout the toast/tray code) terminates the process with essentially no trace
    /// visible to the user — this is almost certainly the cause of "the app just closes".
    /// </summary>
    private static void InstallCrashLogging()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogCrash("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    private static void LogCrash(string source, Exception? ex)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            var text = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{source}]{Environment.NewLine}{ex}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
            File.AppendAllText(logPath, text);
        }
        catch
        {
            // If we can't even write the crash log, there's nothing more we can do here.
        }
    }

    /// <summary>
    /// SQLite resolves a relative "Data Source" path against the process's CURRENT
    /// WORKING DIRECTORY, not against the .exe's own folder — those two are usually the
    /// same when launching via F5 in Visual Studio, but can differ (double-clicking the
    /// .exe from a different folder, custom launch configs, etc.), silently creating a
    /// second, empty-looking database file in a different location. Anchoring the path
    /// to AppContext.BaseDirectory (always the folder the running assembly lives in)
    /// guarantees the database always lands in exactly one, predictable place.
    /// </summary>
    private static string ResolveConnectionString(string configuredConnectionString)
    {
        const string prefix = "Data Source=";
        if (!configuredConnectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return configuredConnectionString; // not a simple SQLite file path — leave as-is
        }

        var fileName = configuredConnectionString[prefix.Length..];
        if (Path.IsPathRooted(fileName))
        {
            return configuredConnectionString; // already absolute — respect it as configured
        }

        var absolutePath = Path.Combine(AppContext.BaseDirectory, fileName);
        return $"{prefix}{absolutePath}";
    }

    private static WebApplication BuildWebHost(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        var configuration = builder.Configuration;
        var configuredConnectionString = configuration.GetConnectionString("Default") ?? "Data Source=informer.db";
        var connectionString = ResolveConnectionString(configuredConnectionString);
        var port = configuration.GetValue<int?>("Kestrel:DefaultPort") ?? 5005;

        // Bind to loopback only by default — this is a local notification receiver, not
        // a public API. Change to 0.0.0.0 in appsettings.json only if senders live on
        // other machines on the LAN, and rely on the API-key + rate-limit middleware.
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        builder.Services.AddDbContext<InformerDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddSingleton<NotificationBus>();

        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();

        var app = builder.Build();

        app.UseMiddleware<RateLimitMiddleware>();
        app.UseMiddleware<ApiKeyMiddleware>();

        app.MapNotificationEndpoints();
        app.MapHistoryEndpoints();
        app.MapSettingsEndpoints();

        return app;
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}