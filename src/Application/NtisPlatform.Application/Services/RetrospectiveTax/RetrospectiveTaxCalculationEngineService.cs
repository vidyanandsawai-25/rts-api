using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.RetrospectiveTax;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using OperationType = NtisPlatform.Application.Enums.OperationType;

namespace NtisPlatform.Application.Services.RetrospectiveTax;

public class RetrospectiveTaxCalculationEngineService : IRetrospectiveTaxCalculationEngineService
{
    /// <summary>
    /// Fixed day-count base for prorating the earliest (partial) chargeable finance year, matching
    /// the legacy Occupation Tax engine's policy (a partial year is always prorated against 365
    /// regardless of leap status; only a FULL year's own leap day was ever added back there, which
    /// this engine does not attempt to replicate since it has no day-based full-year billing model).
    /// </summary>
    private const int FinanceYearProrationBasisDays = 365;

    /// <summary>EvidenceTypeMaster.EvidenceCode -> PropertyCertificateTypeMaster.CertificateTypeCode.</summary>
    private static readonly Dictionary<string, string> EvidenceToCertificateCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OC"] = CertificateTypeCodes.OC,
        ["CC"] = CertificateTypeCodes.CC,
        ["ELECTRICITY"] = CertificateTypeCodes.ElectricBill,
        ["CHANGE_DETECTION"] = CertificateTypeCodes.ChangeDetection,
    };

    private readonly IRepository<RetrospectiveRuleMasterEntity, int> _ruleRepository;
    private readonly IRepository<RetrospectiveRuleEvidenceConditionEntity, int> _evidenceConditionRepository;
    private readonly IRepository<RetrospectiveRuleDateConditionEntity, int> _dateConditionRepository;
    private readonly IRepository<RetrospectiveRuleActionEntity, int> _actionRepository;
    private readonly IRepository<RetrospectivePenaltyRuleEntity, int> _penaltyRepository;
    private readonly IRepository<EvidenceTypeMasterEntity, int> _evidenceTypeRepository;
    private readonly IRepository<PropertyCertificateEntity, int> _certificateRepository;
    private readonly IRepository<PropertyCertificateTypeMasterEntity, int> _certificateTypeRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<WingDetailsMastEntity, int> _wingDetailsMastRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<RetrospectiveTaxCalculationEntity, int> _calculationRepository;
    private readonly IRepository<RetrospectiveTaxCalculationDetailEntity, int> _calculationDetailRepository;
    private readonly IRepository<PolicyTaxDetailsEntity, int> _policyTaxDetailsRepository;
    private readonly IRepository<TransMastEntity, int> _transMastRepository;
    private readonly IRepository<YearMasterEntity, int> _yearRepository;
    private readonly IRateableValueService _rateableValueService;
    private readonly IFinanceYearProvider _financeYearProvider;
    private readonly IPolicyCodeLookupService _policyCodeLookup;
    private readonly ITaxApplicabilityService _taxApplicabilityService;
    private readonly IUnitOfWork _unitOfWork;

    public RetrospectiveTaxCalculationEngineService(
        IRepository<RetrospectiveRuleMasterEntity, int> ruleRepository,
        IRepository<RetrospectiveRuleEvidenceConditionEntity, int> evidenceConditionRepository,
        IRepository<RetrospectiveRuleDateConditionEntity, int> dateConditionRepository,
        IRepository<RetrospectiveRuleActionEntity, int> actionRepository,
        IRepository<RetrospectivePenaltyRuleEntity, int> penaltyRepository,
        IRepository<EvidenceTypeMasterEntity, int> evidenceTypeRepository,
        IRepository<PropertyCertificateEntity, int> certificateRepository,
        IRepository<PropertyCertificateTypeMasterEntity, int> certificateTypeRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<RetrospectiveTaxCalculationEntity, int> calculationRepository,
        IRepository<RetrospectiveTaxCalculationDetailEntity, int> calculationDetailRepository,
        IRepository<PolicyTaxDetailsEntity, int> policyTaxDetailsRepository,
        IRepository<TransMastEntity, int> transMastRepository,
        IRepository<YearMasterEntity, int> yearRepository,
        IRateableValueService rateableValueService,
        IFinanceYearProvider financeYearProvider,
        IPolicyCodeLookupService policyCodeLookup,
        ITaxApplicabilityService taxApplicabilityService,
        IUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository;
        _evidenceConditionRepository = evidenceConditionRepository;
        _dateConditionRepository = dateConditionRepository;
        _actionRepository = actionRepository;
        _penaltyRepository = penaltyRepository;
        _evidenceTypeRepository = evidenceTypeRepository;
        _certificateRepository = certificateRepository;
        _certificateTypeRepository = certificateTypeRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _propertyRepository = propertyRepository;
        _wingDetailsMastRepository = wingDetailsMastRepository;
        _societyRepository = societyRepository;
        _calculationRepository = calculationRepository;
        _calculationDetailRepository = calculationDetailRepository;
        _policyTaxDetailsRepository = policyTaxDetailsRepository;
        _transMastRepository = transMastRepository;
        _yearRepository = yearRepository;
        _rateableValueService = rateableValueService;
        _financeYearProvider = financeYearProvider;
        _policyCodeLookup = policyCodeLookup;
        _taxApplicabilityService = taxApplicabilityService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RetrospectiveTaxEngineResultDto?> CalculateAndSaveAsync(
        int propertyId, int? calculatedBy, CancellationToken cancellationToken = default)
    {
        var currentFinanceYear = _financeYearProvider.GetCurrentFinanceYear();

        var evidenceTypes = await _evidenceTypeRepository.GetQueryable().Where(e => e.IsActive).ToListAsync(cancellationToken);
        var evidenceCodeById = evidenceTypes.ToDictionary(e => e.Id, e => e.EvidenceCode, EqualityComparer<int>.Default);

        var evidenceDates = await ResolveEvidenceDatesAsync(propertyId, cancellationToken);
        var constructionYear = await ResolveConstructionYearAsync(propertyId, cancellationToken);
        // Evidence-condition AVAILABLE/UNAVAILABLE checks (THA-08, PCM-06, FUR-03's "Construction
        // year AVAILABLE" condition) need this key present too, not just the TaxStartMode
        // resolution further below.
        evidenceDates["CONSTRUCTION_YEAR"] = constructionYear.HasValue ? new DateTime(constructionYear.Value, 4, 1) : null;

        var match = await FindMatchingRuleAsync(evidenceCodeById, evidenceDates, cancellationToken);
        if (match is null)
        {
            // No rule matches any more (certificate deleted/date cleared since a prior successful
            // run) -- a property that previously had a valid PolicyTaxDetails/TransMast row must not
            // keep showing it now that there is no computation to justify it.
            await SyncPolicyTaxAndTransMastAsync(propertyId, currentFinanceYear, new List<RetrospectiveTaxEngineYearDto>(), calculatedBy, cancellationToken);
            return null;
        }

        var (rule, action, penalty, dateCondition) = match.Value;

        var chargeableStart = ResolveChargeableStartDate(
            action, evidenceCodeById, evidenceDates, constructionYear, currentFinanceYear);

        if (chargeableStart is null)
        {
            // Matched rule needs an evidence date that isn't actually available — shouldn't happen
            // if evidence conditions were configured consistently with the tax-start evidence.
            throw new ValidationException(
                "AppliedRuleId",
                $"Rule '{rule.RuleCode}' matched, but its configured TaxStartMode evidence date could not be resolved.",
                OperationType.Update);
        }

        var years = BuildFinancialYears(chargeableStart.Value, currentFinanceYear);
        var earliestChargeableYear = years.Count > 0 ? years[0] : currentFinanceYear;

        var currentYearRateCache = new Dictionary<int, decimal>();
        var (penaltyPercent, requiresManualReview) = ResolvePenalty(penalty, evidenceCodeById, evidenceDates);

        var segments = action.TaxCalculationMode == "CC_THEN_OC_MERGE"
            ? BuildCcThenOcMergeSegments(action, years, earliestChargeableYear, chargeableStart.Value, evidenceCodeById, evidenceDates)
            : BuildDefaultSegments(action, years, earliestChargeableYear, chargeableStart.Value, evidenceCodeById, evidenceDates);

        var yearRows = new List<RetrospectiveTaxEngineYearDto>();
        foreach (var segment in segments)
        {
            var yearEnd = new DateTime(segment.FinanceYear + 1, 3, 31);
            var isFullYear = segment.FromDate == FyStart(segment.FinanceYear) && segment.ToDate == yearEnd;
            // Only a genuinely partial span is prorated against the fixed 365-day basis; a full
            // year keeps factor 1.0 even when it actually spans 366 days (its own FY-start year is
            // a leap year) -- otherwise a full leap finance year would be overcharged by 1/365.
            var prorationFactor = isFullYear
                ? 1m
                : ((segment.ToDate - segment.FromDate).Days + 1) / (decimal)FinanceYearProrationBasisDays;

            var fullYearBaseTax = await ResolveYearBaseTaxAsync(propertyId, segment.FinanceYear, action.RateMode, currentFinanceYear, currentYearRateCache);
            var baseTax = prorationFactor == 1m
                ? fullYearBaseTax
                : Math.Round(fullYearBaseTax * prorationFactor, 2, MidpointRounding.AwayFromZero);
            var retroTax = Math.Round(baseTax * segment.Multiplier, 2, MidpointRounding.AwayFromZero);
            var penaltyAmount = penaltyPercent.HasValue
                ? Math.Round(retroTax * penaltyPercent.Value / 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;

            yearRows.Add(new RetrospectiveTaxEngineYearDto
            {
                FinancialYear = FormatFinancialYear(segment.FinanceYear),
                FromDate = segment.FromDate,
                ToDate = segment.ToDate,
                RateMode = action.RateMode,
                BaseTaxAmount = baseTax,
                TaxMultiplier = segment.Multiplier,
                RetrospectiveTaxAmount = retroTax,
                PenaltyPercent = penaltyPercent,
                PenaltyAmount = penaltyAmount,
                TotalAmount = retroTax + penaltyAmount,
                PolicyFamily = segment.PolicyFamily
            });
        }

        var result = await PersistAsync(propertyId, rule, chargeableStart.Value, currentFinanceYear, yearRows, requiresManualReview, calculatedBy, cancellationToken);
        await SyncPolicyTaxAndTransMastAsync(propertyId, currentFinanceYear, yearRows, calculatedBy, cancellationToken);
        return result;
    }

    private async Task<Dictionary<string, DateTime?>> ResolveEvidenceDatesAsync(int propertyId, CancellationToken cancellationToken)
    {
        var certificateTypes = await _certificateTypeRepository.GetQueryable().ToListAsync(cancellationToken);
        var certificateTypeCodeById = certificateTypes.ToDictionary(t => t.Id, t => t.CertificateTypeCode);

        var certificates = await _certificateRepository.GetQueryable()
            .Where(c => c.PropertyId == propertyId && c.IsActive && !c.MarkedForDeletion && c.IssueDate != null)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, DateTime?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (evidenceCode, certificateCode) in EvidenceToCertificateCode)
        {
            result[evidenceCode] = certificates
                .Where(c => certificateTypeCodeById.TryGetValue(c.CertificateTypeId, out var typeCode)
                    && string.Equals(typeCode, certificateCode, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.IssueDate)
                .OrderByDescending(d => d)
                .FirstOrDefault();
        }

        // A unit with no direct Property-level certificate for a given evidence type may still be
        // covered by a Wing-level or Society-level certificate applying to every unit under it (see
        // PropertyCertificateApplicationService.SaveCertificateAsync, which creates those with no
        // PropertyId of their own) -- fall back to Wing then Society before treating evidence as
        // genuinely unavailable.
        var missingCodes = EvidenceToCertificateCode.Where(kv => result[kv.Key] is null).ToList();
        if (missingCodes.Count == 0)
        {
            return result;
        }

        var wingDetailId = await _propertyRepository.GetQueryable()
            .Where(p => p.Id == propertyId)
            .Select(p => p.WingDetailId)
            .FirstOrDefaultAsync(cancellationToken);

        int? societyDetailId = null;

        if (wingDetailId.HasValue)
        {
            societyDetailId = await _wingDetailsMastRepository.GetQueryable()
                .Where(w => w.Id == wingDetailId.Value)
                .Select(w => (int?)w.SocietyDetailsMastId)
                .FirstOrDefaultAsync(cancellationToken);

            var wingCertificates = await _certificateRepository.GetQueryable()
                .Where(c => c.EntityType == "W" && c.WingDetailId == wingDetailId.Value && c.IsActive && !c.MarkedForDeletion && c.IssueDate != null)
                .ToListAsync(cancellationToken);

            foreach (var (evidenceCode, certificateCode) in missingCodes)
            {
                result[evidenceCode] = wingCertificates
                    .Where(c => certificateTypeCodeById.TryGetValue(c.CertificateTypeId, out var typeCode)
                        && string.Equals(typeCode, certificateCode, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.IssueDate)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();
            }
        }
        else
        {
            // No wing -- this may itself be the society's own representative property (e.g. the
            // "apartment society property" with no partition), so resolve society membership that way.
            societyDetailId = await _societyRepository.GetQueryable()
                .Where(s => s.PropertyId == propertyId)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var stillMissingCodes = missingCodes.Where(kv => result[kv.Key] is null).ToList();
        if (stillMissingCodes.Count > 0 && societyDetailId.HasValue)
        {
            var societyCertificates = await _certificateRepository.GetQueryable()
                .Where(c => c.EntityType == "S" && c.SocietyDetailId == societyDetailId.Value && c.IsActive && !c.MarkedForDeletion && c.IssueDate != null)
                .ToListAsync(cancellationToken);

            foreach (var (evidenceCode, certificateCode) in stillMissingCodes)
            {
                result[evidenceCode] = societyCertificates
                    .Where(c => certificateTypeCodeById.TryGetValue(c.CertificateTypeId, out var typeCode)
                        && string.Equals(typeCode, certificateCode, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.IssueDate)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();
            }
        }

        return result;
    }

    private async Task<int?> ResolveConstructionYearAsync(int propertyId, CancellationToken cancellationToken)
    {
        var constructionYears = await _propertyDetailsRepository.GetQueryable()
            .Where(d => d.PropertyId == propertyId && !d.MarkedForDeletion)
            .Select(d => d.ConstructionYear)
            .ToListAsync(cancellationToken);

        var parsedYears = constructionYears
            .Select(y => int.TryParse(y, out var parsed) ? parsed : (int?)null)
            .Where(y => y.HasValue)
            .Select(y => y!.Value)
            .ToList();

        return parsedYears.Count > 0 ? parsedYears.Min() : null;
    }

    private async Task<(RetrospectiveRuleMasterEntity Rule, RetrospectiveRuleActionEntity Action, RetrospectivePenaltyRuleEntity? Penalty, RetrospectiveRuleDateConditionEntity? DateCondition)?> FindMatchingRuleAsync(
        Dictionary<int, string> evidenceCodeById,
        Dictionary<string, DateTime?> evidenceDates,
        CancellationToken cancellationToken)
    {
        var rules = await _ruleRepository.GetQueryable()
            .Where(r => r.IsActive && r.RuleStatus == "Active")
            .OrderBy(r => r.IsFallbackRule)
            .ThenBy(r => r.PriorityNo)
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
            return null;

        var ruleIds = rules.Select(r => r.Id).ToList();

        var evidenceConditions = await _evidenceConditionRepository.GetQueryable()
            .Where(c => ruleIds.Contains(c.RuleId) && c.IsActive)
            .ToListAsync(cancellationToken);
        var dateConditions = await _dateConditionRepository.GetQueryable()
            .Where(c => ruleIds.Contains(c.RuleId) && c.IsActive)
            .ToListAsync(cancellationToken);
        var actions = await _actionRepository.GetQueryable()
            .Where(a => ruleIds.Contains(a.RuleId) && a.IsActive)
            .ToListAsync(cancellationToken);
        var penalties = await _penaltyRepository.GetQueryable()
            .Where(p => ruleIds.Contains(p.RuleId) && p.IsActive)
            .ToListAsync(cancellationToken);

        var evidenceConditionsByRule = evidenceConditions.GroupBy(c => c.RuleId).ToDictionary(g => g.Key, g => g.ToList());
        var dateConditionByRule = dateConditions.GroupBy(c => c.RuleId).ToDictionary(g => g.Key, g => g.First());
        var actionByRule = actions.GroupBy(a => a.RuleId).ToDictionary(g => g.Key, g => g.First());
        var penaltyByRule = penalties.GroupBy(p => p.RuleId).ToDictionary(g => g.Key, g => g.First());

        foreach (var rule in rules)
        {
            if (!actionByRule.TryGetValue(rule.Id, out var action))
                continue; // a rule with no Action section can never be applied — skip it.

            var conditions = evidenceConditionsByRule.TryGetValue(rule.Id, out var c) ? c : new List<RetrospectiveRuleEvidenceConditionEntity>();
            if (!EvaluateEvidenceConditions(conditions, evidenceCodeById, evidenceDates))
                continue;

            var dateCondition = dateConditionByRule.TryGetValue(rule.Id, out var dc) ? dc : null;
            if (!EvaluateDateCondition(dateCondition, evidenceCodeById, evidenceDates))
                continue;

            var penalty = penaltyByRule.TryGetValue(rule.Id, out var p) ? p : null;
            return (rule, action, penalty, dateCondition);
        }

        return null;
    }

    private static bool EvaluateEvidenceConditions(
        List<RetrospectiveRuleEvidenceConditionEntity> conditions,
        Dictionary<int, string> evidenceCodeById,
        Dictionary<string, DateTime?> evidenceDates)
    {
        foreach (var condition in conditions)
        {
            if (!evidenceCodeById.TryGetValue(condition.EvidenceTypeId, out var code))
                continue;

            var isAvailable = evidenceDates.TryGetValue(code, out var date) && date.HasValue;
            if (condition.EvidenceState == "AVAILABLE" && !isAvailable)
                return false;
            if (condition.EvidenceState == "UNAVAILABLE" && isAvailable)
                return false;
        }

        return true;
    }

    private static bool EvaluateDateCondition(
        RetrospectiveRuleDateConditionEntity? condition,
        Dictionary<int, string> evidenceCodeById,
        Dictionary<string, DateTime?> evidenceDates)
    {
        if (condition is null || condition.ComparatorCode == "NONE")
            return true;

        DateTime? ResolveByEvidenceType(int? evidenceTypeId) =>
            evidenceTypeId.HasValue && evidenceCodeById.TryGetValue(evidenceTypeId.Value, out var code)
                ? evidenceDates.GetValueOrDefault(code)
                : null;

        DateTime? EvidenceOrDefault(string code) => evidenceDates.GetValueOrDefault(code);

        switch (condition.ComparatorCode)
        {
            case "ELECTRICITY_BEFORE_CC":
            {
                var left = ResolveByEvidenceType(condition.LeftEvidenceTypeId) ?? EvidenceOrDefault("ELECTRICITY");
                var right = ResolveByEvidenceType(condition.RightEvidenceTypeId) ?? EvidenceOrDefault("CC");
                return left.HasValue && right.HasValue && left.Value < right.Value;
            }
            case "ELECTRICITY_AFTER_CC":
            {
                var left = ResolveByEvidenceType(condition.LeftEvidenceTypeId) ?? EvidenceOrDefault("ELECTRICITY");
                var right = ResolveByEvidenceType(condition.RightEvidenceTypeId) ?? EvidenceOrDefault("CC");
                return left.HasValue && right.HasValue && left.Value > right.Value;
            }
            case "ELECTRICITY_BEFORE_CUTOFF":
            {
                var left = EvidenceOrDefault("ELECTRICITY");
                return left.HasValue && condition.CompareDate.HasValue && left.Value < condition.CompareDate.Value;
            }
            case "ELECTRICITY_AFTER_CUTOFF":
            {
                var left = EvidenceOrDefault("ELECTRICITY");
                return left.HasValue && condition.CompareDate.HasValue && left.Value > condition.CompareDate.Value;
            }
            case "OC_OLDER_THAN_ALLOWED_PERIOD":
            {
                var oc = EvidenceOrDefault("OC");
                return oc.HasValue && condition.CompareYears.HasValue
                    && oc.Value <= DateTime.Now.AddYears(-condition.CompareYears.Value);
            }
            case "OC_WITHIN_ALLOWED_PERIOD":
            {
                var oc = EvidenceOrDefault("OC");
                return oc.HasValue && condition.CompareYears.HasValue
                    && oc.Value > DateTime.Now.AddYears(-condition.CompareYears.Value);
            }
            case "EVIDENCE_GAP_WITHIN_PERIOD":
            {
                // Gap = RightEvidenceTypeId's date minus LeftEvidenceTypeId's date (e.g. OC minus
                // CC) -- a negative or missing gap never satisfies either comparator.
                var left = ResolveByEvidenceType(condition.LeftEvidenceTypeId);
                var right = ResolveByEvidenceType(condition.RightEvidenceTypeId);
                if (!left.HasValue || !right.HasValue || !condition.CompareYears.HasValue || right.Value < left.Value)
                    return false;

                var gapInUnits = CompareGapInUnits((right.Value - left.Value).Days, condition.CompareGapUnit);
                return condition.CompareOperator switch
                {
                    "WITHIN_YEARS" => gapInUnits <= condition.CompareYears.Value,
                    "OLDER_THAN_YEARS" => gapInUnits > condition.CompareYears.Value,
                    _ => false
                };
            }
            default:
                return true;
        }
    }

    /// <summary>Converts a day gap into the configured unit (DAYS/MONTHS/YEARS, default MONTHS).</summary>
    private static int CompareGapInUnits(int gapDays, string? unit) => unit switch
    {
        "DAYS" => gapDays,
        "YEARS" => gapDays / 365,
        _ => gapDays / 30 // MONTHS (default)
    };

    /// <summary>
    /// Resolves the earliest date retrospective tax is chargeable from, per the matched rule's
    /// Action configuration. TaxStartMode picks the evidence-driven start; RetrospectiveLimitType
    /// then floors it (can't go back further than the configured cap) — the two combine via
    /// MAX(), which is what makes a single rule correctly express both "start from the evidence
    /// date" and "but never more than N years back" at once.
    /// </summary>
    private static DateTime? ResolveChargeableStartDate(
        RetrospectiveRuleActionEntity action,
        Dictionary<int, string> evidenceCodeById,
        Dictionary<string, DateTime?> evidenceDates,
        int? constructionYear,
        int currentFinanceYear)
    {
        DateTime? startEvidenceDate = action.StartEvidenceTypeId.HasValue
            && evidenceCodeById.TryGetValue(action.StartEvidenceTypeId.Value, out var startCode)
                ? evidenceDates.GetValueOrDefault(startCode)
                : null;

        DateTime? evidenceStart = action.TaxStartMode switch
        {
            "EVIDENCE_DATE" => startEvidenceDate,
            "FY_START" => startEvidenceDate.HasValue ? FyStart(FyOf(startEvidenceDate.Value)) : null,
            "NEXT_FINANCIAL_YEAR" => startEvidenceDate.HasValue ? FyStart(FyOf(startEvidenceDate.Value) + 1) : null,
            "MONTHS_AFTER" => startEvidenceDate?.AddMonths(action.OffsetMonths ?? 0),
            "FIXED_CUTOFF" => action.CutoffDate,
            "CONSTRUCTION_YEAR" => constructionYear.HasValue ? FyStart(constructionYear.Value) : null,
            _ => null // MAX_LOOK_BACK_DATE / CONSTRUCTION_OR_CAP resolved entirely below
        };

        DateTime? limitFloor = action.RetrospectiveLimitType switch
        {
            "MAXIMUM_YEARS" => action.MaximumYears.HasValue ? FyStart(currentFinanceYear - action.MaximumYears.Value) : null,
            "FIXED_CUTOFF_DATE" => action.CutoffDate,
            _ => null
        };

        if (action.TaxStartMode == "MAX_LOOK_BACK_DATE")
            return limitFloor ?? FyStart(currentFinanceYear);

        if (action.TaxStartMode == "CONSTRUCTION_OR_CAP")
        {
            DateTime? constructionStart = constructionYear.HasValue ? FyStart(constructionYear.Value) : null;
            // "Construction year or N years, whichever gives fewer retro years" == whichever start
            // date is LATER (more recent) between the construction date and the cap.
            if (constructionStart.HasValue && limitFloor.HasValue)
                return constructionStart.Value > limitFloor.Value ? constructionStart.Value : limitFloor.Value;
            return constructionStart ?? limitFloor;
        }

        if (evidenceStart is null)
            return null;

        return limitFloor.HasValue && limitFloor.Value > evidenceStart.Value ? limitFloor.Value : evidenceStart.Value;
    }

    private static DateTime FyStart(int year) => new(year, 4, 1);

    private static int FyOf(DateTime date) => date.Month >= 4 ? date.Year : date.Year - 1;

    private static List<int> BuildFinancialYears(DateTime chargeableStart, int currentFinanceYear)
    {
        var startFinanceYear = FyOf(chargeableStart);
        var years = new List<int>();
        for (var year = startFinanceYear; year <= currentFinanceYear; year++)
            years.Add(year);
        return years;
    }

    /// <summary>One billed span within a single finance year: a whole year, or (for the earliest
    /// chargeable year, and for the CC/OC merge's boundary year) a partial one.</summary>
    private readonly record struct BillingSegment(int FinanceYear, DateTime FromDate, DateTime ToDate, decimal Multiplier, string PolicyFamily);

    /// <summary>
    /// Maps an evidence code to its PolicyCodes certificate family. Evidence with no direct family
    /// (e.g. a fallback rule with TaxStartMode = CONSTRUCTION_YEAR/MAX_LOOK_BACK_DATE, which has no
    /// StartEvidenceTypeId at all) resolves to ElectricBill, matching PolicyCodes.ElectricPartial's
    /// own documented reuse for the no-certificate-fallback case.
    /// </summary>
    private static string ResolvePolicyFamily(string? evidenceCode) => evidenceCode switch
    {
        "OC" => PolicyCodes.Oc,
        "CC" => PolicyCodes.Cc,
        _ => PolicyCodes.ElectricBill
    };

    /// <summary>
    /// Builds one billing segment per finance year, applying <see cref="ResolveMultiplierForYear"/>
    /// (SINGLE or SPLIT) per year and letting the earliest year start mid-year from
    /// <paramref name="chargeableStart"/> when it falls after that year's 01-Apr.
    /// </summary>
    private static List<BillingSegment> BuildDefaultSegments(
        RetrospectiveRuleActionEntity action, List<int> years, int earliestChargeableYear, DateTime chargeableStart,
        Dictionary<int, string> evidenceCodeById, Dictionary<string, DateTime?> evidenceDates)
    {
        var family = ResolvePolicyFamily(
            action.StartEvidenceTypeId.HasValue && evidenceCodeById.TryGetValue(action.StartEvidenceTypeId.Value, out var code)
                ? code
                : null);

        var segments = new List<BillingSegment>();
        foreach (var year in years)
        {
            var yearStart = FyStart(year);
            var yearEnd = new DateTime(year + 1, 3, 31);
            var fromDate = year == earliestChargeableYear && chargeableStart > yearStart ? chargeableStart : yearStart;
            var multiplier = ResolveMultiplierForYear(action, yearStart, evidenceCodeById, evidenceDates);
            segments.Add(new BillingSegment(year, fromDate, yearEnd, multiplier, family));
        }
        return segments;
    }

    /// <summary>
    /// CC-then-OC merge (TaxCalculationMode = CC_THEN_OC_MERGE): CC (the rule's own TaxStartMode
    /// evidence) governs every full finance year strictly before OC's onset year (SplitMultiplier —
    /// reusing the same field SPLIT mode uses for its "before" multiplier); OC (SplitEndEvidenceTypeId)
    /// governs its own onset year onward (AfterSplitMultiplier). The OC-onset year itself is split
    /// into up to two segments sharing that year: CC's portion from wherever CC's coverage starts in
    /// that year up to the day before OC's date, and OC's portion from OC's date to the year end —
    /// mirroring the legacy engine's ComputeCcThenOcMerge, but expressed as day-prorated segments
    /// instead of a bespoke double-engine-invocation merge.
    /// </summary>
    private static List<BillingSegment> BuildCcThenOcMergeSegments(
        RetrospectiveRuleActionEntity action, List<int> years, int earliestChargeableYear, DateTime chargeableStart,
        Dictionary<int, string> evidenceCodeById, Dictionary<string, DateTime?> evidenceDates)
    {
        var splitDate = action.SplitEndEvidenceTypeId.HasValue
            && evidenceCodeById.TryGetValue(action.SplitEndEvidenceTypeId.Value, out var splitCode)
                ? evidenceDates.GetValueOrDefault(splitCode)
                : null;

        var beforeMultiplier = action.SplitMultiplier ?? action.TaxMultiplier;
        var afterMultiplier = action.AfterSplitMultiplier ?? action.TaxMultiplier;

        var segments = new List<BillingSegment>();

        if (!splitDate.HasValue)
        {
            // The switch-over evidence (OC) isn't actually available -- nothing to merge into, so
            // every year is billed under the "before" (CC) multiplier alone.
            foreach (var year in years)
            {
                var yearStart = FyStart(year);
                var fromDate = year == earliestChargeableYear && chargeableStart > yearStart ? chargeableStart : yearStart;
                segments.Add(new BillingSegment(year, fromDate, new DateTime(year + 1, 3, 31), beforeMultiplier, PolicyCodes.Cc));
            }
            return segments;
        }

        var splitYear = FyOf(splitDate.Value);

        foreach (var year in years)
        {
            var yearStart = FyStart(year);
            var yearEnd = new DateTime(year + 1, 3, 31);
            var yearFromDate = year == earliestChargeableYear && chargeableStart > yearStart ? chargeableStart : yearStart;

            if (year < splitYear)
            {
                segments.Add(new BillingSegment(year, yearFromDate, yearEnd, beforeMultiplier, PolicyCodes.Cc));
            }
            else if (year == splitYear)
            {
                var ccPortionEnd = splitDate.Value.AddDays(-1);
                if (ccPortionEnd >= yearFromDate)
                {
                    segments.Add(new BillingSegment(year, yearFromDate, ccPortionEnd, beforeMultiplier, PolicyCodes.Cc));
                }
                segments.Add(new BillingSegment(year, splitDate.Value, yearEnd, afterMultiplier, PolicyCodes.Oc));
            }
            else
            {
                segments.Add(new BillingSegment(year, yearStart, yearEnd, afterMultiplier, PolicyCodes.Oc));
            }
        }

        return segments;
    }

    private static string FormatFinancialYear(int financeYear) => $"{financeYear}-{(financeYear + 1) % 100:D2}";

    private async Task<decimal> ResolveYearBaseTaxAsync(
        int propertyId, int financeYear, string rateMode, int currentFinanceYear, Dictionary<int, decimal> currentYearCache)
    {
        var yearToPrice = rateMode == "CURRENT_YEAR" ? currentFinanceYear : financeYear;

        if (currentYearCache.TryGetValue(yearToPrice, out var cached))
            return cached;

        var preview = await _rateableValueService.PreviewTotalTaxAsync(propertyId, yearToPrice);
        currentYearCache[yearToPrice] = preview.TotalTaxAmount;
        return preview.TotalTaxAmount;
    }

    private static decimal ResolveMultiplierForYear(
        RetrospectiveRuleActionEntity action,
        DateTime yearFromDate,
        Dictionary<int, string> evidenceCodeById,
        Dictionary<string, DateTime?> evidenceDates)
    {
        if (action.TaxCalculationMode != "SPLIT")
            return action.TaxMultiplier;

        DateTime? splitEndDate = action.SplitEndEvidenceTypeId.HasValue
            && evidenceCodeById.TryGetValue(action.SplitEndEvidenceTypeId.Value, out var endCode)
                ? evidenceDates.GetValueOrDefault(endCode)
                : null;

        if (!splitEndDate.HasValue)
            return action.SplitMultiplier ?? action.TaxMultiplier;

        return yearFromDate < splitEndDate.Value
            ? action.SplitMultiplier ?? action.TaxMultiplier
            : action.AfterSplitMultiplier ?? action.TaxMultiplier;
    }

    /// <summary>
    /// Resolves the penalty percent to apply to every charged year, and whether the penalty
    /// condition couldn't be conclusively evaluated (ElseAction = MANUAL_REVIEW) and needs a human
    /// to look at it before the calculation is treated as final.
    /// </summary>
    private static (decimal? PenaltyPercent, bool RequiresManualReview) ResolvePenalty(
        RetrospectivePenaltyRuleEntity? penalty,
        Dictionary<int, string> evidenceCodeById,
        Dictionary<string, DateTime?> evidenceDates)
    {
        if (penalty is null || !penalty.IsPenaltyApplicable || penalty.PenaltyMode == "NONE")
            return (null, false);

        if (penalty.PenaltyMode == "ACT_PENALTY")
            return (penalty.PenaltyPercent, false);

        // DATE_VALIDATION: resolve the source date to check, then compare it against CompareDate/CompareDateTo.
        DateTime? sourceDate = penalty.PenaltyDateSourceType switch
        {
            "EVIDENCE_DATE" => penalty.PenaltyDateEvidenceTypeId.HasValue
                && evidenceCodeById.TryGetValue(penalty.PenaltyDateEvidenceTypeId.Value, out var code)
                    ? evidenceDates.GetValueOrDefault(code)
                    : null,
            "ASSESSMENT_DATE" => DateTime.Now,
            "FIXED_DATE" => penalty.CompareDate,
            _ => null
        };

        var conditionMet = sourceDate.HasValue && penalty.CompareDate.HasValue && (penalty.PenaltyDateCondition switch
        {
            "ON_OR_AFTER" => sourceDate.Value >= penalty.CompareDate.Value,
            "AFTER" => sourceDate.Value > penalty.CompareDate.Value,
            "ON_OR_BEFORE" => sourceDate.Value <= penalty.CompareDate.Value,
            "BEFORE" => sourceDate.Value < penalty.CompareDate.Value,
            "BETWEEN" => penalty.CompareDateTo.HasValue
                && sourceDate.Value >= penalty.CompareDate.Value
                && sourceDate.Value <= penalty.CompareDateTo.Value,
            _ => false
        });

        if (conditionMet)
            return (penalty.PenaltyPercent, false);

        return penalty.ElseAction == "MANUAL_REVIEW" ? (null, true) : (null, false);
    }

    private async Task<RetrospectiveTaxEngineResultDto> PersistAsync(
        int propertyId,
        RetrospectiveRuleMasterEntity rule,
        DateTime chargeableStart,
        int currentFinanceYear,
        List<RetrospectiveTaxEngineYearDto> yearRows,
        bool requiresManualReview,
        int? calculatedBy,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        var calculation = new RetrospectiveTaxCalculationEntity
        {
            PropertyId = propertyId,
            CalculationMode = "PROPERTY",
            AppliedRuleId = rule.Id,
            AssessmentDate = now,
            ChargeableStartDate = chargeableStart,
            ChargeableEndDate = new DateTime(currentFinanceYear + 1, 3, 31),
            BaseTaxAmount = yearRows.Sum(y => y.BaseTaxAmount),
            RetrospectiveTaxAmount = yearRows.Sum(y => y.RetrospectiveTaxAmount),
            PenaltyAmount = yearRows.Sum(y => y.PenaltyAmount),
            TotalAmount = yearRows.Sum(y => y.TotalAmount),
            CalculationStatus = requiresManualReview ? "ManualReview" : "Calculated",
            IsActive = true,
            CreatedBy = calculatedBy,
            CreatedDate = now
        };
        await _calculationRepository.AddAsync(calculation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var year in yearRows)
        {
            await _calculationDetailRepository.AddAsync(new RetrospectiveTaxCalculationDetailEntity
            {
                CalculationId = calculation.Id,
                PropertyId = propertyId,
                // Property-level aggregate, not split per floor — floor-wise breakup (business
                // point #3: already-taxed vs untaxed area per floor) is deferred pending a
                // property-module data model that doesn't exist yet.
                FloorId = 0,
                FinancialYear = year.FinancialYear,
                FromDate = year.FromDate,
                ToDate = year.ToDate,
                RateMode = year.RateMode,
                PercentageMode = year.RateMode,
                BaseTaxAmount = year.BaseTaxAmount,
                TaxMultiplier = year.TaxMultiplier,
                RetrospectiveTaxAmount = year.RetrospectiveTaxAmount,
                PenaltyPercent = year.PenaltyPercent,
                PenaltyAmount = year.PenaltyAmount,
                TotalAmount = year.TotalAmount,
                IsActive = true,
                CreatedBy = calculatedBy,
                CreatedDate = now
            }, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RetrospectiveTaxEngineResultDto
        {
            CalculationId = calculation.Id,
            PropertyId = propertyId,
            AppliedRuleId = rule.Id,
            AppliedRuleCode = rule.RuleCode,
            AppliedRuleName = rule.RuleName,
            ChargeableStartDate = chargeableStart,
            ChargeableEndDate = calculation.ChargeableEndDate.Value,
            // Distinct financial years, not raw row count -- the CC/OC merge's boundary year can
            // contribute two rows (a CC portion and an OC portion) for what is still one year.
            RetroYearCount = yearRows.Select(y => y.FinancialYear).Distinct().Count(),
            TotalBaseTaxAmount = calculation.BaseTaxAmount,
            TotalRetrospectiveTaxAmount = calculation.RetrospectiveTaxAmount,
            TotalPenaltyAmount = calculation.PenaltyAmount,
            GrandTotalAmount = calculation.TotalAmount,
            RequiresManualReview = requiresManualReview,
            YearWiseBreakdown = yearRows
        };
    }

    /// <summary>Full/Partial PolicyCode pair for each certificate family, matching PolicyCodes.cs.</summary>
    private static readonly Dictionary<string, (string Full, string Partial)> FamilyPolicyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        [PolicyCodes.Oc] = (PolicyCodes.Oc, PolicyCodes.OcPartial),
        [PolicyCodes.Cc] = (PolicyCodes.Cc, PolicyCodes.CcPartial),
        [PolicyCodes.ElectricBill] = (PolicyCodes.ElectricBill, PolicyCodes.ElectricPartial),
    };

    /// <summary>
    /// Writes (or cleans up) the property's PolicyTaxDetails/TransMast rows.
    /// <para>
    /// PolicyTaxDetails is current-state only (no FinanceYearId column), so it's still written for
    /// the CURRENT finance year alone, matching the legacy Occupation Tax engine's design.
    /// </para>
    /// <para>
    /// TransMast, by contrast, is PropertyId + FinanceYearId + CalculationType + TaxId keyed — the
    /// single source of tax demand across every year (current, migrated ULB arrears via
    /// PolicyCodes.OldArrears, and retrospective demand). One row is written per (year, TaxId) for
    /// EVERY year in <paramref name="yearRows"/>, not just the current one. Whenever a retro-policy
    /// row is about to become active for a given (FinanceYearId, TaxId), any active OLD_ARREARS row
    /// at that same key is deactivated (IsActive=false only, kept -- not MarkedForDeletion -- for
    /// audit/history) so only one demand stays active per key, matching the "OLD_ARREARS replaced by
    /// retro demand" rule.
    /// </para>
    /// <para>
    /// Each year's total is allocated across the property's existing NETTAX PolicyTaxDetails TaxIds,
    /// proportional to each TaxId's own current NETTAX amount (largest-remainder rounding to whole
    /// rupees) — a deliberate simplification of the legacy engine's GeneralTax/Component-count split
    /// model, since that model's exact shape is itself an approximation with no independent
    /// verification value here. The same current-state NETTAX weights are reused for every
    /// historical year (there is no per-year historical weighting data to use instead).
    /// </para>
    /// <paramref name="yearRows"/> empty means "no valid computation" (no rule matched) — every
    /// existing family row for this property, across every year, is deactivated in that case.
    /// </summary>
    private async Task SyncPolicyTaxAndTransMastAsync(
        int propertyId, int currentFinanceYear, List<RetrospectiveTaxEngineYearDto> yearRows, int? updatedBy, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var currentYearLabel = FormatFinancialYear(currentFinanceYear);
        var currentYearRows = yearRows.Where(y => y.FinancialYear == currentYearLabel).OrderBy(y => y.FromDate).ToList();

        var allFamilyCodes = FamilyPolicyCodes.Values.SelectMany(v => new[] { v.Full, v.Partial }).Distinct().ToList();
        var policyCodeIds = await _policyCodeLookup.GetIdsAsync(allFamilyCodes, cancellationToken);
        // Optional: OLD_ARREARS may not be seeded in every environment yet, so a missing code is
        // tolerated (nothing to replace) rather than failing the whole recalculation.
        var oldArrearsPolicyCodeId = (await _policyCodeLookup.GetExistingIdsAsync(new[] { PolicyCodes.OldArrears }, cancellationToken))
            .GetValueOrDefault(PolicyCodes.OldArrears, 0);

        var resolvedCurrentPolicyCodeId = ResolveFamilyPolicyCodeId(currentYearRows, currentFinanceYear, policyCodeIds);
        var currentYearTotal = currentYearRows.Count > 0
            ? Math.Round(currentYearRows.Sum(y => y.RetrospectiveTaxAmount), 0, MidpointRounding.AwayFromZero)
            : 0m;

        var nettaxId = await _policyCodeLookup.GetIdAsync(PolicyCodes.NetTax, cancellationToken);
        var netTaxDetails = await _policyTaxDetailsRepository.GetQueryable()
            .Where(pt => pt.PropertyId == propertyId && pt.PolicyCodeId == nettaxId && pt.IsActive && !pt.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        // Business decision: certificate demand allocates across the property's current NETTAX
        // rows, so those must exist first. If they don't (RV was never run for this property, or a
        // prior run produced nothing), calculate RV now -- rateable value, then NETTAX -- and retry
        // once, rather than silently writing no certificate demand at all.
        if (netTaxDetails.Count == 0)
        {
            await _rateableValueService.CalculateAndSaveAsync(propertyId, forceRecalculate: true);
            netTaxDetails = await _policyTaxDetailsRepository.GetQueryable()
                .Where(pt => pt.PropertyId == propertyId && pt.PolicyCodeId == nettaxId && pt.IsActive && !pt.MarkedForDeletion)
                .ToListAsync(cancellationToken);
        }

        var exemptedTaxIds = await _taxApplicabilityService.GetExemptedTaxIdsAsync(propertyId, cancellationToken);

        var allFamilyPolicyCodeIds = policyCodeIds.Values.Distinct().ToList();
        var existingPolicyTaxDetails = await _policyTaxDetailsRepository.GetQueryable()
            .Where(pt => pt.PropertyId == propertyId && allFamilyPolicyCodeIds.Contains(pt.PolicyCodeId))
            .ToListAsync(cancellationToken);

        // Every family-coded TransMast row this engine owns for this property, across ALL years —
        // the working set for both this run's upserts and the final "not written this run, must be
        // stale" cleanup below. Deliberately excludes OLD_ARREARS rows: those are never touched by
        // that cleanup (an arrears row for a year this run isn't recalculating must stay untouched,
        // not get swept up as "stale"), only by the explicit DeactivateOldArrearsAsync call below,
        // which reads its own separate list.
        var existingTransMasts = await _transMastRepository.GetQueryable()
            .Where(tm => tm.PropertyId == propertyId && tm.CalculationType == "RV" && allFamilyPolicyCodeIds.Contains(tm.PolicyCodeId))
            .ToListAsync(cancellationToken);

        var existingOldArrears = oldArrearsPolicyCodeId > 0
            ? await _transMastRepository.GetQueryable()
                .Where(tm => tm.PropertyId == propertyId && tm.CalculationType == "RV" && tm.PolicyCodeId == oldArrearsPolicyCodeId && tm.IsActive && !tm.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<TransMastEntity>();

        // Business decision: unlike PolicyTaxDetails (where the NETTAX row and the certificate row
        // both stay active side by side), TransMast is the single-demand ledger -- a NETTAX row must
        // not stay active once a certificate-policy row occupies its (FinanceYearId, TaxId), or the
        // total-demand sum would double count. Hard removal (MarkedForDeletion = true), not the
        // audit-preserving soft deactivation OLD_ARREARS rows get.
        var existingNetTaxTransMasts = nettaxId > 0
            ? await _transMastRepository.GetQueryable()
                .Where(tm => tm.PropertyId == propertyId && tm.CalculationType == "RV" && tm.PolicyCodeId == nettaxId && tm.IsActive && !tm.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<TransMastEntity>();

        var changed = false;
        var keepTaxIds = new HashSet<int>(); // PolicyTaxDetails (current-state only)
        var keepTransMastKeys = new HashSet<(int FinanceYearId, int TaxId)>();

        if (resolvedCurrentPolicyCodeId.HasValue && netTaxDetails.Count > 0)
        {
            var currentYearMaster = await ResolveYearMasterAsync(currentFinanceYear, cancellationToken);
            if (currentYearMaster is not null)
            {
                var allocated = AllocateAcrossNetTaxIds(netTaxDetails, currentYearTotal);

                for (var i = 0; i < netTaxDetails.Count; i++)
                {
                    var taxId = netTaxDetails[i].TaxId;
                    keepTaxIds.Add(taxId);
                    var amount = exemptedTaxIds.Contains(taxId) ? 0m : allocated[i];

                    await UpsertPolicyTaxDetailAsync(existingPolicyTaxDetails, propertyId, resolvedCurrentPolicyCodeId.Value, taxId, amount, updatedBy, now, cancellationToken);
                    await UpsertTransMastAsync(existingTransMasts, propertyId, currentYearMaster.Id, resolvedCurrentPolicyCodeId.Value, taxId, amount, updatedBy, now, cancellationToken);
                    await DeactivateOldArrearsAsync(existingOldArrears, oldArrearsPolicyCodeId, currentYearMaster.Id, taxId, updatedBy, now, cancellationToken);
                    await DeactivateNetTaxTransMastAsync(existingNetTaxTransMasts, currentYearMaster.Id, taxId, updatedBy, now, cancellationToken);
                    keepTransMastKeys.Add((currentYearMaster.Id, taxId));
                    changed = true;
                }
            }
        }

        // Historical years -- TransMast only (PolicyTaxDetails has no FinanceYearId to hold these).
        foreach (var yearGroup in yearRows.Where(y => y.FinancialYear != currentYearLabel).GroupBy(y => y.FinancialYear))
        {
            if (netTaxDetails.Count == 0) break;

            var financeYear = ParseFinancialYearStart(yearGroup.Key);
            var rows = yearGroup.OrderBy(y => y.FromDate).ToList();
            var resolvedPolicyCodeId = ResolveFamilyPolicyCodeId(rows, financeYear, policyCodeIds);
            if (!resolvedPolicyCodeId.HasValue) continue;

            var yearMaster = await ResolveYearMasterAsync(financeYear, cancellationToken);
            if (yearMaster is null) continue;

            var yearTotal = Math.Round(rows.Sum(y => y.RetrospectiveTaxAmount), 0, MidpointRounding.AwayFromZero);
            var allocated = AllocateAcrossNetTaxIds(netTaxDetails, yearTotal);

            for (var i = 0; i < netTaxDetails.Count; i++)
            {
                var taxId = netTaxDetails[i].TaxId;
                var amount = exemptedTaxIds.Contains(taxId) ? 0m : allocated[i];

                await UpsertTransMastAsync(existingTransMasts, propertyId, yearMaster.Id, resolvedPolicyCodeId.Value, taxId, amount, updatedBy, now, cancellationToken);
                await DeactivateOldArrearsAsync(existingOldArrears, oldArrearsPolicyCodeId, yearMaster.Id, taxId, updatedBy, now, cancellationToken);
                await DeactivateNetTaxTransMastAsync(existingNetTaxTransMasts, yearMaster.Id, taxId, updatedBy, now, cancellationToken);
                keepTransMastKeys.Add((yearMaster.Id, taxId));
                changed = true;
            }
        }

        // Deactivate every existing family row this run did NOT just write — a property that
        // switched families (e.g. was CC, is now OC), or has no valid computation at all, must not
        // keep showing a stale row from a prior run. Scoped across every year, not just current.
        foreach (var pt in existingPolicyTaxDetails.Where(pt => pt.IsActive && !pt.MarkedForDeletion &&
                     !(resolvedCurrentPolicyCodeId.HasValue && pt.PolicyCodeId == resolvedCurrentPolicyCodeId.Value && keepTaxIds.Contains(pt.TaxId))))
        {
            pt.IsActive = false;
            pt.MarkedForDeletion = true;
            pt.MarkedForDeletionDate = now;
            pt.UpdatedBy = updatedBy;
            pt.UpdatedDate = now;
            await _policyTaxDetailsRepository.UpdateAsync(pt, cancellationToken);
            changed = true;
        }

        foreach (var tm in existingTransMasts.Where(tm => tm.IsActive && !tm.MarkedForDeletion && !keepTransMastKeys.Contains((tm.FinanceYearId, tm.TaxId))))
        {
            tm.IsActive = false;
            tm.MarkedForDeletion = true;
            tm.MarkedForDeletionDate = now;
            tm.UpdatedBy = updatedBy;
            tm.UpdatedDate = now;
            await _transMastRepository.UpdateAsync(tm, cancellationToken);
            changed = true;
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>Resolves the OC/CC/ElectricBill family PolicyCodeId for one year's rows (Full vs Partial by whether the year's first row starts after FY-start), or null if no rows / no configured code.</summary>
    private static int? ResolveFamilyPolicyCodeId(List<RetrospectiveTaxEngineYearDto> yearRows, int financeYear, Dictionary<string, int> policyCodeIds)
    {
        if (yearRows.Count == 0) return null;

        var family = yearRows[^1].PolicyFamily;
        var isPartial = yearRows[0].FromDate > FyStart(financeYear);

        if (FamilyPolicyCodes.TryGetValue(family, out var codes)
            && policyCodeIds.TryGetValue(isPartial ? codes.Partial : codes.Full, out var id))
        {
            return id;
        }

        return null;
    }

    /// <summary>Largest-remainder allocation of a year's total across the property's NETTAX TaxIds, proportional to each TaxId's own current NETTAX amount.</summary>
    private static IReadOnlyList<decimal> AllocateAcrossNetTaxIds(List<PolicyTaxDetailsEntity> netTaxDetails, decimal total)
    {
        var weights = netTaxDetails.Select(d => Math.Max(d.TaxAmount ?? 0m, 0m)).ToList();
        var totalWeight = weights.Sum();
        var rawShares = totalWeight > 0m
            ? weights.Select(w => total * (w / totalWeight)).ToList()
            : netTaxDetails.Select(_ => total / netTaxDetails.Count).ToList();
        return AllocateByLargestRemainder(rawShares, total);
    }

    private async Task UpsertPolicyTaxDetailAsync(
        List<PolicyTaxDetailsEntity> existingPolicyTaxDetails, int propertyId, int policyCodeId, int taxId, decimal amount, int? updatedBy, DateTime now, CancellationToken cancellationToken)
    {
        var existingRow = existingPolicyTaxDetails.FirstOrDefault(pt => pt.TaxId == taxId && pt.PolicyCodeId == policyCodeId);
        if (existingRow is not null)
        {
            existingRow.TaxAmount = amount;
            existingRow.CalculationValue = amount;
            existingRow.IsActive = true;
            existingRow.MarkedForDeletion = false;
            existingRow.MarkedForDeletionDate = null;
            existingRow.UpdatedBy = updatedBy;
            existingRow.UpdatedDate = now;
            await _policyTaxDetailsRepository.UpdateAsync(existingRow, cancellationToken);
        }
        else
        {
            existingRow = new PolicyTaxDetailsEntity
            {
                PropertyId = propertyId,
                PolicyCodeId = policyCodeId,
                TaxId = taxId,
                TaxAmount = amount,
                CalculationValue = amount,
                IsActive = true,
                CreatedBy = updatedBy,
                CreatedDate = now
            };
            await _policyTaxDetailsRepository.AddAsync(existingRow, cancellationToken);
            existingPolicyTaxDetails.Add(existingRow);
        }
    }

    private async Task UpsertTransMastAsync(
        List<TransMastEntity> existingTransMasts, int propertyId, int financeYearId, int policyCodeId, int taxId, decimal amount, int? updatedBy, DateTime now, CancellationToken cancellationToken)
    {
        var existingTm = existingTransMasts.FirstOrDefault(tm => tm.FinanceYearId == financeYearId && tm.TaxId == taxId && tm.PolicyCodeId == policyCodeId);
        if (existingTm is not null)
        {
            existingTm.CalculationValue = amount;
            existingTm.TaxAmount = amount;
            existingTm.IsActive = true;
            existingTm.MarkedForDeletion = false;
            existingTm.MarkedForDeletionDate = null;
            existingTm.UpdatedBy = updatedBy;
            existingTm.UpdatedDate = now;
            await _transMastRepository.UpdateAsync(existingTm, cancellationToken);
        }
        else
        {
            var newTm = new TransMastEntity
            {
                PropertyId = propertyId,
                FinanceYearId = financeYearId,
                CalculationType = "RV",
                CalculationValue = amount,
                TaxId = taxId,
                PolicyCodeId = policyCodeId,
                TaxAmount = amount,
                IsActive = true,
                CreatedBy = updatedBy,
                CreatedDate = now,
                UpdatedBy = updatedBy,
                UpdatedDate = now
            };
            await _transMastRepository.AddAsync(newTm, cancellationToken);
            existingTransMasts.Add(newTm);
        }
    }

    /// <summary>
    /// Deactivates (IsActive=false only, never MarkedForDeletion -- kept for audit/history) an
    /// active OLD_ARREARS TransMast row at the exact (FinanceYearId, TaxId) key a retro-policy row
    /// is about to occupy, so at most one demand stays active per PropertyId+FinanceYearId+TaxId.
    /// No-op if OLD_ARREARS isn't seeded (oldArrearsPolicyCodeId == 0) or no such row exists.
    /// </summary>
    private async Task DeactivateOldArrearsAsync(
        List<TransMastEntity> existingTransMasts, int oldArrearsPolicyCodeId, int financeYearId, int taxId, int? updatedBy, DateTime now, CancellationToken cancellationToken)
    {
        if (oldArrearsPolicyCodeId <= 0) return;

        var oldArrearsRow = existingTransMasts.FirstOrDefault(tm =>
            tm.FinanceYearId == financeYearId && tm.TaxId == taxId && tm.PolicyCodeId == oldArrearsPolicyCodeId
            && tm.IsActive && !tm.MarkedForDeletion);

        if (oldArrearsRow is null) return;

        oldArrearsRow.IsActive = false;
        oldArrearsRow.UpdatedBy = updatedBy;
        oldArrearsRow.UpdatedDate = now;
        await _transMastRepository.UpdateAsync(oldArrearsRow, cancellationToken);
    }

    /// <summary>
    /// Removes (IsActive=false AND MarkedForDeletion=true -- unlike OLD_ARREARS, no audit-preserving
    /// soft state) an active NETTAX TransMast row at the exact (FinanceYearId, TaxId) key a
    /// retro-policy row is about to occupy. TransMast is the single-demand ledger, so NETTAX and a
    /// certificate policy must never both stay active for the same year+tax there -- unlike
    /// PolicyTaxDetails, where the two coexist by design. No-op if NETTAX isn't seeded or no such
    /// row exists.
    /// </summary>
    private async Task DeactivateNetTaxTransMastAsync(
        List<TransMastEntity> existingNetTaxTransMasts, int financeYearId, int taxId, int? updatedBy, DateTime now, CancellationToken cancellationToken)
    {
        var netTaxRow = existingNetTaxTransMasts.FirstOrDefault(tm => tm.FinanceYearId == financeYearId && tm.TaxId == taxId);
        if (netTaxRow is null) return;

        netTaxRow.IsActive = false;
        netTaxRow.MarkedForDeletion = true;
        netTaxRow.MarkedForDeletionDate = now;
        netTaxRow.UpdatedBy = updatedBy;
        netTaxRow.UpdatedDate = now;
        await _transMastRepository.UpdateAsync(netTaxRow, cancellationToken);
    }

    /// <summary>Inverse of FormatFinancialYear -- "2024-25" -> 2024.</summary>
    private static int ParseFinancialYearStart(string financialYearLabel) => int.Parse(financialYearLabel.Split('-')[0]);

    /// <summary>Same YearMaster-matching heuristic as the legacy engine, for one specific finance year.</summary>
    private async Task<YearMasterEntity?> ResolveYearMasterAsync(int financeYear, CancellationToken cancellationToken)
    {
        var allYearMasters = await _yearRepository.GetQueryable().ToListAsync(cancellationToken);
        return allYearMasters.FirstOrDefault(y =>
        {
            if (y.StartDate.HasValue && y.StartDate.Value.Year == financeYear) return true;
            if (y.Year == financeYear) return true;
            if (!string.IsNullOrEmpty(y.YearCode))
            {
                var clean = y.YearCode.Trim();
                if (clean.StartsWith($"{financeYear}-") || clean.StartsWith($"{financeYear}/") || clean.StartsWith(financeYear.ToString()))
                    return true;

                var parts = clean.Split('-', '/');
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out var startY))
                {
                    if (startY == financeYear) return true;
                    if (startY < 100 && (2000 + startY) == financeYear) return true;
                }
            }
            return false;
        }) ?? allYearMasters.FirstOrDefault(y => y.Year == financeYear + 1 || y.Year == financeYear);
    }

    /// <summary>Distributes a rounded whole-rupee total across weighted raw shares so the parts sum exactly.</summary>
    private static IReadOnlyList<decimal> AllocateByLargestRemainder(IReadOnlyList<decimal> rawAmounts, decimal roundedTotal)
    {
        if (rawAmounts.Count == 0)
            return Array.Empty<decimal>();

        var floors = rawAmounts.Select(Math.Floor).ToArray();
        var remainders = rawAmounts.Select((a, i) => a - floors[i]).ToArray();
        var result = (decimal[])floors.Clone();
        var deficit = (int)(roundedTotal - floors.Sum());

        if (deficit > 0)
        {
            foreach (var i in Enumerable.Range(0, rawAmounts.Count).OrderByDescending(i => remainders[i]).Take(deficit))
                result[i] += 1;
        }
        else if (deficit < 0)
        {
            foreach (var i in Enumerable.Range(0, rawAmounts.Count).OrderBy(i => remainders[i]).Take(-deficit))
                result[i] -= 1;
        }

        return result;
    }
}
