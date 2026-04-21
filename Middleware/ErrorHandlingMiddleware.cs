using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Backend.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse
            {
                Success = false,
                Message = "An error occurred while processing your request"
            };

            switch (exception)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = "Unauthorized access";
                    errorResponse.ErrorCode = "UNAUTHORIZED";
                    break;

                case ArgumentException argEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = argEx.Message;
                    errorResponse.ErrorCode = "INVALID_ARGUMENT";
                    break;

                case InvalidOperationException invEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = invEx.Message;
                    errorResponse.ErrorCode = "INVALID_OPERATION";
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = "Resource not found";
                    errorResponse.ErrorCode = "NOT_FOUND";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.ErrorCode = "INTERNAL_SERVER_ERROR";
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Path { get; set; }
        public string Method { get; set; }
    }

    public class ValidationErrorResponse : ErrorResponse
    {
        public List<ValidationError> Errors { get; set; }
    }

    public class ValidationError
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }
}