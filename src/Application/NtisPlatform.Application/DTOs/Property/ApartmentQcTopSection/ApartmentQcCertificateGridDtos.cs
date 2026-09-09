namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Response for <c>GET api/ApartmentQCCertificateGrid</c>. A certificate uploaded at any scope
/// (Society/Wing/Unit/Floor) applies to everything under it, so every scope that actually has a
/// record for a given certificate type is returned -- as four independent JSON arrays, one per
/// scope, none suppressing another. This is true uniformly for all three query identifiers
/// (societyId/wingDetailsId/propertyId); which arrays end up non-empty just depends on what was
/// actually recorded at/under the scope that was queried. A type with no record anywhere is
/// omitted entirely.
/// </summary>
public sealed class ApartmentQcCertificateGridDto
{
    /// <summary>Distinct wings (WingDetailId values) across all units of this property number.</summary>
    public int WingCount { get; init; }

    /// <summary>Total unit-properties (partitions) sharing this property number.</summary>
    public int UnitCount { get; init; }

    /// <summary>
    /// Society-level certificates (Level "Apartment") -- a certificate uploaded at society scope
    /// applies to every wing and unit under it, so it is always included here regardless of which
    /// scope was queried, alongside whatever more specific records exist elsewhere.
    /// </summary>
    public List<CertificateGridRowDto> SocietyCertificates { get; init; } = new();

    /// <summary>
    /// Certificates recorded directly on a wing (Level "Wing") -- for a societyId query, one row
    /// per wing that has its own record; for a wingDetailsId query, just that wing's own record;
    /// for a propertyId query, that property's wing's own record.
    /// </summary>
    public List<CertificateGridRowDto> WingCertificates { get; init; } = new();

    /// <summary>
    /// Certificates recorded at unit/property scope (Level "Unit") -- for a societyId query,
    /// aggregated across every unit in the apartment that has its own record; for a wingDetailsId
    /// query, aggregated across just that wing's units; for a propertyId query, that exact
    /// property's own record.
    /// </summary>
    public List<CertificateGridRowDto> UnitCertificates { get; init; } = new();

    /// <summary>
    /// propertyId query only: certificates recorded per floor of that property (Level "Floor").
    /// Always empty for the apartment-wide and wingDetailsId queries (floor-level records aren't
    /// aggregated at those broader scopes).
    /// </summary>
    public List<CertificateGridRowDto> FloorCertificates { get; init; } = new();
}

/// <summary>
/// One certificate-grid row. <see cref="Level"/> determines which of the scope-specific fields
/// are populated: Apartment/Wing rows carry a single record's details; Unit rows aggregate
/// coverage across every unit-property that has (or is missing) a certificate of this type,
/// showing the most recently created covered record's details as the representative one.
/// </summary>
public sealed class CertificateGridRowDto
{
    public int RowNumber { get; set; }

    /// <summary>"Apartment" | "Wing" | "Unit" | "Floor".</summary>
    public string Level { get; init; } = string.Empty;

    /// <summary>The record's SocietyDetailId. Set on Apartment rows (and passed through on Wing/Unit/Floor rows when the underlying certificate carries one), else null.</summary>
    public int? SocietyDetailId { get; init; }

    /// <summary>The record's WingDetailId. Set on Wing rows (and passed through on Unit/Floor rows when the underlying certificate carries one), else null.</summary>
    public int? WingDetailId { get; init; }

    /// <summary>The record's PropertyId. Set on Unit/Floor rows (the representative record's PropertyId for aggregated Unit rows), else null.</summary>
    public int? PropertyId { get; init; }

    /// <summary>Floor rows only: the PropertyDetailsId (FK to PropertyDetails) this record was saved against.</summary>
    public int? PropertyDetailsId { get; init; }

    /// <summary>"Entire Apartment" (Apartment) / the wing's display name (Wing) / space-joined covered unit numbers (Unit).</summary>
    public string ApplicableToLabel { get; init; } = string.Empty;

    /// <summary>"{WingCount} wings · {UnitCount} units" (Apartment) / "{N} units in this wing" (Wing) / null (Unit — see UnitsCoveredCount etc. instead).</summary>
    public string? ApplicableToSubLabel { get; init; }

    /// <summary>Unit-level only: how many of the applicable units have a certificate of this type.</summary>
    public int? UnitsCoveredCount { get; init; }

    /// <summary>Unit-level only: how many units this certificate type applies to.</summary>
    public int? UnitsTotalCount { get; init; }

    /// <summary>Unit-level only: UnitsTotalCount - UnitsCoveredCount.</summary>
    public int? UnitsMissingCount { get; init; }

    /// <summary>Unit-level only: FlatOrShopNo of each covered unit.</summary>
    public List<string> CoveredUnitNumbers { get; init; } = new();

    public int CertificateTypeId { get; init; }
    public string CertificateTypeCode { get; init; } = string.Empty;
    public string CertificateTypeName { get; init; } = string.Empty;

    /// <summary>The representative record's IssueDate (latest by CreatedDate when a type has multiple records at this scope).</summary>
    public DateTime? CertificateDate { get; init; }
    public string? CertificateNumber { get; init; }

    /// <summary>"Active" when the representative record has both CertificateNo and IssueDate set, else "Pending".</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Whether the representative record has a linked document (a certificate can hold at most one).</summary>
    public bool HasDocument { get; init; }

    /// <summary>The linked document's GUID (CORE.Document.DocumentGuid), for building a download link. Null when HasDocument is false.</summary>
    public Guid? DocumentGuid { get; init; }
}
