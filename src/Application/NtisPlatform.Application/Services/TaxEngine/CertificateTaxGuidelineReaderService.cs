using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.Globalization;

namespace NtisPlatform.Application.Services.TaxEngine;

public sealed class CertificateTaxGuidelineReaderService : ICertificateTaxGuidelineReaderService
{
    private readonly IRepository<CertificateTaxGuidelineEntity, int> _repository;

    public CertificateTaxGuidelineReaderService(IRepository<CertificateTaxGuidelineEntity, int> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<CertificateTaxGuidelineSettings> GetActiveSettingsAsync(CancellationToken cancellationToken = default)
    {
        var rawRows = await _repository.GetQueryable()
            .Where(g => g.IsActive)
            .Select(g => new { Code = g.GuidelineCode ?? "", Value = g.GuidelineValue ?? "" })
            .ToListAsync(cancellationToken);

        var rows = rawRows
            .Where(g => !string.IsNullOrWhiteSpace(g.Code))
            .GroupBy(g => g.Code)
            .ToDictionary(g => g.Key, g => g.First().Value);

        return new CertificateTaxGuidelineSettings(
            EnableCertificateBasedTax: RequireBool(rows, "ENABLE_CERTIFICATE_BASED_TAX"),
            ApplyOnlyTaxableCertTypes: RequireBool(rows, "APPLY_ONLY_TAXABLE_CERT_TYPES"),
            DatePriority1: RequireString(rows, "DATE_PRIORITY_1"),
            DatePriority2: RequireString(rows, "DATE_PRIORITY_2"),
            DatePriority3: RequireString(rows, "DATE_PRIORITY_3"),
            DatePriority4: RequireString(rows, "DATE_PRIORITY_4"),
            CertificateRequireNoAndDate: RequireBool(rows, "CERTIFICATE_REQUIRE_NO_AND_DATE"),
            MissingCertificateNoAction: RequireString(rows, "MISSING_CERTIFICATE_NO_ACTION"),
            MissingCertificateDateAction: RequireString(rows, "MISSING_CERTIFICATE_DATE_ACTION"),
            IgnoreCcToOcWithinValue: RequireInt(rows, "IGNORE_CC_TO_OC_WITHIN_VALUE"),
            IgnoreCcToOcWithinType: RequireString(rows, "IGNORE_CC_TO_OC_WITHIN_TYPE"),
            CcOcGapComparison: RequireString(rows, "CC_OC_GAP_COMPARISON"),
            CcOcGapWithinAction: RequireString(rows, "CC_OC_GAP_WITHIN_ACTION"),
            CcOcGapExceededAction: RequireString(rows, "CC_OC_GAP_EXCEEDED_ACTION"),
            InvalidCcOcDateOrderAction: RequireString(rows, "INVALID_CC_OC_DATE_ORDER_ACTION"),
            CcOnlyAction: RequireString(rows, "CC_ONLY_ACTION"),
            OcOnlyAction: RequireString(rows, "OC_ONLY_ACTION"),
            FinancialYearStartMonth: RequireByte(rows, "FINANCIAL_YEAR_START_MONTH", 4),
            FinancialYearStartDay: RequireByte(rows, "FINANCIAL_YEAR_START_DAY", 1),
            CCPeriodMultiplier: RequireDecimal(rows, "CC_PERIOD_MULTIPLIER", 1m),
            OCPeriodMultiplier: RequireDecimal(rows, "OC_PERIOD_MULTIPLIER", 1m),
            ElectricBillDateRule: RequireString(rows, "ELECTRIC_BILL_DATE_RULE"),
            ElectricBillAddMonths: RequireInt(rows, "ELECTRIC_BILL_ADD_MONTHS"),
            ElectricBillMultiplier: RequireDecimal(rows, "ELECTRIC_BILL_MULTIPLIER", 1m),
            ElectricBillMinimumFinancialYear: RequireInt(rows, "ELECTRIC_BILL_MINIMUM_FINANCIAL_YEAR"),
            EnableRetrospectiveTax: RequireBool(rows, "ENABLE_RETROSPECTIVE_TAX"),
            NoDateRule: RequireString(rows, "NO_DATE_RULE"),
            LookbackYears: RequireInt(rows, "NO_DATE_LOOKBACK_YEARS"),
            DefaultRetrospectiveMultiplier: RequireDecimal(rows, "NO_DATE_RETROSPECTIVE_MULTIPLIER", 1m),
            EnableCurrentYearProration: RequireBool(rows, "ENABLE_CURRENT_YEAR_PRORATION"),
            ProrationMethod: RequireString(rows, "PRORATION_METHOD"),
            CurrentYearProrationStartRule: RequireString(rows, "CURRENT_YEAR_PRORATION_START_RULE"),
            TaxPersistenceMode: RequireString(rows, "TAX_PERSISTENCE_MODE"),
            SaveInPolicyTaxDetails: RequireBool(rows, "SAVE_CERTIFICATE_TAX_IN_POLICY_TAX_DETAILS"),
            SaveInTransMast: RequireBool(rows, "SAVE_CERTIFICATE_TAX_IN_TRANSMAST"),
            DoNotUpdateNettax: RequireBool(rows, "DO_NOT_UPDATE_NETTAX"),
            RecalculateOnSave: RequireBool(rows, "RECALCULATE_ON_CERTIFICATE_SAVE"),
            RecalculateOnDelete: RequireBool(rows, "RECALCULATE_ON_CERTIFICATE_DELETE"),
            GuidelineChangeApplyMode: RequireString(rows, "GUIDELINE_CHANGE_APPLY_MODE"),
            CcPartialPolicyCode: RequireString(rows, "CC_PARTIAL_POLICY_CODE"),
            CcFullPolicyCode: RequireString(rows, "CC_FULL_POLICY_CODE"),
            OcPartialPolicyCode: RequireString(rows, "OC_PARTIAL_POLICY_CODE"),
            OcFullPolicyCode: RequireString(rows, "OC_FULL_POLICY_CODE"),
            ElectricBillPartialPolicyCode: RequireString(rows, "ELECTRIC_BILL_PARTIAL_POLICY_CODE"),
            ElectricBillFullPolicyCode: RequireString(rows, "ELECTRIC_BILL_FULL_POLICY_CODE"),
            CertificateTaxScopeMode: RequireString(rows, "CERTIFICATE_TAX_SCOPE_MODE"),
            AllowFloorWiseCertificateMetadata: RequireBool(rows, "ALLOW_FLOOR_WISE_CERTIFICATE_METADATA"),
            EnableCcToOcSplit: RequireBool(rows, "ENABLE_CC_TO_OC_SPLIT"),
            ElectricBillCertificateCodes: RequireString(rows, "ELECTRIC_BILL_CERTIFICATE_CODES"),
            RetrospectiveCurrentYearCount: RequireInt(rows, "RETROSPECTIVE_CURRENT_YEAR_COUNT"),
            RetrospectivePendingYearCountMode: RequireString(rows, "RETROSPECTIVE_PENDING_YEAR_COUNT_MODE"),
            FloorPolicyDisplayRule: RequireString(rows, "FLOOR_POLICY_DISPLAY_RULE"),
            // "HISTORICAL_YEAR_WISE" | "CURRENT_YEAR_FOR_ALL" (default) -- defaulting to
            // CURRENT_YEAR_FOR_ALL preserves today's behavior (one snapshot reused for every year)
            // for any deployment that hasn't configured this key yet.
            TaxationRateMode: RequireString(rows, "TAXATION_RATE_MODE", "CURRENT_YEAR_FOR_ALL"),
            // "HISTORICAL_YEAR_WISE" | "CURRENT_YEAR_FOR_ALL" (default) | "FIXED_FOR_ALL"
            TaxPercentageMode: RequireString(rows, "TAX_PERCENTAGE_MODE", "CURRENT_YEAR_FOR_ALL"),
            // Only consulted when TaxPercentageMode is FIXED_FOR_ALL.
            FixedTaxPercentage: RequireDecimal(rows, "FIXED_TAX_PERCENTAGE", 0m));
    }

    private static string RequireString(IReadOnlyDictionary<string, string> rows, string guidelineCode, string defaultValue = "") =>
        rows.TryGetValue(guidelineCode, out var value) && !string.IsNullOrWhiteSpace(value) ? value : defaultValue;

    private static int RequireInt(IReadOnlyDictionary<string, string> rows, string guidelineCode, int defaultValue = 0) =>
        rows.TryGetValue(guidelineCode, out var val) && int.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;

    private static byte RequireByte(IReadOnlyDictionary<string, string> rows, string guidelineCode, byte defaultValue = 0) =>
        rows.TryGetValue(guidelineCode, out var val) && byte.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;

    private static decimal RequireDecimal(IReadOnlyDictionary<string, string> rows, string guidelineCode, decimal defaultValue = 0m) =>
        rows.TryGetValue(guidelineCode, out var val) && decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;

    private static bool RequireBool(IReadOnlyDictionary<string, string> rows, string guidelineCode, bool defaultValue = false) =>
        rows.TryGetValue(guidelineCode, out var val) && (val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase)) ? true : defaultValue;
}
