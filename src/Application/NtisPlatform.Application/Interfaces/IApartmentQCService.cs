using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Application-layer service for Apartment Quality Check operations.
/// Handles read queries, DTO mapping, input validation, and coordinated writes
/// via <c>IUnitOfWork</c>.
/// </summary>
public interface IApartmentQCService
{
    /// <summary>
    /// Returns a paginated list of apartment QC records, one aggregated row per property.
    /// </summary>
    Task<PagedResult<PropertyApartmentTaxDto>> GetPagedAsync(
        ApartmentQCQueryParameters query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one DTO per <c>PropertyDetails</c> row for the given property (expanded view).
    /// <paramref name="resultType"/> controls which tax-calculation fields are included.
    /// </summary>
    Task<PagedResult<PropertyApartmentTaxDto>> GetByPropertyDetailAsync(
        int propertyId,
        ApartmentQCResultType resultType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically patches one or more <c>PropertyDetails</c> rows for a property.
    /// Business validation (batch size, duplicates, empty rows, FK existence, DetailId scope)
    /// runs before any write. If any row fails, NO rows are written.
    /// </summary>
    /// <param name="updatedBy">Actor Id extracted from the caller's JWT claims.</param>
    /// <returns>
    /// <c>null</c> — property not found (HTTP 404).
    /// Result with <see cref="ApartmentQCBulkUpdateResultDto.Failures"/> populated — validation
    /// failed (HTTP 400, atomic, no partial writes).
    /// Result with <see cref="ApartmentQCBulkUpdateResultDto.Updated"/> == total requested — success (HTTP 200).
    /// </returns>
    Task<ApartmentQCBulkUpdateResultDto?> UpdateDetailAsync(
        int propertyId,
        List<UpdateApartmentQCDetailsDto> dtos,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all distinct values for the four filterable columns (Wing, ApartmentType,
    /// FlatOrShopNo, PropertyType) for the given scope. Used to populate filter dropdowns
    /// in the UI before the user applies a column filter.
    /// </summary>
    Task<ApartmentQCFilterOptionsDto> GetFilterOptionsAsync(
        ApartmentQCQueryParameters query,
        string? field,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up the <c>PropertyMastOld</c> record matching <paramref name="oldPropertyNo"/>
    /// and returns its associated old-data fields.
    /// Returns <c>null</c> when the number is not found.
    /// Used by the UI to auto-fill OldRV, OldConstructionArea, etc. after the user
    /// changes the OldPropertyNo field.
    /// </summary>
    Task<OldPropertyLookupDto?> GetOldPropertyDataAsync(
        string oldPropertyNo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates basic property details across PropertyMast and related tables.
    /// </summary>
    /// <param name="updatedBy">Actor Id extracted from the caller's JWT claims.</param>
    /// <returns>
    /// <see cref="BasicDetailsPatchOutcome.PropertyNotFound"/> → HTTP 404;<br/>
    /// <see cref="BasicDetailsPatchOutcome.OldPropertyNoNotFound"/> → HTTP 400;<br/>
    /// <see cref="BasicDetailsPatchOutcome.Success"/> → HTTP 200.
    /// </returns>
    Task<BasicDetailsPatchOutcome> UpdateBasicDetailsAsync(
        int propertyId,
        UpdateApartmentQCBasicDetailsDto dto,
        int updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recomputes <c>CarpetAreaSqMeter</c>, <c>CarpetAreaSqFeet</c>,
    /// <c>BuiltupAreaSqMeter</c>, <c>BuiltupAreaSqFeet</c>, and <c>NoOfRooms</c>
    /// on the <c>PropertyDetails</c> row from the current live state of its
    /// <c>RoomWiseSubmissionDetails</c> rows. Call AFTER room changes are flushed to the DB
    /// (i.e. after <c>IUnitOfWork.SaveChangesAsync</c>) so the aggregate query sees the
    /// latest writes. Stamps <c>UpdatedDate</c> always; stamps <c>UpdatedBy</c> when provided.
    /// Returns <c>false</c> when no <c>PropertyDetails</c> row exists for the given id.
    /// </summary>
    Task<bool> SyncRoomAggregatesAsync(
        int propertyDetailsId,
        int? updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds an .xlsx workbook containing the same rows that <see cref="GetPagedAsync"/>
    /// would return for <paramref name="query"/>, ignoring pagination and using the
    /// <c>MaxExportRowCount</c> options cap as the hard upper bound.
    /// <paramref name="resultType"/> controls which column sections appear:
    /// <see cref="ApartmentQCResultType.Rateable"/> emits RV columns only,
    /// <see cref="ApartmentQCResultType.Capital"/> emits CV columns only,
    /// <see cref="ApartmentQCResultType.Dual"/> emits both (default).
    /// Throws <see cref="InvalidOperationException"/> (mapped to HTTP 400 by the global handler)
    /// when the matching row count exceeds the configured cap.
    /// </summary>
    Task<byte[]> ExportToExcelAsync(
        ApartmentQCQueryParameters query,
        ApartmentQCResultType resultType = ApartmentQCResultType.Dual,
        CancellationToken cancellationToken = default);
}
