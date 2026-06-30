using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces.Property;

/// <summary>
/// Persistence port for the "Historical Property Data" use-case on the Property aggregate
/// (legacy property master, historical taxes and historical floor sub-sections).
/// <para>
/// Persistence only; business rules live in <c>IPropertyOldDetailsService</c> and saving is
/// delegated to <c>IUnitOfWork</c>. Extends <see cref="IPropertyAggregateRepository"/> — the
/// shared aggregate-root load is inherited, not repeated.
/// </para>
/// </summary>
public interface IPropertyOldDetailsRepository : IPropertyAggregateRepository
{
    // ---- Old Property Details sub-section ----

    /// <summary>Reads the composed old-property projection (PropertyMastOld + first PropertyDetailsOld + computed old taxes), or null when the property is not found.</summary>
    Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Reads the tab header info (StatusName and Old property details) for the specified property, or null when not found.</summary>
    Task<PropertyTabHeaderInfoDto?> GetTabHeaderInfoAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new PropertyMastOld row for insertion (persisted later via the unit of work).</summary>
    Task AddPropertyMastOldAsync(PropertyMastOldEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Loads a tracked PropertyMastOld row by id, or null.</summary>
    Task<PropertyMastOldEntity?> GetPropertyMastOldByIdAsync(int propertyMastOldId, CancellationToken cancellationToken = default);

    /// <summary>Returns the id of the first active, non-deleted PropertyDetailsOld row for the old master, or 0 when none exists.</summary>
    Task<int> GetFirstOldDetailsIdAsync(int propertyMastOldId, CancellationToken cancellationToken = default);

    /// <summary>Loads a tracked PropertyDetailsOld row by id, or null.</summary>
    Task<PropertyDetailsOldEntity?> GetOldDetailsByIdAsync(int oldDetailsId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new PropertyDetailsOld row for insertion (persisted later via the unit of work).</summary>
    Task AddOldDetailsAsync(PropertyDetailsOldEntity entity, CancellationToken cancellationToken = default);

    // ---- Old Taxes Details sub-section ----

    /// <summary>Reads the old-taxes projection (latest finance year + per-tax amounts), or null when the property is not found.</summary>
    Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    // Old-taxes validation-data queries (the service applies the business rules and throws).

    /// <summary>Maps the given finance-year ids to their Year value (only ids that exist are returned).</summary>
    Task<Dictionary<int, int>> GetYearsByIdsAsync(IReadOnlyCollection<int> financeYearIds, CancellationToken cancellationToken = default);

    /// <summary>Returns, of the given tax ids, those that exist in TaxMaster with OldTaxStatus = true.</summary>
    Task<List<int>> GetValidOldTaxIdsAsync(IReadOnlyCollection<int> taxIds, CancellationToken cancellationToken = default);

    /// <summary>Returns the (FinanceYearId, TaxId) keys that already have an active transaction for the old master and requested years.</summary>
    Task<HashSet<(int FinanceYearId, int TaxId)>> GetActiveOldTaxKeysAsync(int propertyMastOldId, IReadOnlyCollection<int> financeYearIds, CancellationToken cancellationToken = default);

    /// <summary>Maps finance-year ids to their YearCode (for conflict messages).</summary>
    Task<Dictionary<int, string?>> GetYearCodeMapAsync(IReadOnlyCollection<int> financeYearIds, CancellationToken cancellationToken = default);

    /// <summary>Maps tax ids to their TaxName (for conflict messages).</summary>
    Task<Dictionary<int, string>> GetTaxNameMapAsync(IReadOnlyCollection<int> taxIds, CancellationToken cancellationToken = default);

    // Old-taxes transactional persistence (the service validates first; these contain no business rules).

    /// <summary>Inserts the requested old-taxes transactions atomically (creating the old master if needed) and recomputes totals. Returns the refreshed projection, or null when not found.</summary>
    Task<PropertyOldTaxesDetailsDto?> PersistNewOldTaxesAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>Upserts (reactivating soft-deleted rows) the requested old-taxes transactions and recomputes totals. Returns the refreshed projection, or null when not found.</summary>
    Task<PropertyOldTaxesDetailsDto?> PersistUpsertedOldTaxesAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default);

    // ---- Old Floor Details sub-section ----

    /// <summary>Lists all historical floor rows for a property, or null when the property is not found.</summary>
    Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>Lists historical floor rows with filtering, sorting and pagination, or null when the property is not found.</summary>
    Task<FloorDetailsOldPagedResult?> GetFloorDetailsOldPagedAsync(int propertyId, FloorDetailsOldQuery query, CancellationToken cancellationToken = default);

    /// <summary>Reads a single historical floor row by id, or null when not found.</summary>
    Task<PropertyDetailsOldDto?> GetFloorDetailsOldByIdAsync(int propertyId, int floorId, CancellationToken cancellationToken = default);

    /// <summary>Adds a historical floor row (references validated). Returns the created row, or null when the property is not found.</summary>
    Task<PropertyDetailsOldDto?> AddFloorDetailsOldAsync(int propertyId, AddPropertyDetailsOldDto dto, CancellationToken cancellationToken = default);

    /// <summary>Updates a historical floor row. Returns the updated row, or null when not found.</summary>
    Task<PropertyDetailsOldDto?> UpdateFloorDetailsOldAsync(int propertyId, int floorId, UpdatePropertyDetailsOldDto dto, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a historical floor row. Returns true when deleted.</summary>
    Task<bool> DeleteFloorDetailsOldAsync(int propertyId, int floorId, CancellationToken cancellationToken = default);
}
