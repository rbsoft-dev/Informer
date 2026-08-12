using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Informer.App.Localization;
using Informer.Core.Entities;
using Informer.Core.Services;
using Informer.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Informer.App.ViewModels;

public partial class ToastQueueViewModel : ObservableObject
{
    private const int MaxItems = 50;

    private readonly List<ToastNotificationViewModel> _items = new();
    private int _currentIndex = -1;

    [ObservableProperty] private string _sender = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTime _createdAtLocal;
    [ObservableProperty] private IBrush _borderColor = Brushes.Gray;
    [ObservableProperty] private string _positionLabel = string.Empty;

    [NotifyPropertyChangedFor(nameof(NewMessagesTooltip))]
    [ObservableProperty] private bool _hasNewMessages;

    [NotifyPropertyChangedFor(nameof(NewMessagesTooltip))]
    [ObservableProperty] private int _newMessagesCount;

    public string NewMessagesTooltip => $"{LocalizationManager.Get("NewMessagesTooltip")} ({NewMessagesCount})";

    public bool HasPrevious => _currentIndex > 0;
    public bool HasNext => _currentIndex >= 0 && _currentIndex < _items.Count - 1;

    public void AddNotification(Notification notification, bool anchorToIt)
    {
        _items.Add(new ToastNotificationViewModel(notification));
        if (_items.Count > MaxItems)
        {
            _items.RemoveAt(0);
            if (_currentIndex > 0)
            {
                _currentIndex--;
            }
        }

        if (anchorToIt || _currentIndex == -1)
        {
            _currentIndex = _items.Count - 1;
        }

        UpdateNewMessagesBadge();
        RefreshDisplayed();
    }

    [RelayCommand(CanExecute = nameof(HasNext))]
    private void Next()
    {
        if (HasNext)
        {
            _currentIndex++;
            UpdateNewMessagesBadge();
            RefreshDisplayed();
        }
    }

    [RelayCommand(CanExecute = nameof(HasPrevious))]
    private void Previous()
    {
        if (HasPrevious)
        {
            _currentIndex--;
            UpdateNewMessagesBadge();
            RefreshDisplayed();
        }
    }

    [RelayCommand]
    private void JumpToLatest()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _currentIndex = _items.Count - 1;
        UpdateNewMessagesBadge();
        RefreshDisplayed();
    }

    private void UpdateNewMessagesBadge()
    {
        NewMessagesCount = Math.Max(0, _items.Count - 1 - _currentIndex);
        HasNewMessages = NewMessagesCount > 0;
    }

    private void RefreshDisplayed()
    {
        if (_currentIndex < 0 || _currentIndex >= _items.Count)
        {
            return;
        }

        var current = _items[_currentIndex];
        Sender = current.Sender;
        Description = current.Description;
        CreatedAtLocal = current.CreatedAtLocal;
        BorderColor = current.BorderColor;
        PositionLabel = $"{_currentIndex + 1} из {_items.Count}";

        NextCommand.NotifyCanExecuteChanged();
        PreviousCommand.NotifyCanExecuteChanged();

        _ = MarkAsReadAsync(current.Id);
    }

    private static async Task MarkAsReadAsync(int notificationId)
    {
        using var scope = Informer.App.Program.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();
        var entity = await db.Notifications.FindAsync(notificationId);
        if (entity is not null && !entity.IsRead)
        {
            entity.IsRead = true;
            await db.SaveChangesAsync();

            scope.ServiceProvider.GetRequiredService<NotificationBus>().PublishReadStatusChanged();
        }
    }
}