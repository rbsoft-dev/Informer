using System.Text.Json;
using System.Text.Json.Serialization;

namespace Informer.Core.Dto;

/// <summary>
/// Mirrors the JSON envelope produced by the sender (e.g. the 1C driver):
/// {
///   "header": "...",
///   "description": "...",
///   "ApiKey": "...",
///   "type": "info" | "warning" | "error",
///   "ResponseBody": { ... arbitrary ... }
/// }
/// "header" is the free-form sender identifier (see project notes) — it is stored
/// verbatim and used only for display/filtering, never for authorization.
/// "type" is optional and case-insensitive; unrecognized or missing values default to
/// "info" (see NotificationEndpoints.ParseSeverity).
/// </summary>
public class IncomingNotificationDto
{
    [JsonPropertyName("header")]
    public string Header { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("ApiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("ResponseBody")]
    public JsonElement ResponseBody { get; set; }
}

/// <summary>Lightweight projection returned by GET /api/history.</summary>
public record NotificationListItemDto(
    int Id,
    string Sender,
    string Description,
    string ResponseBodyJson,
    DateTime CreatedAtUtc,
    bool IsRead,
    Entities.NotificationSeverity Severity);