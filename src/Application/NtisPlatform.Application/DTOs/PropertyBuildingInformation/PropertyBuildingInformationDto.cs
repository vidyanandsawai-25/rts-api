namespace NtisPlatform.Application.DTOs.PropertyBuildingInformation;

/// <summary>
/// DTO for Property Building Information - includes data from PropertyMastOld, SocietyDetailsMast, and RoomWiseSubmissionDetails
/// Used for the POST /building-information/search API endpoint
/// </summary>
public class PropertyBuildingInformationDto
{
    public int PropertyId { get; set; }

    // From PropertyMastOld
    public int? Id { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OldWing { get; set; }
    public string? OldFlatOrShopNumber { get; set; }
    public int? OldPropertyTypeId { get; set; }
    public string? OldOwnerName { get; set; }
    public string? OldMobileNo { get; set; }
    public decimal? OldRV { get; set; }
    public decimal? OldTotalTax { get; set; }

    // From SocietyDetailsMast / PropertyMastOld
    public string? BuilderName { get; set; }
    public string? SocietyName { get; set; }
    public string? BuilderNameEnglish { get; set; }
    public string? BuilderMobileNo { get; set; }
    public int? BuilderMobileNoRemarkId { get; set; }

    // From RoomWiseSubmissionDetails
    public decimal? AreaSqMtr { get; set; }
    public decimal? TotalAreaSqMtr { get; set; }

    // From PropertyMapDetail - Identifies if property is in DRAFT/ACTIVE status
    public bool Identify { get; set; }
}