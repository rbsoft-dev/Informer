using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Informer.App.Utilities;
using Informer.App.ViewModels;
using Informer.Core.Entities;

namespace Informer.App.Views;

public partial class SettingsWindow : Window
{
    private const double ScreenMargin = 20;
    public SettingsWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }
    private void OnDigitsOnlyTextInput(object? sender, TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text) && !e.Text.All(char.IsDigit))
        {
            e.Handled = true;
        }
    }
    private void OnOpened(object? sender, EventArgs e)
    {
        var screen = ScreenPositioning.PickScreen(Screens);
        if (screen is null) return;

        Position = ScreenPositioning.BottomRight(screen, Width, Height, ScreenMargin);
        foreach (var numeric in new[] { RetentionDaysInput, RateLimitMaxRequestsInput, RateLimitWindowSecondsInput, ToastDisplaySecondsInput })
        {
            numeric.AddHandler(InputElement.TextInputEvent, OnDigitsOnlyTextInput, RoutingStrategies.Tunnel);
        }
    }
    private void OnApiKeysPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(ApiKeysList).Properties.IsRightButtonPressed)
        {
            return;
        }

        if (e.Source is Control source)
        {
            var container = source.FindAncestorOfType<ListBoxItem>();
            if (container?.DataContext is ApiKeyEntity keyEntity)
            {
                ApiKeysList.SelectedItem = keyEntity;
            }
        }
    }

    private async void OnCopyKeyClick(object? sender, RoutedEventArgs e)
    {
        if (ApiKeysList.SelectedItem is not ApiKeyEntity key)
        {
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(key.Key);
        }
    }

    private async void OnDeleteKeyClick(object? sender, RoutedEventArgs e)
    {
        if (ApiKeysList.SelectedItem is not ApiKeyEntity key)
        {
            return;
        }

        if (DataContext is SettingsWindowViewModel vm)
        {
            await vm.DeleteApiKeyAsync(key);
        }
    }

    private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
