using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Informer.App.Utilities;
using Informer.App.ViewModels;
using Informer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Informer.App.Views;

public partial class ToastWindow : Window
{
    private DispatcherTimer? _closeTimer;
    private bool _positioned;
    private double _scaling = 1.0;
    private int _anchoredRightX;
    private int _anchoredBottomY;
    private int _lastAppliedHeightPx = -1;

    public ToastWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        LayoutUpdated += OnLayoutUpdated;

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

        _scaling = screen.Scaling;

        var heightDip = Bounds.Height > 0 ? Bounds.Height : MinHeight;
        Position = ScreenPositioning.BottomRight(screen, Width, heightDip, 16);

        var widthPx = (int)(Width * _scaling);
        var heightPx = (int)(heightDip * _scaling);
        _anchoredRightX = Position.X + widthPx;
        _anchoredBottomY = Position.Y + heightPx;
        _lastAppliedHeightPx = heightPx;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (!_positioned) return;

        var heightPx = (int)(Bounds.Height * _scaling);
        if (heightPx == _lastAppliedHeightPx) return;

        _lastAppliedHeightPx = heightPx;
        var widthPx = (int)(Bounds.Width * _scaling);
        Position = new Avalonia.PixelPoint(_anchoredRightX - widthPx, _anchoredBottomY - heightPx);
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

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Hide();

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
}