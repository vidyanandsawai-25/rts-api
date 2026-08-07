using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Value-based tax configuration: per-type-of-use percentages on RV
/// (PTIS.TaxPercentageMasterRV) backing the VALUE_BASED calculation mode.
/// </summary>
public interface IValueBasedTaxService
{
    Task<PagedResult<ValueBasedTaxRowDto>> GetPercentagesAsync(
        int taxId,
        int? yearRangeRVId,
        string? userGroup,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> SaveAsync(
        SaveValueBasedTaxRequest request,
        CancellationToken cancellationToken = default);

    Task<int> BulkApplyAsync(
        BulkApplyValueBasedTaxRequest request,
        CancellationToken cancellationToken = default);
}
