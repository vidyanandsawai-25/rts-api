using MockQueryable;
using Moq;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application.Services.TaxEngine;

public class CertificateTaxGuidelineReaderServiceTests
{
    private static CertificateTaxGuidelineEntity Row(string code, string value, bool isActive = true) => new()
    {
        GuidelineCode = code,
        GuidelineName = code,
        GuidelineGroup = "TEST",
        DataType = "VARCHAR",
        GuidelineValue = value,
        IsActive = isActive,
    };

    private static List<CertificateTaxGuidelineEntity> FullRowSet() => new()
    {
        Row("ENABLE_CERTIFICATE_BASED_TAX", "1"),
        Row("APPLY_ONLY_TAXABLE_CERT_TYPES", "1"),
        Row("DATE_PRIORITY_1", "CC"),
        Row("DATE_PRIORITY_2", "OC"),
        Row("DATE_PRIORITY_3", "ELECTRIC_BILL"),
        Row("DATE_PRIORITY_4", "RETROSPECTIVE"),
        Row("CERTIFICATE_REQUIRE_NO_AND_DATE", "1"),
        Row("MISSING_CERTIFICATE_NO_ACTION", "IGNORE_FOR_TAX"),
        Row("MISSING_CERTIFICATE_DATE_ACTION", "IGNORE_FOR_TAX"),
        Row("IGNORE_CC_TO_OC_WITHIN_VALUE", "6"),
        Row("IGNORE_CC_TO_OC_WITHIN_TYPE", "MONTHS"),
        Row("CC_OC_GAP_COMPARISON", "LESS_THAN_OR_EQUAL"),
        Row("CC_OC_GAP_WITHIN_ACTION", "APPLY_OC_ONLY"),
        Row("CC_OC_GAP_EXCEEDED_ACTION", "APPLY_CC_THEN_OC"),
        Row("INVALID_CC_OC_DATE_ORDER_ACTION", "USE_PRIORITY_AND_LOG"),
        Row("CC_ONLY_ACTION", "APPLY_FROM_CC_DATE"),
        Row("OC_ONLY_ACTION", "APPLY_FROM_OC_DATE"),
        Row("FINANCIAL_YEAR_START_MONTH", "4"),
        Row("FINANCIAL_YEAR_START_DAY", "1"),
        Row("CC_PERIOD_MULTIPLIER", "1.5000"),
        Row("OC_PERIOD_MULTIPLIER", "1.0000"),
        Row("ELECTRIC_BILL_DATE_RULE", "FROM_FY_START"),
        Row("ELECTRIC_BILL_ADD_MONTHS", "0"),
        Row("ELECTRIC_BILL_MULTIPLIER", "1.0000"),
        Row("ELECTRIC_BILL_MINIMUM_FINANCIAL_YEAR", "2016"),
        Row("ENABLE_RETROSPECTIVE_TAX", "0"),
        Row("NO_DATE_RULE", "DEFAULT_RETROSPECTIVE"),
        Row("NO_DATE_LOOKBACK_YEARS", "5"),
        Row("NO_DATE_RETROSPECTIVE_MULTIPLIER", "1.0000"),
        Row("ENABLE_CURRENT_YEAR_PRORATION", "1"),
        Row("PRORATION_METHOD", "DAILY"),
        Row("CURRENT_YEAR_PRORATION_START_RULE", "EXACT_DATE"),
        Row("TAX_PERSISTENCE_MODE", "PROPERTY_AGGREGATED"),
        Row("SAVE_CERTIFICATE_TAX_IN_POLICY_TAX_DETAILS", "1"),
        Row("SAVE_CERTIFICATE_TAX_IN_TRANSMAST", "1"),
        Row("DO_NOT_UPDATE_NETTAX", "1"),
        Row("RECALCULATE_ON_CERTIFICATE_SAVE", "1"),
        Row("RECALCULATE_ON_CERTIFICATE_DELETE", "1"),
        Row("GUIDELINE_CHANGE_APPLY_MODE", "NEXT_CALCULATION"),
        Row("CC_PARTIAL_POLICY_CODE", "PARTIAL_CC"),
        Row("CC_FULL_POLICY_CODE", "CC"),
        Row("OC_PARTIAL_POLICY_CODE", "PARTIAL_OC"),
        Row("OC_FULL_POLICY_CODE", "OC"),
        Row("ELECTRIC_BILL_PARTIAL_POLICY_CODE", "PARTIAL_ELECTRIC_BILL"),
        Row("ELECTRIC_BILL_FULL_POLICY_CODE", "ELECTRIC_BILL"),
        Row("CERTIFICATE_TAX_SCOPE_MODE", "FLOOR_WISE"),
        Row("ALLOW_FLOOR_WISE_CERTIFICATE_METADATA", "1"),
        Row("ENABLE_CC_TO_OC_SPLIT", "1"),
        Row("ELECTRIC_BILL_CERTIFICATE_CODES", "ELECTRIC_BILL,EleBillDt"),
        Row("RETROSPECTIVE_CURRENT_YEAR_COUNT", "1"),
        Row("RETROSPECTIVE_PENDING_YEAR_COUNT_MODE", "TOTAL_MINUS_CURRENT"),
        Row("FLOOR_POLICY_DISPLAY_RULE", "BIGGEST_AREA_FLOOR_POLICY"),
        Row("TAXATION_RATE_MODE", "HISTORICAL_YEAR_WISE"),
        Row("TAX_PERCENTAGE_MODE", "FIXED_FOR_ALL"),
        Row("FIXED_TAX_PERCENTAGE", "12.5000"),
    };

