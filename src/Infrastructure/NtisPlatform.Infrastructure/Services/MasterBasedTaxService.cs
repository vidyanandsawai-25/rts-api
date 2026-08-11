using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Master-based tax configuration engine. Uses <see cref="ApplicationDbContext"/> directly
/// (keyed reads + transactional bulk upsert), mirroring the FieldRegistry service pattern.
/// </summary>
public class MasterBasedTaxService : IMasterBasedTaxService
{
    private static readonly HashSet<string> ValidResultModes = new() { "FIXED", "PERCENT" };
    private static readonly HashSet<string> ValidResultBases = new() { "NONE", "RV", "ALV" };

    private readonly ApplicationDbContext _context;

    public MasterBasedTaxService(ApplicationDbContext context)
    {
        _context = context;
    }

    private static void ValidateResultModeAndBase(string resultMode, string resultBase)
    {
        if (!ValidResultModes.Contains(resultMode))
        {
            throw new ArgumentException($"Invalid ResultMode '{resultMode}'. Must be one of: {string.Join(", ", ValidResultModes)}.");
        }
        if (!ValidResultBases.Contains(resultBase))
        {
            throw new ArgumentException($"Invalid ResultBase '{resultBase}'. Must be one of: {string.Join(", ", ValidResultBases)}.");
        }
    }

