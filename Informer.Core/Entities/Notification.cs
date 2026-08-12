namespace Informer.Core.Entities;

/// <summary>
/// A single stored notification received from an external sender (e.g. 1C driver).
/// "Sender" corresponds to the free-form "header" value from the incoming JSON envelope
/// and is NOT a foreign key — it is stored and filtered as a raw string, because the
/// value is generated dynamically by the sending side and cannot be enumerated in advance.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>
    /// Free-form sender identifier taken from the "header" field of the incoming JSON.
    /// Example: "1C:Session:MainBase:ivanov".
    /// </summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary>Human readable description / title shown in the toast and history list.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Raw JSON of the "ResponseBody" object, stored as-is for later inspection.</summary>
    public string ResponseBodyJson { get; set; } = string.Empty;

    /// <summary>Remote IP address the notification was received from (audit / anti-spam).</summary>
    public string? RemoteIpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
}
