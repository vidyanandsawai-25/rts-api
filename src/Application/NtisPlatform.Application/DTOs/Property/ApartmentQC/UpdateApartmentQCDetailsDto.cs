using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// One row in the PATCH <c>apartmentqc/{propertyId}</c> bulk payload.
/// All FK / value fields are optional — only non-null values are written.
/// At least one non-DetailId field MUST be supplied (validated by the service).
/// </summary>
public class UpdateApartmentQCDetailsDto
{
    /// <summary>Target PropertyDetails.Id (PDNId in the GET response). Must be &gt; 0.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ApartmentQC_DetailId_Invalid")]
    public int DetailId { get; set; }

    /// <summary>Id from FloorMaster / FloorEntity.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ApartmentQC_FloorId_Invalid")]
    public int? FloorId { get; set; }

    /// <summary>Id from ConstructionTypeEntity.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ApartmentQC_ConstructionTypeId_Invalid")]
    public int? ConstructionTypeId { get; set; }

    /// <summary>Id from TypeOfUseEntity.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ApartmentQC_TypeOfUseId_Invalid")]
    public int? TypeOfUseId { get; set; }

    /// <summary>Id from SubTypeOfUseEntity.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "ApartmentQC_SubTypeOfUseId_Invalid")]
    public int? SubTypeOfUseId { get; set; }

    /// <summary>4-digit construction year, e.g. "2024". Stored as varchar(4).</summary>
    [StringLength(4, MinimumLength = 4, ErrorMessage = "ApartmentQC_ConstructionYear_MaxLen_4")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "ApartmentQC_ConstructionYear_Invalid")]
    public string? ConstructionYear { get; set; }

    /// <summary>4-digit assessment year, e.g. "2024". Stored as nvarchar(4).</summary>
    [StringLength(4, MinimumLength = 4, ErrorMessage = "ApartmentQC_AssessmentYear_MaxLen_4")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "ApartmentQC_AssessmentYear_Invalid")]
    public string? AssessmentYear { get; set; }
}
