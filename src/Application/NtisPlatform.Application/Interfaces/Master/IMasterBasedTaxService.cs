using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Master-based tax configuration: keyed lookup rows (key → FIXED/PERCENT result)
/// backing the MASTER_BASED calculation mode of the Dynamic Tax Register.
/// </summary>
public interface IMasterBasedTaxService
{
    /// <summary>
    /// Paged mapping rows for a tax, optionally filtered by assessment-year range. A tax's
    /// mappings belong to the tax itself — changing its linked rule no longer leaves a separate,
    /// invisible set behind, so there is nothing further to scope by.
    /// </summary>
    Task<PagedResult<TaxMasterMappingDto>> GetMappingsAsync(
        int taxId,
        int? assessmentYearRangeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Insert/update the supplied rows transactionally. Returns rows affected.</summary>
    Task<int> SaveAsync(
        SaveMasterMappingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Set the same result mode/base/value across all rows of a tax + year range.</summary>
    Task<int> BulkApplyAsync(
        BulkApplyMasterMappingRequest request,
        CancellationToken cancellationToken = default);
}
