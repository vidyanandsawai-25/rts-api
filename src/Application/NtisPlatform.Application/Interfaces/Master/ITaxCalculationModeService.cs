using NtisPlatform.Application.DTOs.Master;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Single point of truth for the Dynamic Tax Register's calculation modes, read from
/// PTIS.TaxCalculationModeMaster instead of a hardcoded list.
///
/// Nothing outside this service should hardcode a mode code or Id: callers resolve a mode here and
/// then branch on its capability flags (<c>UsesValueConfig</c> etc.), so adding a mode that reuses
/// an existing mechanism stays a pure DB insert.
/// </summary>
public interface ITaxCalculationModeService
{
    /// <summary>Active modes in DisplayOrder — what the Rule Type dropdown is built from.</summary>
    Task<IReadOnlyList<TaxCalculationModeDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Resolves a mode by its code (case-insensitive). Null when unknown OR inactive —
    /// callers use this for validation, so an inactive mode must not be selectable.</summary>
    Task<TaxCalculationModeDto?> GetByCodeAsync(string? modeCode, CancellationToken cancellationToken = default);

    /// <summary>Resolves a mode by its primary key, including inactive ones — an existing tax may
    /// still point at a mode that has since been retired, and reading it must keep working.</summary>
    Task<TaxCalculationModeDto?> GetByIdAsync(int? modeId, CancellationToken cancellationToken = default);
}
