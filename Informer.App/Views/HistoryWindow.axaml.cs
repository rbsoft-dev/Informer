using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Informer.App.Utilities;
using Informer.App.ViewModels;

namespace Informer.App.Views;

public partial class HistoryWindow : Window
{
    private const double ScreenMargin = 20;

    public HistoryWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closed += (_, _) => (DataContext as HistoryWindowViewModel)?.Cleanup();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        var screen = ScreenPositioning.PickScreen(Screens);
        if (screen is null) return;

        Position = ScreenPositioning.BottomRight(screen, Width, Height, ScreenMargin);
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnMaximizeClick(object? sender, RoutedEventArgs e) => ToggleMaximize();

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "🗗" : "🗖";
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void OnJumpToNewestClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not HistoryWindowViewModel vm)
        {
            return;
        }

        if (vm.Notifications.Count > 0)
        {
            HistoryGrid.ScrollIntoView(vm.Notifications[0], null);
        }

        vm.AcknowledgeNewMessages();
    }
    private void OnGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(HistoryGrid).Properties.IsRightButtonPressed)
        {
            return;
        }

        if (e.Source is Control source)
        {
            var row = source.FindAncestorOfType<DataGridRow>();
            if (row?.DataContext is NotificationRowViewModel rowVm)
            {
                HistoryGrid.SelectedItem = rowVm;
            }
        }
    }

    private async void OnCopyClick(object? sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not NotificationRowViewModel row)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(row.Description);
        }
    }

    private async void OnDeleteClick(object? sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not NotificationRowViewModel row)
        {
            return;
        }

        if (DataContext is HistoryWindowViewModel vm)
        {
            await vm.DeleteNotificationAsync(row);
        }
    }
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
}