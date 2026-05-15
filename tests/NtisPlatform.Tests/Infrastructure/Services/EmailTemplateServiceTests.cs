using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class EmailTemplateServiceTests : IDisposable
{
    private readonly string _templateDir;
    private readonly EmailTemplateService _service;

    public EmailTemplateServiceTests()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _templateDir = Path.Combine(baseDir, "Templates", "Emails");
        Directory.CreateDirectory(_templateDir);
        _service = new EmailTemplateService(new Mock<ILogger<EmailTemplateService>>().Object);
    }

    public void Dispose()
    {
        // Clean up any files we wrote, but leave directory for other parallel tests
        foreach (var file in Directory.EnumerateFiles(_templateDir, "test-*.html"))
        {
            try { File.Delete(file); } catch { /* best effort */ }
        }
    }

    private string WriteTemplate(string name, string content)
    {
        var path = Path.Combine(_templateDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task GetTemplateAsync_ReplacesPlaceholders()
    {
        WriteTemplate("test-welcome.html", "Hello {{Name}}, your code is {{Code}}.");

        var result = await _service.GetTemplateAsync("test-welcome", new Dictionary<string, string>
        {
            ["Name"] = "Alice",
            ["Code"] = "ABC123"
        });

        Assert.Equal("Hello Alice, your code is ABC123.", result);
    }

    [Fact]
    public async Task GetTemplateAsync_HtmlEncodesPlaceholderValues()
    {
        WriteTemplate("test-encode.html", "Bio: {{Bio}}");

        var result = await _service.GetTemplateAsync("test-encode", new Dictionary<string, string>
        {
            ["Bio"] = "<script>alert('x')</script>"
        });

        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public async Task GetTemplateAsync_AppendsHtmlExtensionWhenMissing()
    {
        WriteTemplate("test-noext.html", "ok");

        var result = await _service.GetTemplateAsync("test-noext", new Dictionary<string, string>());

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task GetTemplateAsync_AcceptsExplicitHtmlExtension()
    {
        WriteTemplate("test-explicit.html", "ok");

        var result = await _service.GetTemplateAsync("test-explicit.html", new Dictionary<string, string>());

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task GetTemplateAsync_Throws_WhenTemplateNameNullOrWhitespace()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetTemplateAsync("", new Dictionary<string, string>()));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetTemplateAsync("   ", new Dictionary<string, string>()));
    }

    [Fact]
    public async Task GetTemplateAsync_Throws_WhenPlaceholdersNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetTemplateAsync("test-x", null!));
    }

    [Fact]
    public async Task GetTemplateAsync_Throws_WhenFileMissing()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetTemplateAsync("test-missing-" + Guid.NewGuid(), new Dictionary<string, string>()));
    }

    [Fact]
    public async Task GetTemplateAsync_PlaceholderReplacementIsCaseInsensitive()
    {
        WriteTemplate("test-case.html", "{{name}} {{NAME}}");

        var result = await _service.GetTemplateAsync("test-case", new Dictionary<string, string>
        {
            ["Name"] = "Bob"
        });

        Assert.Equal("Bob Bob", result);
    }

    [Fact]
    public async Task GetTemplateAsync_NullPlaceholderValueIsTreatedAsEmpty()
    {
        WriteTemplate("test-null.html", "Hi {{Name}}!");

        var result = await _service.GetTemplateAsync("test-null", new Dictionary<string, string>
        {
            ["Name"] = null!
        });

        Assert.Equal("Hi !", result);
    }
}
