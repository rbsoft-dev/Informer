using Informer.Core.Entities;
using Informer.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Informer.Api.Endpoints;

public record UpdateSettingsRequest(
    int RetentionDays,
    bool RequireApiKey,
    int ToastDisplaySeconds,
    int RateLimitMaxRequests,
    int RateLimitWindowSeconds);

public record CreateApiKeyRequest(string Label);

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings", GetSettings).WithName("GetSettings");
        app.MapPut("/api/settings", UpdateSettings).WithName("UpdateSettings");

        app.MapGet("/api/apikeys", GetApiKeys).WithName("GetApiKeys");
        app.MapPost("/api/apikeys", CreateApiKey).WithName("CreateApiKey");
        app.MapDelete("/api/apikeys/{id:int}", RevokeApiKey).WithName("RevokeApiKey");
    }

    private static async Task<IResult> GetSettings(InformerDbContext db)
    {
        var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync();
        return settings is null ? Results.NotFound() : Results.Ok(settings);
    }

    private static async Task<IResult> UpdateSettings(UpdateSettingsRequest request, InformerDbContext db)
    {
        if (request.RetentionDays < 1)
        {
            return Results.BadRequest(new { error = "RetentionDays must be at least 1." });
        }

        var settings = await db.AppSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            return Results.NotFound();
        }

        settings.RetentionDays = request.RetentionDays;
        settings.RequireApiKey = request.RequireApiKey;
        settings.ToastDisplaySeconds = request.ToastDisplaySeconds;
        settings.RateLimitMaxRequests = request.RateLimitMaxRequests;
        settings.RateLimitWindowSeconds = request.RateLimitWindowSeconds;

        await db.SaveChangesAsync();
        return Results.Ok(settings);
    }

    private static async Task<IResult> GetApiKeys(InformerDbContext db)
    {
        var keys = await db.ApiKeys.AsNoTracking()
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToListAsync();
        return Results.Ok(keys);
    }

    private static async Task<IResult> CreateApiKey(CreateApiKeyRequest request, InformerDbContext db)
    {
        var entity = new ApiKeyEntity
        {
            Key = Guid.NewGuid().ToString("N"),
            Label = string.IsNullOrWhiteSpace(request.Label) ? "Unnamed" : request.Label.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync();

        return Results.Created($"/api/apikeys/{entity.Id}", entity);
    }

    private static async Task<IResult> RevokeApiKey(int id, InformerDbContext db)
    {
        var entity = await db.ApiKeys.FindAsync(id);
        if (entity is null)
        {
            return Results.NotFound();
        }

        entity.IsActive = false;
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
}
