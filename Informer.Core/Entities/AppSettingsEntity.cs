namespace Informer.Core.Entities;

/// <summary>
/// Таблица из одной строки, содержащая настраиваемые во время выполнения параметры
/// приложения. Доступ всегда осуществляется по Id == 1 (см. seed в InformerDbContext).
/// </summary>
public class AppSettingsEntity
{
    public int Id { get; set; }

    /// <summary>Notifications older than this many days are purged by the cleanup service.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>If true, POST /api/notify requires a valid, active API key.</summary>
    public bool RequireApiKey { get; set; } = true;

    /// <summary>How many seconds a toast stays on screen before auto-closing.</summary>
    public int ToastDisplaySeconds { get; set; } = 8;

    /// <summary>Max requests allowed per RateLimitWindowSeconds per IP (anti-spam, req. 7).</summary>
    public int RateLimitMaxRequests { get; set; } = 20;

    public int RateLimitWindowSeconds { get; set; } = 10;

    /// <summary>TCP port Kestrel listens on for the notify API.</summary>
    public int ListenPort { get; set; } = 4399;
    public bool ShowInfoToasts { get; set; } = true;
    public bool ShowWarningToasts { get; set; } = true;
    public bool ShowErrorToasts { get; set; } = true;
    public string Language { get; set; } = "";
}
