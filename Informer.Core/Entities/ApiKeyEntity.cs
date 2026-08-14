namespace Informer.Core.Entities;

/// <summary>
/// Зарегистрированный API-ключ, которому разрешено отправлять уведомления через POST.
/// Приложение может использоваться несколькими отправителями, каждый со своим ключом,
/// поэтому ключи можно отзывать по отдельности.
/// </summary>
public class ApiKeyEntity
{
    public int Id { get; set; }

    /// <summary>The secret key value compared against the X-Api-Key header.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Friendly label, e.g. "1C Main Base".</summary>
    public string Label { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
