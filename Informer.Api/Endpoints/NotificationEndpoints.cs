using System.Text.Json;
using Informer.Api.Validation;
using Informer.Core.Dto;
using Informer.Core.Entities;
using Informer.Core.Services;
using Informer.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Informer.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/notify", HandleNotify)
           .WithName("PostNotify")
           .Produces(StatusCodes.Status201Created)
           .Produces(StatusCodes.Status400BadRequest)
           .Produces(StatusCodes.Status401Unauthorized)
           .Produces(StatusCodes.Status429TooManyRequests);
    }

    private static async Task<IResult> HandleNotify(
        HttpContext context,
        InformerDbContext db,
        NotificationBus bus,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("NotificationEndpoints");

        string rawBody;
        using (var reader = new StreamReader(context.Request.Body))
        {
            rawBody = await reader.ReadToEndAsync();
        }

        IncomingNotificationDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<IncomingNotificationDto>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Malformed JSON received from {Ip}", context.Connection.RemoteIpAddress);
            return Results.BadRequest(new { error = "Malformed JSON." });
        }

        if (dto is null)
        {
            return Results.BadRequest(new { error = "Empty payload." });
        }

        if (!IncomingNotificationValidator.TryValidate(dto, rawBody, out var validationError))
        {
            return Results.BadRequest(new { error = validationError });
        }

        var entity = new Notification
        {
            Sender = dto.Header.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            ResponseBodyJson = dto.ResponseBody.GetRawText(),
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false,
            Severity = ParseSeverity(dto.Type)
        };

        db.Notifications.Add(entity);
        await db.SaveChangesAsync();

        // Hand off to the UI layer (toast + tray badge) without blocking the HTTP response.
        bus.Publish(entity);

        return Results.Created($"/api/history/{entity.Id}", new { entity.Id });
    }

    /// <summary>
    /// Case-insensitive parse of the incoming "type" field ("info"/"warning"/"error").
    /// Missing or unrecognized values default to Info, so senders that don't know about
    /// this field yet keep behaving exactly as before.
    /// </summary>
    private static NotificationSeverity ParseSeverity(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "warning" or "warn" => NotificationSeverity.Warning,
        "error" or "err" => NotificationSeverity.Error,
        _ => NotificationSeverity.Info
    };
}