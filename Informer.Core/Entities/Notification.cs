namespace Informer.Core.Entities;

/// <summary>
/// Отдельное сохранённое уведомление, полученное от внешнего отправителя (например,
/// драйвера 1С). "Sender" соответствует произвольному значению "header" из входящего
/// JSON-конверта и НЕ является внешним ключом — оно хранится и фильтруется как обычная
/// строка, поскольку значение формируется динамически на стороне отправителя и не может
/// быть заранее перечислено.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    /// <summary>
    /// Произвольный идентификатор отправителя, взятый из поля "header" входящего JSON.
    /// Пример: "1C:Session:MainBase:ivanov".
    /// </summary>
    public string Sender { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ResponseBodyJson { get; set; } = string.Empty;

    public string? RemoteIpAddress { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
}
