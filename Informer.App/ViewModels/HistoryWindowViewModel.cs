using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Informer.App.Localization;
using Informer.Core.Dto;
using Informer.Core.Entities;
using Informer.Core.Services;
using Informer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Informer.App.ViewModels;

public partial class HistoryWindowViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationBus _bus;

    [ObservableProperty]
    private ObservableCollection<NotificationRowViewModel> _notifications = new();

    [ObservableProperty]
    private ObservableCollection<string> _senders = new() { AllSendersLabel };

    [ObservableProperty]
    private string _selectedSender = AllSendersLabel;

    [ObservableProperty]
    private bool _isLoading;

    [NotifyPropertyChangedFor(nameof(NewMessagesTooltip))]
    [ObservableProperty]
    private bool _hasNewMessages;

    [NotifyPropertyChangedFor(nameof(NewMessagesTooltip))]
    [ObservableProperty]
    private int _newMessagesCount;

    public string NewMessagesTooltip => $"{LocalizationManager.Get("NewMessagesTooltip")} ({NewMessagesCount})";

    private static string AllSendersLabel => LocalizationManager.Get("AllSendersOption");

    public HistoryWindowViewModel()
    {
        _scopeFactory = Informer.App.Program.Services.GetRequiredService<IServiceScopeFactory>();
        _bus = Informer.App.Program.Services.GetRequiredService<NotificationBus>();

        _bus.NotificationReceived += OnNotificationReceived;
        LocalizationManager.LanguageChanged += OnLanguageChanged;

        _ = LoadAsync();
    }

    public void Cleanup()
    {
        _bus.NotificationReceived -= OnNotificationReceived;
        LocalizationManager.LanguageChanged -= OnLanguageChanged;
    }

    private void OnNotificationReceived(Notification notification)
    {
        Dispatcher.UIThread.Post(() =>
        {
            NewMessagesCount++;
            HasNewMessages = true;
            _ = LoadAsync();
        });
    }

    public void AcknowledgeNewMessages()
    {
        HasNewMessages = false;
        NewMessagesCount = 0;
    }

    private void OnLanguageChanged(AppLanguage language)
    {
        var wasShowingAll = SelectedSender == AllSendersLabel || Senders.Count == 0 || SelectedSender == Senders.FirstOrDefault();
        Dispatcher.UIThread.Post(() =>
        {
            if (wasShowingAll)
            {
                SelectedSender = AllSendersLabel;
            }

            _ = LoadAsync();
        });
    }

    partial void OnSelectedSenderChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    public async Task DeleteNotificationAsync(NotificationRowViewModel row)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

        var entity = await db.Notifications.FindAsync(row.Id);
        if (entity is not null)
        {
            db.Notifications.Remove(entity);
            await db.SaveChangesAsync();
            _bus.PublishReadStatusChanged();
        }

        Notifications.Remove(row);
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();

            var senderList = await db.Notifications.AsNoTracking()
                .Select(n => n.Sender)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            SyncSenderList(senderList);

            var query = db.Notifications.AsNoTracking().AsQueryable();
            if (SelectedSender != AllSendersLabel)
            {
                query = query.Where(n => n.Sender == SelectedSender);
            }

            var items = await query
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(500)
                .Select(n => new NotificationListItemDto(
                    n.Id, n.Sender, n.Description, n.ResponseBodyJson, n.CreatedAtUtc, n.IsRead, n.Severity))
                .ToListAsync();

            Notifications = new ObservableCollection<NotificationRowViewModel>(
                items.Select(dto => new NotificationRowViewModel(dto)));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SyncSenderList(System.Collections.Generic.List<string> senderList)
    {
        var desired = new[] { AllSendersLabel }.Concat(senderList).ToList();

        for (var i = Senders.Count - 1; i >= 0; i--)
        {
            if (!desired.Contains(Senders[i]))
            {
                Senders.RemoveAt(i);
            }
        }

        for (var i = 0; i < desired.Count; i++)
        {
            if (i >= Senders.Count || Senders[i] != desired[i])
            {
                Senders.Insert(i, desired[i]);
            }
        }
    }
}