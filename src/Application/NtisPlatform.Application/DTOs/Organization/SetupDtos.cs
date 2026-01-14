namespace NtisPlatform.Application.DTOs.Organization;

/// <summary>
/// DTO for initial organization setup
/// </summary>
public class InitialSetupRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public string PrimaryContactEmail { get; set; } = string.Empty;
    public string? PrimaryContactPhone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
}

/// <summary>
/// Response for initial setup
/// </summary>
public class InitialSetupResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public BasicOrganizationResponse? Organization { get; set; }
}

/// <summary>
/// Check if setup is required
/// </summary>
public class SetupStatusResponse
{
    public bool IsSetupRequired { get; set; }
    public string? OrganizationName { get; set; }
}
