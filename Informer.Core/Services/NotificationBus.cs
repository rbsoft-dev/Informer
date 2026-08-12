using System;
using Informer.Core.Entities;

namespace Informer.Core.Services;

public sealed class NotificationBus
{
    public event Action<Notification>? NotificationReceived;

    public void Publish(Notification notification) => NotificationReceived?.Invoke(notification);
}