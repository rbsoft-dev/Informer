namespace Informer.Core.Entities;

/// <summary>
/// Registered API key allowed to POST notifications. Multiple senders can share the
/// application, each with its own key, so keys can be revoked individually.
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
