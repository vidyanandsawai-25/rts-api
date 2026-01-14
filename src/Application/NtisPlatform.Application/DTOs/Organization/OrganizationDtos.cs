namespace NtisPlatform.Application.DTOs.Organization;

/// <summary>
/// DTO for basic organization entity (without settings)
/// </summary>
public class BasicOrganizationResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsSetupComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for updating basic organization entity
/// </summary>
public class UpdateBasicOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public bool? IsSetupComplete { get; set; }
}

/// <summary>
/// DTO for updating organization information (full - used for backward compatibility)
/// </summary>
public class UpdateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int? LogoWidth { get; set; }
    public int? LogoHeight { get; set; }
    public string? LocalizedName { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? PortalTitle { get; set; }
    public string PrimaryContactEmail { get; set; } = string.Empty;
    public string? PrimaryContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "India";
    public string? Description { get; set; }
    public string? TaxId { get; set; }
}

/// <summary>
/// Response DTO for organization information
/// </summary>
public class OrganizationResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int? LogoWidth { get; set; }
    public int? LogoHeight { get; set; }
    public string? LocalizedName { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? PortalTitle { get; set; }
    public string PrimaryContactEmail { get; set; } = string.Empty;
    public string? PrimaryContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "India";
    public string? Description { get; set; }
    public string? TaxId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for updating organization settings (key-value pairs)
/// </summary>
public class UpdateOrganizationSettingsRequest
{
    public Dictionary<string, string> Settings { get; set; } = new();
}

/// <summary>
/// Response DTO for organization settings update
/// </summary>
public class UpdateOrganizationSettingsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UpdatedCount { get; set; }
}
