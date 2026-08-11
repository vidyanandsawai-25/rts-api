using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Projects <c>PTIS.TaxMaster</c> (+ its linked Rule Master entry) into the Dynamic Tax
/// Register read model, and persists General-tab settings.
/// </summary>
public class DynamicTaxRegisterService : IDynamicTaxRegisterService
{
    /// <summary>
    /// Tax categories that must never appear anywhere on the Dynamic Tax Register screen —
    /// State Education Tax (EDU) and State Employment Tax (EMP) are handled by a separate
    /// pipeline (RateableValueService.IsEducationTax/IsEmploymentTax) and are excluded from
    /// the list, stats, and config overview. Matched by CategoryCode (stable) rather than Id.
    /// </summary>
    private static readonly string[] ExcludedCategoryCodes = { "EDU", "EMP" };

    private readonly ApplicationDbContext _context;
    private readonly ITaxCalculationModeService _modeService;

    public DynamicTaxRegisterService(ApplicationDbContext context, ITaxCalculationModeService modeService)
    {
        _context = context;
        _modeService = modeService;
    }

    /// <summary>Resolves the TaxCategoryMaster Ids for <see cref="ExcludedCategoryCodes"/>, for use in a "not in" filter.</summary>
    private async Task<List<int>> GetExcludedCategoryIdsAsync(CancellationToken cancellationToken) =>
        await _context.TaxCategoryMaster.AsNoTracking()
            .Where(c => ExcludedCategoryCodes.Contains(c.CategoryCode))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<DynamicTaxRegisterRowDto>> GetRegisterAsync(
        DynamicTaxRegisterQueryParameters qp,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = qp.PageNumber < 1 ? 1 : qp.PageNumber;

        var excludedCategoryIds = await GetExcludedCategoryIdsAsync(cancellationToken);

        // Left-join TaxMaster → DynamicTaxRuleMaster to resolve the friendly Rule Name.
        // EDU/EMP-category taxes are excluded outright — they never surface on this screen.
        var query =
            from t in _context.TaxMaster.AsNoTracking()
            where !excludedCategoryIds.Contains(t.TaxCategoryId)
            join r in _context.DynamicTaxRuleMaster.AsNoTracking()
                on t.RuleDefinitionId equals r.Id into rj
            from rule in rj.DefaultIfEmpty()
            select new { t, rule };

        if (!string.IsNullOrWhiteSpace(qp.Search))
        {
            var term = qp.Search.Trim();
            query = query.Where(x =>
                x.t.TaxName.Contains(term) ||
                x.t.TaxCode.Contains(term) ||
                x.t.Id.ToString().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(qp.Mode))
        {
            // The API still filters by mode CODE (the contract is unchanged); it just resolves
            // through the FK now that TaxMaster no longer stores the string.
            var mode = qp.Mode.Trim();
            query = query.Where(x => x.t.CalculationModeMaster!.ModeCode == mode);
        }

        if (!string.IsNullOrWhiteSpace(qp.Status))
        {
            var active = string.Equals(qp.Status.Trim(), "ACTIVE", StringComparison.OrdinalIgnoreCase);
            query = query.Where(x => x.t.IsActive == active);
        }

        query = query.OrderBy(x => x.t.DisplayOrder).ThenBy(x => x.t.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var (_, effectivePageSize, skip) = PagingGuard.Normalize(pageNumber, qp.PageSize, totalCount);

        var raw = await query
            .Skip(skip)
            .Take(effectivePageSize)
            .Select(x => new
            {
                x.t.Id,
                x.t.TaxName,
                x.t.TaxNameAlias,
                x.t.TaxCode,
                // Mode code + its capability flags travel together, so everything below decides
                // behaviour from CAPABILITIES rather than by comparing the code to a literal.
                CalculationMode = x.t.CalculationModeMaster != null ? x.t.CalculationModeMaster.ModeCode : null,
                UsesValueConfig = x.t.CalculationModeMaster != null && x.t.CalculationModeMaster.UsesValueConfig,
                UsesConditionConfig = x.t.CalculationModeMaster != null && x.t.CalculationModeMaster.UsesConditionConfig,
                UsesMasterConfig = x.t.CalculationModeMaster != null && x.t.CalculationModeMaster.UsesMasterConfig,
                UsesHybridConfig = x.t.CalculationModeMaster != null && x.t.CalculationModeMaster.UsesHybridConfig,
                x.t.RuleDefinitionId,
                x.t.IsActive,
                x.t.AssessmentStatus,
                x.t.OldTaxStatus,
                RuleName = x.rule != null ? x.rule.DisplayName : null,
                RuleRef = x.rule != null ? x.rule.AttachedReference : null
            })
            .ToListAsync(cancellationToken);

        // A HYBRID tax's own RuleDefinitionId points at its condition/Hybrid-type rule (an
        // internal rule-engine reference, not something meaningful to show as "Source") — its
        // real Source/Basis is the combination of the master rule feeding its Master Data
        // Mapping section (e.g. "PropertyType") and its Hybrid config's ResultBase (e.g. "RV"),
        // matching the reference design's "PropertyType + RV" / "OwnerType + ALV" format.
        var hybridTaxIds = raw.Where(x => x.UsesHybridConfig).Select(x => x.Id).ToList();
        var hybridResultBase = new Dictionary<int, string?>();
        var hybridMasterSource = new Dictionary<int, string?>();
        if (hybridTaxIds.Count > 0)
        {
            foreach (var h in await _context.TaxHybridConfigs.AsNoTracking()
                .Where(h => hybridTaxIds.Contains(h.TaxId))
                .ToListAsync(cancellationToken))
            {
                hybridResultBase[h.TaxId] = h.ResultBase;
            }

            // Pick the rule whose rows were most recently touched (edited or created), not just
            // most recently inserted (Id order) — otherwise switching back to an earlier rule and
            // editing its existing rows would never be reflected here, since no new Ids are created.
            var masterSourceByTax = await (
                from m in _context.TaxMasterMappings.AsNoTracking()
                where hybridTaxIds.Contains(m.TaxId) && m.RuleDefinitionId != null && m.IsActive
                join r in _context.DynamicTaxRuleMaster.AsNoTracking() on m.RuleDefinitionId equals r.Id
                where r.RuleType == "MASTER_BASED"
                group new { Touched = m.UpdatedDate ?? m.CreatedDate, r.AttachedReference } by m.TaxId into g
                select new { TaxId = g.Key, Source = g.OrderByDescending(x => x.Touched).Select(x => x.AttachedReference).First() }
            ).ToListAsync(cancellationToken);

            foreach (var m in masterSourceByTax)
            {
                hybridMasterSource[m.TaxId] = m.Source;
            }
        }

        // CONDITION_BASED's Source used to be the linked rule's AttachedReference (an external
        // RuleEngine RuleCode) — conditions now live per-Tax in PTIS.TaxConditionRule instead, so
        // Source becomes a row count (scoped to the tax's currently selected rule slot, same
        // stale-row-avoidance idea as hybridMasterSource above).
        // Condition-only modes: a hybrid also uses condition rules, but its Source shows the
        // master+base combination instead (handled above), so it is excluded here.
        var conditionTaxIds = raw.Where(x => x.UsesConditionConfig && !x.UsesHybridConfig).Select(x => x.Id).ToList();
        var conditionRowCounts = new Dictionary<int, int>();
        if (conditionTaxIds.Count > 0)
        {
            var counts = await (
                from c in _context.TaxConditionRules.AsNoTracking()
                join t in _context.TaxMaster.AsNoTracking() on c.TaxId equals t.Id
                where conditionTaxIds.Contains(c.TaxId) && c.IsActive
                   && (c.RuleDefinitionId == null || c.RuleDefinitionId == t.RuleDefinitionId)
                group c by c.TaxId into g
                select new { TaxId = g.Key, Count = g.Count() }
            ).ToListAsync(cancellationToken);
            foreach (var c in counts) conditionRowCounts[c.TaxId] = c.Count;
        }

        var rows = raw.Select(x => new DynamicTaxRegisterRowDto
        {
            TaxId = x.Id,
            TaxName = x.TaxName,
            TaxNameAlias = x.TaxNameAlias,
            TaxCode = x.TaxCode,
            CalculationMode = x.CalculationMode ?? string.Empty,
            RuleDefinitionId = x.RuleDefinitionId,
            RuleName = x.RuleName,
            RuleCategory = RuleCategoryFor(x.UsesValueConfig, x.UsesConditionConfig, x.UsesMasterConfig, x.UsesHybridConfig),
            Source = x.UsesHybridConfig
                ? CombineHybridSource(hybridMasterSource.GetValueOrDefault(x.Id), hybridResultBase.GetValueOrDefault(x.Id))
                : x.UsesConditionConfig
                    ? FormatConditionSource(conditionRowCounts.GetValueOrDefault(x.Id))
                    : x.RuleRef,
            Status = x.IsActive ? "ACTIVE" : "DEACTIVE",
            AssessmentStatus = x.AssessmentStatus,
            OldTaxStatus = x.OldTaxStatus,
            RuleSummary = SummaryFor(x.UsesValueConfig, x.UsesConditionConfig, x.UsesMasterConfig, x.UsesHybridConfig)
        }).ToList();

        return new PagedResult<DynamicTaxRegisterRowDto>(rows, totalCount, pageNumber, effectivePageSize);
    }

    /// <summary>Formats a HYBRID tax's Source/Basis as "{MasterSource} + {ResultBase}" (e.g. "PropertyType + RV").</summary>
    private static string? CombineHybridSource(string? masterSource, string? resultBase)
    {
        var hasBase = !string.IsNullOrWhiteSpace(resultBase) && resultBase != "NONE";
        if (!string.IsNullOrWhiteSpace(masterSource) && hasBase) return $"{masterSource} + {resultBase}";
        if (!string.IsNullOrWhiteSpace(masterSource)) return masterSource;
        return hasBase ? resultBase : null;
    }

    /// <summary>Formats a CONDITION_BASED tax's Source/Basis as its active condition-row count.</summary>
    private static string FormatConditionSource(int count) => count switch
    {
        0 => "No condition rows",
        1 => "1 condition row",
        _ => $"{count} condition rows"
    };

    public async Task<DynamicTaxRegisterStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var excludedCategoryIds = await GetExcludedCategoryIdsAsync(cancellationToken);

        var counts = await _context.TaxMaster.AsNoTracking()
            .Where(t => !excludedCategoryIds.Contains(t.TaxCategoryId))
            .GroupBy(t => t.CalculationModeMaster!.ModeCode)
            .Select(g => new { Mode = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int Get(string mode) => counts.FirstOrDefault(c => c.Mode == mode)?.Count ?? 0;

        // The four codes are named explicitly here because DynamicTaxRegisterStatsDto has four
        // fixed properties, mirroring the four hero cards on the register screen. A mode added in
        // the DB is fully usable but will not get its own card until this contract becomes a
        // per-mode list — the one remaining place the four originals are still spelled out.
        var stats = new DynamicTaxRegisterStatsDto
        {
            ValueBased = Get("VALUE_BASED"),
            ConditionBased = Get("CONDITION_BASED"),
            MasterBased = Get("MASTER_BASED"),
            Hybrid = Get("HYBRID")
        };
        // Summed from every mode actually present, not the four named fields above — a DB-added
        // 5th mode has no hero card yet, but its taxes must still count toward the total (matching
        // the register grid's own TotalCount, which counts every mode without exception).
        stats.Total = counts.Sum(c => c.Count);
        return stats;
    }

    public async Task<List<TaxCategoryOptionDto>> GetTaxCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaxCategoryMaster.AsNoTracking()
            .Where(c => c.IsActive && !ExcludedCategoryCodes.Contains(c.CategoryCode))
            .OrderBy(c => c.Id)
            .Select(c => new TaxCategoryOptionDto { Id = c.Id, Code = c.CategoryCode, Name = c.CategoryName })
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<TaxCalculationModeDto>> GetCalculationModesAsync(CancellationToken cancellationToken = default)
        => _modeService.GetActiveAsync(cancellationToken);

    public async Task<bool> UpdateSettingsAsync(
        int taxId,
        UpdateTaxRegisterSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var isActive = ValidateAndParseStatus(request.Status);

        // Validated against PTIS.TaxCalculationModeMaster, not a hardcoded list — an unknown or
        // deactivated mode is rejected. Deliberately strict about blank too: the DTO has no
        // default, so an omitted CalculationMode is an error rather than a silent "VALUE_BASED"
        // that would wipe this tax's configuration below.
        var newModeRow = await _modeService.GetByCodeAsync(request.CalculationMode, cancellationToken);
        if (newModeRow is null)
        {
            var available = await _modeService.GetActiveAsync(cancellationToken);
            throw new ArgumentException(
                $"CalculationMode '{request.CalculationMode}' is invalid. Must be one of: {string.Join(", ", available.Select(m => m.ModeCode))}.");
        }

        if (request.RuleDefinitionId.HasValue)
        {
            var ruleType = await _context.DynamicTaxRuleMaster
                .Where(r => r.Id == request.RuleDefinitionId.Value && r.IsActive)
                .Select(r => r.RuleType)
                .FirstOrDefaultAsync(cancellationToken);
            if (ruleType is null)
            {
                throw new ArgumentException($"RuleDefinitionId={request.RuleDefinitionId} does not exist or is inactive.");
            }
            if (!string.Equals(ruleType, newModeRow.ModeCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"RuleDefinitionId={request.RuleDefinitionId} belongs to mode '{ruleType}', not '{newModeRow.ModeCode}'.");
            }
        }

        // Everything from here on reads-then-writes the SAME TaxMaster row, so it runs inside one
        // transaction with a pessimistic lock (WITH UPDLOCK) taken on the initial read. Without
        // this, two concurrent settings saves for the same tax (most dangerously two different
        // mode changes) both read the same "old" mode, both pass the ExpectedCurrentMode check
        // below, and both proceed — the loser's cleanup and mode write race the winner's with no
        // detection. UPDLOCK serializes them: the second request blocks here until the first
        // commits, then reads the ALREADY-CHANGED mode, so its own ExpectedCurrentMode check
        // (built from what its caller saw before either request started) now correctly fails and
        // throws the existing TaxModeChangeConflictException — a 409, not a silent race.
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var tax = await _context.TaxMaster
                .FromSqlInterpolated($"SELECT * FROM [PTIS].[TaxMaster] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {taxId}")
                .FirstOrDefaultAsync(cancellationToken);
            if (tax is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // Capture the stored mode BEFORE overwriting — everything below keys off old-vs-new.
            var oldModeRow = await _modeService.GetByIdAsync(tax.CalculationModeId, cancellationToken);
            var oldMode = oldModeRow?.ModeCode ?? string.Empty;
            var modeChanged = oldModeRow is null || oldModeRow.Id != newModeRow.Id;

            if (modeChanged)
            {
                // The caller may have rendered its confirmation against a stale view of this tax (its
                // own fetch can fail soft, or a concurrent request already changed the mode under the
                // lock above), in which case whatever it warned the user about does not match what we
                // are about to delete. Refuse rather than guess.
                var expectedMode = request.ExpectedCurrentMode?.Trim();
                if (string.IsNullOrWhiteSpace(expectedMode))
                {
                    throw TaxModeChangeConflictException.ExpectedModeRequired(oldMode, newModeRow.ModeCode);
                }

                if (!string.Equals(expectedMode, oldMode, StringComparison.OrdinalIgnoreCase))
                {
                    throw TaxModeChangeConflictException.StaleClient(oldMode, expectedMode.ToUpperInvariant());
                }

                if (!request.ConfirmModeChangeCleanup)
                {
                    throw TaxModeChangeConflictException.ConfirmationRequired(oldMode, newModeRow.ModeCode);
                }

                await StageModeChangeCleanupAsync(taxId, oldModeRow, newModeRow, request.RuleDefinitionId, cancellationToken);
            }

            // Tax Name is now editable from the General tab — update it when a (non-blank) value is
            // sent; a null/blank leaves the stored name untouched.
            if (!string.IsNullOrWhiteSpace(request.TaxName))
            {
                tax.TaxName = request.TaxName.Trim();
            }

            // Alias is optional, so blank means "cleared" rather than "unchanged" (otherwise an admin
            // could never remove one). Only a null — i.e. the field was not sent at all — leaves it be.
            if (request.TaxNameAlias is not null)
            {
                var alias = request.TaxNameAlias.Trim();
                tax.TaxNameAlias = alias.Length == 0 ? null : alias;
            }

            tax.IsActive = isActive;
            // Null means "not supplied — leave unchanged" (see the DTO's own doc-comment); only a
            // non-nullable bool used to be accepted here, so an omitted field silently reset it.
            if (request.AssessmentStatus.HasValue) tax.AssessmentStatus = request.AssessmentStatus.Value;
            if (request.OldTaxStatus.HasValue) tax.OldTaxStatus = request.OldTaxStatus.Value;
            tax.CalculationModeId = newModeRow.Id;
            tax.RuleDefinitionId = request.RuleDefinitionId;
            tax.UpdatedBy = request.UpdatedBy;
            tax.UpdatedDate = DateTime.Now;

            // One SaveChanges covers the TaxMaster update AND every staged delete. Do NOT convert
            // the deletes to ExecuteDeleteAsync: that runs as its own auto-committed statement,
            // outside the explicit transaction here, and breaks the atomicity guarantee.
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Marks the abandoned mode's configuration rows for deletion (not saved here — the caller's
    /// single SaveChangesAsync commits them together with the TaxMaster update). Two passes:
    /// tables the old mode used and the new one doesn't are emptied entirely; tables BOTH modes
    /// use keep only rows belonging to the new rule (or to no rule at all).
    /// </summary>
    private async Task StageModeChangeCleanupAsync(
        int taxId,
        TaxCalculationModeDto? oldTables,
        TaxCalculationModeDto newTables,
        int? newRuleDefinitionId,
        CancellationToken cancellationToken)
    {
        // Capability flags come straight from PTIS.TaxCalculationModeMaster — nothing here asks
        // "is this HYBRID?". A mode row whose flags are all false simply has nothing to clean up.
        if (oldTables is null) return;

        if (oldTables.UsesValueConfig && !newTables.UsesValueConfig)
        {
            _context.TaxPercentageMasterRVs.RemoveRange(
                await _context.TaxPercentageMasterRVs.Where(p => p.TaxId == taxId).ToListAsync(cancellationToken));
        }

        if (oldTables.UsesConditionConfig && !newTables.UsesConditionConfig)
        {
            _context.TaxConditionRules.RemoveRange(
                await _context.TaxConditionRules.Where(c => c.TaxId == taxId).ToListAsync(cancellationToken));
        }
        else if (oldTables.UsesConditionConfig && newTables.UsesConditionConfig)
        {
            // Kept table: drop rows scoped to a DIFFERENT rule, which would otherwise survive
            // invisibly (the drawer and grid both scope condition rows by the tax's current rule).
            // Null RuleDefinitionId means "applies to any rule" — left alone.
            _context.TaxConditionRules.RemoveRange(
                await _context.TaxConditionRules
                    .Where(c => c.TaxId == taxId && c.RuleDefinitionId != null && c.RuleDefinitionId != newRuleDefinitionId)
                    .ToListAsync(cancellationToken));
        }

        if (oldTables.UsesMasterConfig && !newTables.UsesMasterConfig)
        {
            _context.TaxMasterMappings.RemoveRange(
                await _context.TaxMasterMappings.Where(m => m.TaxId == taxId).ToListAsync(cancellationToken));
        }
        else if (oldTables.UsesMasterConfig && newTables.UsesMasterConfig)
        {
            // Same as above — a mode that also uses master config (e.g. Hybrid) keeps mappings
            // carrying its own master rule's id, which is not the id the new mode reads under.
            _context.TaxMasterMappings.RemoveRange(
                await _context.TaxMasterMappings
                    .Where(m => m.TaxId == taxId && m.RuleDefinitionId != null && m.RuleDefinitionId != newRuleDefinitionId)
                    .ToListAsync(cancellationToken));
        }

        if (oldTables.UsesHybridConfig && !newTables.UsesHybridConfig)
        {
            _context.TaxHybridConfigs.RemoveRange(
                await _context.TaxHybridConfigs.Where(h => h.TaxId == taxId).ToListAsync(cancellationToken));
        }
    }

    public async Task<TaxConfigSummaryDto?> GetConfigSummaryAsync(
        int taxId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.TaxMaster.AsNoTracking().AnyAsync(t => t.Id == taxId, cancellationToken);
        if (!exists) return null;

        // Counts every row regardless of IsActive, matching exactly what the cleanup above
        // deletes — so a confirmation built from these numbers can never under-report.
        return new TaxConfigSummaryDto
        {
            TaxId = taxId,
            ValueRowCount = await _context.TaxPercentageMasterRVs.AsNoTracking().CountAsync(p => p.TaxId == taxId, cancellationToken),
            ConditionRowCount = await _context.TaxConditionRules.AsNoTracking().CountAsync(c => c.TaxId == taxId, cancellationToken),
            MasterMappingCount = await _context.TaxMasterMappings.AsNoTracking().CountAsync(m => m.TaxId == taxId, cancellationToken),
            HasHybridConfig = await _context.TaxHybridConfigs.AsNoTracking().AnyAsync(h => h.TaxId == taxId, cancellationToken),
        };
    }

    /// <summary>Parses Status into the IsActive bool it maps to, rejecting anything but "ACTIVE"/
    /// "DEACTIVE" (case-insensitive). Previously any other value — including a typo — silently
    /// mapped to false (deactivated) with no error.</summary>
    private static bool ValidateAndParseStatus(string? status)
    {
        var trimmed = status?.Trim();
        if (string.Equals(trimmed, "ACTIVE", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(trimmed, "DEACTIVE", StringComparison.OrdinalIgnoreCase)) return false;
        throw new ArgumentException($"Status '{status}' is invalid. Must be one of: ACTIVE, DEACTIVE.");
    }

    public async Task<int> CreateAsync(
        CreateTaxRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoryExists = await _context.TaxCategoryMaster.AnyAsync(c => c.Id == request.TaxCategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new ArgumentException($"TaxCategoryId={request.TaxCategoryId} does not exist.");
        }
        if (request.RuleDefinitionId.HasValue)
        {
            var ruleExists = await _context.DynamicTaxRuleMaster.AnyAsync(r => r.Id == request.RuleDefinitionId.Value, cancellationToken);
            if (!ruleExists)
            {
                throw new ArgumentException($"RuleDefinitionId={request.RuleDefinitionId} does not exist.");
            }
        }

        // Resolved against PTIS.TaxCalculationModeMaster — same DB-driven validation as the
        // settings update, so a new tax can never be created in an unknown/retired mode.
        var modeRow = await _modeService.GetByCodeAsync(request.CalculationMode, cancellationToken);
        if (modeRow is null)
        {
            var available = await _modeService.GetActiveAsync(cancellationToken);
            throw new ArgumentException(
                $"CalculationMode '{request.CalculationMode}' is invalid. Must be one of: {string.Join(", ", available.Select(m => m.ModeCode))}.");
        }

        var isActive = ValidateAndParseStatus(request.Status);

        var tax = new Core.Entities.Master.TaxMasterEntity
        {
            TaxName = request.TaxName.Trim(),
            TaxNameAlias = string.IsNullOrWhiteSpace(request.TaxNameAlias) ? null : request.TaxNameAlias.Trim(),
            TaxCode = request.TaxCode.Trim(),
            TaxCategoryId = request.TaxCategoryId,
            CalculationModeId = modeRow.Id,
            RuleDefinitionId = request.RuleDefinitionId,
            IsActive = isActive,
            AssessmentStatus = request.AssessmentStatus,
            OldTaxStatus = request.OldTaxStatus,
            CreatedBy = request.CreatedBy,
            CreatedDate = DateTime.Now
        };

        _context.TaxMaster.Add(tax);
        await _context.SaveChangesAsync(cancellationToken);
        return tax.Id;
    }

    public async Task<ConfigOverviewPageDto> GetConfigOverviewAsync(
        ConfigOverviewQueryParameters qp,
        CancellationToken cancellationToken = default)
    {
        var tab = string.IsNullOrWhiteSpace(qp.Tab) ? ConfigOverviewTab.Value : qp.Tab.Trim();
        var result = new ConfigOverviewPageDto
        {
            Tab = tab,
            PageNumber = qp.PageNumber < 1 ? 1 : qp.PageNumber,
            PageSize = qp.PageSize
        };

        // Year-range label lookup — AssessmentYearRange has no label column, so compose "{From}-{To}".
        var yearRanges = await _context.AssessmentYearRangeEntities.AsNoTracking()
            .Select(y => new { y.Id, y.FromYear, y.ToYear })
            .ToListAsync(cancellationToken);
        var yearLabelById = yearRanges.ToDictionary(y => y.Id, y => $"{y.FromYear}-{y.ToYear}");

        // Tax identities (fetched once). EDU/EMP-category taxes are excluded here, which flows
        // through to whichever bucket is requested below.
        var excludedCategoryIds = await GetExcludedCategoryIdsAsync(cancellationToken);
        var taxes = await _context.TaxMaster.AsNoTracking()
            .Where(t => !excludedCategoryIds.Contains(t.TaxCategoryId))
            .Select(t => new
            {
                t.Id,
                t.TaxName,
                t.TaxCode,
                t.DisplayOrder,
                t.RuleDefinitionId,
                // Capability flags, so each tab below selects its taxes by what a mode USES
                // rather than by comparing its code to a literal.
                UsesValueConfig = t.CalculationModeMaster != null && t.CalculationModeMaster.UsesValueConfig,
                UsesConditionConfig = t.CalculationModeMaster != null && t.CalculationModeMaster.UsesConditionConfig,
                UsesMasterConfig = t.CalculationModeMaster != null && t.CalculationModeMaster.UsesMasterConfig,
                UsesHybridConfig = t.CalculationModeMaster != null && t.CalculationModeMaster.UsesHybridConfig,
            })
            .ToListAsync(cancellationToken);
        var taxById = taxes.ToDictionary(t => t.Id);

        // Only the requested section is materialized; each is filtered then paged (in memory,
        // because the value pivot, the condition JSON, and the master-name resolution all defy a
        // pure SQL Skip/Take — the browser still receives just one page).
        switch (tab)
        {
            case ConfigOverviewTab.Value:
            {
                var valueTaxIds = taxes.Where(t => t.UsesValueConfig).Select(t => t.Id).ToList();

                // Columns = every value-based tax (never filtered — filters only narrow the rows).
                result.ValueTaxes = taxes
                    .Where(t => t.UsesValueConfig)
                    .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Id)
                    .Select(t => new OverviewTaxDto { TaxId = t.Id, TaxName = t.TaxName, TaxCode = t.TaxCode })
                    .ToList();

                var rows = new List<ValueOverviewRowDto>();
                if (valueTaxIds.Count > 0)
                {
                    var pctRows = await (
                        from p in _context.TaxPercentageMasterRVs.AsNoTracking()
                        join u in _context.TypeOfUse.AsNoTracking() on p.TypeOfUseId equals u.Id into uj
                        from tou in uj.DefaultIfEmpty()
                        // YearRangeRVId/TypeOfUseId are plain indexed columns on this table — pushed
                        // into SQL instead of fetching every value-based row and filtering the pivot
                        // afterward. TypeOfUseGroupId stays in-memory below (it lives on the joined
                        // TypeOfUse side and the pivot still needs every tax's percentage per key).
                        where valueTaxIds.Contains(p.TaxId)
                           && (!qp.YearRangeRVId.HasValue || p.YearRangeRVId == qp.YearRangeRVId.Value)
                           && (!qp.TypeOfUseId.HasValue || p.TypeOfUseId == qp.TypeOfUseId.Value)
                        select new
                        {
                            p.TaxId,
                            p.TypeOfUseId,
                            p.YearRangeRVId,
                            p.TaxPercentage,
                            TypeOfUseCode = tou != null ? tou.TypeOfUseCode : null,
                            Description = tou != null ? tou.Description : null,
                            Type = tou != null ? tou.Type : null,
                            TypeOfUseGroupId = tou != null ? tou.TypeOfUseGroupId : 0
                        }
                    ).ToListAsync(cancellationToken);

                    var pivot = new Dictionary<(int TypeOfUseId, int YearRangeRVId), ValueOverviewRowDto>();
                    var groupByTypeOfUse = new Dictionary<int, int>();
                    foreach (var r in pctRows)
                    {
                        groupByTypeOfUse[r.TypeOfUseId] = r.TypeOfUseGroupId;
                        var key = (r.TypeOfUseId, r.YearRangeRVId);
                        if (!pivot.TryGetValue(key, out var row))
                        {
                            row = new ValueOverviewRowDto
                            {
                                TypeOfUseId = r.TypeOfUseId,
                                TypeOfUseCode = r.TypeOfUseCode,
                                Description = r.Description,
                                Type = r.Type,
                                YearRangeRVId = r.YearRangeRVId,
                                YearRangeLabel = yearLabelById.GetValueOrDefault(r.YearRangeRVId, string.Empty)
                            };
                            pivot[key] = row;
                        }
                        row.Percentages[r.TaxId] = r.TaxPercentage;
                    }

                    // YearRangeRVId/TypeOfUseId are already applied above, in SQL.
                    IEnumerable<ValueOverviewRowDto> q = pivot.Values;
                    if (qp.TypeOfUseGroupId.HasValue)
                        q = q.Where(x => groupByTypeOfUse.GetValueOrDefault(x.TypeOfUseId) == qp.TypeOfUseGroupId.Value);

                    rows = q.OrderBy(x => x.TypeOfUseId).ThenBy(x => x.YearRangeRVId).ToList();
                }

                result.TotalCount = rows.Count;
                result.ValueRows = PaginateInMemory(rows, result.PageNumber, qp.PageSize, out var effValue);
                result.PageSize = effValue;
                break;
            }

            case ConfigOverviewTab.Condition:
            case ConfigOverviewTab.HybridCondition:
            {
                var isHybrid = tab == ConfigOverviewTab.HybridCondition;
                // The Hybrid tabs show multi-surface modes; the plain tabs show modes that use
                // ONLY that surface — so the two never overlap and a tax appears in exactly one.
                var ids = taxes
                    .Where(t => isHybrid ? t.UsesHybridConfig : t.UsesConditionConfig && !t.UsesHybridConfig)
                    .Select(t => t.Id).ToList();

                var rows = new List<ConditionOverviewRowDto>();
                if (ids.Count > 0)
                {
                    // Same IsActive + rule-scope filter as the register grid's own condition-row
                    // count (GetRegisterAsync) — without this, the same admin sees a different
                    // "how many condition rows does this tax have" number on the register badge
                    // vs. this read-only overview for the exact same tax.
                    var condEntities = await (
                        from c in _context.TaxConditionRules.AsNoTracking()
                        join t in _context.TaxMaster.AsNoTracking() on c.TaxId equals t.Id
                        where ids.Contains(c.TaxId) && c.IsActive
                           && (c.RuleDefinitionId == null || c.RuleDefinitionId == t.RuleDefinitionId)
                        select c
                    )
                        .OrderBy(c => c.TaxId).ThenBy(c => c.SortOrder).ThenBy(c => c.Id)
                        .ToListAsync(cancellationToken);

                    // Names for OTHER_TAX rows' referenced taxes. Queried directly rather than read
                    // from taxById: that dictionary is filtered (EDU/EMP excluded), so a referenced
                    // tax outside it would silently come back unnamed.
                    var referencedTaxIds = condEntities
                        .Where(c => c.ReferenceTaxId.HasValue)
                        .Select(c => c.ReferenceTaxId!.Value)
                        .Distinct()
                        .ToList();
                    var referenceTaxNameById = referencedTaxIds.Count == 0
                        ? new Dictionary<int, string>()
                        : await _context.TaxMaster.AsNoTracking()
                            .Where(t => referencedTaxIds.Contains(t.Id))
                            .Select(t => new { t.Id, t.TaxName, t.TaxCode })
                            .ToDictionaryAsync(
                                t => t.Id,
                                t => string.IsNullOrWhiteSpace(t.TaxName) ? t.TaxCode : t.TaxName,
                                cancellationToken);

                    foreach (var c in condEntities)
                    {
                        var tax = taxById.GetValueOrDefault(c.TaxId);
                        rows.Add(new ConditionOverviewRowDto
                        {
                            TaxId = c.TaxId,
                            TaxName = tax?.TaxName,
                            TaxCode = tax?.TaxCode,
                            SortOrder = c.SortOrder,
                            Conditions = ParseConditions(c.ConditionsJson),
                            ResultMode = c.ResultMode,
                            ResultBase = c.ResultBase,
                            ResultValue = c.ResultValue,
                            UnitFieldId = c.UnitFieldId,
                            ReferenceTaxName = c.ReferenceTaxId.HasValue
                                ? referenceTaxNameById.GetValueOrDefault(c.ReferenceTaxId.Value)
                                : null,
                            IsActive = c.IsActive,
                            StopFurtherProcessing = c.StopFurtherProcessing,
                            AssessmentBasis = c.IsBuildingBased ? "BUILDING_BASED" : "PROPERTY_BASED",
                            AssessmentYearRangeId = c.AssessmentYearRangeId,
                            YearRangeLabel = c.AssessmentYearRangeId.HasValue
                                ? yearLabelById.GetValueOrDefault(c.AssessmentYearRangeId.Value)
                                : null
                        });
                    }
                }

                result.TotalCount = rows.Count;
                result.ConditionRows = PaginateInMemory(rows, result.PageNumber, qp.PageSize, out var effCond);
                result.PageSize = effCond;
                break;
            }

            case ConfigOverviewTab.Master:
            case ConfigOverviewTab.HybridMaster:
            {
                var isHybrid = tab == ConfigOverviewTab.HybridMaster;
                var ids = taxes
                    .Where(t => isHybrid ? t.UsesHybridConfig : t.UsesMasterConfig && !t.UsesHybridConfig)
                    .Select(t => t.Id).ToList();

                var rows = new List<MasterOverviewRowDto>();
                if (ids.Count > 0)
                {
                    // See the master-name resolution note on ResolveMasterName: match each row's
                    // (id + denormalized DisplayValue) against each master's (id + name).
                    var typeOfUseSet = (await _context.TypeOfUse.AsNoTracking()
                        .Select(x => new { x.Id, x.Description }).ToListAsync(cancellationToken))
                        .Select(x => MasterMatchKey(x.Id.ToString(), x.Description)).ToHashSet();
                    var ownerSet = (await _context.OwnerTypeMaster.AsNoTracking()
                        .Select(x => new { x.Id, x.OwnerType }).ToListAsync(cancellationToken))
                        .Select(x => MasterMatchKey(x.Id.ToString(), x.OwnerType)).ToHashSet();
                    var propertySet = (await _context.PropertyTypeMasters.AsNoTracking()
                        .Select(x => new { x.Id, x.PropertyDescription }).ToListAsync(cancellationToken))
                        .Select(x => MasterMatchKey(x.Id.ToString(), x.PropertyDescription)).ToHashSet();

                    var mapQuery = _context.TaxMasterMappings.AsNoTracking()
                        .Where(m => ids.Contains(m.TaxId) && m.IsActive);
                    if (qp.TaxId.HasValue)
                        mapQuery = mapQuery.Where(m => m.TaxId == qp.TaxId.Value);

                    var mapRows = await (
                        from m in mapQuery
                        join r in _context.DynamicTaxRuleMaster.AsNoTracking() on m.RuleDefinitionId equals r.Id into rj
                        from rule in rj.DefaultIfEmpty()
                        orderby m.TaxId, m.Id
                        select new
                        {
                            m.TaxId,
                            m.MasterKey,
                            m.DisplayValue,
                            m.ResultMode,
                            m.ResultBase,
                            m.ResultValue,
                            m.AssessmentYearRangeId,
                            RuleRef = rule != null ? rule.AttachedReference : null
                        })
                        .ToListAsync(cancellationToken);

                    foreach (var m in mapRows)
                    {
                        var tax = taxById.GetValueOrDefault(m.TaxId);
                        rows.Add(new MasterOverviewRowDto
                        {
                            TaxId = m.TaxId,
                            TaxName = tax?.TaxName,
                            TaxCode = tax?.TaxCode,
                            MasterName = ResolveMasterName(m.MasterKey, m.DisplayValue, m.RuleRef, typeOfUseSet, ownerSet, propertySet),
                            MasterKey = m.MasterKey,
                            DisplayValue = m.DisplayValue,
                            ResultMode = m.ResultMode,
                            ResultBase = m.ResultBase,
                            ResultValue = m.ResultValue,
                            AssessmentYearRangeId = m.AssessmentYearRangeId,
                            YearRangeLabel = yearLabelById.GetValueOrDefault(m.AssessmentYearRangeId)
                        });
                    }

                    // MasterName is derived in memory (not a column), so its filter is applied here.
                    if (!string.IsNullOrWhiteSpace(qp.MasterName))
                    {
                        var name = qp.MasterName.Trim();
                        rows = rows
                            .Where(x => string.Equals(x.MasterName, name, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                }

                result.TotalCount = rows.Count;
                result.MasterRows = PaginateInMemory(rows, result.PageNumber, qp.PageSize, out var effMaster);
                result.PageSize = effMaster;
                break;
            }

            default:
                // Unknown tab → empty page; Tab is echoed back so the caller can detect the mismatch.
                result.TotalCount = 0;
                result.PageSize = PagingGuard.Normalize(result.PageNumber, qp.PageSize, 0).EffectivePageSize;
                break;
        }

        return result;
    }

    /// <summary>In-memory Skip/Take over an already-built (and filtered) list. Mirrors the register
    /// feature's paging convention, including <c>pageSize == -1</c> meaning "return everything"
    /// (see PagingGuard for the ceiling under that).</summary>
    private static List<T> PaginateInMemory<T>(List<T> source, int pageNumber, int pageSize, out int effectivePageSize)
    {
        var (_, effective, skip) = PagingGuard.Normalize(pageNumber, pageSize, source.Count);
        effectivePageSize = effective;
        return source.Skip(skip).Take(effectivePageSize).ToList();
    }

    /// <summary>Composite (id + name) match key for identifying which master a mapping row belongs to.
    /// Both sides normalize the same way (trim), so a stored MasterKey/DisplayValue can be tested
    /// against a master's Id/name.</summary>
    private static string MasterMatchKey(string? key, string? name) =>
        $"{(key ?? string.Empty).Trim()}\u0001{(name ?? string.Empty).Trim()}";

    /// <summary>Resolves a mapping row's master (PropertyType / OwnerType / TypeOfUse) by matching
    /// its (MasterKey + DisplayValue) against each master's (id + name). Falls back to the linked
    /// rule's AttachedReference when nothing matches (custom/legacy rows whose display drifted).</summary>
    private static string? ResolveMasterName(
        string? masterKey,
        string? displayValue,
        string? ruleRef,
        HashSet<string> typeOfUse,
        HashSet<string> owner,
        HashSet<string> property)
    {
        var composite = MasterMatchKey(masterKey, displayValue);
        if (typeOfUse.Contains(composite)) return "TypeOfUse";
        if (owner.Contains(composite)) return "OwnerType";
        if (property.Contains(composite)) return "PropertyType";
        return ruleRef;
    }

    /// <summary>Deserializes a stored ConditionsJson blob; malformed/legacy JSON degrades to an invalid
    /// state item rather than an empty catch-all list (mirrors <c>TaxConditionRuleService.ParseConditions</c>).</summary>
    private static List<TaxConditionItemDto> ParseConditions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<TaxConditionItemDto>>(json) ?? new();
        }
        catch
        {
            return new() { new TaxConditionItemDto { FieldId = "__INVALID_JSON__", Operator = "__INVALID__" } };
        }
    }

    /// <summary>Register category badge, per the register UI (Value / Field / Data).</summary>
    /// <summary>
    /// The UI's rule category ("Value" / "Field" / "Data"), derived from a mode's CAPABILITIES
    /// rather than its code — a mode using more than one configuration surface has no single
    /// category (the drawer renders its combined Hybrid view instead), hence null.
    /// </summary>
    private static string? RuleCategoryFor(bool usesValue, bool usesCondition, bool usesMaster, bool usesHybrid)
    {
        if (usesHybrid) return null;
        if (usesValue) return "Value";
        if (usesCondition) return "Field";
        if (usesMaster) return "Data";
        return null;
    }

    /// <summary>Short human description of how a mode computes, from its capability flags.</summary>
    private static string SummaryFor(bool usesValue, bool usesCondition, bool usesMaster, bool usesHybrid)
    {
        if (usesHybrid) return "Master + condition";
        if (usesValue) return "Percentage based";
        if (usesCondition) return "Condition rules";
        if (usesMaster) return "Master mapping";
        return string.Empty;
    }
}
