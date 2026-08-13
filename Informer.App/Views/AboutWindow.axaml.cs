using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Informer.App.Utilities;

namespace Informer.App.Views;

public partial class AboutWindow : Window
{
    private const double ScreenMargin = 20;

    public AboutWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        var screen = ScreenPositioning.PickScreen(Screens);
        if (screen is null) return;

        Position = ScreenPositioning.BottomRight(screen, Width, Height, ScreenMargin);
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnResizeGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control
            && control.Tag is string tag
            && Enum.TryParse<WindowEdge>(tag, out var edge)
            && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginResizeDrag(edge, e);
        }
    }

    private void OnEmailClick(object? sender, PointerPressedEventArgs e) =>
        OpenUrl("mailto:online@rbsoft.ru");

    private void OnTelegramClick(object? sender, PointerPressedEventArgs e) =>
        OpenUrl("https://t.me/rbsoft_official");
    private void OnBrowserClick(object? sender, PointerPressedEventArgs e) =>
        OpenUrl("https://www.rbsoft.ru/");

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
        }
    }
}