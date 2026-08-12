using Informer.Data;
using Microsoft.EntityFrameworkCore;

namespace Informer.Api.Middleware;

/// <summary>
/// Enforces API-key authorization on POST /api/notify only, and only when the
/// "RequireApiKey" checkbox (AppSettingsEntity.RequireApiKey) is enabled.
/// The key is expected in the "X-Api-Key" request header. Checking it here (before the
/// request body is even read) means an unauthorized caller never triggers a DB write or
/// JSON deserialization — the earliest possible rejection point.
/// </summary>
public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, InformerDbContext db)
    {
        // Only guard the ingestion endpoint; history/settings are local-UI-only concerns
        // handled separately (they are not exposed for remote consumption by design).
        if (!IsProtectedEndpoint(context))
        {
            await _next(context);
            return;
        }

        var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync();
        var requireApiKey = settings?.RequireApiKey ?? true;

        if (!requireApiKey)
        {
            await _next(context);
            return;
        }

        var providedKey = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            ? headerValue.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(providedKey))
        {
            _logger.LogWarning("Rejected request from {Ip}: missing {Header}", context.Connection.RemoteIpAddress, HeaderName);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key required." });
            return;
        }

        var isValid = await db.ApiKeys.AsNoTracking()
            .AnyAsync(k => k.Key == providedKey && k.IsActive);

        if (!isValid)
        {
            _logger.LogWarning("Rejected request from {Ip}: invalid API key", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive API key." });
            return;
        }

        await _next(context);
    }

    private static bool IsProtectedEndpoint(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method) &&
        context.Request.Path.StartsWithSegments("/api/notify", StringComparison.OrdinalIgnoreCase);
}
