using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HotelManagement.Middleware
{
    /// <summary>
    /// Global exception-handling middleware.
    /// Catches all unhandled exceptions and returns a structured JSON error response.
    /// Must be registered FIRST in Program.cs so it wraps the entire pipeline.
    ///
    /// Usage: app.UseMiddleware&lt;ErrorHandlingMiddleware&gt;();
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ErrorHandlingMiddleware(
            RequestDelegate next,
            ILogger<ErrorHandlingMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception | Method: {Method} | Path: {Path} | TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);

                await HandleExceptionAsync(context, ex);
            }
        }

        // ── Exception → HTTP status mapping ──────────────────────────────────

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Don't overwrite a response that has already started streaming
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response already started — cannot write error response.");
                return;
            }

            var (statusCode, message, errorCode) = MapException(exception);

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var payload = new ErrorResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                StatusCode = statusCode,
                TraceId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow,
                // Include stack trace only in Development
                Detail = _env.IsDevelopment() ? exception.ToString() : null,
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(payload, _jsonOptions));
        }

        private static (int StatusCode, string Message, string ErrorCode) MapException(Exception exception)
        {
            return exception switch
            {
                // ── Auth & Authorization ──────────────────────────────────────
                UnauthorizedAccessException =>
                    (StatusCodes.Status401Unauthorized,
                     "You are not authorized to perform this action.",
                     "UNAUTHORIZED"),

                // ── Not Found ─────────────────────────────────────────────────
                KeyNotFoundException e =>
                    (StatusCodes.Status404NotFound,
                     string.IsNullOrWhiteSpace(e.Message) ? "The requested resource was not found." : e.Message,
                     "NOT_FOUND"),

                // ── Validation / Bad Input ────────────────────────────────────
                ArgumentNullException e =>
                    (StatusCodes.Status400BadRequest,
                     $"A required value was missing: {e.ParamName}.",
                     "BAD_REQUEST"),

                ArgumentOutOfRangeException e =>
                    (StatusCodes.Status400BadRequest,
                     string.IsNullOrWhiteSpace(e.Message) ? "A value was out of the acceptable range." : e.Message,
                     "BAD_REQUEST"),

                ArgumentException e =>
                    (StatusCodes.Status400BadRequest,
                     string.IsNullOrWhiteSpace(e.Message) ? "Invalid request data." : e.Message,
                     "BAD_REQUEST"),

                // ── Conflict / Business Rule Violations ───────────────────────
                InvalidOperationException e =>
                    (StatusCodes.Status409Conflict,
                     string.IsNullOrWhiteSpace(e.Message) ? "The operation is not valid in the current state." : e.Message,
                     "CONFLICT"),

                // ── Not Supported ─────────────────────────────────────────────
                NotSupportedException e =>
                    (StatusCodes.Status400BadRequest,
                     string.IsNullOrWhiteSpace(e.Message) ? "This operation is not supported." : e.Message,
                     "NOT_SUPPORTED"),

                // ── Timeout ───────────────────────────────────────────────────
                TimeoutException =>
                    (StatusCodes.Status504GatewayTimeout,
                     "The request timed out. Please try again.",
                     "TIMEOUT"),

                // ── Task Cancelled (client disconnect) ────────────────────────
                OperationCanceledException =>
                    (StatusCodes.Status499ClientClosedRequest,
                     "The request was cancelled.",
                     "REQUEST_CANCELLED"),

                // ── Fallback ─ Internal Server Error ──────────────────────────
                _ =>
                    (StatusCodes.Status500InternalServerError,
                     "An unexpected error occurred. Please try again later.",
                     "INTERNAL_SERVER_ERROR"),
            };
        }

        // ── Response DTO ──────────────────────────────────────────────────────

        private sealed class ErrorResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string ErrorCode { get; set; } = string.Empty;
            public int StatusCode { get; set; }
            public string TraceId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string? Detail { get; set; }  // only populated in Development
        }
    }
}