using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Specialized repository for Apartment QC read and write-preparation operations.
/// All write-preparation methods mutate EF-tracked entities WITHOUT calling SaveChanges.
/// The Application-layer service owns transaction and <c>IUnitOfWork.SaveChangesAsync</c>.
/// </summary>
public interface IApartmentQCRepository
{
    // ──────────────────────────────── READ ────────────────────────────────

    /// <summary>Total number of properties matching the query filters.</summary>
    Task<int> CountAsync(ApartmentQCQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the raw materialized data for the paged list view.
    /// Returns <see cref="ApartmentQCFetchedData.Empty"/> when no properties match.
    /// Pass <paramref name="resultType"/> to include RV/CV calculation tables (export path);
    /// leave <c>null</c> (default) to skip them (list-view path).
    /// The Application service assembles the returned data into API DTOs.
    /// </summary>
    Task<ApartmentQCFetchedData> FetchPagedDataAsync(
        ApartmentQCQueryParameters query,
        int skip,
        int take,
        ApartmentQCResultType? resultType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the raw materialized data for a single property's expanded (per-detail) view.
    /// <paramref name="resultType"/> controls whether RV/CV calculation tables are queried.
    /// Returns <see cref="ApartmentQCFetchedData.Empty"/> when the property is not found.
    /// The Application service assembles the returned data into API DTOs.
    /// </summary>
    Task<ApartmentQCFetchedData> FetchByPropertyDataAsync(
        int propertyId,
        ApartmentQCResultType resultType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns distinct values for the filterable columns across the given scope query.
    /// The caller (service) is responsible for stripping column-specific filters from
    /// <paramref name="query"/> before calling this method so options reflect full scope.
    /// When <paramref name="column"/> is supplied only that column's list is populated;
    /// pass <c>null</c> to populate all four in a single round-trip.
    /// </summary>
    Task<ApartmentQCFilterOptionsDto> GetFilterOptionsAsync(
        ApartmentQCQueryParameters query,
        ApartmentQCFilterColumn? column,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a <c>PropertyMastOld</c> record by its <paramref name="oldPropertyNo"/> string.
    /// Returns <c>null</c> when no matching record exists.
    /// Used by the UI auto-fill flow: when the user changes OldPropertyNo, the frontend
    /// calls this to refresh OldRV, OldConstructionArea, and the other old-data fields.
    /// </summary>
    Task<OldPropertyLookupDto?> GetOldPropertyDataByNoAsync(
        string oldPropertyNo,
        CancellationToken cancellationToken = default);

    // ──────────────────────── FK EXISTENCE CHECKS ──────────────────────────
    // Pure read operations (AsNoTracking). Used by the service layer for
    // business validation before applying writes. Returns the set of IDs
    // that actually exist so the service can detect and report missing ones.

    /// <summary>Returns true when an active, non-deleted property with the given id exists.</summary>
    Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the subset of <paramref name="ids"/> that exist in FloorEntity.</summary>
    Task<HashSet<int>> GetExistingFloorIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    /// <summary>Returns the subset of <paramref name="ids"/> that exist in ConstructionTypeEntity.</summary>
    Task<HashSet<int>> GetExistingConstructionTypeIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    /// <summary>Returns the subset of <paramref name="ids"/> that exist in TypeOfUse.</summary>
    Task<HashSet<int>> GetExistingTypeOfUseIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    /// <summary>Returns the subset of <paramref name="ids"/> that exist in SubTypeOfUse.</summary>
    Task<HashSet<int>> GetExistingSubTypeOfUseIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

    // ──────────────────── ROOM AGGREGATE READ ─────────────────────────────────

    /// <summary>
    /// Returns the SUM of <c>TotalAreaSqMtr</c> and the COUNT of active, non-deleted
    /// <c>RoomWiseSubmissionDetails</c> rows for the given <paramref name="propertyDetailsId"/>.
    /// Both values are zero when no active rooms exist (e.g. after a full soft-delete).
    /// </summary>
    Task<(double TotalAreaSqMtr, int Count)> GetRoomAggregatesAsync(
        int propertyDetailsId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the tracked (change-tracked) <c>PropertyDetailsEntity</c> for
    /// <paramref name="propertyDetailsId"/>, or <c>null</c> when not found.
    /// Used by <see cref="IApartmentQCService.SyncRoomAggregatesAsync"/> to update
    /// aggregate fields without loading the full navigation graph.
    /// </summary>
    Task<PropertyDetailsEntity?> GetTrackedPropertyDetailsByIdAsync(
        int propertyDetailsId,
        CancellationToken cancellationToken = default);

    // ──────────────────── WRITE-PREPARATION (no SaveChanges) ────────────────────
    // Methods below load and/or mutate EF-tracked entities.
    // The calling service MUST call IUnitOfWork.SaveChangesAsync after these methods.

    /// <summary>
    /// Loads the tracked <c>PropertyDetails</c> entities for <paramref name="propertyId"/>
    /// whose ids are in <paramref name="detailIds"/>.
    /// Returns an empty dictionary when no rows match (property scoping prevents cross-property writes).
    /// </summary>
    Task<Dictionary<int, PropertyDetailsEntity>> GetTrackedDetailsForUpdateAsync(
        int propertyId,
        IEnumerable<int> detailIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the non-null fields from each <see cref="UpdateApartmentQCDetailsDto"/>
    /// onto the corresponding tracked <c>PropertyDetails</c> entity and stamps
    /// <paramref name="updatedBy"/> / <c>DateTime.Now</c>. Does NOT call SaveChanges.
    /// </summary>
    void ApplyDetailPatches(
        Dictionary<int, PropertyDetailsEntity> detailsById,
        IEnumerable<UpdateApartmentQCDetailsDto> dtos,
        int updatedBy);

    /// <summary>
    /// Loads and mutates all tracked entities involved in a basic-details patch
    /// (PropertyMast, SocietyDetailsMast, PropertyMastOld, PropertyMastDetails, RenterMast).
    /// Returns <see cref="BasicDetailsPatchOutcome.PropertyNotFound"/> when the property does not exist,
    /// <see cref="BasicDetailsPatchOutcome.OldPropertyNoNotFound"/> when <paramref name="dto"/> supplies
    /// an <c>OldPropertyNo</c> that has no matching <c>PropertyMastOld</c> row, and
    /// <see cref="BasicDetailsPatchOutcome.Success"/> otherwise. Does NOT call SaveChanges.
    /// </summary>
    Task<BasicDetailsPatchOutcome> PrepareBasicDetailsPatchAsync(
        int propertyId,
        UpdateApartmentQCBasicDetailsDto dto,
        int updatedBy,
        CancellationToken cancellationToken = default);
}
