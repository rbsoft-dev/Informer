using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Informer.App.Utilities;
using Informer.App.ViewModels;
using Informer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Informer.App.Views;

public partial class ToastWindow : Window
{
    private DispatcherTimer? _closeTimer;
    private bool _positioned;

    public ToastWindow()
    {
        InitializeComponent();
        Opened += OnOpened;

        DataContextChanged += (_, _) =>
        {
            if (DataContext is ToastQueueViewModel queue)
            {
                queue.PropertyChanged += (_, _) => RestartAutoCloseTimer();
            }
        };
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_positioned) return;
        _positioned = true;

        var screen = ScreenPositioning.PickScreen(Screens);
        if (screen is null) return;

        var heightDip = Bounds.Height > 0 ? Bounds.Height : MinHeight;
        Position = ScreenPositioning.BottomRight(screen, Width, heightDip, 16);
    }

    private void RestartAutoCloseTimer() => _ = RestartAutoCloseTimerAsync();

    private async Task RestartAutoCloseTimerAsync()
    {
        _closeTimer?.Stop();

        var seconds = await GetToastDisplaySecondsAsync();
        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer?.Stop();
            Hide();
        };
        _closeTimer.Start();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => DismissAndHide();


    private void DismissAndHide()
    {
        _closeTimer?.Stop();

        if (DataContext is ToastQueueViewModel queue && queue.CurrentNotificationId is int id)
        {
            _ = MarkAsReadAsync(id);
        }

        Hide();
    }

    private static async Task<int> GetToastDisplaySecondsAsync()
    {
        try
        {
            using var scope = Program.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();
            var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync();
            return settings?.ToastDisplaySeconds ?? 8;
        }
        catch
        {
            return 8;
        }
    }

    private static async Task MarkAsReadAsync(int notificationId)
    {
        using var scope = Program.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();
        var entity = await db.Notifications.FindAsync(notificationId);
        if (entity is not null)
        {
            entity.IsRead = true;
            await db.SaveChangesAsync();
        }
    }
}