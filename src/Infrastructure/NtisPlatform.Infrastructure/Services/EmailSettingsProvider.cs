using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Email;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Retrieves email SMTP settings from config tables with memory caching
/// </summary>
public class EmailSettingsProvider : IEmailSettingsProvider
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EmailSettingsProvider> _logger;
    private const string CacheKey = "EmailSettings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    // Expected configuration keys
    private const string CategoryCode = "EmailSettings";
    private const string SmtpHostKey = "SmtpHost";
    private const string SmtpPortKey = "SmtpPort";
    private const string SmtpUserNameKey = "SmtpUserName";
    private const string SmtpPasswordKey = "SmtpPassword";
    private const string FromEmailKey = "FromEmail";
    private const string FromNameKey = "FromName";
    private const string SecureSocketOptionsKey = "SecureSocketOptions";
    private const string LoginUrlKey = "LoginUrl";

    public EmailSettingsProvider(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<EmailSettingsProvider> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EmailSettingsDto> GetEmailSettingsAsync(CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue(CacheKey, out EmailSettingsDto? cachedSettings) && cachedSettings != null)
        {
            _logger.LogDebug("Email settings retrieved from cache");
            return cachedSettings;
        }

        _logger.LogInformation("Loading email settings from database");

        // Query config tables
        // Join: ConfigCategoryMaster -> ConfigKeyMaster -> ConfigValueMaster
        // CategoryCode comparison uses ToUpper() on both sides (translated to SQL UPPER(), same
        // pattern as UserRepository.GetByUsernameAsync) so this doesn't depend on DB collation.
        var categoryCodeUpper = CategoryCode.ToUpper();
        var configValues = await (
            from cat in _context.ConfigCategoryMasters
            where cat.CategoryCode.ToUpper() == categoryCodeUpper && cat.IsActive
            join key in _context.ConfigKeyMasters on cat.Id equals key.CategoryId
            where key.IsActive
            join val in _context.ConfigValueMasters on key.Id equals val.ConfigKeyId
            where val.IsActive && val.DepartmentId == null && val.ModuleId == null
            select new
            {
                ConfigCode = key.ConfigCode,
                Value = val.Value
            }
        ).ToListAsync(cancellationToken);

        if (!configValues.Any())
        {
            _logger.LogError("Email settings category '{CategoryCode}' not found or contains no active values", CategoryCode);
            throw new InvalidOperationException($"Email settings category '{CategoryCode}' not found in configuration tables");
        }

        // Case-insensitive lookup — ConfigCode casing conventions vary (e.g. SMTPHOST vs
        // SmtpHost); matching this to how ISecuritySettingsService resolves SECURITY_AUTH keys.
        var configMap = configValues
            .Where(c => !string.IsNullOrWhiteSpace(c.ConfigCode))
            .ToDictionary(c => c.ConfigCode!, c => c.Value, StringComparer.OrdinalIgnoreCase);

        // Build settings DTO
        var settings = new EmailSettingsDto();
        var missingKeys = new List<string>();

        if (configMap.TryGetValue(SmtpHostKey, out var smtpHost))
            settings.SmtpHost = smtpHost?.Trim() ?? string.Empty;
        if (configMap.TryGetValue(SmtpPortKey, out var smtpPortRaw))
        {
            if (int.TryParse(smtpPortRaw?.Trim(), out var port))
                settings.SmtpPort = port;
            else
                _logger.LogWarning("Invalid SmtpPort value: {Value}", smtpPortRaw);
        }
        if (configMap.TryGetValue(SmtpUserNameKey, out var smtpUserName))
            settings.SmtpUserName = smtpUserName?.Trim() ?? string.Empty;
        if (configMap.TryGetValue(SmtpPasswordKey, out var smtpPassword))
            settings.SmtpPassword = smtpPassword?.Trim() ?? string.Empty;
        if (configMap.TryGetValue(FromEmailKey, out var fromEmail))
            settings.FromEmail = fromEmail?.Trim() ?? string.Empty;
        if (configMap.TryGetValue(FromNameKey, out var fromName))
            settings.FromName = fromName?.Trim() ?? string.Empty;
        if (configMap.TryGetValue(SecureSocketOptionsKey, out var secureSocketOptions))
            settings.SecureSocketOptions = secureSocketOptions?.Trim() ?? "Auto";
        if (configMap.TryGetValue(LoginUrlKey, out var loginUrl))
            settings.LoginUrl = loginUrl?.Trim();

        // Validate required settings
        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
            missingKeys.Add(SmtpHostKey);
        if (settings.SmtpPort == 0)
            missingKeys.Add(SmtpPortKey);
        if (string.IsNullOrWhiteSpace(settings.SmtpUserName))
            missingKeys.Add(SmtpUserNameKey);
        if (string.IsNullOrWhiteSpace(settings.SmtpPassword))
            missingKeys.Add(SmtpPasswordKey);
        if (string.IsNullOrWhiteSpace(settings.FromEmail))
            missingKeys.Add(FromEmailKey);
        if (string.IsNullOrWhiteSpace(settings.FromName))
            missingKeys.Add(FromNameKey);

        if (missingKeys.Any())
        {
            var missing = string.Join(", ", missingKeys);
            _logger.LogError("Missing required email configuration keys: {MissingKeys}", missing);
            throw new InvalidOperationException($"Missing required email configuration keys: {missing}");
        }

        // Cache the settings
        _cache.Set(CacheKey, settings, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheDuration,
            Size = 1
        });
        _logger.LogInformation("Email settings loaded and cached successfully");

        return settings;
    }

    public Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("Email settings cache cleared.");
        return Task.CompletedTask;
    }
}
