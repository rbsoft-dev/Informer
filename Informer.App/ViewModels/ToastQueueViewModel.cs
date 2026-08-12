using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Informer.Core.Entities;
using System;
using System.Collections.Generic;

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

    public bool HasPrevious => _currentIndex > 0;
    public bool HasNext => _currentIndex >= 0 && _currentIndex < _items.Count - 1;

    public int? CurrentNotificationId => _currentIndex >= 0 && _currentIndex < _items.Count
        ? _items[_currentIndex].Id
        : null;

    public void AddNotification(Notification notification)
    {
        _items.Add(new ToastNotificationViewModel(notification));
        if (_items.Count > MaxItems)
        {
            _items.RemoveAt(0);
        }

        _currentIndex = _items.Count - 1;
        RefreshDisplayed();
    }

    [RelayCommand(CanExecute = nameof(HasNext))]
    private void Next()
    {
        if (HasNext)
        {
            _currentIndex++;
            RefreshDisplayed();
        }
    }

    [RelayCommand(CanExecute = nameof(HasPrevious))]
    private void Previous()
    {
        if (HasPrevious)
        {
            _currentIndex--;
            RefreshDisplayed();
        }
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
    }
}