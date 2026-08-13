using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Informer.App.Services;
using Informer.App.Utilities;
using Informer.App.ViewModels;
using Informer.App.Views;
using Informer.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Informer.App.Localization;

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
    private NativeMenuItem? _aboutMenuItem;
    private NativeMenuItem? _exitMenuItem;
    private TrayIcon? _trayIcon;
    private AboutWindow? _aboutWindow;

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
        _trayIcon = icons?.Count > 0 ? icons[0] : null;
        var menu = _trayIcon?.Menu;
        if (menu is null) return;

        _historyMenuItem = menu.Items.ElementAtOrDefault(0) as NativeMenuItem;
        _settingsMenuItem = menu.Items.ElementAtOrDefault(1) as NativeMenuItem;
        _aboutMenuItem = menu.Items.ElementAtOrDefault(2) as NativeMenuItem;
        _exitMenuItem = menu.Items.ElementAtOrDefault(4) as NativeMenuItem;
    }

    private static void ApplyStartupLanguage()
    {
        LocalizationManager.RescanAvailableLanguages();

        var language = "ru";
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
                settings.Language = language;
                db.SaveChanges();
            }
            else
            {
                language = settings.Language;
            }
        }
        catch
        {
        }

        LocalizationManager.Apply(language);
    }

    private static string DetectSystemLanguage()
    {
        var twoLetterCode = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

        var match = LocalizationManager.AvailableLanguages.FirstOrDefault(l => l.Code == twoLetterCode);
        return match?.Code ?? LocalizationManager.AvailableLanguages.FirstOrDefault()?.Code ?? "ru";
    }

    private void UpdateTrayMenuText(string language)
    {
        UpdateHistoryMenuItemHeader();
        if (_settingsMenuItem is not null) _settingsMenuItem.Header = LocalizationManager.Get("TraySettings");
        if (_aboutMenuItem is not null) _aboutMenuItem.Header = LocalizationManager.Get("TrayAbout"); 
        if (_exitMenuItem is not null) _exitMenuItem.Header = LocalizationManager.Get("TrayExit");
    }

    private void StartNotificationListener()
    {
        _bus = Program.Services.GetRequiredService<NotificationBus>();
        _bus.NotificationReceived += OnNotificationReceivedForToast;
        _bus.NotificationReceived += n => _ = RefreshTrayBadgeAsync();
        _bus.ReadStatusChanged += () => _ = RefreshTrayBadgeAsync();

        _ = RefreshTrayBadgeAsync();
    }

    private static async Task RefreshTrayBadgeAsync()
    {
        try
        {
            using var scope = Program.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Informer.Data.InformerDbContext>();
            var unread = await db.Notifications.CountAsync(n => !n.IsRead);

            var app = (App)Application.Current!;
            Dispatcher.UIThread.Post(() => app.ApplyTrayBadge(unread));
        }
        catch
        {
        }
    }

    private int _lastUnreadCount;

    private void ApplyTrayBadge(int unreadCount)
    {
        _lastUnreadCount = unreadCount;

        if (_trayIcon is not null)
        {
            _trayIcon.Icon = TrayBadgeRenderer.Render(unreadCount);
            _trayIcon.ToolTipText = unreadCount > 0
                ? $"{LocalizationManager.Get("NewMessagesTooltip")} ({unreadCount})"
                : "Информер";
        }

        UpdateHistoryMenuItemHeader();
    }

    private void UpdateHistoryMenuItemHeader()
    {
        if (_historyMenuItem is null) return;

        var baseText = LocalizationManager.Get("TrayHistory");
        _historyMenuItem.Header = _lastUnreadCount > 0 ? $"{baseText} ({_lastUnreadCount})" : baseText;
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

                    var wasVisible = _toastWindow!.IsVisible;
                    _toastQueue!.AddNotification(notification, anchorToIt: !wasVisible);

                    if (!wasVisible)
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
    private void OnAboutMenuClick(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (_aboutWindow is null || !_aboutWindow.IsVisible)
                {
                    _aboutWindow = new AboutWindow
                    {
                        DataContext = new AboutWindowViewModel()
                    };
                    _aboutWindow.Show();
                }
                else
                {
                    _aboutWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                LogToastFailure(ex);
            }
        });
    }

    private void OnExitMenuClick(object? sender, EventArgs e)
    {
        Environment.Exit(0);
    }
}