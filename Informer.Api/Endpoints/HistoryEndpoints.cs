using Informer.Core.Dto;
using Informer.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Informer.Api.Endpoints;

/// <summary>
/// Read-only endpoints used by the Avalonia history window. Intentionally NOT protected by
/// the API-key middleware (that middleware only guards POST /api/notify) — these are meant
/// to be called by the local UI process (Kestrel bound to localhost), not external senders.
/// If remote/browser access to history is ever required, add explicit auth here first.
/// </summary>
public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/history", GetHistory).WithName("GetHistory");
        app.MapGet("/api/history/senders", GetDistinctSenders).WithName("GetHistorySenders");
        app.MapPost("/api/history/{id:int}/read", MarkRead).WithName("MarkNotificationRead");
    }

    private static async Task<IResult> GetHistory(
        InformerDbContext db,
        string? sender,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page = 1,
        int pageSize = 100)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 500 ? 100 : pageSize;

        var query = db.Notifications.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(sender))
        {
            query = query.Where(n => n.Sender == sender);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(n => n.CreatedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(n => n.CreatedAtUtc <= toUtc.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationListItemDto(
                n.Id, n.Sender, n.Description, n.ResponseBodyJson, n.CreatedAtUtc, n.IsRead, n.Severity))
            .ToListAsync();

        return Results.Ok(new { total, page, pageSize, items });
    }

    private static async Task<IResult> GetDistinctSenders(InformerDbContext db)
    {
        var senders = await db.Notifications.AsNoTracking()
            .Select(n => n.Sender)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        return Results.Ok(senders);
    }

    private static async Task<IResult> MarkRead(int id, InformerDbContext db)
    {
        var entity = await db.Notifications.FindAsync(id);
        if (entity is null)
        {
            return Results.NotFound();
        }

        entity.IsRead = true;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
