using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HotelManagement.Middleware
{
    /// <summary>
    /// Custom in-memory per-IP rate-limiting middleware.
    /// Complements the ASP.NET Core built-in RateLimiter (registered via RateLimitingHelper).
    /// Place BEFORE app.UseRateLimiter() in the Program.cs pipeline.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;

        // Thread-safe store: storeKey → entry
        private static readonly ConcurrentDictionary<string, RateLimitEntry> _store = new();

        // Path-prefix rules — most specific FIRST, global fallback LAST
        private static readonly List<(string PathPrefix, int Limit, TimeSpan Window)> _rules =
        [
            ("/api/auth/forgot-password", 3,   TimeSpan.FromMinutes(5)),
            ("/api/auth/reset-password",  3,   TimeSpan.FromMinutes(5)),
            ("/api/auth/login",           10,  TimeSpan.FromMinutes(1)),
            ("/api/auth/register",        5,   TimeSpan.FromMinutes(1)),
            ("/api/hotels/search",        30,  TimeSpan.FromMinutes(1)),
            ("/api/bookings",             20,  TimeSpan.FromMinutes(1)),
            ("/api/promotions",           20,  TimeSpan.FromMinutes(1)),
            ("/api/",                     100, TimeSpan.FromMinutes(1)),  // global fallback
        ];

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Skip rate limiting for infrastructure endpoints
            if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var ip = GetClientIp(context);
            var (limit, window) = GetRule(path);
            var ruleKey = GetRuleKey(path);
            var storeKey = $"{ip}:{ruleKey}";

            if (IsRateLimited(storeKey, limit, window))
            {
                _logger.LogWarning(
                    "Rate limit exceeded | IP: {IP} | Path: {Path} | Limit: {Limit} / {Window}s",
                    ip, path, limit, (int)window.TotalSeconds);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                context.Response.Headers.Append("Retry-After", ((int)window.TotalSeconds).ToString());
                context.Response.Headers.Append("X-RateLimit-Limit", limit.ToString());
                context.Response.Headers.Append("X-RateLimit-Window", ((int)window.TotalSeconds).ToString());

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(new
                    {
                        success = false,
                        message = "Too many requests. Please slow down and try again.",
                        retryAfterSeconds = (int)window.TotalSeconds,
                        statusCode = 429,
                    },
                    _jsonOptions));

                return;
            }

            await _next(context);
        }

        // ── Private Helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Thread-safe sliding counter. Returns true when the caller is over the limit.
        /// Uses a mutable class (not struct) so AddOrUpdate can mutate in place safely.
        /// </summary>
        private static bool IsRateLimited(string key, int limit, TimeSpan window)
        {
            var now = DateTime.UtcNow;

            var entry = _store.AddOrUpdate(
                key,
                // First request for this key
                _ => new RateLimitEntry { Count = 1, WindowStart = now },
                // Subsequent requests
                (_, existing) =>
                {
                    lock (existing)
                    {
                        if (now - existing.WindowStart >= window)
                        {
                            existing.Count = 1;       // reset window
                            existing.WindowStart = now;
                        }
                        else
                        {
                            existing.Count++;
                        }
                    }
                    return existing;
                });

            return entry.Count > limit;
        }

        /// <summary>Extracts the real client IP, honouring proxy headers.</summary>
        private static string GetClientIp(HttpContext context)
        {
            var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
                return forwarded.Split(',')[0].Trim();

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
                return realIp.Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static (int Limit, TimeSpan Window) GetRule(string path)
        {
            foreach (var (prefix, limit, window) in _rules)
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return (limit, window);

            return (100, TimeSpan.FromMinutes(1));
        }

        private static string GetRuleKey(string path)
        {
            foreach (var (prefix, _, _) in _rules)
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return prefix;

            return "global";
        }

        // ── Entry record ──────────────────────────────────────────────────────

        private sealed class RateLimitEntry
        {
            public int Count { get; set; }
            public DateTime WindowStart { get; set; }
        }
    }
}