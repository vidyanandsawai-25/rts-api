using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Use-case boundary for the "Historical Property Data" capability of the Property aggregate —
/// the legacy record of the property as it existed before the current assessment regime,
/// comprising three sub-capabilities:
/// <list type="bullet">
///   <item><description><b>Old Property Details</b> — the pre-migration ward number, plot area,
///   RV, ALV and construction data (PropertyMastOld + PropertyDetailsOld).</description></item>
///   <item><description><b>Old Taxes</b> — the tax amounts per finance year as recorded in the
///   legacy register (TransMastOld), with dynamic total recomputation.</description></item>
///   <item><description><b>Old Floor Details</b> — the historical per-floor area and usage data
///   (PropertyDetailsOld) with full CRUD and pagination.</description></item>
/// </list>
/// <para>
/// Each method is an explicit use-case operation (query or command). Business rules —
/// the PropertyMastOld upsert, the finance-year and tax-id validation, the conflict check for
/// create-only old-taxes, and transaction boundaries — are enforced by the implementation.
/// </para>
/// <para>
/// Tab naming ("Old Details") is a Presentation-layer concern; inner layers refer to this
/// capability by its domain intent.
/// </para>
/// </summary>
public interface IPropertyOldDetailsService
{
    // ── Old Property Details (query + command) ──────────────────────────────────────────

    /// <summary>
    /// <b>Query</b> — Returns the legacy property projection for a property,
    /// or <see langword="null"/> when the property does not exist. Controller maps null to 404.
    /// </summary>
    Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Command</b> — Upserts the legacy property record (PropertyMastOld + first
    /// PropertyDetailsOld row). Returns the refreshed projection, or <see langword="null"/>
    /// when the property is not found.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown when required fields (OldFloorId, OldConstructionTypeId, OldTypeOfUseId) are
    /// missing when creating a new legacy details row.
    /// </exception>
    Task<PropertyOldDetailsDto?> UpdateOldDetailsAsync(int propertyId, UpdatePropertyOldDetailsDto dto, CancellationToken cancellationToken = default);

    // ── Old Taxes (query + two commands) ────────────────────────────────────────────────

    /// <summary>
    /// <b>Query</b> — Returns the legacy tax amounts for the latest finance year,
    /// or <see langword="null"/> when the property does not exist.
    /// </summary>
    Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Command (create-only)</b> — Inserts legacy tax records for requested finance years;
    /// rejects any year-tax combination that already has an active record.
    /// Returns the refreshed projection, or <see langword="null"/> when the property is not found.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown on duplicate years, invalid or future finance years, invalid tax ids, or
    /// duplicate tax ids within a year. Also thrown when records already exist for the
    /// requested year-tax combinations (create-only semantics).
    /// </exception>
    Task<PropertyOldTaxesDetailsDto?> CreateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// <b>Command (upsert)</b> — Updates or reactivates legacy tax records; existing and
    /// soft-deleted rows for the requested year-tax combinations are updated rather than
    /// rejected (upsert semantics, no conflict check).
    /// Returns the refreshed projection, or <see langword="null"/> when the property is not found.
    /// </summary>
    /// <exception cref="NtisPlatform.Application.Exceptions.PropertyValidationException">
    /// Thrown on duplicate years, invalid or future finance years, invalid tax ids, or
    /// duplicate tax ids within a year.
    /// </exception>
    Task<PropertyOldTaxesDetailsDto?> UpdateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default);

    // ── Old Floor Details (CRUD queries + commands) ─────────────────────────────────────

    /// <summary><b>Query</b> — Lists all historical floor rows for a property, or <see langword="null"/> when not found.</summary>
    Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary><b>Query</b> — Returns a paged list of historical floor rows, or <see langword="null"/> when the property is not found.</summary>
    Task<PagedResult<PropertyDetailsOldDto>?> GetFloorDetailsOldPagedAsync(int propertyId, FloorDetailsOldQueryParameters queryParameters, CancellationToken cancellationToken = default);

    /// <summary><b>Query</b> — Reads a single historical floor row, or <see langword="null"/> when not found.</summary>
    Task<PropertyDetailsOldDto?> GetFloorDetailsOldByIdAsync(int propertyId, int floorId, CancellationToken cancellationToken = default);

    /// <summary><b>Command</b> — Adds a historical floor row. Returns the created row, or <see langword="null"/> when the property is not found.</summary>
    Task<PropertyDetailsOldDto?> AddFloorDetailsOldAsync(int propertyId, AddPropertyDetailsOldDto dto, CancellationToken cancellationToken = default);

    /// <summary><b>Command</b> — Updates a historical floor row. Returns the updated row, or <see langword="null"/> when not found.</summary>
    Task<PropertyDetailsOldDto?> UpdateFloorDetailsOldAsync(int propertyId, int floorId, UpdatePropertyDetailsOldDto dto, CancellationToken cancellationToken = default);

    /// <summary><b>Command</b> — Soft-deletes a historical floor row. Returns <see langword="true"/> when deleted, <see langword="false"/> when not found.</summary>
    Task<bool> DeleteFloorDetailsOldAsync(int propertyId, int floorId, CancellationToken cancellationToken = default);
}
