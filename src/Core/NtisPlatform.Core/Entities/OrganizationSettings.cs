namespace NtisPlatform.Core.Entities;

/// <summary>
/// Organization settings stored as key-value pairs for flexible configuration
/// Allows adding new settings without schema changes
/// </summary>
public class OrganizationSetting : BaseEntity
{
    /// <summary>
    /// Setting key (e.g., "Security.RequiresTwoFactor")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Setting value (stored as string, parsed based on DataType)
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Data type for proper parsing: String, Int, Bool, Json
    /// </summary>
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Category for grouping: Security, Notification, Theme, Business, Feature
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the setting
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this setting is required for setup completion
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Whether this value should be encrypted (e.g., passwords, API keys)
    /// </summary>
    public bool IsEncrypted { get; set; } = false;
}

/// <summary>
/// Setting categories
/// </summary>
public static class SettingCategories
{
    public const string Security = "Security";
    public const string Notification = "Notification";
    public const string Theme = "Theme";
    public const string Branding = "Branding";
    public const string Organization = "Organization";
    public const string Business = "Business";
    public const string Feature = "Feature";
}

/// <summary>
/// Common setting keys
/// </summary>
public static class SettingKeys
{
    // Security
    public const string RequiresTwoFactor = "Security.RequiresTwoFactor";
    public const string TwoFactorMethod = "Security.TwoFactorMethod";
    public const string SessionTimeoutMinutes = "Security.SessionTimeoutMinutes";
    public const string IdleTimeoutMinutes = "Security.IdleTimeoutMinutes";
    public const string MaxFailedLoginAttempts = "Security.MaxFailedLoginAttempts";
    public const string LockoutDurationMinutes = "Security.LockoutDurationMinutes";
    public const string AllowPasswordReset = "Security.AllowPasswordReset";
    public const string PasswordExpiryDays = "Security.PasswordExpiryDays";
    public const string MinPasswordLength = "Security.MinPasswordLength";
    public const string RequirePasswordComplexity = "Security.RequirePasswordComplexity";
    
    // Notification
    public const string EmailEnabled = "Notification.EmailEnabled";
    public const string SmsEnabled = "Notification.SmsEnabled";
    public const string SmtpHost = "Notification.SmtpHost";
    public const string SmtpPort = "Notification.SmtpPort";
    public const string SmtpUsername = "Notification.SmtpUsername";
    public const string SmtpPassword = "Notification.SmtpPassword";
    public const string SmsProvider = "Notification.SmsProvider";
    public const string SmsApiKey = "Notification.SmsApiKey";
    
    // Branding
    public const string LogoUrl = "Branding.LogoUrl";
    public const string LogoWidth = "Branding.LogoWidth";
    public const string LogoHeight = "Branding.LogoHeight";
    public const string LocalizedName = "Branding.LocalizedName";
    public const string BackgroundImageUrl = "Branding.BackgroundImageUrl";
    public const string PortalTitle = "Branding.PortalTitle";
    
    // Organization
    public const string PrimaryContactEmail = "Organization.PrimaryContactEmail";
    public const string PrimaryContactPhone = "Organization.PrimaryContactPhone";
    public const string WebsiteUrl = "Organization.WebsiteUrl";
    public const string OrganizationAddress = "Organization.Address";
    public const string City = "Organization.City";
    public const string State = "Organization.State";
    public const string PostalCode = "Organization.PostalCode";
    public const string Country = "Organization.Country";
    public const string Description = "Organization.Description";
    public const string TaxId = "Organization.TaxId";
    
    // Theme
    public const string PrimaryColor = "Theme.PrimaryColor";
    public const string SecondaryColor = "Theme.SecondaryColor";
    public const string CustomCssUrl = "Theme.CustomCssUrl";
    public const string ThemeConfig = "Theme.Config";
    
    // Business
    public const string Address = "Business.Address";
    public const string SecondaryContactEmail = "Business.SecondaryContactEmail";
    public const string SecondaryContactPhone = "Business.SecondaryContactPhone";
    public const string WorkingHours = "Business.WorkingHours";
    public const string TimeZone = "Business.TimeZone";
    public const string HolidayCalendar = "Business.HolidayCalendar";
    
    // Feature
    public const string MaintenanceMode = "Feature.MaintenanceMode";
    public const string MaintenanceMessage = "Feature.MaintenanceMessage";
}

/// <summary>
/// OTP delivery method for two-factor authentication
/// </summary>
public enum OtpDeliveryMethod
{
    Email = 1,
    Sms = 2,
    Both = 3
}