    private static CertificateTaxGuidelineReaderService CreateService(List<CertificateTaxGuidelineEntity> rows)
    {
        var repository = new Mock<IRepository<CertificateTaxGuidelineEntity, int>>();
        repository.Setup(r => r.GetQueryable()).Returns(rows.BuildMock());
        return new CertificateTaxGuidelineReaderService(repository.Object);
    }

    [Fact]
    public async Task GetActiveSettingsAsync_WithFullRowSet_MapsEveryFieldCorrectly()
    {
        var service = CreateService(FullRowSet());

        var settings = await service.GetActiveSettingsAsync();

        Assert.True(settings.EnableCertificateBasedTax);
        Assert.True(settings.ApplyOnlyTaxableCertTypes);
        Assert.Equal("CC", settings.DatePriority1);
        Assert.Equal("OC", settings.DatePriority2);
        Assert.Equal("ELECTRIC_BILL", settings.DatePriority3);
        Assert.Equal("RETROSPECTIVE", settings.DatePriority4);
        Assert.True(settings.CertificateRequireNoAndDate);
        Assert.Equal("IGNORE_FOR_TAX", settings.MissingCertificateNoAction);
        Assert.Equal("IGNORE_FOR_TAX", settings.MissingCertificateDateAction);
        Assert.Equal(6, settings.IgnoreCcToOcWithinValue);
        Assert.Equal("MONTHS", settings.IgnoreCcToOcWithinType);
        Assert.Equal("LESS_THAN_OR_EQUAL", settings.CcOcGapComparison);
        Assert.Equal("APPLY_OC_ONLY", settings.CcOcGapWithinAction);
        Assert.Equal("APPLY_CC_THEN_OC", settings.CcOcGapExceededAction);
        Assert.Equal("USE_PRIORITY_AND_LOG", settings.InvalidCcOcDateOrderAction);
        Assert.Equal("APPLY_FROM_CC_DATE", settings.CcOnlyAction);
        Assert.Equal("APPLY_FROM_OC_DATE", settings.OcOnlyAction);
        Assert.Equal((byte)4, settings.FinancialYearStartMonth);
        Assert.Equal((byte)1, settings.FinancialYearStartDay);
        Assert.Equal(1.5m, settings.CCPeriodMultiplier);
        Assert.Equal(1.0m, settings.OCPeriodMultiplier);
        Assert.Equal("FROM_FY_START", settings.ElectricBillDateRule);
        Assert.Equal(0, settings.ElectricBillAddMonths);
        Assert.Equal(1.0m, settings.ElectricBillMultiplier);
        Assert.Equal(2016, settings.ElectricBillMinimumFinancialYear);
        Assert.False(settings.EnableRetrospectiveTax);
        Assert.Equal("DEFAULT_RETROSPECTIVE", settings.NoDateRule);
        Assert.Equal(5, settings.LookbackYears);
        Assert.Equal(1.0m, settings.DefaultRetrospectiveMultiplier);
        Assert.True(settings.EnableCurrentYearProration);
        Assert.Equal("DAILY", settings.ProrationMethod);
        Assert.Equal("EXACT_DATE", settings.CurrentYearProrationStartRule);
        Assert.Equal("PROPERTY_AGGREGATED", settings.TaxPersistenceMode);
        Assert.True(settings.SaveInPolicyTaxDetails);
        Assert.True(settings.SaveInTransMast);
        Assert.True(settings.DoNotUpdateNettax);
        Assert.True(settings.RecalculateOnSave);
        Assert.True(settings.RecalculateOnDelete);
        Assert.Equal("NEXT_CALCULATION", settings.GuidelineChangeApplyMode);
        Assert.Equal("PARTIAL_CC", settings.CcPartialPolicyCode);
        Assert.Equal("CC", settings.CcFullPolicyCode);
        Assert.Equal("PARTIAL_OC", settings.OcPartialPolicyCode);
        Assert.Equal("OC", settings.OcFullPolicyCode);
        Assert.Equal("PARTIAL_ELECTRIC_BILL", settings.ElectricBillPartialPolicyCode);
        Assert.Equal("ELECTRIC_BILL", settings.ElectricBillFullPolicyCode);
        Assert.Equal("FLOOR_WISE", settings.CertificateTaxScopeMode);
        Assert.True(settings.AllowFloorWiseCertificateMetadata);
        Assert.True(settings.EnableCcToOcSplit);
        Assert.Equal("ELECTRIC_BILL,EleBillDt", settings.ElectricBillCertificateCodes);
        Assert.Equal(1, settings.RetrospectiveCurrentYearCount);
        Assert.Equal("TOTAL_MINUS_CURRENT", settings.RetrospectivePendingYearCountMode);
        Assert.Equal("BIGGEST_AREA_FLOOR_POLICY", settings.FloorPolicyDisplayRule);
        Assert.Equal("HISTORICAL_YEAR_WISE", settings.TaxationRateMode);
        Assert.Equal("FIXED_FOR_ALL", settings.TaxPercentageMode);
        Assert.Equal(12.5m, settings.FixedTaxPercentage);
    }

