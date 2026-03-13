using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using System.Reflection;


namespace NtisPlatform.Tests.Application;

public class OrganizationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OrganizationService _service;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationServiceTests()
    {
        // Create an in-memory DbContext for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        //_logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<OrganizationService>();
        _logger = NullLogger<OrganizationService>.Instance;

        _service = new OrganizationService(_context, _memoryCache, _logger);
    }

    [Fact]
    public async Task InitializeOrganizationAsync_CreatesAndCachesOrganization()
    {
        var org = new Organization { Name = "TestOrg" };
        var created = await _service.InitializeOrganizationAsync(org, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("TestOrg", created.Name);

        // Should exist in DB
        var dbOrg = await _context.Organizations.FirstOrDefaultAsync();
        Assert.NotNull(dbOrg);
        Assert.Equal("TestOrg", dbOrg!.Name);

        // Should be returned by GetOrganizationAsync (from cache or DB)
        var cached = await _service.GetOrganizationAsync(CancellationToken.None);
        Assert.NotNull(cached);
        Assert.Equal("TestOrg", cached!.Name);
    }

    [Fact]
    public async Task IsSetupRequiredAsync_Behavior()
    {
        // Initially no org => setup required
        var required = await _service.IsSetupRequiredAsync(CancellationToken.None);
        Assert.True(required);

        // Initialize
        var org = new Organization { Name = "O" };
        await _service.InitializeOrganizationAsync(org, CancellationToken.None);

        // Now setup required because IsSetupComplete default false
        required = await _service.IsSetupRequiredAsync(CancellationToken.None);
        Assert.True(required);

        // Mark as complete and save
        var existing = await _context.Organizations.FirstOrDefaultAsync();
        existing!.IsSetupComplete = true;
        await _context.SaveChangesAsync();

        required = await _service.IsSetupRequiredAsync(CancellationToken.None);
        Assert.False(required);
    }

    [Fact]
    public async Task CompleteInitialSetupAsync_CreatesOrgAndAdminUser()
    {
        // Ensure roles table has SuperAdmin role so assignment code can run if present
        _context.Roles.Add(new Role { Name = "SuperAdmin", CreatedDate = DateTime.Now });
        await _context.SaveChangesAsync();

        var org = new Organization { Name = "InitOrg" };
        var admin = new User { Username = "admin", Email = "admin@test", PasswordHash = "hash" };

        var result = await _service.CompleteInitialSetupAsync(org, admin, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.IsSetupComplete);

        // Admin user should exist
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        Assert.NotNull(user);
        Assert.True(user!.IsActive);
    }

    [Fact]
    public async Task UpdateOrganizationSettingsAsync_AddsAndUpdatesSettings()
    {
        var settings = new Dictionary<string, string>
        {
            ["Branding.Logo"] = "logo.png",
            ["Security.Allowed"] = "true"
        };

        var updatedCount = await _service.UpdateOrganizationSettingsAsync(settings, CancellationToken.None);
        Assert.Equal(2, updatedCount);

        // Check DB
        var dbSettings = await _context.OrganizationSettings.ToListAsync();
        Assert.Equal(2, dbSettings.Count);

        // Update again with a change
        settings["Branding.Logo"] = "logo2.png";
        updatedCount = await _service.UpdateOrganizationSettingsAsync(settings, CancellationToken.None);
        Assert.Equal(2, updatedCount);

        var updatedValue = await _context.OrganizationSettings.FirstOrDefaultAsync(s => s.Key == "Branding.Logo");
        Assert.Equal("logo2.png", updatedValue!.Value);
    }

    [Fact]
    public async Task DeleteOrganizationSettingAsync_ReturnsFalseWhenMissing_TrueWhenDeleted()
    {
        var key = "Test.Key";
        var result = await _service.DeleteOrganizationSettingAsync(key, CancellationToken.None);
        Assert.False(result);

        await _service.UpdateOrganizationSettingsAsync(new Dictionary<string, string> { [key] = "v" }, CancellationToken.None);

        result = await _service.DeleteOrganizationSettingAsync(key, CancellationToken.None);
        Assert.True(result);

        var exists = await _context.OrganizationSettings.AnyAsync(s => s.Key == key);
        Assert.False(exists);
    }

    [Fact]
    public async Task UpdateOrganizationAsync_ThrowsWhenNoOrganization()
    {
        var org = new Organization { Name = "X" };
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UpdateOrganizationAsync(org, CancellationToken.None)
        );
    }

    [Fact]
    public async Task UpdateOrganizationAsync_UpdatesAndInvalidatesCache()
    {
        // Initialize first
        var org = new Organization { Name = "Original", IsSetupComplete = false, IsActive = true };
        var created = await _service.InitializeOrganizationAsync(org, CancellationToken.None);

        // Ensure cached
        var cached1 = await _service.GetOrganizationAsync(CancellationToken.None);
        Assert.Equal("Original", cached1!.Name);

        // Update
        created.Name = "Updated";
        created.IsSetupComplete = true;
        var updated = await _service.UpdateOrganizationAsync(created, CancellationToken.None);

        Assert.Equal("Updated", updated.Name);

        // Get should reflect updated value (cache invalidated inside UpdateOrganizationAsync)
        var cached2 = await _service.GetOrganizationAsync(CancellationToken.None);
        Assert.Equal("Updated", cached2!.Name);
    }

    [Fact]
    public async Task UpdateOrganizationSettingsAsync_AssignsCategories_GetByCategoryAndGetByKeys()
    {
        var settings = new Dictionary<string, string>
        {
            ["Branding.Logo"] = "logo.png",
            ["Security.Policy"] = "strict",
            ["Theme.PrimaryColor"] = "#fff",
            ["Notification.Email"] = "enabled",
            ["Other.Custom"] = "value"
        };

        var updatedCount = await _service.UpdateOrganizationSettingsAsync(settings, CancellationToken.None);
        Assert.Equal(5, updatedCount);

        // Validate categories assigned correctly in DB
        var branding = await _context.OrganizationSettings.FirstOrDefaultAsync(s => s.Key == "Branding.Logo");
        Assert.Equal("Branding", branding!.Category);

        var security = await _context.OrganizationSettings.FirstOrDefaultAsync(s => s.Key == "Security.Policy");
        Assert.Equal("Security", security!.Category);

        var theme = await _context.OrganizationSettings.FirstOrDefaultAsync(s => s.Key == "Theme.PrimaryColor");
        Assert.Equal("Theme", theme!.Category);

        var notification = await _context.OrganizationSettings.FirstOrDefaultAsync(s => s.Key == "Notification.Email");
        Assert.Equal("Notification", notification!.Category);

        var other = await _context.OrganizationSettings.FirstOrDefaultAsync(s => s.Key == "Other.Custom");
        Assert.Equal("Other", other!.Category);

        // Test GetOrganizationSettingsByCategoryAsync
        var brandingSettings = await _service.GetOrganizationSettingsByCategoryAsync("Branding", CancellationToken.None);
        Assert.Single(brandingSettings);
        Assert.Equal("logo.png", brandingSettings["Branding.Logo"]);

        var securitySettings = await _service.GetOrganizationSettingsByCategoryAsync("Security", CancellationToken.None);
        Assert.Single(securitySettings);
        Assert.Equal("strict", securitySettings["Security.Policy"]);

        // Test GetOrganizationSettingsAsync for specific keys
        var keys = new[] { "Branding.Logo", "Theme.PrimaryColor" };
        var selected = await _service.GetOrganizationSettingsAsync(keys, CancellationToken.None);
        Assert.Equal(2, selected.Count);
        Assert.Equal("logo.png", selected["Branding.Logo"]);
        Assert.Equal("#fff", selected["Theme.PrimaryColor"]);
    }

    [Fact]
    public void DetermineCategory_ReturnsExpectedCategories()
    {
        var method = typeof(OrganizationService).GetMethod("DetermineCategory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object? Invoke(string key) => method!.Invoke(null, new object[] { key });

        Assert.Equal("Security", Invoke("Security.Password"));
        Assert.Equal("Notification", Invoke("Notification.Email"));
        Assert.Equal("Theme", Invoke("Theme.Primary"));
        Assert.Equal("Branding", Invoke("Branding.Logo"));
        Assert.Equal("Business", Invoke("Business.Name"));
        Assert.Equal("Feature", Invoke("Feature.Flag"));
        Assert.Equal("Other", Invoke("Misc.Key"));
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
        _context?.Database.EnsureDeleted();
        _context?.Dispose();
    }
}