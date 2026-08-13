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
    /// Paged mapping rows for a tax (optionally filtered by assessment-year range and by the
    /// linked rule — a tax can have mapping rows left over from a previously-linked "Choose
    /// from List" rule; pass <paramref name="ruleDefinitionId"/> to see only the rows for the
    /// rule currently selected).
    /// </summary>
    Task<PagedResult<TaxMasterMappingDto>> GetMappingsAsync(
        int taxId,
        int? assessmentYearRangeId,
        int pageNumber,
        int pageSize,
        int? ruleDefinitionId = null,
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
