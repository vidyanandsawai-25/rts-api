using System.Net;
using System.Text.Json;
using NtisPlatform.Application.Exceptions;

namespace NtisPlatform.Api.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Handle ValidationException with detailed response
        if (exception is ValidationException validationException)
        {
            _logger.LogWarning(validationException, "A validation error occurred: {Message}", validationException.Message);
            await HandleValidationExceptionAsync(context, validationException);
            return;
        }

        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            KeyNotFoundException => HttpStatusCode.NotFound,
            _ => HttpStatusCode.InternalServerError
        };

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = GetErrorMessage(exception, statusCode),
            Details = _environment.IsDevelopment() ? exception.StackTrace : null
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var response = new ValidationErrorResponse
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = exception.Message,
            OperationType = exception.OperationType.ToString(),
            Errors = exception.Errors
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private string GetErrorMessage(Exception exception, HttpStatusCode statusCode)
    {
        // In production, return generic messages for security
        if (!_environment.IsDevelopment())
        {
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => "Unauthorized access",
                HttpStatusCode.BadRequest => "Invalid request",
                HttpStatusCode.NotFound => "Resource not found",
                _ => "An error occurred while processing your request"
            };
        }

        // In development, return actual exception messages
        return exception.Message;
    }
}

/// <summary>
/// Standard error response model
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

/// <summary>
/// Validation error response model with operation type and errors dictionary
/// </summary>
public class ValidationErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public Dictionary<string, string> Errors { get; set; } = new();
}
