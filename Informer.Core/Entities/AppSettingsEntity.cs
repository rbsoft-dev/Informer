namespace Informer.Core.Entities;

/// <summary>
/// Таблица из одной строки, содержащая настраиваемые во время выполнения параметры
/// приложения. Доступ всегда осуществляется по Id == 1 (см. seed в InformerDbContext).
/// </summary>
public class AppSettingsEntity
{
    public int Id { get; set; }
    public int RetentionDays { get; set; } = 30;
    public bool RequireApiKey { get; set; } = true;
    public int ToastDisplaySeconds { get; set; } = 8;
    public int RateLimitMaxRequests { get; set; } = 20;
    public int RateLimitWindowSeconds { get; set; } = 10;
    public int ListenPort { get; set; } = 4399;
    public bool ShowInfoToasts { get; set; } = true;
    public bool ShowWarningToasts { get; set; } = true;
    public bool ShowErrorToasts { get; set; } = true;
    public string Language { get; set; } = "";
}
