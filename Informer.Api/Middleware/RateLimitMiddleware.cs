using System.Collections.Concurrent;
using Informer.Data;
using Microsoft.EntityFrameworkCore;

namespace Informer.Api.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

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
                    return (now, 1);
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
