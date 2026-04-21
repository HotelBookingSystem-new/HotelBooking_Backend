using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Backend.Middleware
{
    /// <summary>
    /// Custom in-memory rate-limiting middleware that complements the ASP.NET Core
    /// built-in RateLimiter (registered in Program.cs via RateLimitingHelper).
    /// This layer adds per-IP tracking with configurable rules and detailed logging.
    ///
    /// Usage: app.UseMiddleware&lt;RateLimitingMiddleware&gt;();
    ///        — Place BEFORE app.UseRateLimiter() in Program.cs pipeline.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        // Thread-safe store: IP → (request count, window start)
        private static readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _store = new();

        // Rules per path prefix
        private static readonly Dictionary<string, (int Limit, TimeSpan Window)> _rules = new()
        {
            { "/api/auth/login",    (10, TimeSpan.FromMinutes(1)) },
            { "/api/auth/register", (5,  TimeSpan.FromMinutes(1)) },
            { "/api/auth/forgot",   (3,  TimeSpan.FromMinutes(5)) },
            { "/api/hotels/search", (30, TimeSpan.FromMinutes(1)) },
            { "/api/bookings",      (20, TimeSpan.FromMinutes(1)) },
            { "/api/",              (100,TimeSpan.FromMinutes(1)) }, // global fallback
        };

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ip = GetClientIp(context);
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

            var (limit, window) = GetRule(path);
            var key = $"{ip}:{GetRuleKey(path)}";

            if (IsRateLimited(key, limit, window))
            {
                _logger.LogWarning("Rate limit exceeded | IP: {IP} | Path: {Path}", ip, path);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                context.Response.Headers.Append("Retry-After", window.TotalSeconds.ToString());

                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Too many requests. Please slow down and try again.",
                    retryAfterSeconds = (int)window.TotalSeconds,
                }));
                return;
            }

            await _next(context);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsRateLimited(string key, int limit, TimeSpan window)
        {
            var now = DateTime.UtcNow;

            _store.AddOrUpdate(key,
                // add new entry
                _ => (1, now),
                // update existing
                (_, existing) =>
                {
                    if (now - existing.WindowStart >= window)
                        return (1, now);   // reset window
                    return (existing.Count + 1, existing.WindowStart);
                });

            return _store.TryGetValue(key, out var current) && current.Count > limit;
        }

        private static string GetClientIp(HttpContext context)
        {
            // Respect reverse-proxy headers
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwarded))
                return forwarded.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static (int Limit, TimeSpan Window) GetRule(string path)
        {
            foreach (var rule in _rules)
                if (path.StartsWith(rule.Key, StringComparison.OrdinalIgnoreCase))
                    return rule.Value;

            return (100, TimeSpan.FromMinutes(1)); // safe default
        }

        private static string GetRuleKey(string path)
        {
            foreach (var rule in _rules)
                if (path.StartsWith(rule.Key, StringComparison.OrdinalIgnoreCase))
                    return rule.Key;
            return "global";
        }
    }
}