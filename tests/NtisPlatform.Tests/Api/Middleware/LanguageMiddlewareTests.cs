using Microsoft.AspNetCore.Http;
using NtisPlatform.Api.Middleware;
using NtisPlatform.Core.Constants;

namespace NtisPlatform.Tests.Api.Middleware;

/// <summary>
/// Tests for LanguageMiddleware covering Accept-Language parsing and normalization
/// </summary>
public class LanguageMiddlewareTests
{
    [Theory]
    [InlineData("hi-IN", "hi")]
    [InlineData("mr-IN", "mr")]
    [InlineData("en-US", "en")]
    [InlineData("en", "en")]
    public async Task InvokeAsync_WithSupportedLanguage_SetsCorrectLanguage(
        string acceptLanguageHeader,
        string expectedLanguageKey)
    {
        // Arrange
        var middleware = new LanguageMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = acceptLanguageHeader;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(expectedLanguageKey, context.Items[HttpContextKeys.CurrentLanguage]);
    }

    [Theory]
    [InlineData("hi-IN;q=0.9, en-US;q=0.8", "hi")]
    [InlineData("hi_IN", "hi")]
    [InlineData("HI-IN", "hi")]
    [InlineData("hi", "hi")]
    public async Task InvokeAsync_WithVariousHeaderFormats_ParsesCorrectly(
        string acceptLanguageHeader,
        string expectedLanguage)
    {
        // Arrange
        var middleware = new LanguageMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = acceptLanguageHeader;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(expectedLanguage, context.Items[HttpContextKeys.CurrentLanguage]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task InvokeAsync_WithEmptyHeader_DefaultsToEnglish(string? acceptLanguageHeader)
    {
        // Arrange
        var middleware = new LanguageMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        if (acceptLanguageHeader != null)
        {
            context.Request.Headers["Accept-Language"] = acceptLanguageHeader;
        }

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("en", context.Items[HttpContextKeys.CurrentLanguage]);
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("xx-YY")]
    [InlineData("invalid")]
    public async Task InvokeAsync_WithUnsupportedLanguage_FallsBackToEnglish(string acceptLanguageHeader)
    {
        // Arrange
        var middleware = new LanguageMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = acceptLanguageHeader;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("en", context.Items[HttpContextKeys.CurrentLanguage]);
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleLanguages_UsesFirstPreference()
    {
        // Arrange
        var middleware = new LanguageMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = "mr-IN;q=0.9, hi-IN;q=0.8, en-US;q=0.7";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("mr", context.Items[HttpContextKeys.CurrentLanguage]);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = new LanguageMiddleware(next);
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = "en-US";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("hi-IN, *", "hi")]
    [InlineData("hi-IN;q=1.0", "hi")]
    [InlineData("hi-IN;q=0.5", "hi")]
    public async Task InvokeAsync_WithEdgeCaseHeaders_HandlesGracefully(
        string acceptLanguageHeader,
        string expectedLanguage)
    {
        // Arrange
        var middleware = new LanguageMiddleware(next: _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Headers["Accept-Language"] = acceptLanguageHeader;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(expectedLanguage, context.Items[HttpContextKeys.CurrentLanguage]);
    }
}