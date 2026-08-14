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
    public static IServiceProvider Services { get; private set; } = null!;

    private static WebApplication? _webApp;

    [STAThread]
    public static void Main(string[] args)
    {
        InstallCrashLogging();

        var totalStopwatch = Stopwatch.StartNew();
        var phaseStopwatch = Stopwatch.StartNew();

        var earlyConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var connectionString = ResolveConnectionString(
            earlyConfig.GetConnectionString("Default") ?? "Data Source=informer.db");
        var port = MigrateAndResolvePort(connectionString, earlyConfig);
        Debug.WriteLine($"[STARTUP] Early migrate + port resolve: {phaseStopwatch.ElapsedMilliseconds} ms");
        phaseStopwatch.Restart();

        var webApp = BuildWebHost(args, connectionString, port, earlyConfig);
        Debug.WriteLine($"[STARTUP] BuildWebHost: {phaseStopwatch.ElapsedMilliseconds} ms");
        phaseStopwatch.Restart();

        _webApp = webApp;
        Services = webApp.Services;

        var kestrelTask = webApp.RunAsync();
        Debug.WriteLine($"[STARTUP] webApp.RunAsync (fire-and-forget call itself): {phaseStopwatch.ElapsedMilliseconds} ms");
        phaseStopwatch.Restart();

        var avaloniaApp = BuildAvaloniaApp();
        Debug.WriteLine($"[STARTUP] BuildAvaloniaApp: {phaseStopwatch.ElapsedMilliseconds} ms");
        phaseStopwatch.Restart();

        Debug.WriteLine($"[STARTUP] TOTAL before StartWithClassicDesktopLifetime: {totalStopwatch.ElapsedMilliseconds} ms");

        try
        {
            avaloniaApp.StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            webApp.StopAsync().GetAwaiter().GetResult();
            kestrelTask.GetAwaiter().GetResult();
        }

        Environment.Exit(0);
    }
    private static int MigrateAndResolvePort(string connectionString, IConfiguration earlyConfig)
    {
        var fallbackPort = earlyConfig.GetValue<int?>("Kestrel:DefaultPort") ?? 4399;

        try
        {
            var options = new DbContextOptionsBuilder<InformerDbContext>()
                .UseSqlite(connectionString)
                .Options;

            using var db = new InformerDbContext(options);
            db.Database.Migrate();

            var settings = db.AppSettings.AsNoTracking().FirstOrDefault();
            return settings is { ListenPort: > 0 } ? settings.ListenPort : fallbackPort;
        }
        catch
        {
            return fallbackPort;
        }
    }

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
        var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
        var text = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{source}]{Environment.NewLine}{ex}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
        File.AppendAllText(logPath, text);
    }

    private static string ResolveConnectionString(string configuredConnectionString)
    {
        const string prefix = "Data Source=";
        if (!configuredConnectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return configuredConnectionString;
        }

        var fileName = configuredConnectionString[prefix.Length..];
        if (Path.IsPathRooted(fileName))
        {
            return configuredConnectionString; 
        }

        var absolutePath = Path.Combine(AppContext.BaseDirectory, fileName);
        return $"{prefix}{absolutePath}";
    }

    private static WebApplication BuildWebHost(string[] args, string connectionString, int port, IConfiguration earlyConfig)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        var bindAddress = earlyConfig.GetValue<string>("Kestrel:BindAddress") ?? "0.0.0.0";
        builder.WebHost.UseUrls($"http://{bindAddress}:{port}");

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