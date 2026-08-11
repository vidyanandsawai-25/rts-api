using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application.Services.TaxEngine;

/// <summary>
/// Golden-figure tests for <see cref="HistoricalNetTaxBaselineService"/>, the pure/read-only
/// per-year NETTAX recomputation backing TAXATION_RATE_MODE/TAX_PERCENTAGE_MODE.
///
/// Fixture: one property, one taxable detail, 100 sqm carpet area (matches the default
/// AreaType=CarpetArea/AreaUnit=SqMeter policy), General Tax only. Two AssessmentYearRange rows:
/// 2020-2023 ("historical") rated at 100/sqm with a 10% tax percentage, and 2024-2027 ("current")
/// rated at 200/sqm with a 20% tax percentage -- chosen so historical and current baselines are
/// unambiguously different and the arithmetic is easy to hand-verify:
///   Historical: RV = (100 sqm x 100/sqm) - 10% maintenance = 10,000 - 1,000 = 9,000; tax = 10% = 900.
///   Current:    RV = (100 sqm x 200/sqm) - 10% maintenance = 20,000 - 2,000 = 18,000; tax = 20% = 3,600.
/// </summary>
public class HistoricalNetTaxBaselineServiceTests
{
    private const int PropertyId = 1;
    private const int TaxZoneId = 1;
    private const int WardId = 1;
    private const int ConstructionTypeId = 1;
    private const int TypeOfUseId = 1;
    private const int TypeOfUseGroupId = 1;
    private const int GeneralTaxId = 1;
    private const int HistoricalYearRangeId = 100; // FY2020-FY2023
    private const int CurrentYearRangeId = 200;    // FY2024-FY2027

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedProperty(ApplicationDbContext context, bool withDetails = true)
    {
        context.PropertyMast.Add(new PropertyEntity { Id = PropertyId, TaxZoneId = TaxZoneId, WardId = WardId, IsActive = true });
        if (withDetails)
        {
            context.PropertyDetails.Add(new PropertyDetailsEntity
            {
                Id = 1,
                PropertyId = PropertyId,
                TypeOfUseId = TypeOfUseId,
                ConstructionTypeId = ConstructionTypeId,
                CarpetAreaSqMeter = 100d,
                IsTaxable = true,
                MarkedForDeletion = false
            });
        }
        context.SaveChanges();
    }

    private static Mock<ITaxMasterDataService> BuildMasterDataMock()
    {
        var typeOfUse = new TypeOfUseEntity { Id = TypeOfUseId, TypeOfUseGroupId = TypeOfUseGroupId, Type = "R" };
        var yearRanges = new List<AssessmentYearRangeEntity>
        {
            new() { Id = HistoricalYearRangeId, FromYear = 2020, ToYear = 2023 },
            new() { Id = CurrentYearRangeId, FromYear = 2024, ToYear = 2027 }
        };
        var rates = new List<RateEntity>
        {
            new() { TaxZoneId = TaxZoneId, ConstructionTypeId = ConstructionTypeId, TypeOfUseGroupId = TypeOfUseGroupId, YearRangeRVId = HistoricalYearRangeId, RateSquareMeter = 100m, IsActive = true },
            new() { TaxZoneId = TaxZoneId, ConstructionTypeId = ConstructionTypeId, TypeOfUseGroupId = TypeOfUseGroupId, YearRangeRVId = CurrentYearRangeId, RateSquareMeter = 200m, IsActive = true }
        };
        var generalTax = new TaxMasterEntity { Id = GeneralTaxId, TaxName = "GeneralTax", TaxCode = "GEN", TaxCategoryId = 1, IsActive = true, TaxCategoryMaster = new TaxCategoryMasterEntity { Id = 1, CategoryCode = "TAX", CategoryName = "Property Tax" } };
        var taxPercentages = new List<TaxPercentageMasterRVEntity>
        {
            new() { TaxId = GeneralTaxId, TypeOfUseId = TypeOfUseId, YearRangeRVId = HistoricalYearRangeId, TaxPercentage = 10m, BaseType = "RV" },
            new() { TaxId = GeneralTaxId, TypeOfUseId = TypeOfUseId, YearRangeRVId = CurrentYearRangeId, TaxPercentage = 20m, BaseType = "RV" }
        };

        var mock = new Mock<ITaxMasterDataService>();
        mock.Setup(m => m.GetActiveTypeOfUsesAsync()).ReturnsAsync(new List<TypeOfUseEntity> { typeOfUse });
        mock.Setup(m => m.GetRateSectionIdForWardAsync(WardId)).ReturnsAsync(1);
        mock.Setup(m => m.GetRatesForSectionAsync(1)).ReturnsAsync(rates);
        mock.Setup(m => m.GetActiveDepreciationsAsync()).ReturnsAsync(new List<DepreciationMasterEntity>());
        mock.Setup(m => m.GetActiveYearRangesAsync()).ReturnsAsync(yearRanges);
        mock.Setup(m => m.GetActiveTaxesAsync()).ReturnsAsync(new List<TaxMasterEntity> { generalTax });
        mock.Setup(m => m.GetActiveTaxPercentagesAsync()).ReturnsAsync(taxPercentages);
        return mock;
    }

