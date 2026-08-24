using NtisPlatform.Core.Models;
using NtisPlatform.Core.Models.AutomationDashboard;
using NtisPlatform.Application.DTOs.PropertySignature;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for property sign-off operations.
/// Orchestrates sequential-signing validation and delegates data access to the repository.
/// </summary>
public interface IPropertySignatureService
{
    /// <summary>Returns all active signing authorities ordered by SequenceOrder.</summary>
    Task<List<SignAuthorityDto>> GetAuthoritiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns properties eligible to be signed by the given authority.
    /// Applies sequential rule: property must already be approved by the previous authority.
    /// </summary>
    Task<List<EligiblePropertyDto>> GetEligiblePropertiesAsync(
        int signAuthorityId,
        int? zoneId,
        int? wardId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits property approvals for a signing authority.
    /// Validates: sequential rule, duplicate check.
    /// UserId is taken from the calling context (set by the controller).
    /// </summary>
    Task<SubmitSignatureResponseDto> SubmitApprovalsAsync(
        int userId,
        SubmitSignatureRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports property approvals from an Excel file.
    /// SignAuthorityId is supplied separately (for dropdown-driven selection).
    /// User/audit fields are always taken from the current session user.
    /// </summary>
    Task<PropertySignatureExcelUploadResultDto> UploadApprovalsFromExcelAsync(
        int userId,
        int signAuthorityId,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an Excel template for PropertySignature upload.
    /// Template contains: PropertyId (required), Remarks (optional).
    /// </summary>
    Task<byte[]> GetApprovalUploadTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns approvals submitted by the current user for a given authority and optional zone.</summary>
    Task<List<SignatureApprovalDto>> GetMyApprovalsAsync(
        int userId,
        int signAuthorityId,
        int? zoneId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the full approval chain status for a single property.</summary>
    Task<PropertySignatureStatusDto?> GetPropertySignatureStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes an approval record. Returns true if found and revoked.</summary>
    Task<bool> RevokeApprovalAsync(
        int propertyId,
        int signAuthorityId,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets division-wise/zone-wise grid statistics for the Sign-off workflow stages.
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
    Task<PropertySignaturePagedResultDto<PropertySignatureSubGridDto>> GetSubGridAsync(
        PropertySignatureBuildingWiseQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property-level signature rows for a building number, including all partitions.
    /// </summary>
    Task<PropertySignaturePagedResultDto<PropertySignaturePropertyWiseDto>> GetPropertyWiseDataAsync(
        PropertySignaturePropertyWiseQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending property signature rows for export for the given authority.
    /// </summary>
    Task<List<PropertySignaturePendingExportDto>> GetPendingExportDataAsync(
        int signAuthorityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending sign rows for the selected user and signing authority.
    /// </summary>
    Task<PropertySignaturePagedResultDto<PropertySignaturePendingSignDto>> GetPendingSignsAsync(
        PropertySignaturePendingSignsQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves the current pending sign row and creates the next pending row when configured.
    /// </summary>
    Task<PropertySignatureUpdateSignResponseDto> UpdateSignAsync(
        PropertySignatureUpdateSignRequestDto request,
        CancellationToken cancellationToken = default);
}
