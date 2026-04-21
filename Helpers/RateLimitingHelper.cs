using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Helpers
{
    /// <summary>
    /// Central place to define and register all rate-limit policies.
    /// Referenced by Program.cs → services.AddRateLimiter(RateLimitingHelper.ConfigurePolicies).
    /// </summary>
    public static class RateLimitingHelper
    {
        // ── Policy names (use these constants in [EnableRateLimiting("...")] attributes) ──
        public const string AuthPolicy = "auth";       // login / register
        public const string ApiPolicy = "api";        // general endpoints
        public const string SearchPolicy = "search";     // hotel search
        public const string BookingPolicy = "booking";    // create bookings

        /// <summary>Called from Program.cs: services.AddRateLimiter(RateLimitingHelper.ConfigurePolicies)</summary>
        public static void ConfigurePolicies(RateLimiterOptions options)
        {
            // ── Auth endpoints: 10 requests / minute per IP ──────────────────
            options.AddFixedWindowLimiter(AuthPolicy, o =>
            {
                o.Window = TimeSpan.FromMinutes(1);
                o.PermitLimit = 10;
                o.QueueLimit = 0;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // ── General API: 100 requests / minute per IP ────────────────────
            options.AddFixedWindowLimiter(ApiPolicy, o =>
            {
                o.Window = TimeSpan.FromMinutes(1);
                o.PermitLimit = 100;
                o.QueueLimit = 10;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // ── Search: 30 requests / minute per IP (sliding window) ─────────
            options.AddSlidingWindowLimiter(SearchPolicy, o =>
            {
                o.Window = TimeSpan.FromMinutes(1);
                o.PermitLimit = 30;
                o.SegmentsPerWindow = 6;
                o.QueueLimit = 5;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // ── Bookings: 5 per minute per IP (token bucket) ─────────────────
            options.AddTokenBucketLimiter(BookingPolicy, o =>
            {
                o.TokenLimit = 5;
                o.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
                o.TokensPerPeriod = 5;
                o.AutoReplenishment = true;
                o.QueueLimit = 2;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            // Global rejection response
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    """{"success":false,"message":"Too many requests. Please try again later."}""",
                    cancellationToken);
            };
        }
    }
}