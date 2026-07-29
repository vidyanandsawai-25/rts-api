using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Repository interface for property sign-off operations.
/// Handles data access for SignAuthorityMaster and PropertySignatureDetails.
/// </summary>
public interface IPropertySignatureRepository
{
    /// <summary>Returns all active signing authorities ordered by SequenceOrder.</summary>
    Task<List<SignAuthorityDto>> GetAuthoritiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active candidate properties for signature after applying only DB filters.
    /// Business eligibility rules are applied in the service layer.
    /// </summary>
    Task<List<EligiblePropertyDto>> GetEligiblePropertiesAsync(
        int signAuthorityId,
        int? zoneId,
        int? wardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns property IDs from the input list signed by the provided authority.
    /// </summary>
    Task<List<int>> GetSignedPropertyIdsAsync(
        List<int> propertyIds,
        int signAuthorityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns propertyIds from the input list that are ALREADY approved
    /// by this authority (IsActive=true). Used for duplicate check.
    /// </summary>
    Task<List<int>> GetAlreadyApprovedPropertyIdsAsync(
        List<int> propertyIds,
        int signAuthorityId,
        CancellationToken cancellationToken = default);

    /// <summary>Saves a batch of property approvals. Returns the number of rows inserted.</summary>
    Task<int> SaveApprovalsAsync(
        int userId,
        int signAuthorityId,
        List<PropertyApprovalItemDto> approvals,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns approvals submitted by the current user for a given authority and optional zone.
    /// </summary>
    Task<List<SignatureApprovalDto>> GetMyApprovalsAsync(
        int userId,
        int signAuthorityId,
        int? zoneId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the full approval chain status for a single property.</summary>
    Task<PropertySignatureStatusDto?> GetPropertySignatureStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an approval (sets IsActive=false).
    /// Returns true if the record was found and deactivated.
    /// </summary>
    Task<bool> RevokeApprovalAsync(
        int propertyId,
        int signAuthorityId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets division-wise/zone-wise grid statistics for the Sign Authority workflow stages.
    /// Returns signed property counts (structure & unit) and total demand details.
    /// </summary>
    Task<SignAuthorityGridResponseDto> GetSignAuthorityGridDataAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets ward-wise grid statistics for a specific zone for the Sign-off workflow stages.
    /// </summary>
    Task<SignAuthorityGridResponseDto> GetSignAuthorityWardGridDataAsync(
        int zoneId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets building-level property signature sub-grid data for a ward and workflow stage.
    /// </summary>
    Task<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>> GetBuildingWiseDataAsync(
        int wardId,
        int workflowStageId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property-level signature rows for a building number, including all partitions.
    /// </summary>
    Task<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>> GetPropertyWiseDataAsync(
        string propertyNo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active signing authorities used to calculate pending export rows.
    /// </summary>
    Task<List<PropertySignaturePendingExportAuthorityDto>> GetPendingExportAuthoritiesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets signature-started property data and signed authority ids for export.
    /// </summary>
    Task<List<PropertySignaturePendingExportSourceDto>> GetPendingExportSourceDataAsync(
        CancellationToken cancellationToken = default);
}
