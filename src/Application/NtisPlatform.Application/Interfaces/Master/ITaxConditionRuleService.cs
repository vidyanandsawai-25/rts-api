using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces.Master;

/// <summary>
/// Condition-based tax configuration: priority-ordered, flat AND/OR condition rows
/// (key → FIXED/PERCENT/PER_UNIT result) backing the CONDITION_BASED calculation mode of the
/// Dynamic Tax Register, plus a standalone evaluator to test a tax's saved rows against
/// a real property. Does not touch the live billing pipeline (RateableValueService).
/// </summary>
public interface ITaxConditionRuleService
{
    /// <summary>
    /// All condition rows for a tax (both active and inactive — admins need to see and
    /// re-enable inactive rows), optionally filtered by the linked rule.
    /// </summary>
    Task<PagedResult<TaxConditionRuleDto>> GetByTaxAsync(
        int taxId,
        int? ruleDefinitionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Insert/update the supplied rows transactionally. Returns rows affected.</summary>
    Task<int> SaveAsync(
        SaveTaxConditionRuleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes one condition row (a real SQL DELETE, not a soft IsActive flip) —
    /// scoped to <paramref name="taxId"/> so a stale/mismatched id from another tax can't be
    /// purged by mistake. Throws <see cref="ArgumentException"/> if no such row exists for
    /// that tax.
    /// </summary>
    Task DeleteAsync(int id, int taxId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a tax's saved, active condition rows (priority order — first match wins)
    /// against one real property's resolved field values. Never throws for a clean "no match"
    /// or an unresolved field; only invalid input (bad PropertyId/PropertyDetailsId) is surfaced
    /// as <see cref="ArgumentException"/>.
    /// </summary>
    Task<EvaluateTaxConditionRuleResponseDto> EvaluateAsync(
        EvaluateTaxConditionRuleRequest request,
        CancellationToken cancellationToken = default);
}
