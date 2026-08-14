using System.Text.Json;
using System.Text.Json.Serialization;

namespace Informer.Core.Dto;

/// <summary>
/// Отражает JSON-конверт, формируемый отправителем (например, драйвером 1С):
/// {
///   "header": "...",
///   "description": "...",
///   "ApiKey": "...",
///   "type": "info" | "warning" | "error",
///   "ResponseBody": { ... произвольные данные ... }
/// }
/// "header" — это произвольный идентификатор отправителя (см. заметки по проекту) —
/// сохраняется как есть и используется только для отображения/фильтрации, но никогда
/// для авторизации.
/// "type" необязателен и не зависит от регистра; нераспознанные или отсутствующие
/// значения по умолчанию принимаются как "info" (см. NotificationEndpoints.ParseSeverity).
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