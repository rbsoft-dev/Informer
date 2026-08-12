using System.Collections.Concurrent;
using Informer.Data;
using Microsoft.EntityFrameworkCore;

namespace Informer.Api.Middleware;

/// <summary>
/// Basic per-IP fixed-window rate limiter guarding /api/notify from being flooded
/// (requirement 7: "protection from garbage — same port hit with many requests").
/// Kept as a small self-contained middleware (no external package) so the exact
/// behaviour is easy to audit and tune from AppSettingsEntity at runtime.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    // ip -> (window start, count)
    private static readonly ConcurrentDictionary<string, (DateTime WindowStart, int Count)> Buckets = new();

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, InformerDbContext db)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.Request.Path.StartsWithSegments("/api/notify", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync();
        var maxRequests = settings?.RateLimitMaxRequests ?? 20;
        var windowSeconds = settings?.RateLimitWindowSeconds ?? 10;

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var bucket = Buckets.AddOrUpdate(
            ip,
            _ => (now, 1),
            (_, existing) =>
            {
                if ((now - existing.WindowStart).TotalSeconds > windowSeconds)
                {
                    return (now, 1); // window elapsed, reset
                }
                return (existing.WindowStart, existing.Count + 1);
            });

        if (bucket.Count > maxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for {Ip}: {Count} requests in current window", ip, bucket.Count);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = windowSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new { error = "Too many requests." });
            return;
        }

        await _next(context);
    }
}
