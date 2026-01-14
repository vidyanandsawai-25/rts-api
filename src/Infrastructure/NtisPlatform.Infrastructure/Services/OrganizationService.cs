using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for managing organization information with caching
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OrganizationService> _logger;
    private const string CacheKey = "Organization";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);

    public OrganizationService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<OrganizationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Organization?> GetOrganizationAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache
        if (_cache.TryGetValue(CacheKey, out Organization? cachedOrg))
        {
            return cachedOrg;
        }

        // Get from database (should only be one row)
        var organization = await _context.Organizations
            .FirstOrDefaultAsync(cancellationToken);

        if (organization != null)
        {
            // Cache the result
            _cache.Set(CacheKey, organization, CacheExpiration);
        }

        return organization;
    }

    public async Task<Organization> UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        // Get existing organization (should only be one)
        var existing = await _context.Organizations.FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            throw new InvalidOperationException("Organization not found. Initialize organization first.");
        }

        // Update only core entity fields (other properties are in OrganizationSettings)
        existing.Name = organization.Name;
        existing.IsActive = organization.IsActive;
        existing.IsSetupComplete = organization.IsSetupComplete;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        _cache.Remove(CacheKey);

        _logger.LogInformation("Organization updated: {OrganizationName}, IsSetupComplete: {IsSetupComplete}", 
            existing.Name, existing.IsSetupComplete);

        return existing;
    }

    public async Task<Organization> InitializeOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        // Check if organization already exists
        var existing = await _context.Organizations.FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            _logger.LogWarning("Organization already initialized: {OrganizationName}", existing.Name);
            return existing;
        }

        // Create new organization
        organization.CreatedAt = DateTime.UtcNow;
        organization.UpdatedAt = DateTime.UtcNow;

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync(cancellationToken);

        // Cache the result
        _cache.Set(CacheKey, organization, CacheExpiration);

        _logger.LogInformation("Organization initialized: {OrganizationName}", organization.Name);

        return organization;
    }

    public async Task<bool> IsSetupRequiredAsync(CancellationToken cancellationToken = default)
    {
        var organization = await _context.Organizations.FirstOrDefaultAsync(cancellationToken);
        
        // Setup required if no organization exists or setup not complete
        return organization == null || !organization.IsSetupComplete;
    }

    public async Task<Organization> CompleteInitialSetupAsync(Organization organization, Core.Entities.User adminUser, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        // Check if setup already completed
        var existing = await _context.Organizations.FirstOrDefaultAsync(cancellationToken);
        if (existing != null && existing.IsSetupComplete)
        {
            throw new InvalidOperationException("Initial setup has already been completed");
        }

        // Create or update organization (only core fields)
        if (existing == null)
        {
            organization.CreatedAt = DateTime.UtcNow;
            organization.UpdatedAt = DateTime.UtcNow;
            organization.IsSetupComplete = true;
            organization.IsActive = true;

            _context.Organizations.Add(organization);
        }
        else
        {
            existing.Name = organization.Name;
            existing.IsSetupComplete = true;
            existing.UpdatedAt = DateTime.UtcNow;
            
            organization = existing;
        }

        // Create admin user if doesn't exist
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == adminUser.Username || u.Email == adminUser.Email, cancellationToken);

        if (existingUser == null)
        {
            adminUser.CreatedAt = DateTime.UtcNow;
            adminUser.IsActive = true;
            adminUser.RequiresTwoFactor = false;
            
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync(cancellationToken);

            // Assign SuperAdmin role
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin", cancellationToken);
            if (superAdminRole != null)
            {
                var userRole = new Core.Entities.UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = superAdminRole.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRole);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Invalidate cache
        _cache.Remove(CacheKey);

        _logger.LogInformation("Initial setup completed for organization: {OrganizationName}", organization.Name);

        return organization;
    }

    public async Task<int> UpdateOrganizationSettingsAsync(Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        int updatedCount = 0;

        foreach (var setting in settings)
        {
            var existingSetting = await _context.OrganizationSettings
                .FirstOrDefaultAsync(s => s.Key == setting.Key, cancellationToken);

            if (existingSetting != null)
            {
                existingSetting.Value = setting.Value;
                existingSetting.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }
            else
            {
                // Create new setting if it doesn't exist
                var newSetting = new OrganizationSetting
                {
                    Key = setting.Key,
                    Value = setting.Value,
                    DataType = "String", // Default to string
                    Category = DetermineCategory(setting.Key),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.OrganizationSettings.Add(newSetting);
                updatedCount++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated {Count} organization settings", updatedCount);

        return updatedCount;
    }

    public async Task<Dictionary<string, string>> GetOrganizationSettingsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        var settings = await _context.OrganizationSettings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty, cancellationToken);

        return settings;
    }

    public async Task<Dictionary<string, string>> GetOrganizationSettingsByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var settings = await _context.OrganizationSettings
            .Where(s => s.Category == category)
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? string.Empty, cancellationToken);

        _logger.LogInformation("Retrieved {Count} settings for category {Category}", settings.Count, category);

        return settings;
    }

    public async Task<bool> DeleteOrganizationSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _context.OrganizationSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (setting == null)
        {
            _logger.LogWarning("Setting with key {Key} not found for deletion", key);
            return false;
        }

        _context.OrganizationSettings.Remove(setting);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted setting with key {Key}", key);

        return true;
    }

    private static string DetermineCategory(string key)
    {
        if (key.StartsWith("Security.")) return "Security";
        if (key.StartsWith("Notification.")) return "Notification";
        if (key.StartsWith("Theme.")) return "Theme";
        if (key.StartsWith("Branding.")) return "Branding";
        if (key.StartsWith("Business.")) return "Business";
        if (key.StartsWith("Feature.")) return "Feature";
        return "Other";
    }
}
