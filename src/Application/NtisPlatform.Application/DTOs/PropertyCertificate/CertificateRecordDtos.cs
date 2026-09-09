using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyCertificate;

/// <summary>
/// Scope level selected in the "Add Certificate Record" UI: the whole apartment (Society), one
/// wing, or a set of units under one wing.
/// </summary>
public enum CertificateRecordLevel
{
    Apartment = 0,
    Wing = 1,
    Unit = 2
}

/// <summary>
/// Request for POST /api/ApartmentQC/certificate-record. Creates one or more
/// PropertyCertificate rows in a single call, matching the "Add Certificate Record" UI's
/// Apartment/Wing/Unit Level tabs:
/// <list type="bullet">
///   <item>Apartment: one Society-scoped row (EntityType 'S', PropertyId null).</item>
///   <item>Wing: one Wing-scoped row (EntityType 'W', PropertyId null).</item>
///   <item>Unit, every unit under the wing selected: collapses to the same single Wing-scoped row
///   as Wing level -- functionally identical, so it is stored the same way.</item>
///   <item>Unit, only some units selected: one Property-scoped row per selected unit (EntityType
///   'P', with SocietyDetailId/WingDetailId carried alongside PropertyId for context -- the DB
///   constraint's 'P' branch only requires PropertyId, it does not forbid the other two).</item>
/// </list>
/// Document upload is NOT handled here -- upload via the Global Document endpoint
/// (POST /api/documents/upload, ReferenceTableName=PropertyCertificates, ReferenceTableId=each
/// PropertyCertificateId this call returns) once per created row, exactly like the existing
/// CC/OC/Electric Bill save-then-upload flow.
/// </summary>
public class CreateCertificateRecordRequestDto
{
    [Required]
    public CertificateRecordLevel Level { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "SocietyDetailId must be greater than 0")]
    public int SocietyDetailId { get; set; }

    /// <summary>Required when Level is Wing or Unit; ignored for Apartment.</summary>
    public int? WingDetailId { get; set; }

    /// <summary>Required when Level is Unit: the PropertyIds of the checked units under WingDetailId.</summary>
    public List<int>? UnitPropertyIds { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CertificateTypeId must be greater than 0")]
    public int CertificateTypeId { get; set; }

    [MaxLength(100)]
    public string? CertificateNo { get; set; }

    public DateTime? CertificateIssueDate { get; set; }
}

/// <summary>
/// Response for POST /api/ApartmentQC/certificate-record.
/// </summary>
public class CreateCertificateRecordResponseDto
{
    /// <summary>The scope the request actually resolved to -- "Society", "Wing", or "Unit" ("Unit" only when a genuinely partial selection was made; a full-wing selection resolves to "Wing").</summary>
    public string EffectiveScope { get; set; } = string.Empty;

    /// <summary>One entry per row created/updated -- one row for Apartment/Wing (or an all-units Unit selection), one row per selected unit for a partial Unit selection.</summary>
    public List<int> PropertyCertificateIds { get; set; } = new();

    /// <summary>Number of units this record covers (every unit under the society/wing for Apartment/Wing scope, or the count of selected units for a partial Unit selection).</summary>
    public int UnitCount { get; set; }

    public bool TaxRecalculationTriggered { get; set; }

    /// <summary>
    /// Populated for Apartment (Society) and Wing scope, where one certificate row recalculates
    /// every member property -- how many succeeded versus failed, with a plain-language reason per
    /// failure. Null for a partial Unit selection, where each unit is saved independently.
    /// </summary>
    public PropertyTaxRecalculationSummaryDto? RecalculationSummary { get; set; }
}
