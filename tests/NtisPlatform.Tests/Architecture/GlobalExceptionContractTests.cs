using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Moq;
using NtisPlatform.Api.Filters;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Models;
using System.Reflection;
using Xunit;

namespace NtisPlatform.Tests.Architecture;

/// <summary>
/// Verifies the global exception-to-HTTP contract declared in PropertyApiExceptionFilter (Critical #2).
/// These tests act as a regression guard: changing the mapping without updating the tests fails loudly.
/// </summary>
public class GlobalExceptionContractTests
{
    [Fact]
    public void PropertyApiExceptionFilter_is_public_and_implements_IExceptionFilter()
    {
        var filterType = typeof(PropertyApiExceptionFilter);

        Assert.True(filterType.IsPublic,
            "PropertyApiExceptionFilter must be public so the DI container and [TypeFilter] can resolve it.");

        var implementsInterface = filterType
            .GetInterfaces()
            .Any(i => i.Name.Contains("ExceptionFilter"));

        Assert.True(implementsInterface,
            "PropertyApiExceptionFilter must implement IExceptionFilter or IAsyncExceptionFilter " +
            "to participate in the ASP.NET Core exception-handling pipeline.");
    }

    [Fact]
    public void PropertyValidationException_inherits_InvalidOperationException()
    {
        // PropertyApiExceptionFilter catches InvalidOperationException → 400.
        // PropertyValidationException must descend from it so validation errors also produce 400.
        Assert.True(
            typeof(InvalidOperationException).IsAssignableFrom(typeof(PropertyValidationException)),
            "PropertyValidationException must extend InvalidOperationException. " +
            "PropertyApiExceptionFilter maps InvalidOperationException → 400; if this hierarchy breaks, " +
            "validation errors will escape as 500.");
    }

