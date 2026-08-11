using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Condition-based tax configuration engine. Uses <see cref="ApplicationDbContext"/> directly
/// (keyed reads + transactional bulk upsert), mirroring <see cref="MasterBasedTaxService"/>'s
/// pattern. <see cref="EvaluateAsync"/> is a standalone evaluator — it never touches the live
/// billing pipeline (RateableValueService/RateableValueTaxCalculator).
/// </summary>
public class TaxConditionRuleService : ITaxConditionRuleService
{
    private static readonly HashSet<string> ValidResultModes = new() { "FIXED", "PERCENT", "PER_UNIT" };
    private static readonly HashSet<string> ValidResultBases = new() { "NONE", "RV", "ALV", "OTHER_TAX" };

    /// <summary>Per-mode ceilings for ResultValue. PERCENT and FIXED share the long-standing
    /// 3-digit limit; PER_UNIT is a RATE that gets multiplied by a count, so it needs headroom for
    /// real currency amounts. The DTO's [Range] is only an absolute backstop; these are the real
    /// rules.</summary>
    private static decimal MaxResultValueFor(string resultMode) => resultMode switch
    {
        "PERCENT" => 100m,
        "PER_UNIT" => 99999m,
        _ => 999m,
    };

    private readonly ApplicationDbContext _context;
    private readonly IRepository<RulesFieldEntity, int> _rulesFieldRepo;
    private readonly IPropertyContextLoaderService _propertyContextLoaderService;
    private readonly IPropertyFieldFlattenerService _fieldFlattener;
    private readonly IConditionRuleEvaluator _evaluator;
    private readonly ITaxMasterDataService _masterDataService;
    private readonly IFinanceYearProvider _financeYearProvider;

    public TaxConditionRuleService(
        ApplicationDbContext context,
        IRepository<RulesFieldEntity, int> rulesFieldRepo,
        IPropertyContextLoaderService propertyContextLoaderService,
        IPropertyFieldFlattenerService fieldFlattener,
        IConditionRuleEvaluator evaluator,
        ITaxMasterDataService masterDataService,
        IFinanceYearProvider financeYearProvider)
    {
        _context = context;
        _rulesFieldRepo = rulesFieldRepo;
        _propertyContextLoaderService = propertyContextLoaderService;
        _fieldFlattener = fieldFlattener;
        _evaluator = evaluator;
        _masterDataService = masterDataService;
        _financeYearProvider = financeYearProvider;
    }

    /// <summary>
    /// Single home for every mode/base/value rule. Also NORMALIZES the row: fields that belong to
    /// another mode are nulled out, because leaving a stale value behind would desynchronize this
    /// service's duplicate-signature guard from the client's (which reads the value the UI already
    /// cleared) — two rows rendering identically would then pass one check and fail the other.
    /// </summary>
    private static void ValidateAndNormalizeResult(TaxConditionRuleDto row)
    {
        if (!ValidResultModes.Contains(row.ResultMode))
        {
            throw new ArgumentException($"Invalid ResultMode '{row.ResultMode}'. Must be one of: {string.Join(", ", ValidResultModes)}.");
        }
        if (!ValidResultBases.Contains(row.ResultBase))
        {
            throw new ArgumentException($"Invalid ResultBase '{row.ResultBase}'. Must be one of: {string.Join(", ", ValidResultBases)}.");
        }

        // A base only means anything for PERCENT; PER_UNIT and FIXED carry no base.
        if (row.ResultMode != "PERCENT")
        {
            row.ResultBase = "NONE";
        }

        if (row.ResultBase != "OTHER_TAX")
        {
            row.ReferenceTaxId = null;
        }
        else if (row.ReferenceTaxId is null)
        {
            throw new ArgumentException("ReferenceTaxId is required when ResultBase is 'OTHER_TAX'.");
        }

        if (row.ResultMode == "PER_UNIT")
        {
            row.UnitFieldId = row.UnitFieldId?.Trim();
            if (string.IsNullOrWhiteSpace(row.UnitFieldId))
            {
                throw new ArgumentException("UnitFieldId is required when ResultMode is 'PER_UNIT' — it names the numeric field the rate is multiplied by.");
            }
        }
        else
        {
            row.UnitFieldId = null;
        }

        var maxValue = MaxResultValueFor(row.ResultMode);
        if (row.ResultValue > maxValue)
        {
            throw new ArgumentException($"ResultValue {row.ResultValue} exceeds the maximum of {maxValue} for ResultMode '{row.ResultMode}'.");
        }
    }

