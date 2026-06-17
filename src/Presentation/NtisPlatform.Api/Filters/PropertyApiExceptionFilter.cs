using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Filters;

/// <summary>
/// Maps application-level exceptions thrown by <c>PropertyController</c> actions to HTTP responses
/// using the standard <see cref="ApiResponse{T}"/> envelope. Applied at the controller class level so
/// that every action method is a thin adapter with no inline catch blocks.
///
/// Responsibilities (Critical #1 &amp; #2 — Clean Architecture):
/// <list type="bullet">
///   <item>HTTP status-code selection is this filter's concern; it must not leak into controllers.</item>
///   <item>Business exceptions thrown by Application services are transport-agnostic;
///         this filter is the single translation point to HTTP.</item>
///   <item>It handles the same exception types as <c>GlobalExceptionHandlerMiddleware</c> would map to
///         4xx, but emits the <see cref="ApiResponse{T}"/> envelope so that <b>every</b> Property
///         endpoint error response shares one shape (the middleware uses a different <c>ErrorResponse</c>
///         shape). Only genuinely unexpected exceptions fall through to the middleware → 500.</item>
/// </list>
///
/// Mapping rules:
/// <list type="table">
///   <item><term><see cref="InvalidOperationException"/> whose message contains "already exist"</term>
///         <description>409 Conflict — uniqueness violation (e.g. duplicate old-tax record).</description></item>
///   <item><term>Any other <see cref="InvalidOperationException"/></term>
///         <description>400 Bad Request — includes <c>PropertyValidationException</c> (derived type).</description></item>
///   <item><term><see cref="ArgumentException"/> (incl. <see cref="ArgumentNullException"/>)</term>
///         <description>400 Bad Request.</description></item>
///   <item><term><see cref="KeyNotFoundException"/></term>
///         <description>404 Not Found.</description></item>
///   <item><term><see cref="UnauthorizedAccessException"/></term>
///         <description>401 Unauthorized.</description></item>
///   <item><term>Everything else</term>
///         <description>Not handled here; propagates to <c>GlobalExceptionHandlerMiddleware</c> → 500.</description></item>
/// </list>
///
/// Business exception messages (<see cref="InvalidOperationException"/> / <c>PropertyValidationException</c>)
/// are part of the API contract and are always surfaced. Framework exceptions (argument / lookup / auth)
/// may carry internal detail, so outside the Development environment a generic message is returned —
/// mirroring <c>GlobalExceptionHandlerMiddleware</c>'s production posture.
/// </summary>
public sealed class PropertyApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<PropertyApiExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    public PropertyApiExceptionFilter(ILogger<PropertyApiExceptionFilter> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            // Uniqueness violations are 409 Conflict, not 400. Business message is part of the contract.
            case InvalidOperationException ex when ex.Message.Contains("already exist", StringComparison.OrdinalIgnoreCase):
                _logger.LogWarning(ex, "Conflict: {Message}", ex.Message);
                context.Result = new ConflictObjectResult(Envelope(ex.Message));
                break;

            // PropertyValidationException (derived) and other business InvalidOperationExceptions → 400.
            case InvalidOperationException ex:
                _logger.LogWarning(ex, "Validation error: {Message}", ex.Message);
                context.Result = new BadRequestObjectResult(Envelope(ex.Message));
                break;

            // Bad input (includes ArgumentNullException) → 400. Message hidden outside Development.
            case ArgumentException ex:
                _logger.LogWarning(ex, "Invalid argument: {Message}", ex.Message);
                context.Result = new BadRequestObjectResult(Envelope(SafeMessage(ex, "Invalid request")));
                break;

            case KeyNotFoundException ex:
                _logger.LogWarning(ex, "Not found: {Message}", ex.Message);
                context.Result = new NotFoundObjectResult(Envelope(SafeMessage(ex, "Resource not found")));
                break;

            case UnauthorizedAccessException ex:
                _logger.LogWarning(ex, "Unauthorized: {Message}", ex.Message);
                context.Result = new UnauthorizedObjectResult(Envelope(SafeMessage(ex, "Unauthorized access")));
                break;

            default:
                return; // genuinely unexpected → GlobalExceptionHandlerMiddleware → 500
        }

        context.ExceptionHandled = true;
    }

    private static ApiResponse<object> Envelope(string message) => new()
    {
        Success = false,
        Message = message
    };

    /// <summary>
    /// Framework exceptions may leak internal detail; outside Development return a generic message,
    /// matching the security posture of <c>GlobalExceptionHandlerMiddleware</c>.
    /// </summary>
    private string SafeMessage(Exception ex, string genericMessage)
        => _environment.IsDevelopment() ? ex.Message : genericMessage;
}