    private static HistoricalNetTaxBaselineService BuildService(ApplicationDbContext context, Mock<ITaxMasterDataService> masterDataMock)
    {
        var policyConfigMock = new Mock<IPolicyConfigurationService>();
        policyConfigMock
            .Setup(p => p.GetPolicyValuesAsync(It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>());

        return new HistoricalNetTaxBaselineService(
            new Repository<PropertyEntity, int>(context),
            new Repository<PropertyDetailsEntity, int>(context),
            new Repository<RenterMastEntity, int>(context),
            masterDataMock.Object,
            new RateableValueCalculatorService(Microsoft.Extensions.Logging.Abstractions.NullLogger<RateableValueCalculatorService>.Instance),
            policyConfigMock.Object);
    }

    [Fact]
    public async Task ComputeBaselineAsync_HistoricalYear_UsesThatYearsOwnRateAndPercentage()
    {
        using var context = CreateContext();
        SeedProperty(context);
        var service = BuildService(context, BuildMasterDataMock());

        var result = await service.ComputeBaselineAsync(PropertyId, rateFinanceYear: 2021, percentageFinanceYear: 2021, fixedTaxPercentage: null);

        Assert.NotNull(result);
        Assert.Equal(900m, result!.Value.AnnualNetTax);
        Assert.Equal(900m, result.Value.GeneralTaxPortion); // only tax head is General
    }

    [Fact]
    public async Task ComputeBaselineAsync_CurrentYear_UsesCurrentRateAndPercentage()
    {
        using var context = CreateContext();
        SeedProperty(context);
        var service = BuildService(context, BuildMasterDataMock());

        var result = await service.ComputeBaselineAsync(PropertyId, rateFinanceYear: 2026, percentageFinanceYear: 2026, fixedTaxPercentage: null);

        Assert.NotNull(result);
        Assert.Equal(3_600m, result!.Value.AnnualNetTax);
    }

    [Fact]
    public async Task ComputeBaselineAsync_MixedAxes_RateHistoricalPercentageCurrent_AppliesEachIndependently()
    {
        using var context = CreateContext();
        SeedProperty(context);
        var service = BuildService(context, BuildMasterDataMock());

        // Historical rate (2021 -> 100/sqm -> RV 9,000) but CURRENT percentage (20%): 9,000 * 20% = 1,800.
        var result = await service.ComputeBaselineAsync(PropertyId, rateFinanceYear: 2021, percentageFinanceYear: 2026, fixedTaxPercentage: null);

        Assert.NotNull(result);
        Assert.Equal(1_800m, result!.Value.AnnualNetTax);
    }

    [Fact]
    public async Task ComputeBaselineAsync_FixedPercentage_OverridesLookupRegardlessOfPercentageYear()
    {
        using var context = CreateContext();
        SeedProperty(context);
        var service = BuildService(context, BuildMasterDataMock());

        // Current rate (2026 -> 200/sqm -> RV 18,000) with a FIXED 15% override: 18,000 * 15% = 2,700.
        // percentageFinanceYear is irrelevant once fixedTaxPercentage is supplied.
        var result = await service.ComputeBaselineAsync(PropertyId, rateFinanceYear: 2026, percentageFinanceYear: 1900, fixedTaxPercentage: 15m);

        Assert.NotNull(result);
        Assert.Equal(2_700m, result!.Value.AnnualNetTax);
    }

    [Fact]
    public async Task ComputeBaselineAsync_NoActivePropertyDetails_ReturnsNullFailOpen()
    {
        using var context = CreateContext();
        SeedProperty(context, withDetails: false);
        var service = BuildService(context, BuildMasterDataMock());

        var result = await service.ComputeBaselineAsync(PropertyId, rateFinanceYear: 2026, percentageFinanceYear: 2026, fixedTaxPercentage: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ComputeBaselineAsync_NoYearRangeForRequestedRateYear_ReturnsNull()
    {
        using var context = CreateContext();
        SeedProperty(context);
        var service = BuildService(context, BuildMasterDataMock());

        // 2050 falls outside both seeded AssessmentYearRange rows (2020-2023, 2024-2027).
        var result = await service.ComputeBaselineAsync(PropertyId, rateFinanceYear: 2050, percentageFinanceYear: 2050, fixedTaxPercentage: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ComputeBaselineAsync_PropertyNotFound_ReturnsNull()
    {
        using var context = CreateContext();
        // Deliberately do not seed any property.
        var service = BuildService(context, BuildMasterDataMock());

        var result = await service.ComputeBaselineAsync(propertyId: 999, rateFinanceYear: 2026, percentageFinanceYear: 2026, fixedTaxPercentage: null);

        Assert.Null(result);
    }
}
