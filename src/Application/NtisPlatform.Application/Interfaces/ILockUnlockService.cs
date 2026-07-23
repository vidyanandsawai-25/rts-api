using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface ILockUnlockService
{
    Task<List<LockableScreenDto>> GetLockableScreensAsync(
        string? search = null, int? id = null, int? moduleId = null, CancellationToken ct = default);

    Task<PagedResult<PropertyLockRowDto>> GetPropertyLocksAsync(
        FilterPropertyLocksRequestDto request, CancellationToken ct);

    /// <summary>
    /// Same category-based scoping as PropertySearchByCategory (Zone-wise, Ward-wise,
    /// Building-wise, or a From/To property-number range, plus PartType/PropertyCategoryName/
    /// PropertyAssessmentStatusId/IsWing filters), enriched with each property's lock status.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when SearchCategory is invalid, or when the fields required by the selected
    /// category are missing or malformed (delegated to IPropertySearchService).
    /// </exception>
    Task<PagedResult<PropertyLockRowDto>> GetPropertyLocksByCategoryAsync(
        PropertySearchByCategoryQueryParameters request, CancellationToken ct);

    Task<BulkLockResultDto> BulkApplyAsync(
        BulkLockRequestDto request, int actingUserId, CancellationToken ct);

    /// <summary>
    /// Bulk lock/unlock scoped by SearchCategory (Zone/Ward/Building/Range) instead of an
    /// explicit PropertyIds list - resolves every matching property server-side and applies the
    /// action via set-based SQL operations (fast, all-or-nothing per transaction).
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when the Scope's SearchCategory is invalid or missing required fields.
    /// </exception>
    Task<BulkLockResultDto> BulkApplyByCategoryAsync(
        BulkLockByCategoryRequestDto request, int actingUserId, CancellationToken ct);
}
