using System;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Informer.Core.Entities;

namespace Informer.App.ViewModels;

public partial class ToastNotificationViewModel : ObservableObject
{
    public int Id { get; }
    public string Sender { get; }
    public string Description { get; }
    public string ResponseBodyJson { get; }
    public DateTime CreatedAtLocal { get; }
    public NotificationSeverity Severity { get; }

    public IBrush BorderColor { get; }

    public ToastNotificationViewModel(Notification notification)
    {
        Id = notification.Id;
        Sender = notification.Sender;
        Description = notification.Description;
        ResponseBodyJson = notification.ResponseBodyJson;
        CreatedAtLocal = notification.CreatedAtUtc.ToLocalTime();
        Severity = notification.Severity;
        BorderColor = ResolveBorderBrush(Severity);
    }

    private static IBrush ResolveBorderBrush(NotificationSeverity severity)
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
}