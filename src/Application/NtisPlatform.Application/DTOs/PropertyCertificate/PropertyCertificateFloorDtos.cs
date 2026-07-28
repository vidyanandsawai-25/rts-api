using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyCertificate;

/// <summary>
/// Scope a certificate applies to: the whole property, or one specific floor
/// (PTIS.PropertyDetails row).
/// </summary>
public enum CertificateScope
{
    Property = 0,
    Floor = 1
}

/// <summary>
/// One floor row for the Building Permission tab's floor-wise certificate display
/// (GET /api/property-certificates/floor-certificates).
/// </summary>
public class FloorCertificateDto
{
    public int PropertyDetailsId { get; set; }
    public int PropertyId { get; set; }
    public string? FloorDescription { get; set; }
    public string? SubFloorDescription { get; set; }
    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
    public string? ConstructionTypeDescription { get; set; }
    public string? TypeOfUseDescription { get; set; }
    public string? SubTypeOfUseDescription { get; set; }
    public double? CarpetAreaSqFeet { get; set; }
    public double? CarpetAreaSqMeter { get; set; }
    public double? BuiltupAreaSqFeet { get; set; }
    public double? BuiltupAreaSqMeter { get; set; }

    /// <summary>True when this is the floor identified by selectedPropertyDetailsId in the request.</summary>
    public bool IsSelected { get; set; }

    /// <summary>True when at least one active certificate (property-wise or floor-wise) applies to this floor.</summary>
    public bool CertificateApplicable { get; set; }

    public DateTime? CcDate { get; set; }
    public DateTime? OcDate { get; set; }
    public DateTime? ElectricBillDate { get; set; }
    public string? CcCertificateNo { get; set; }
    public string? OcCertificateNo { get; set; }
    public string? ElectricBillNo { get; set; }
}

/// <summary>
/// Response for GET /api/property-certificates/floor-certificates.
/// </summary>
public class FloorCertificatesResponseDto
{
    public int PropertyId { get; set; }
    public int? SelectedPropertyDetailsId { get; set; }

    /// <summary>
    /// The floor identified by selectedPropertyDetailsId, split out from the rest so the UI can
    /// highlight/auto-open it without scanning OtherFloors for IsSelected. Null when
    /// selectedPropertyDetailsId wasn't passed, or didn't match any floor on this property.
    /// </summary>
    public FloorCertificateDto? SelectedFloor { get; set; }

    /// <summary>Every floor on the property except SelectedFloor (all of them, if none is selected).</summary>
    public List<FloorCertificateDto> OtherFloors { get; set; } = new();

    /// <summary>Certificates saved at property scope (PropertyDetailsId IS NULL) — apply to the whole property.</summary>
    public List<PropertyCertificateWithStatusDto> PropertyWiseCertificates { get; set; } = new();
}

/// <summary>
/// Request for POST /api/property-certificates/save-certificate.
/// Building Permission tab "Save" — saves certificate metadata ONLY and triggers Occupation Tax
/// recalculation for taxable certificate types (IsTaxable=1). Document upload always goes through
/// the Global Document endpoint (POST /api/documents/upload with ReferenceTableName=PropertyCertificates,
/// ReferenceTableId=the PropertyCertificateId returned by this call) — never through this endpoint.
/// </summary>
public class SaveCertificateRequestDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId must be greater than 0")]
    public int PropertyId { get; set; }

    /// <summary>Required when CertificateScope = Floor; must be null when CertificateScope = Property.</summary>
    public int? PropertyDetailsId { get; set; }

    [Required]
    public CertificateScope CertificateScope { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CertificateTypeId must be greater than 0")]
    public int CertificateTypeId { get; set; }

    [MaxLength(100)]
    public string? CertificateNo { get; set; }

    public DateTime? CertificateIssueDate { get; set; }
}

/// <summary>
/// Response for POST /api/property-certificates/save-certificate.
/// </summary>
public class SaveCertificateResponseDto
{
    public int PropertyCertificateId { get; set; }
    public int PropertyId { get; set; }
    public int? PropertyDetailsId { get; set; }
    public CertificateScope CertificateScope { get; set; }
    public int CertificateTypeId { get; set; }
    public string? CertificateNo { get; set; }
    public DateTime? CertificateIssueDate { get; set; }
    public Guid? DocumentGuid { get; set; }
    public int? DocumentBindingId { get; set; }

    /// <summary>True when this certificate type is taxable (IsTaxable=1) and Occupation Tax recalculation was triggered. Distinct from IsProtected, which only gates delete-protection and does not affect tax recalculation.</summary>
    public bool TaxRecalculationTriggered { get; set; }
}

/// <summary>
/// Request for POST /api/property-certificates/replace-certificate -- atomically moves a
/// certificate from one floor/property-wide scope to another (or changes its date while also
/// changing scope) in a single call. If the scope isn't changing, use
/// POST /api/property-certificates/save-certificate instead -- it updates the existing row in
/// place with no delete step.
/// </summary>
public class ReplaceCertificateRequestDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PropertyId must be greater than 0")]
    public int PropertyId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CertificateTypeId must be greater than 0")]
    public int CertificateTypeId { get; set; }

    /// <summary>Current scope of the certificate being replaced (null for property-wise).</summary>
    public int? OldPropertyDetailsId { get; set; }

    /// <summary>New scope for the replacement certificate (null for property-wise).</summary>
    public int? NewPropertyDetailsId { get; set; }

    public string? NewCertificateNo { get; set; }
    public DateTime? NewIssueDate { get; set; }
}

/// <summary>
/// Response for POST /api/property-certificates/replace-certificate.
/// </summary>
public class ReplaceCertificateResponseDto
{
    public int PropertyCertificateId { get; set; }
}