    public async Task<PagedResult<TaxMasterMappingDto>> GetMappingsAsync(
        int taxId,
        int? assessmentYearRangeId,
        int pageNumber,
        int pageSize,
        int? ruleDefinitionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TaxMasterMappings
            .AsNoTracking()
            .Where(m => m.TaxId == taxId && m.IsActive);

        if (assessmentYearRangeId.HasValue)
        {
            query = query.Where(m => m.AssessmentYearRangeId == assessmentYearRangeId.Value);
        }

        if (ruleDefinitionId.HasValue)
        {
            // Rows seeded under a previously-linked "Choose from List" rule stay in the table
            // (never auto-deleted) — scope to the currently selected rule so switching Rule
            // Name doesn't show a stale mix of the old and new source's keys.
            query = query.Where(m => m.RuleDefinitionId == ruleDefinitionId.Value);
        }

        query = query.OrderBy(m => m.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var (normalizedPageNumber, effectivePageSize, skip) = PagingGuard.Normalize(pageNumber, pageSize, totalCount);
        pageNumber = normalizedPageNumber;

        var rows = await query
            .Skip(skip)
            .Take(effectivePageSize)
            .Select(m => new TaxMasterMappingDto
            {
                Id = m.Id,
                TaxId = m.TaxId,
                RuleDefinitionId = m.RuleDefinitionId,
                MasterKey = m.MasterKey,
                DisplayValue = m.DisplayValue,
                AssessmentYearRangeId = m.AssessmentYearRangeId,
                ResultMode = m.ResultMode,
                ResultBase = m.ResultBase,
                ResultValue = m.ResultValue
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TaxMasterMappingDto>(rows, totalCount, pageNumber, effectivePageSize);
    }

    public async Task<int> SaveAsync(
        SaveMasterMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        // Reject duplicate (year, key) pairs within the same batch up front — letting them reach
        // SaveChangesAsync would trip the unique index mid-transaction and roll back every valid
        // row in the same request with an opaque DB exception instead of a clear 400.
        var duplicateKey = request.Rows
            .GroupBy(r => (r.AssessmentYearRangeId, MasterKey: (r.MasterKey ?? string.Empty).Trim().ToUpperInvariant()))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateKey != null)
        {
            throw new ArgumentException(
                $"Duplicate row for MasterKey='{duplicateKey.Key.MasterKey}' at AssessmentYearRangeId={duplicateKey.Key.AssessmentYearRangeId} — each master key can only appear once per year for this rule.");
        }

        foreach (var row in request.Rows)
        {
            ValidateResultModeAndBase(row.ResultMode, row.ResultBase);
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var ids = request.Rows.Where(r => r.Id > 0).Select(r => r.Id).ToList();
            // Scoped to this TaxId — a stale or mismatched Id from another tax falls through to
            // the natural-key/insert path below instead of silently overwriting that other tax's row.
            var existingById = await _context.TaxMasterMappings
                .Where(m => ids.Contains(m.Id)
                         && m.TaxId == request.TaxId
                         && m.RuleDefinitionId == request.RuleDefinitionId)
                .ToDictionaryAsync(m => m.Id, cancellationToken);

            // Natural-key fallback: Id-only matching misses rows the client never learned the Id
            // of (e.g. inserted moments earlier by the Data tab's auto-seed, or by a separate
            // concurrent save) — that blind-insert path is how a stale/drifted client-side
            // RuleDefinitionId used to create a second physical row for "the same" MasterKey+Year,
            // since the unique index is partly keyed on RuleDefinitionId (nullable — two rows can
            // legally differ only there and both pass the index). Load every row on file for this
            // (TaxId, RuleDefinitionId) once — a tax's mapping set is bounded — and key it in
            // memory by (AssessmentYearRangeId, MasterKey); a tuple-keyed .Contains/.Where does not
            // reliably translate to SQL in EF Core, so this is one flat query, not a per-row lookup.
            var existingForRule = await _context.TaxMasterMappings
                .Where(m => m.TaxId == request.TaxId && m.RuleDefinitionId == request.RuleDefinitionId)
                .ToListAsync(cancellationToken);

            static (int Year, string Key) NaturalKey(int yearId, string masterKey) =>
                (yearId, (masterKey ?? string.Empty).Trim().ToUpperInvariant());

            // GroupBy + First (not a straight ToDictionary) defensively tolerates any duplicate
            // rows that may already exist in the table today, instead of throwing on load.
            var existingByNaturalKey = existingForRule
                .GroupBy(m => NaturalKey(m.AssessmentYearRangeId, m.MasterKey))
                .ToDictionary(g => g.Key, g => g.First());

            var affected = 0;
            foreach (var row in request.Rows)
            {
                var naturalKey = NaturalKey(row.AssessmentYearRangeId, row.MasterKey);
                TaxMasterMappingEntity? entity = null;
                if (row.Id > 0 && existingById.TryGetValue(row.Id, out var byId))
                {
                    entity = byId;
                }
                else
                {
                    existingByNaturalKey.TryGetValue(naturalKey, out entity);
                }

                if (entity != null)
                {
                    entity.MasterKey = row.MasterKey;
                    entity.DisplayValue = row.DisplayValue;
                    // Each row carries its own (possibly just-edited) AssessmentYearRangeId —
                    // use it, not the request-level year (the toolbar's filter value), or a
                    // per-row year change would always be silently discarded on save.
                    entity.AssessmentYearRangeId = row.AssessmentYearRangeId;
                    entity.ResultMode = row.ResultMode;
                    entity.ResultBase = row.ResultBase;
                    entity.ResultValue = row.ResultValue;
                    entity.RuleDefinitionId = request.RuleDefinitionId;
                    entity.UpdatedBy = request.UpdatedBy;
                    entity.UpdatedDate = DateTime.Now;
                }
                else
                {
                    entity = new TaxMasterMappingEntity
                    {
                        TaxId = request.TaxId,
                        RuleDefinitionId = request.RuleDefinitionId,
                        MasterKey = row.MasterKey,
                        DisplayValue = row.DisplayValue,
                        AssessmentYearRangeId = row.AssessmentYearRangeId,
                        ResultMode = row.ResultMode,
                        ResultBase = row.ResultBase,
                        ResultValue = row.ResultValue,
                        IsActive = true,
                        CreatedBy = request.UpdatedBy,
                        CreatedDate = DateTime.Now
                    };
                    _context.TaxMasterMappings.Add(entity);
                }

                // Keep the lookup current so a later row in this same batch that resolves to the
                // same natural key updates this entity too, instead of inserting a duplicate.
                existingByNaturalKey[naturalKey] = entity;
                affected++;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<int> BulkApplyAsync(
        BulkApplyMasterMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateResultModeAndBase(request.ResultMode, request.ResultBase);

        var query = _context.TaxMasterMappings
            .Where(m => m.TaxId == request.TaxId
                     && m.AssessmentYearRangeId == request.AssessmentYearRangeId
                     && m.IsActive);

        // Scope to the requesting rule when provided — a Hybrid tax can have more than one
        // rule's rows coexisting at the same TaxId+year; without this a bulk-apply meant for
        // one rule would also silently overwrite a different rule's rows.
        if (request.RuleDefinitionId.HasValue)
        {
            query = query.Where(m => m.RuleDefinitionId == request.RuleDefinitionId.Value);
        }

        var rows = await query.ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.ResultMode = request.ResultMode;
            row.ResultBase = request.ResultBase;
            row.ResultValue = request.ResultValue;
            row.UpdatedBy = request.UpdatedBy;
            row.UpdatedDate = DateTime.Now;
        }

        if (rows.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return rows.Count;
    }
}
