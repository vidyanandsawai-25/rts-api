using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Read model + settings for the Dynamic Tax Register grid (projects TaxMaster with its
/// calculation mode, linked rule, and status flags).
/// </summary>
public interface IDynamicTaxRegisterService
{
    Task<PagedResult<DynamicTaxRegisterRowDto>> GetRegisterAsync(
        DynamicTaxRegisterQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<DynamicTaxRegisterStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>Selectable tax categories for the Add-Tax dropdown — active rows only, EDU/EMP excluded.</summary>
    Task<List<TaxCategoryOptionDto>> GetTaxCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Active calculation modes from PTIS.TaxCalculationModeMaster, in DisplayOrder —
    /// what the Rule Type dropdown is built from, carrying the capability flags so the UI can
    /// decide which configuration tabs apply without branching on a mode's code.</summary>
    Task<IReadOnlyList<TaxCalculationModeDto>> GetCalculationModesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update status/flags/mode/rule for a tax. Returns false if the tax does not exist.
    /// Changing CalculationMode also deletes the abandoned mode's configuration rows, but only
    /// when the caller explicitly opts in — otherwise it throws
    /// <see cref="Exceptions.TaxModeChangeConflictException"/> and writes nothing.
    /// </summary>
    Task<bool> UpdateSettingsAsync(
        int taxId,
        UpdateTaxRegisterSettingsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Configuration row counts for one tax, so the UI can name exactly what a
    /// CalculationMode change would delete. Returns null if the tax does not exist.</summary>
    Task<TaxConfigSummaryDto?> GetConfigSummaryAsync(
        int taxId,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a new tax from the "Add Tax" action. Returns the new tax's Id.</summary>
    Task<int> CreateAsync(
        CreateTaxRegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read-only aggregate for the "Show Config" overview — every tax's configuration grouped by
    /// calculation mode (value pivot, condition rows, master mappings, hybrid). Requested one
    /// section (tab) at a time with server-side filtering + pagination, so the caller only ever
    /// receives a single page. See <see cref="ConfigOverviewQueryParameters"/>.
    /// </summary>
    Task<ConfigOverviewPageDto> GetConfigOverviewAsync(
        ConfigOverviewQueryParameters queryParameters,
        CancellationToken cancellationToken = default);
}
