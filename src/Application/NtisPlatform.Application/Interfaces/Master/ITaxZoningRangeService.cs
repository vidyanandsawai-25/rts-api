using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Hand-rolled service for the Tax Zoning Range feature (ward + property-number-range/whole-ward
/// tax zone assignment). Not exposed through <see cref="ICommonCrudService{TEntity, TDto, TCreateDto, TUpdateDto, TQueryParams, TKey}"/> —
/// create/update/delete each carry a bespoke side-effect (chunked bulk update of
/// <c>PTIS.PropertyMast.TaxZoneId</c>) that doesn't fit the generic CRUD
/// validation hooks, which are documented as read-only.
/// </summary>
public interface ITaxZoningRangeService
{
    Task<PagedResult<TaxZoningRangeDto>> GetAllAsync(TaxZoningRangeQueryParameters queryParameters, CancellationToken cancellationToken = default);

    Task<TaxZoningRangeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates ward(s)/tax zone, enforces the range-vs-whole-ward branching (WardIds.Count &gt; 1
    /// forces whole-ward mode for every selected ward), persists one <c>TaxZoningRange</c> row per
    /// ward, and bulk-updates <c>PropertyMast.TaxZoneId</c> for every matching property. Gaps against
    /// other ranges are allowed — untouched property numbers simply stay/become pending.
    /// Throws <see cref="ArgumentException"/> on validation failure (missing ward/zone).
    /// </summary>
    Task<IReadOnlyList<TaxZoningRangeDto>> CreateAsync(CreateTaxZoningRangeDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same validation as <see cref="CreateAsync"/> (single ward only). Properties excluded by a
    /// narrowed range are re-derived from bounds math (not a stored back-reference) and revert to
    /// the range's previous zone via a new "remainder" range record before the updated predicate
    /// is re-applied. Returns null when the range does not exist.
    /// </summary>
    Task<TaxZoningRangeDto?> UpdateAsync(int id, UpdateTaxZoningRangeDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// All-or-nothing-per-row bulk import for the "Validate &amp; Update" drawer: every row is
    /// validated before any row commits; rows that fail validation are skipped and reported in
    /// <c>Errors</c>, valid rows are persisted (including the PropertyMast bulk update) in a single
    /// transaction. Gaps against other ranges are allowed.
    /// </summary>
    Task<RangeResult<TaxZoningRangeDto>> BulkUpsertAsync(BulkTaxZoningRangeRequest request, CancellationToken cancellationToken = default);

    Task<TaxZoningCoverageDto> GetCoverageAsync(IReadOnlyList<int>? wardIds = null, CancellationToken cancellationToken = default);

    Task<PagedResult<WardZoningAbstractDto>> GetWardAbstractAsync(WardAbstractQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paged list of properties from <c>PropertyMast</c> for a given ward.
    /// Pass <c>PageSize = -1</c> to get all properties (used by the Add/Edit form dropdowns).
    /// </summary>
    Task<PagedResult<WardPropertyDto>> GetPropertiesByWardAsync(WardPropertyQueryParameters queryParams, CancellationToken cancellationToken = default);

    /// <summary>Exports the full ward-wise zoning abstract as an .xlsx workbook (ClosedXML). All wards, no pagination.</summary>
    Task<byte[]> ExportWardAbstractToExcelAsync(WardAbstractQueryParameters queryParams, string ulbName = "", CancellationToken cancellationToken = default);

    /// <summary>Exports the tax zoning range records matching the filter as an .xlsx workbook. No pagination.</summary>
    Task<byte[]> ExportRangesToExcelAsync(TaxZoningRangeQueryParameters queryParams, string ulbName = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports every property currently NOT covered by an active TaxZoningRange (Ward No., Property
    /// No., Partition No.) as an .xlsx workbook. Optionally scoped to a single ward.
    /// </summary>
    Task<byte[]> ExportPendingPropertiesToExcelAsync(int? wardId = null, string ulbName = "", CancellationToken cancellationToken = default);

    /// <summary>Generates the blank bulk-update Excel template (.xlsx) with styled headers.</summary>
    byte[] GenerateBulkTemplate();

    /// <summary>
    /// Called from Property Data Entry when a property's TaxZoneId is changed manually. Creates a
    /// new single-property range for the new zone and carves it out of whatever range previously
    /// covered that property number (trimming/splitting as needed). The caller is responsible for
    /// writing the new TaxZoneId onto PropertyMast; this only keeps TaxZoningRange's own bounds
    /// bookkeeping correct. No-op if the zone has not actually changed.
    /// </summary>
    Task ReconcilePropertyZoneChangeAsync(int propertyId, int wardId, string propertyNo, int newTaxZoneId, int userId, CancellationToken cancellationToken = default);
}
