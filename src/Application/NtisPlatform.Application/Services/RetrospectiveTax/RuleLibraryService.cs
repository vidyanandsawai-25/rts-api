using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants.RetrospectiveTax;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RuleLibrary;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RuleLibraryService : IRuleLibraryService
{
    private readonly IRepository<RetrospectiveRuleMasterEntity, int> _ruleRepository;
    private readonly IRepository<RetrospectiveRuleActionEntity, int> _actionRepository;
    private readonly IRepository<RetrospectivePenaltyRuleEntity, int> _penaltyRepository;
    private readonly IRepository<EvidenceTypeMasterEntity, int> _evidenceTypeRepository;
    private readonly IRepository<RetrospectiveTaxPolicyEntity, int> _taxPolicyRepository;

    public RuleLibraryService(
        IRepository<RetrospectiveRuleMasterEntity, int> ruleRepository,
        IRepository<RetrospectiveRuleActionEntity, int> actionRepository,
        IRepository<RetrospectivePenaltyRuleEntity, int> penaltyRepository,
        IRepository<EvidenceTypeMasterEntity, int> evidenceTypeRepository,
        IRepository<RetrospectiveTaxPolicyEntity, int> taxPolicyRepository)
    {
        _ruleRepository = ruleRepository;
        _actionRepository = actionRepository;
        _penaltyRepository = penaltyRepository;
        _evidenceTypeRepository = evidenceTypeRepository;
        _taxPolicyRepository = taxPolicyRepository;
    }

    public async Task<RuleLibraryDto> GetLibraryAsync(RuleLibraryQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = _ruleRepository.GetQueryable().Where(r => r.IsActive);
        query = query.ApplyFilters(queryParameters);
        query = query.ApplySearch(queryParameters);
        query = query.ApplySort(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedQuery = query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

        var rules = await pagedQuery.ToListAsync(cancellationToken);
        var ruleIds = rules.Select(r => r.Id).ToList();

        var actionsByRuleId = await _actionRepository.GetQueryable()
            .Where(a => a.IsActive && ruleIds.Contains(a.RuleId))
            .ToDictionaryAsync(a => a.RuleId, cancellationToken);

        var penaltiesByRuleId = await _penaltyRepository.GetQueryable()
            .Where(p => p.IsActive && ruleIds.Contains(p.RuleId))
            .ToDictionaryAsync(p => p.RuleId, cancellationToken);

        var evidenceNamesById = await _evidenceTypeRepository.GetQueryable()
            .Where(e => e.IsActive)
            .ToDictionaryAsync(e => e.Id, e => e.EvidenceName, cancellationToken);

        var rows = rules.Select(rule => BuildRow(rule, actionsByRuleId.GetValueOrDefault(rule.Id), penaltiesByRuleId.GetValueOrDefault(rule.Id), evidenceNamesById)).ToList();

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new RuleLibraryDto
        {
            CommonTaxation = await BuildCommonTaxationAsync(cancellationToken),
            Rules = new PagedResult<RuleLibraryRowDto>(rows, totalCount, pageNumber, pageSize)
        };
    }

    private async Task<RuleLibraryCommonTaxationDto?> BuildCommonTaxationAsync(CancellationToken cancellationToken)
    {
        var activePolicy = await _taxPolicyRepository.GetQueryable()
            .Where(p => p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (activePolicy is null)
            return null;

        var rateLabel = RetrospectiveTaxPolicyOptions.RateModes.FirstOrDefault(o => o.Code == activePolicy.RateMode)?.Label;
        var percentageLabel = RetrospectiveTaxPolicyOptions.PercentageModes.FirstOrDefault(o => o.Code == activePolicy.PercentageMode)?.Label;

        return new RuleLibraryCommonTaxationDto
        {
            RateModeCode = activePolicy.RateMode,
            RateModeLabel = rateLabel,
            PercentageModeCode = activePolicy.PercentageMode,
            PercentageModeLabel = percentageLabel
        };
    }

    private static RuleLibraryRowDto BuildRow(
        RetrospectiveRuleMasterEntity rule,
        RetrospectiveRuleActionEntity? action,
        RetrospectivePenaltyRuleEntity? penalty,
        Dictionary<int, string> evidenceNamesById)
    {
        return new RuleLibraryRowDto
        {
            Id = rule.Id,
            RuleCode = rule.RuleCode,
            RuleName = rule.RuleName,
            RuleStatus = rule.RuleStatus,
            AuthorizationStatus = rule.AuthorizationStatus,
            ConditionDescription = rule.RuleDescription,
            ConditionTag = BuildConditionTag(rule.AuthorizationStatus),
            StartLogicSummary = BuildStartLogicSummary(rule, action, evidenceNamesById),
            StartLogicBoundary = BuildStartLogicBoundary(action),
            TaxMultiplierNote = BuildTaxMultiplierNote(action, evidenceNamesById),
            PenaltySummary = BuildPenaltySummary(rule.AuthorizationStatus, penalty, evidenceNamesById)
        };
    }

    private static string? BuildConditionTag(string? authorizationStatus) => authorizationStatus switch
    {
        "AUTHORIZED" => "Authorized: OC or CC available",
        "UNAUTHORIZED" => "Unauthorized: OC & CC unavailable",
        "UNDETERMINED" => "Undetermined: rule condition incomplete",
        _ => null
    };

    private static string EvidenceName(int? evidenceTypeId, Dictionary<int, string> evidenceNamesById) =>
        evidenceTypeId.HasValue && evidenceNamesById.TryGetValue(evidenceTypeId.Value, out var name) ? name : "evidence";

    private static string? BuildStartLogicSummary(
        RetrospectiveRuleMasterEntity rule,
        RetrospectiveRuleActionEntity? action,
        Dictionary<int, string> evidenceNamesById)
    {
        if (action is null)
            return null;

        return action.TaxStartMode switch
        {
            "EVIDENCE_DATE" => $"From {EvidenceName(action.StartEvidenceTypeId, evidenceNamesById)} date",
            "FY_START" => $"1 April aligned to {EvidenceName(action.StartEvidenceTypeId, evidenceNamesById)} date",
            "NEXT_FINANCIAL_YEAR" => $"Next FY after {EvidenceName(action.StartEvidenceTypeId, evidenceNamesById)} date",
            "MONTHS_AFTER" => $"{action.OffsetMonths} months after {EvidenceName(action.StartEvidenceTypeId, evidenceNamesById)} date",
            "FIXED_CUTOFF" => action.CutoffDate.HasValue ? $"Fixed cutoff {action.CutoffDate:dd MMM yyyy}" : "Fixed cutoff date",
            "MAX_LOOK_BACK_DATE" => $"Rolling {rule.LegalCapYears}-year boundary",
            "CONSTRUCTION_YEAR" => "Construction year/date",
            "CONSTRUCTION_OR_CAP" => "Later of construction date or rolling cap",
            _ => null
        };
    }

    private static string? BuildStartLogicBoundary(RetrospectiveRuleActionEntity? action)
    {
        if (action is null)
            return null;

        return action.RetrospectiveLimitType switch
        {
            "MAXIMUM_YEARS" when action.MaximumYears.HasValue => $"Boundary: {action.MaximumYears} years",
            "FIXED_CUTOFF_DATE" when action.CutoffDate.HasValue => $"Boundary: {action.CutoffDate:yyyy-MM-dd}",
            _ => null
        };
    }

    private static string? BuildTaxMultiplierNote(RetrospectiveRuleActionEntity? action, Dictionary<int, string> evidenceNamesById)
    {
        if (action is null)
            return null;

        if (action.TaxCalculationMode == "SPLIT")
        {
            var startName = EvidenceName(action.SplitStartEvidenceTypeId, evidenceNamesById);
            var endName = EvidenceName(action.SplitEndEvidenceTypeId, evidenceNamesById);
            return $"{action.SplitMultiplier}x from {startName} date to {endName} date, then {action.AfterSplitMultiplier}x";
        }

        return action.TaxMultiplier != 1.00m ? $"Retrospective tax x {action.TaxMultiplier}" : null;
    }

    private static string? BuildPenaltySummary(
        string? authorizationStatus,
        RetrospectivePenaltyRuleEntity? penalty,
        Dictionary<int, string> evidenceNamesById)
    {
        if (authorizationStatus == "AUTHORIZED")
            return "Not applicable - OC/CC available";

        if (penalty is null || penalty.PenaltyMode == "NONE")
            return "Do not apply penalty";

        if (penalty.PenaltyMode is "ACT_PENALTY" or "ACT_UNLAWFUL")
            return "Apply penalty as per the Act";

        if (penalty.PenaltyMode == "DATE_VALIDATION")
        {
            var dateSourceLabel = penalty.PenaltyDateSourceType switch
            {
                "EVIDENCE_DATE" => $"{EvidenceName(penalty.PenaltyDateEvidenceTypeId, evidenceNamesById)} date",
                "ASSESSMENT_DATE" => "Assessment date",
                "FIXED_DATE" => "Fixed date",
                _ => "date"
            };

            if (penalty.PenaltyDateCondition == "BETWEEN")
            {
                return $"Apply when {dateSourceLabel} is between {penalty.CompareDate:dd MMM yyyy} and {penalty.CompareDateTo:dd MMM yyyy}";
            }

            var conditionLabel = penalty.PenaltyDateCondition switch
            {
                "ON_OR_AFTER" => "on or after",
                "AFTER" => "after",
                "ON_OR_BEFORE" => "on or before",
                "BEFORE" => "before",
                _ => "on"
            };

            return $"Apply when {dateSourceLabel} is {conditionLabel} {penalty.CompareDate:dd MMM yyyy}";
        }

        return "Do not apply penalty";
    }
}
