using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Informer.App.Localization;
using Informer.App.Services;
using Informer.App.ViewModels;
using Informer.App.Views;
using Informer.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Informer.App;

public partial class App : Application
{
    private HistoryWindow? _historyWindow;
    private SettingsWindow? _settingsWindow;
    private ToastWindow? _toastWindow;
    private ToastQueueViewModel? _toastQueue;
    private readonly CancellationTokenSource _cts = new();
    private BackgroundCleanupService? _cleanupService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private NotificationBus? _bus;

    private NativeMenuItem? _historyMenuItem;
    private NativeMenuItem? _settingsMenuItem;
    private NativeMenuItem? _exitMenuItem;

    public override void OnFrameworkInitializationCompleted()
    {
        ResolveTrayMenuItems();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) =>
            {
                _cts.Cancel();
                if (_bus is not null)
                {
                    _bus.NotificationReceived -= OnNotificationReceivedForToast;
                }
            };
        }

        LocalizationManager.LanguageChanged += UpdateTrayMenuText;
        ApplyStartupLanguage();

        StartNotificationListener();
        StartBackgroundCleanup();

        base.OnFrameworkInitializationCompleted();
    }

    private void ResolveTrayMenuItems()
    {
        var icons = TrayIcon.GetIcons(this);
        var menu = icons?.Count > 0 ? icons[0].Menu : null;
        if (menu is null) return;

        _historyMenuItem = menu.Items.ElementAtOrDefault(0) as NativeMenuItem;
        _settingsMenuItem = menu.Items.ElementAtOrDefault(1) as NativeMenuItem;
        _exitMenuItem = menu.Items.ElementAtOrDefault(3) as NativeMenuItem;
    }

    private static void ApplyStartupLanguage()
    {
        var language = AppLanguage.Russian;
        try
        {
            using var scope = Program.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Informer.Data.InformerDbContext>();
            var settings = db.AppSettings.FirstOrDefault();

            if (settings is null)
            {
                LocalizationManager.Apply(language);
                return;
            }

            if (string.IsNullOrEmpty(settings.Language))
            {
                language = DetectSystemLanguage();
                settings.Language = language == AppLanguage.English ? "en" : "ru";
                db.SaveChanges();
            }
            else
            {
                language = settings.Language == "en" ? AppLanguage.English : AppLanguage.Russian;
            }
        }
        catch
        {
        }

        LocalizationManager.Apply(language);
    }

    private static AppLanguage DetectSystemLanguage()
    {
        var twoLetterCode = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return twoLetterCode.Equals("ru", StringComparison.OrdinalIgnoreCase)
            ? AppLanguage.Russian
            : AppLanguage.English;
    }

    private void UpdateTrayMenuText(AppLanguage language)
    {
        if (_historyMenuItem is not null) _historyMenuItem.Header = LocalizationManager.Get("TrayHistory");
        if (_settingsMenuItem is not null) _settingsMenuItem.Header = LocalizationManager.Get("TraySettings");
        if (_exitMenuItem is not null) _exitMenuItem.Header = LocalizationManager.Get("TrayExit");
    }

    private void StartNotificationListener()
    {
        _bus = Program.Services.GetRequiredService<NotificationBus>();
        _bus.NotificationReceived += OnNotificationReceivedForToast;
    }

    private async void OnNotificationReceivedForToast(Informer.Core.Entities.Notification notification)
    {
        try
        {
            if (!await ShouldShowToastAsync(notification.Severity))
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    EnsureToastWindow();
                    _toastQueue!.AddNotification(notification);

                    if (!_toastWindow!.IsVisible)
                    {
                        _toastWindow.Show();
                    }
                    else
                    {
                        _toastWindow.Activate();
                    }
                }
                catch (Exception ex)
                {
                    LogToastFailure(ex);
                }
            });
        }
        catch (Exception ex)
        {
            LogToastFailure(ex);
        }
    }

    private void EnsureToastWindow()
    {
        if (_toastWindow is not null)
        {
            return;
        }

        _toastQueue = new ToastQueueViewModel();
        _toastWindow = new ToastWindow { DataContext = _toastQueue };
    }

    private static void LogToastFailure(Exception ex)
    {
        try
        {
            var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log");
            var text = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ToastDisplayFailure]{Environment.NewLine}{ex}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
            System.IO.File.AppendAllText(logPath, text);
        }
        catch
        {
        }
    }

    private static async Task<bool> ShouldShowToastAsync(Informer.Core.Entities.NotificationSeverity severity)
    {
        try
        {
            using var scope = Program.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Informer.Data.InformerDbContext>();
            var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync();
            if (settings is null) return true;

            return severity switch
            {
                Informer.Core.Entities.NotificationSeverity.Warning => settings.ShowWarningToasts,
                Informer.Core.Entities.NotificationSeverity.Error => settings.ShowErrorToasts,
                _ => settings.ShowInfoToasts
            };
        }
        catch
        {
            return true;
        }
    }

    private void StartBackgroundCleanup()
    {
        _cleanupService = new BackgroundCleanupService(Program.Services);
        _ = _cleanupService.RunAsync(_cts.Token);
    }

    private void OnHistoryMenuClick(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_historyWindow is null || !_historyWindow.IsVisible)
            {
                _historyWindow = new HistoryWindow
                {
                    DataContext = new HistoryWindowViewModel()
                };
                _historyWindow.Show();
            }
            else
            {
                _historyWindow.Activate();
            }
        });
    }

    private void OnSettingsMenuClick(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_settingsWindow is null || !_settingsWindow.IsVisible)
            {
                _settingsWindow = new SettingsWindow
                {
                    DataContext = new SettingsWindowViewModel()
                };
                _settingsWindow.Show();
            }
            else
            {
                _settingsWindow.Activate();
            }
        });
    }

    private void OnExitMenuClick(object? sender, EventArgs e)
    {
        Environment.Exit(0);
    }
}