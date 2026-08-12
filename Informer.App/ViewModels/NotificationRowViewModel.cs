using System;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Informer.Core.Dto;
using Informer.Core.Entities;
using Informer.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Informer.App.ViewModels;

public partial class NotificationRowViewModel : ObservableObject
{
    public int Id { get; }
    public string Sender { get; }
    public string Description { get; }
    public string ResponseBodyJson { get; }
    public DateTime CreatedAtUtc { get; }
    public NotificationSeverity Severity { get; }

    public string SeverityLabel => Severity switch
    {
        NotificationSeverity.Warning => "Предупреждение",
        NotificationSeverity.Error => "Ошибка",
        _ => "Сообщение"
    };

    public IBrush SeverityBrush { get; }

    public DateTime CreatedAtLocal => DateTime.SpecifyKind(CreatedAtUtc, DateTimeKind.Utc).ToLocalTime();

    [ObservableProperty]
    private bool _isRead;

    public NotificationRowViewModel(NotificationListItemDto dto)
    {
        Id = dto.Id;
        Sender = dto.Sender;
        Description = dto.Description;
        ResponseBodyJson = dto.ResponseBodyJson;
        CreatedAtUtc = dto.CreatedAtUtc;
        Severity = dto.Severity;
        SeverityBrush = ResolveSeverityBrush(dto.Severity);
        _isRead = dto.IsRead;
    }

    private static IBrush ResolveSeverityBrush(NotificationSeverity severity)
    {
        var resourceKey = severity switch
        {
            NotificationSeverity.Error => "FlyoutDangerBrush",
            NotificationSeverity.Warning => "FlyoutWarningBrush",
            _ => "FlyoutSuccessBrush"
        };

        if (Application.Current?.Resources.TryGetResource(resourceKey, null, out var resource) == true
            && resource is IBrush brush)
        {
            return brush;
        }

        return Brushes.Gray;
    }

    partial void OnIsReadChanged(bool value) => _ = PersistAsync(value);

    private async System.Threading.Tasks.Task PersistAsync(bool value)
    {
        using var scope = Informer.App.Program.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InformerDbContext>();
        var entity = await db.Notifications.FindAsync(Id);
        if (entity is not null)
        {
            entity.IsRead = value;
            await db.SaveChangesAsync();
        }
    }
}