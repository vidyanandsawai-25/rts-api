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
        var configValues = await (
            from cat in _context.ConfigCategoryMasters
            where cat.CategoryCode == CategoryCode && cat.IsActive
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

        // Build settings DTO
        var settings = new EmailSettingsDto();
        var missingKeys = new List<string>();

        foreach (var config in configValues)
        {
            switch (config.ConfigCode)
            {
                case SmtpHostKey:
                    settings.SmtpHost = config.Value ?? string.Empty;
                    break;
                case SmtpPortKey:
                    if (int.TryParse(config.Value, out var port))
                        settings.SmtpPort = port;
                    else
                        _logger.LogWarning("Invalid SmtpPort value: {Value}", config.Value);
                    break;
                case SmtpUserNameKey:
                    settings.SmtpUserName = config.Value ?? string.Empty;
                    break;
                case SmtpPasswordKey:
                    settings.SmtpPassword = config.Value ?? string.Empty;
                    break;
                case FromEmailKey:
                    settings.FromEmail = config.Value ?? string.Empty;
                    break;
                case FromNameKey:
                    settings.FromName = config.Value ?? string.Empty;
                    break;
                case SecureSocketOptionsKey:
                    settings.SecureSocketOptions = config.Value ?? "Auto";
                    break;
                case LoginUrlKey:
                    settings.LoginUrl = config.Value;
                    break;
            }
        }

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
        _cache.Set(CacheKey, settings, CacheDuration);
        _logger.LogInformation("Email settings loaded and cached successfully");

        return settings;
    }
}