    /// <summary>Maps the wire AssessmentBasis value to TaxConditionRuleEntity.IsBuildingBased.</summary>
    private static bool ValidateAndParseAssessmentBasis(string? assessmentBasis)
    {
        var trimmed = assessmentBasis?.Trim();
        if (string.Equals(trimmed, "PROPERTY_BASED", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(trimmed, "BUILDING_BASED", StringComparison.OrdinalIgnoreCase)) return true;
        throw new ArgumentException($"AssessmentBasis '{assessmentBasis}' is invalid. Must be one of: PROPERTY_BASED, BUILDING_BASED.");
    }

    private static List<TaxConditionItemDto> ParseConditions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<TaxConditionItemDto>>(json) ?? new();
        }
        catch
        {
            // Malformed/legacy JSON never 500s a read — surfaces an invalid-state signal,
            // matching ConditionRuleEvaluator so read DTOs reflect the non-matching behavior
            // and resaving without edits does not turn an invalid rule into an active catch-all.
            return new() { new TaxConditionItemDto { FieldId = "__INVALID_JSON__", Operator = "__INVALID__" } };
        }
    }

    public async Task<PagedResult<TaxConditionRuleDto>> GetByTaxAsync(
        int taxId,
        int? ruleDefinitionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TaxConditionRules
            .AsNoTracking()
            .Where(c => c.TaxId == taxId);

        if (ruleDefinitionId.HasValue)
        {
            query = query.Where(c => c.RuleDefinitionId == ruleDefinitionId.Value);
        }

        query = query.OrderBy(c => c.SortOrder).ThenBy(c => c.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var (normalizedPageNumber, effectivePageSize, skip) = PagingGuard.Normalize(pageNumber, pageSize, totalCount);
        pageNumber = normalizedPageNumber;

        // Conditions is JSON — can't translate to SQL, so materialize scalars first, then
        // deserialize in-memory.
        var entities = await query
            .Skip(skip)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken);

        var rows = entities.Select(c => new TaxConditionRuleDto
        {
            Id = c.Id,
            TaxId = c.TaxId,
            RuleDefinitionId = c.RuleDefinitionId,
            SortOrder = c.SortOrder,
            Conditions = ParseConditions(c.ConditionsJson),
            AssessmentYearRangeId = c.AssessmentYearRangeId,
            ResultMode = c.ResultMode,
            ResultBase = c.ResultBase,
            ResultValue = c.ResultValue,
            ReferenceTaxId = c.ReferenceTaxId,
            UnitFieldId = c.UnitFieldId,
            IsActive = c.IsActive,
            StopFurtherProcessing = c.StopFurtherProcessing,
            AssessmentBasis = c.IsBuildingBased ? "BUILDING_BASED" : "PROPERTY_BASED",
        }).ToList();

        return new PagedResult<TaxConditionRuleDto>(rows, totalCount, pageNumber, effectivePageSize);
    }

    public async Task<int> SaveAsync(
        SaveTaxConditionRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        foreach (var row in request.Rows)
        {
            ValidateAndNormalizeResult(row);

            if (row.ResultBase == "OTHER_TAX" && row.ReferenceTaxId == request.TaxId)
            {
                throw new ArgumentException("A condition rule cannot reference its own tax as ReferenceTaxId.");
            }

            // An empty Conditions list is a valid "always matches" catch-all row — only the
            // shape of each *present* item is validated.
            foreach (var item in row.Conditions)
            {
                if (string.IsNullOrWhiteSpace(item.FieldId) || string.IsNullOrWhiteSpace(item.Operator))
                {
                    throw new ArgumentException("Each condition item must have a non-blank FieldId and Operator.");
                }

                // Normalize to a known value rather than rejecting the save — a blank/garbage
                // LogicalOperator (e.g. an older client that never sent one) defaults to AND,
                // matching the evaluator's own left-to-right fold behavior.
                item.LogicalOperator = string.Equals(item.LogicalOperator, "OR", StringComparison.OrdinalIgnoreCase)
                    ? "OR"
                    : "AND";
            }
        }

        // OTHER_TAX rows must reference an existing, active tax (any CalculationMode — not
        // restricted to VALUE_BASED). Note a non-VALUE_BASED reference's persisted
        // PropertyTaxCalculationRVResults.TaxAmount is always 0 today (see
        // RateableValueTaxCalculator.ApplyTax), so EvaluateAsync will compute 0 for those until
        // the calculation engine wires up the other modes. One batched lookup for every
        // referenced id across all rows, rather than a query per row.
        var referenceTaxIds = request.Rows
            .Where(r => r.ResultBase == "OTHER_TAX" && r.ReferenceTaxId.HasValue)
            .Select(r => r.ReferenceTaxId!.Value)
            .Distinct()
            .ToList();
        if (referenceTaxIds.Count > 0)
        {
            var validReferenceTaxIds = await _context.TaxMaster
                .AsNoTracking()
                .Where(t => referenceTaxIds.Contains(t.Id) && t.IsActive)
                .Select(t => t.Id)
                .ToListAsync(cancellationToken);
            var invalidIds = referenceTaxIds.Except(validReferenceTaxIds).ToList();
            if (invalidIds.Count > 0)
            {
                throw new ArgumentException(
                    $"ReferenceTaxId must point to an existing, active tax. Invalid id(s): {string.Join(", ", invalidIds)}.");
            }
        }

        // PER_UNIT rows must name a field that actually exists and is active. Without this a typo
        // or a since-renamed field saves cleanly and then silently evaluates to 0 forever — the
        // same reasoning as the ReferenceTaxId check above. Matched on FieldName OR
        // DatabaseColumnName because the client sends whichever it has (see the UI's fieldId).
        var unitFieldIds = request.Rows
            .Where(r => r.ResultMode == "PER_UNIT" && !string.IsNullOrWhiteSpace(r.UnitFieldId))
            .Select(r => r.UnitFieldId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (unitFieldIds.Count > 0)
        {
            var knownFieldKeys = await _rulesFieldRepo.GetQueryable()
                .AsNoTracking()
                .Where(f => f.IsActive)
                .Select(f => new { f.FieldName, f.DatabaseColumnName })
                .ToListAsync(cancellationToken);

            var knownSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in knownFieldKeys)
            {
                if (!string.IsNullOrWhiteSpace(f.FieldName)) knownSet.Add(f.FieldName.Trim());
                if (!string.IsNullOrWhiteSpace(f.DatabaseColumnName)) knownSet.Add(f.DatabaseColumnName.Trim());
            }

            var unknownFields = unitFieldIds.Where(id => !knownSet.Contains(id)).ToList();
            if (unknownFields.Count > 0)
            {
                throw new ArgumentException(
                    $"UnitFieldId must name an existing, active rule field. Unknown field(s): {string.Join(", ", unknownFields)}.");
            }
        }

        // Block exact-duplicate rows within this tax's set (same conditions + assessment year +
        // result). They are redundant regardless of StopFurtherProcessing — under the current
        // sum-all-matches evaluation, two identical rows would both match and double-count the same
        // amount — so we reject the save rather than persist copies (LogicalOperator is already
        // normalized above, so identical conditions serialize identically here).
        //
        // Reads are paged and saves are explicitly upsert-only — seed seenSignatures from existing
        // persisted rows for this tax/rule (excluding IDs being updated by this request) so a new
        // row cannot duplicate an existing persisted row on another page/request.
        var updatingIds = request.Rows.Where(r => r.Id > 0).Select(r => r.Id).ToList();

        var existingPersistedQuery = _context.TaxConditionRules
            .AsNoTracking()
            .Where(c => c.TaxId == request.TaxId && c.RuleDefinitionId == request.RuleDefinitionId);

        if (updatingIds.Count > 0)
        {
            existingPersistedQuery = existingPersistedQuery.Where(c => !updatingIds.Contains(c.Id));
        }

        var existingPersistedRows = await existingPersistedQuery.ToListAsync(cancellationToken);

        var seenSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var existing in existingPersistedRows)
        {
            var existingConditions = ParseConditions(existing.ConditionsJson);
            var signature = string.Join(
                "|",
                JsonSerializer.Serialize(existingConditions),
                existing.AssessmentYearRangeId?.ToString(CultureInfo.InvariantCulture) ?? "null",
                existing.ResultMode,
                existing.ResultBase,
                existing.ResultValue.ToString(CultureInfo.InvariantCulture),
                existing.ReferenceTaxId?.ToString(CultureInfo.InvariantCulture) ?? "null",
                existing.UnitFieldId ?? "null");
            seenSignatures.Add(signature);
        }

        foreach (var row in request.Rows)
        {
            var signature = string.Join(
                "|",
                JsonSerializer.Serialize(row.Conditions),
                row.AssessmentYearRangeId?.ToString(CultureInfo.InvariantCulture) ?? "null",
                row.ResultMode,
                row.ResultBase,
                row.ResultValue.ToString(CultureInfo.InvariantCulture),
                row.ReferenceTaxId?.ToString(CultureInfo.InvariantCulture) ?? "null",
                row.UnitFieldId ?? "null");
            if (!seenSignatures.Add(signature))
            {
                throw new ArgumentException(
                    "Duplicate condition rule: two rows have the same conditions, assessment year, and result. Remove the duplicate before saving.");
            }
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // No natural key exists (ConditionsJson is arbitrary JSON) — match by Id only,
            // scoped to this TaxId so a stale/mismatched Id from another tax falls through to
            // an insert instead of silently overwriting that other tax's row.
            var ids = request.Rows.Where(r => r.Id > 0).Select(r => r.Id).ToList();
            var existingById = await _context.TaxConditionRules
                .Where(c => ids.Contains(c.Id)
                         && c.TaxId == request.TaxId
                         && c.RuleDefinitionId == request.RuleDefinitionId)
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            var affected = 0;
            foreach (var row in request.Rows)
            {
                var conditionsJson = JsonSerializer.Serialize(row.Conditions);

                if (row.Id > 0 && existingById.TryGetValue(row.Id, out var entity))
                {
                    entity.RuleDefinitionId = request.RuleDefinitionId;
                    entity.SortOrder = row.SortOrder;
                    entity.ConditionsJson = conditionsJson;
                    entity.AssessmentYearRangeId = row.AssessmentYearRangeId;
                    entity.ResultMode = row.ResultMode;
                    entity.ResultBase = row.ResultBase;
                    entity.ResultValue = row.ResultValue;
                    entity.ReferenceTaxId = row.ReferenceTaxId;
                    entity.UnitFieldId = row.UnitFieldId;
                    entity.IsActive = row.IsActive;
                    entity.StopFurtherProcessing = row.StopFurtherProcessing;
                    entity.IsBuildingBased = ValidateAndParseAssessmentBasis(row.AssessmentBasis);
                    entity.UpdatedBy = request.UpdatedBy;
                    entity.UpdatedDate = DateTime.Now;
                }
                else
                {
                    entity = new TaxConditionRuleEntity
                    {
                        TaxId = request.TaxId,
                        RuleDefinitionId = request.RuleDefinitionId,
                        SortOrder = row.SortOrder,
                        ConditionsJson = conditionsJson,
                        AssessmentYearRangeId = row.AssessmentYearRangeId,
                        ResultMode = row.ResultMode,
                        ResultBase = row.ResultBase,
                        ResultValue = row.ResultValue,
                        ReferenceTaxId = row.ReferenceTaxId,
                        UnitFieldId = row.UnitFieldId,
                        IsActive = row.IsActive,
                        StopFurtherProcessing = row.StopFurtherProcessing,
                        IsBuildingBased = ValidateAndParseAssessmentBasis(row.AssessmentBasis),
                        CreatedBy = request.UpdatedBy,
                        CreatedDate = DateTime.Now,
                    };
                    _context.TaxConditionRules.Add(entity);
                }

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

    public async Task DeleteAsync(int id, int taxId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.TaxConditionRules
            .FirstOrDefaultAsync(c => c.Id == id && c.TaxId == taxId, cancellationToken);
        if (entity == null)
        {
            throw new ArgumentException($"Condition rule row Id={id} not found for TaxId={taxId}.");
        }

        _context.TaxConditionRules.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EvaluateTaxConditionRuleResponseDto> EvaluateAsync(
        EvaluateTaxConditionRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var financeYear = request.FinanceYear ?? _financeYearProvider.GetCurrentFinanceYear();

        PropertyCalculationContext propertyContext;
        try
        {
            propertyContext = await _propertyContextLoaderService.LoadPropertyContextAsync(request.PropertyId, financeYear, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new ArgumentException(ex.Message);
        }

        var response = new EvaluateTaxConditionRuleResponseDto
        {
            TaxId = request.TaxId,
            PropertyId = request.PropertyId,
        };

        if (propertyContext.Details.Count == 0)
        {
            throw new ArgumentException($"Property {request.PropertyId} has no active details to evaluate against.");
        }

        var detail = request.PropertyDetailsId.HasValue
            ? propertyContext.Details.FirstOrDefault(d => d.Id == request.PropertyDetailsId.Value)
            : propertyContext.Details.OrderBy(d => d.Id).FirstOrDefault();

        if (detail == null)
        {
            throw new ArgumentException($"PropertyDetailsId={request.PropertyDetailsId} does not belong to PropertyId={request.PropertyId}.");
        }

        response.PropertyDetailsId = detail.Id;

        var typeOfUses = await _masterDataService.GetActiveTypeOfUsesAsync();
        var detailTypeOfUse = typeOfUses.FirstOrDefault(t => t.Id == detail.TypeOfUseId);

        // Same "nothing to evaluate" guard as RuleApplierService.ApplyRulesAsync — graceful,
        // not an exception.
        if (detail.FloorId <= 0 || detailTypeOfUse == null || detailTypeOfUse.TypeOfUseGroupId <= 0)
        {
            response.Matched = false;
            response.Trace.Add(new TaxConditionRuleEvaluationTraceDto
            {
                Skipped = true,
                SkipReason = "Property detail has no resolved Floor/TypeOfUseGroup — nothing to evaluate.",
            });
            return response;
        }

        var activeFields = await _rulesFieldRepo.GetQueryable()
            .Where(f => f.IsActive)
            .ToListAsync(cancellationToken);

        var clonedContext = propertyContext.CloneForDetail(detail, detailTypeOfUse);
        var fieldValues = _fieldFlattener.Flatten(clonedContext, activeFields);

        var detailYearRangeRVId = propertyContext.DetailYearRangeRVIdMap.TryGetValue(detail.Id, out var yr)
            ? yr
            : propertyContext.Parameters.YearRangeRVId;

        var rows = await (
                 from c in _context.TaxConditionRules.AsNoTracking()
                 join t in _context.TaxMaster.AsNoTracking() on c.TaxId equals t.Id
                 where c.TaxId == request.TaxId
                    && c.IsActive
                    && (c.RuleDefinitionId == null || c.RuleDefinitionId == t.RuleDefinitionId)
                 orderby c.SortOrder, c.Id
                 select c)
            .ToListAsync(cancellationToken);

        // RV/ALV base is shared property-level context, not per-row — compute it once up front if
        // ANY candidate row might need it, rather than re-querying per matched row.
        decimal? rvSum = null;
        double? alvSum = null;
        var currentFy = _financeYearProvider.GetCurrentFinanceYear();
        var isCurrentYear = (financeYear == currentFy);

        if (rows.Any(r => r.ResultBase is "RV" or "ALV"))
        {
            if (isCurrentYear)
            {
                var rvAlvByDetail = await _context.RVCalculationResults
                    .AsNoTracking()
                    .Where(r => r.PropertyId == request.PropertyId && r.IsActive && !r.MarkedForDeletion)
                    .GroupBy(r => r.PropertyDetailsId)
                    .Select(g => new { RV = g.Max(x => x.RateableValue) ?? 0m, ALV = g.Max(x => x.AnnualRentalValue) ?? 0d })
                    .ToListAsync(cancellationToken);

                rvSum = rvAlvByDetail.Sum(x => x.RV);
                alvSum = rvAlvByDetail.Sum(x => x.ALV);
            }
            else
            {
                var yearMaster = await _context.YearMaster
                    .AsNoTracking()
                    .FirstOrDefaultAsync(y => y.Year == financeYear && y.IsActive, cancellationToken);

                if (yearMaster != null)
                {
                    var transMastRows = await _context.TransMast
                        .AsNoTracking()
                        .Where(t => t.PropertyId == request.PropertyId && t.FinanceYearId == yearMaster.Id && t.IsActive && !t.MarkedForDeletion)
                        .ToListAsync(cancellationToken);

                    var rvRow = transMastRows.FirstOrDefault(t => string.Equals(t.CalculationType, "RV", StringComparison.OrdinalIgnoreCase));
                    if (rvRow != null)
                    {
                        rvSum = rvRow.CalculationValue;
                        alvSum = (double?)(rvRow.CalculationAnnualValue ?? 0m);
                    }
                }
            }

            response.RateableValueUsed = rvSum;
            response.AnnualRentalValueUsed = alvSum;
        }

        var matchedResults = new List<TaxConditionRuleMatchResultDto>();

        foreach (var row in rows)
        {
            if (row.AssessmentYearRangeId.HasValue && row.AssessmentYearRangeId.Value != detailYearRangeRVId)
            {
                response.Trace.Add(new TaxConditionRuleEvaluationTraceDto
                {
                    RuleId = row.Id,
                    SortOrder = row.SortOrder,
                    Skipped = true,
                    SkipReason = "AssessmentYearRangeId does not match this property detail's resolved year range.",
                });
                continue;
            }

            var evalResult = _evaluator.Evaluate(row.ConditionsJson, fieldValues);
            response.Trace.Add(new TaxConditionRuleEvaluationTraceDto
            {
                RuleId = row.Id,
                SortOrder = row.SortOrder,
                IsMatch = evalResult.IsMatch,
                UnresolvedFields = evalResult.ConditionResults
                    .Where(c => !c.FieldResolved)
                    .Select(c => c.FieldId)
                    .ToList(),
                Conditions = evalResult.ConditionResults,
            });

            if (!evalResult.IsMatch)
            {
                continue;
            }

            var matchResult = new TaxConditionRuleMatchResultDto
            {
                RuleId = row.Id,
                SortOrder = row.SortOrder,
                ResultMode = row.ResultMode,
                ResultBase = row.ResultBase,
                StoppedFurtherProcessing = row.StopFurtherProcessing,
            };

            // PER_UNIT — a rate multiplied by a per-property count (e.g. 150 per toilet). The count
            // comes from the SAME already-built fieldValues dictionary the conditions matched
            // against, resolved through the evaluator so the key convention and numeric coercion can
            // never drift from what conditions accept.
            if (row.ResultMode == "PER_UNIT")
            {
                var resolved = _evaluator.TryResolveNumericField(row.UnitFieldId, fieldValues, out var unitCount);
                matchResult.UnitCountResolved = resolved;

                if (!resolved)
                {
                    // The row DID match, so a bare 0 would read as a confident answer — the caller
                    // must explain the count isn't recorded for this property rather than presenting
                    // a confident ₹0.
                    matchResult.UnitCountUsed = null;
                    matchResult.ComputedAmount = 0m;
                }
                else
                {
                    matchResult.UnitCountUsed = unitCount;
                    // Rounded like the PERCENT branches below — this is a derived amount, and a
                    // DECIMAL multiplier (e.g. an area) would otherwise show fractions where every
                    // sibling mode shows whole rupees.
                    matchResult.ComputedAmount = Math.Round(row.ResultValue * unitCount, 0, MidpointRounding.AwayFromZero);
                }
            }
            else if (row.ResultMode == "FIXED")
            {
                matchResult.ComputedAmount = row.ResultValue;
            }
            // PERCENT — OTHER_TAX reads the referenced tax's already-persisted amount (a flat,
            // non-recursive lookup — never calls RateableValueTaxCalculator/touches billing).
            // RV/ALV instead sum across this property's already-computed results, same guarantee.
            else if (row.ResultBase == "OTHER_TAX" && row.ReferenceTaxId.HasValue)
            {
                List<decimal> referenceTaxAmounts;
                if (isCurrentYear)
                {
                    referenceTaxAmounts = await (
                            from td in _context.RVCalculationTaxDetails.AsNoTracking()
                            join r in _context.RVCalculationResults.AsNoTracking() on td.RVCalculationResultsId equals r.Id
                            where r.PropertyId == request.PropertyId
                               && r.IsActive && !r.MarkedForDeletion
                               && td.IsActive && !td.MarkedForDeletion
                               && td.TaxId == row.ReferenceTaxId.Value
                            select td.TaxAmount ?? 0m)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    var yearMasterId = await _context.YearMaster.AsNoTracking()
                        .Where(y => y.Year == financeYear && y.IsActive)
                        .Select(y => (int?)y.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    referenceTaxAmounts = yearMasterId.HasValue
                        ? await _context.TransMast.AsNoTracking()
                            .Where(t => t.PropertyId == request.PropertyId
                                     && t.FinanceYearId == yearMasterId.Value
                                     && t.TaxId == row.ReferenceTaxId.Value
                                     && t.IsActive && !t.MarkedForDeletion)
                            .Select(t => t.TaxAmount)
                            .ToListAsync(cancellationToken)
                        : new List<decimal>();
                }

                matchResult.ReferenceTaxAmountResolved = referenceTaxAmounts.Count > 0;
                var referenceTaxAmount = referenceTaxAmounts.Sum();
                matchResult.ReferenceTaxAmountUsed = referenceTaxAmount;
                matchResult.ComputedAmount = Math.Round(referenceTaxAmount * row.ResultValue / 100m, 0, MidpointRounding.AwayFromZero);
            }
            else
            {
                var baseValue = row.ResultBase == "RV" ? (rvSum ?? 0m)
                    : row.ResultBase == "ALV" ? (decimal)(alvSum ?? 0d)
                    : 0m;
                matchResult.ComputedAmount = Math.Round(baseValue * row.ResultValue / 100m, 0, MidpointRounding.AwayFromZero);
            }

            matchedResults.Add(matchResult);

            if (row.StopFurtherProcessing)
            {
                break; // Reproduces first-match-wins from this row onward — rows below are never evaluated.
            }
        }

        response.Matched = matchedResults.Count > 0;
        response.ComputedAmount = matchedResults.Sum(m => m.ComputedAmount);
        response.MatchedResults = matchedResults;
        return response;
    }
}