    [Fact]
    public void PropertyApiExceptionFilter_OnException_method_exists()
    {
        var filterType = typeof(PropertyApiExceptionFilter);
        var method = filterType.GetMethod("OnException",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
    }

    [Fact]
    public void PropertyApiExceptionFilter_assembly_is_Presentation_not_Application_or_Core()
    {
        // Filters are Presentation concerns — they must live in the outermost layer.
        var asm = typeof(PropertyApiExceptionFilter).Assembly.GetName().Name;

        Assert.True(asm == "NtisPlatform.Api",
            $"PropertyApiExceptionFilter must reside in NtisPlatform.Api (Presentation) but is in '{asm}'. " +
            "Placing it in Application or Core would invert the dependency direction.");
    }

    // ── Behavioral exception-mapping tests ──────────────────────────────────────────
    // These tests exercise the actual OnException logic and verify the documented mappings.

    [Fact]
    public void OnException_InvalidOperationException_with_already_exist_message_returns_409_Conflict()
    {
        // "already exist" message → 409 Conflict (uniqueness violation).
        var logger = Mock.Of<ILogger<PropertyApiExceptionFilter>>();
        var environment = new TestWebHostEnvironment(isDevelopment: true);

        var filter = new PropertyApiExceptionFilter(logger, environment);
        var context = CreateExceptionContext(
            new InvalidOperationException("Property with code 'ABC' already exist in this ward"));

        filter.OnException(context);

        var result = context.Result as ConflictObjectResult;
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        var envelope = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(envelope.Success);
        Assert.Contains("already exist", envelope.Message);
    }

    [Theory]
    [InlineData(typeof(PropertyValidationException), StatusCodes.Status400BadRequest, "Property validation failure")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status400BadRequest, "Invalid operation")]
    public void OnException_InvalidOperationException_without_already_exist_returns_400_BadRequest(Type exceptionType, int expectedStatus, string message)
    {
        // All InvalidOperationException except "already exist" → 400 Bad Request.
        var logger = Mock.Of<ILogger<PropertyApiExceptionFilter>>();
        var environment = new TestWebHostEnvironment(isDevelopment: true);

        var filter = new PropertyApiExceptionFilter(logger, environment);
        var ex = Activator.CreateInstance(exceptionType, message) as Exception
            ?? throw new InvalidOperationException($"Cannot create {exceptionType.Name}");
        var context = CreateExceptionContext(ex);

        filter.OnException(context);

        var result = context.Result as BadRequestObjectResult;
        Assert.NotNull(result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        var envelope = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(envelope.Success);
    }

    [Fact]
    public void OnException_ArgumentException_returns_400_BadRequest()
    {
        // ArgumentException → 400 Bad Request. Message hidden outside Development.
        var logger = Mock.Of<ILogger<PropertyApiExceptionFilter>>();
        var environment = new TestWebHostEnvironment(isDevelopment: false);

        var filter = new PropertyApiExceptionFilter(logger, environment);
        var context = CreateExceptionContext(new ArgumentException("Invalid argument details"));

        filter.OnException(context);

        var result = context.Result as BadRequestObjectResult;
        Assert.NotNull(result);
        var envelope = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(envelope.Success);
        Assert.Equal("Invalid request", envelope.Message); // Generic message in production
    }

    [Fact]
    public void OnException_KeyNotFoundException_returns_404_NotFound()
    {
        // KeyNotFoundException → 404 Not Found.
        var logger = Mock.Of<ILogger<PropertyApiExceptionFilter>>();
        var environment = new TestWebHostEnvironment(isDevelopment: false);

        var filter = new PropertyApiExceptionFilter(logger, environment);
        var context = CreateExceptionContext(new KeyNotFoundException("Entity not found"));

        filter.OnException(context);

        var result = context.Result as NotFoundObjectResult;
        Assert.NotNull(result);
        var envelope = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(envelope.Success);
    }

    [Fact]
    public void OnException_UnauthorizedAccessException_returns_401_Unauthorized()
    {
        // UnauthorizedAccessException → 401 Unauthorized.
        var logger = Mock.Of<ILogger<PropertyApiExceptionFilter>>();
        var environment = new TestWebHostEnvironment(isDevelopment: false);

        var filter = new PropertyApiExceptionFilter(logger, environment);
        var context = CreateExceptionContext(new UnauthorizedAccessException("Access denied"));

        filter.OnException(context);

        var result = context.Result as UnauthorizedObjectResult;
        Assert.NotNull(result);
        var envelope = Assert.IsType<ApiResponse<object>>(result.Value);
        Assert.False(envelope.Success);
    }

    [Fact]
    public void OnException_UnhandledException_does_not_set_result()
    {
        // Unexpected exception → NOT handled by filter, falls through to middleware.
        var logger = Mock.Of<ILogger<PropertyApiExceptionFilter>>();
        var environment = new TestWebHostEnvironment(isDevelopment: true);

        var filter = new PropertyApiExceptionFilter(logger, environment);
        var context = CreateExceptionContext(new ApplicationException("Unexpected error"));

        filter.OnException(context);

        Assert.Null(context.Result); // Filter did not handle it
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new(), new());
        var exceptionContext = new ExceptionContext(actionContext, new List<IFilterMetadata>()) { Exception = exception };
        return exceptionContext;
    }

    // Test double for IWebHostEnvironment to support the extension method IsDevelopment()
    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        private readonly bool _isDevelopment;

        public TestWebHostEnvironment(bool isDevelopment)
        {
            _isDevelopment = isDevelopment;
            EnvironmentName = isDevelopment ? "Development" : "Production";
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Test";
        public string WebRootPath { get; set; } = "/";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private class NullFileProvider : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath) => new NullDirectoryContents();
        public IFileInfo GetFileInfo(string subpath) => new NullFileInfo();
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }

    private class NullDirectoryContents : IDirectoryContents
    {
        public bool Exists => false;
        public IEnumerator<IFileInfo> GetEnumerator() => Enumerable.Empty<IFileInfo>().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class NullFileInfo : IFileInfo
    {
        public bool Exists => false;
        public bool IsDirectory => false;
        public DateTimeOffset LastModified => DateTimeOffset.Now;
        public long Length => 0;
        public string Name => "";
        public string PhysicalPath => "";
        public Stream CreateReadStream() => Stream.Null;
    }

    private class NullChangeToken : IChangeToken
    {
        public static readonly IChangeToken Singleton = new NullChangeToken();
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => new NullDisposable();
    }

    private class NullDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
