namespace NtisPlatform.Core.Models;

/// <summary>
/// DTO for Property Society Details Tab - includes joined data from SocietyDetailsMast and WingMaster
/// Used for the GET /{propertyId}/society-details API endpoint
/// </summary>
public class PropertySocietyDetailsDto
{
    public int? PropertyId { get; set; }
    public int? SocietyDetailId { get; set; }

    // Wing Information (from WingMaster)
    public int? WingId { get; set; }
    public string? WingNo { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }

    // From SocietyDetailsMast
    public string? WingName { get; set; }
    public string? SocietyName { get; set; }
    public string? SocietyAddress { get; set; }
    public string? SecretaryName { get; set; }
    public string? ManagerName { get; set; }
    public string? LandOwnerName { get; set; }
    public string? BuilderName { get; set; }

    // English versions
    public string? SocietyNameEnglish { get; set; }
    public string? SocietyAddressEnglish { get; set; }
    public string? SecretaryNameEnglish { get; set; }
    public string? ManagerNameEnglish { get; set; }
    public string? LandOwnerNameEnglish { get; set; }
    public string? BuilderNameEnglish { get; set; }

    // Contact Information
    public string? ManagerMobileNo { get; set; }
    public string? SecretaryMobileNo { get; set; }
    public string? SocietyEmailId { get; set; }
    public string? SecretaryEmailId { get; set; }
    public string? ManagerEmailId { get; set; }
    public int PropertyCount { get; set; } = 0;
    public int AminityCount { get; set; } = 0;

}
