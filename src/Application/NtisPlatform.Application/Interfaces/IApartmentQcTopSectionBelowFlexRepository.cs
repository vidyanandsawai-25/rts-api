using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Read-only repository backing the ApartmentQC "below flex" status-badge strip: every
/// configured workflow stage and certificate type for a property, with completion/issuance
/// status and raw audit fields. CreatedByName/UpdatedByName are resolved by the service layer,
/// not here — matches the split already used by <c>PropertyWorkflowDetailsService</c>.
/// All reads are AsNoTracking — this feature never writes.
/// </summary>
public interface IApartmentQcTopSectionBelowFlexRepository
{
    /// <summary>
    /// Every active PropertyWorkflowStageMaster stage (ordered by DisplayOrder), with whether
    /// this property has completed it (the latest PropertyWorkflowDetails row for the stage has
    /// CurrentStatus = true) and that row's audit fields.
    /// </summary>
    Task<List<WorkflowStageStatusDto>> GetWorkflowStagesAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every active PropertyCertificateTypeMaster type (ordered by DisplayOrder), with whether
    /// this property has an active, property-level PropertyCertificates row of that type and its
    /// audit fields.
    /// </summary>
    Task<List<CertificateTypeStatusDto>> GetCertificateTypesAsync(int propertyId, CancellationToken cancellationToken = default);
}
