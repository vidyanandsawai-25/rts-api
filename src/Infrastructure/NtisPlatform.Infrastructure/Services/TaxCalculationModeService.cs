using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Reads the Dynamic Tax Register's calculation modes from PTIS.TaxCalculationModeMaster.
///
/// Memoized per instance (the service is scoped, so per request): the table is four-ish rows read
/// several times in one request, but deliberately NOT cached across requests — an admin editing
/// the table must see the effect on the next request, without a cache-invalidation hook that
/// nothing else in this codebase has (note <c>TaxMasterDataService.GetOrCacheAsync</c> is a no-op
/// stub, so there is no existing cache infrastructure to hang this off).
/// </summary>
public class TaxCalculationModeService : ITaxCalculationModeService
{
    private readonly ApplicationDbContext _context;
    private List<TaxCalculationModeDto>? _all;
    private List<int>? _activeIds;

    public TaxCalculationModeService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>Loads every mode, active or not. Inactive ones are filtered per-caller: they must
    /// stay resolvable for taxes already pointing at them, but must not be newly selectable.</summary>
    private async Task<List<TaxCalculationModeDto>> LoadAllAsync(CancellationToken cancellationToken)
    {
        return _all ??= await _context.TaxCalculationModeMaster
            .AsNoTracking()
            .OrderBy(m => m.DisplayOrder)
            .ThenBy(m => m.Id)
            .Select(m => new TaxCalculationModeDto
            {
                Id = m.Id,
                ModeCode = m.ModeCode,
                ModeName = m.ModeName,
                DisplayOrder = m.DisplayOrder,
                UsesValueConfig = m.UsesValueConfig,
                UsesConditionConfig = m.UsesConditionConfig,
                UsesMasterConfig = m.UsesMasterConfig,
                UsesHybridConfig = m.UsesHybridConfig,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaxCalculationModeDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        // IsActive is filtered in memory (not in the projection above) so the same single query
        // serves both this and the by-id lookup, which must see inactive rows too. Memoized like
        // _all — this method is called 2-3x per settings save, and was previously re-querying
        // every time despite the class's own doc-comment claiming the table is read "several
        // times in one request" (implying it should be cheap to do so).
        _activeIds ??= await _context.TaxCalculationModeMaster
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var all = await LoadAllAsync(cancellationToken);
        return all.Where(m => _activeIds.Contains(m.Id)).ToList();
    }

    public async Task<TaxCalculationModeDto?> GetByCodeAsync(string? modeCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modeCode)) return null;
        var trimmed = modeCode.Trim();
        var active = await GetActiveAsync(cancellationToken);
        return active.FirstOrDefault(m => string.Equals(m.ModeCode, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<TaxCalculationModeDto?> GetByIdAsync(int? modeId, CancellationToken cancellationToken = default)
    {
        if (!modeId.HasValue) return null;
        var all = await LoadAllAsync(cancellationToken);
        return all.FirstOrDefault(m => m.Id == modeId.Value);
    }
}