    [Fact]
    public async Task GetActiveSettingsAsync_IgnoresInactiveRows()
    {
        var rows = FullRowSet();
        rows.Add(Row("DATE_PRIORITY_1", "OC", isActive: false));

        var service = CreateService(rows);

        var settings = await service.GetActiveSettingsAsync();

        Assert.Equal("CC", settings.DatePriority1);
    }

    [Fact]
    public async Task GetActiveSettingsAsync_InvalidBitValue_DefaultsToFalse()
    {
        var rows = FullRowSet();
        rows.RemoveAll(r => r.GuidelineCode == "ENABLE_CERTIFICATE_BASED_TAX");
        rows.Add(Row("ENABLE_CERTIFICATE_BASED_TAX", "yes"));

        var service = CreateService(rows);

        var settings = await service.GetActiveSettingsAsync();

        Assert.False(settings.EnableCertificateBasedTax);
    }

    [Fact]
    public async Task GetActiveSettingsAsync_MissingRequiredCode_UsesDefaultValue()
    {
        var rows = FullRowSet();
        rows.RemoveAll(r => r.GuidelineCode == "CC_PERIOD_MULTIPLIER");

        var service = CreateService(rows);

        var settings = await service.GetActiveSettingsAsync();

        Assert.Equal(1m, settings.CCPeriodMultiplier);
    }

    [Fact]
    public async Task GetActiveSettingsAsync_TaxationRateAndPercentageModeMissing_DefaultsToCurrentYearForAll()
    {
        // A deployment that hasn't configured these new keys yet must get exactly today's
        // behavior (one snapshot reused for every year) -- no migration required.
        var rows = FullRowSet();
        rows.RemoveAll(r => r.GuidelineCode is "TAXATION_RATE_MODE" or "TAX_PERCENTAGE_MODE" or "FIXED_TAX_PERCENTAGE");

        var service = CreateService(rows);

        var settings = await service.GetActiveSettingsAsync();

        Assert.Equal("CURRENT_YEAR_FOR_ALL", settings.TaxationRateMode);
        Assert.Equal("CURRENT_YEAR_FOR_ALL", settings.TaxPercentageMode);
        Assert.Equal(0m, settings.FixedTaxPercentage);
    }
}
