using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

public class EmailSettingsProviderTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly EmailSettingsProvider _provider;

    public EmailSettingsProviderTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _provider = new EmailSettingsProvider(_context, _cache, new Mock<ILogger<EmailSettingsProvider>>().Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }

    private void Seed(Dictionary<string, string?> values)
    {
        var category = new ConfigCategoryMasterEntity { Id = 1, CategoryCode = "EmailSettings", CategoryName = "Email", IsActive = true };
        _context.ConfigCategoryMasters.Add(category);

        int keyId = 100;
        foreach (var kvp in values)
        {
            var key = new ConfigKeyMasterEntity { Id = keyId, CategoryId = 1, ConfigCode = kvp.Key, ConfigName = kvp.Key, IsActive = true };
            _context.ConfigKeyMasters.Add(key);
            _context.ConfigValueMasters.Add(new ConfigValueMasterEntity
            {
                Id = keyId + 1000, ConfigKeyId = keyId, Value = kvp.Value, IsActive = true,
                DepartmentId = null, ModuleId = null
            });
            keyId++;
        }
        _context.SaveChanges();
    }

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["SmtpHost"] = "smtp.example.com",
        ["SmtpPort"] = "587",
        ["SmtpUserName"] = "user",
        ["SmtpPassword"] = "pass",
        ["FromEmail"] = "no-reply@example.com",
        ["FromName"] = "NTIS",
        ["SecureSocketOptions"] = "Auto",
        ["LoginUrl"] = "https://login"
    };

    [Fact]
    public async Task GetEmailSettingsAsync_ReturnsSettings_FromDatabase()
    {
        Seed(ValidSettings());

        var result = await _provider.GetEmailSettingsAsync();

        Assert.Equal("smtp.example.com", result.SmtpHost);
        Assert.Equal(587, result.SmtpPort);
        Assert.Equal("user", result.SmtpUserName);
        Assert.Equal("pass", result.SmtpPassword);
        Assert.Equal("no-reply@example.com", result.FromEmail);
        Assert.Equal("NTIS", result.FromName);
        Assert.Equal("Auto", result.SecureSocketOptions);
        Assert.Equal("https://login", result.LoginUrl);
    }

    [Fact]
    public async Task GetEmailSettingsAsync_CachesResult()
    {
        Seed(ValidSettings());

        var first = await _provider.GetEmailSettingsAsync();
        // Mutate DB after first read - should still return cached value
        _context.ConfigValueMasters.RemoveRange(_context.ConfigValueMasters);
        await _context.SaveChangesAsync();

        var cached = await _provider.GetEmailSettingsAsync();

        Assert.Same(first, cached);
    }

    [Fact]
    public async Task GetEmailSettingsAsync_Throws_WhenCategoryMissing()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.GetEmailSettingsAsync());
    }

    [Fact]
    public async Task GetEmailSettingsAsync_Throws_WhenRequiredKeysMissing()
    {
        // Only SmtpHost set - rest are missing
        Seed(new Dictionary<string, string?> { ["SmtpHost"] = "smtp.example.com" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.GetEmailSettingsAsync());
        Assert.Contains("Missing required email configuration", ex.Message);
    }

    [Fact]
    public async Task GetEmailSettingsAsync_HandlesInvalidPortAsZero()
    {
        var settings = ValidSettings();
        settings["SmtpPort"] = "not-a-number";
        Seed(settings);

        // SmtpPort=0 -> reported as missing
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _provider.GetEmailSettingsAsync());
        Assert.Contains("SmtpPort", ex.Message);
    }
}
