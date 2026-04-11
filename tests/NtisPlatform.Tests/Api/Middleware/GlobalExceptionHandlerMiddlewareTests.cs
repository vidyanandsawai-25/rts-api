using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Middleware;
using System.Text.Json;
using Xunit;

namespace NtisPlatform.Tests.Api.Middleware;

/// <summary>
/// Tests for GlobalExceptionHandlerMiddleware
/// Achieves 100% code coverage for exception handling middleware
/// </summary>
public class GlobalExceptionHandlerMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoException_CallsNext()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var nextCalled = false;
        RequestDelegate next = (HttpContext hc) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        RequestDelegate next = (HttpContext hc) => throw new UnauthorizedAccessException("Unauthorized");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentNullException_Returns400()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        RequestDelegate next = (HttpContext hc) => throw new ArgumentNullException("param");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        RequestDelegate next = (HttpContext hc) => throw new ArgumentException("Invalid argument");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_Returns400()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        RequestDelegate next = (HttpContext hc) => throw new InvalidOperationException("Invalid operation");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        RequestDelegate next = (HttpContext hc) => throw new KeyNotFoundException("Key not found");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        RequestDelegate next = (HttpContext hc) => throw new Exception("Internal error");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentMode_IncludesStackTrace()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        RequestDelegate next = (HttpContext hc) => throw new Exception("Error with stack trace");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Details);
    }

    [Fact]
    public async Task InvokeAsync_ProductionMode_HidesStackTrace()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        RequestDelegate next = (HttpContext hc) => throw new Exception("Error");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(errorResponse);
        Assert.Null(errorResponse.Details);
    }

    [Fact]
    public async Task InvokeAsync_LogsException()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        var exception = new Exception("Test exception");
        RequestDelegate next = (HttpContext hc) => throw exception;

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("An unhandled exception occurred")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ErrorResponse_AllProperties_GetSet()
    {
        var error = new ErrorResponse
        {
            StatusCode = 500,
            Message = "Error message",
            Details = "Stack trace"
        };

        Assert.Equal(500, error.StatusCode);
        Assert.Equal("Error message", error.Message);
        Assert.Equal("Stack trace", error.Details);
    }

    [Fact]
    public void ErrorResponse_DefaultConstructor_InitializesDefaults()
    {
        var error = new ErrorResponse();

        Assert.Equal(0, error.StatusCode);
        Assert.Equal(string.Empty, error.Message);
        Assert.Null(error.Details);
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentMode_ReturnsDetailedMessage()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        RequestDelegate next = (HttpContext hc) => throw new Exception("Detailed error message");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.Contains("Detailed error message", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_ProductionMode_ReturnsGenericMessage()
    {
        var mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        RequestDelegate next = (HttpContext hc) => throw new Exception("Sensitive error");

        var middleware = new GlobalExceptionHandlerMiddleware(next, mockLogger.Object, mockEnv.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        Assert.DoesNotContain("Sensitive error", responseBody);
        Assert.Contains("An error occurred while processing your request", responseBody);
    }
}
