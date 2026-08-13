using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Application.DynamicTaxRegister;

/// <summary>
/// <see cref="ValueBasedTaxService"/>. The behaviour worth pinning here is that Base Type is a
/// tax+year-WIDE setting, not per row: a save must rewrite it across every row of that tax and
/// year, including rows the client never sent because they were on another page.
/// </summary>
public class ValueBasedTaxServiceTests
{
    private const int TaxId = 10;
    private const int YearRangeId = 1;

    private static (ValueBasedTaxService Service, ApplicationDbContext Context) Create()
    {
        var context = DynamicTaxTestContext.Create();
        // The service gates on the tax's mode actually supporting value configuration, so the mode
        // rows have to be present for the capability flag to resolve.
        context.TaxCalculationModeMaster.AddRange(DynamicTaxTestContext.SeededModes());
        context.TaxMaster.Add(DynamicTaxTestContext.Tax(TaxId, "VAL", calculationModeId: 1));
        context.SaveChanges();
        return (new ValueBasedTaxService(context), context);
    }

    private static TaxPercentageMasterRVEntity Pct(
        int id, int typeOfUseId, decimal percentage, string baseType = "RV", int yearRangeId = YearRangeId) => new()
        {
            Id = id,
            TaxId = TaxId,
            TypeOfUseId = typeOfUseId,
            YearRangeRVId = yearRangeId,
            BaseType = baseType,
            TaxPercentage = percentage,
            IsActive = true,
        };

    /// <summary>Rows are matched for update by their own Id; an Id of 0 means "insert".</summary>
    private static SaveValueBasedTaxRequest Request(
        string baseType, params (int Id, int TypeOfUseId, decimal Percentage)[] rows) => new()
        {
            TaxId = TaxId,
            YearRangeRVId = YearRangeId,
            BaseType = baseType,
            Rows = rows.Select(r => new ValueBasedTaxRowDto
            {
                Id = r.Id,
                TypeOfUseId = r.TypeOfUseId,
                TaxPercentage = r.Percentage,
            }).ToList(),
        };

    [Fact]
    public async Task Save_InsertsRowsThatDoNotExistYet()
    {
        var (service, context) = Create();
        using var _ = context;

        var affected = await service.SaveAsync(Request("RV", (0, 1, 2.5m), (0, 2, 3m)));

        Assert.Equal(2, affected);
        Assert.Equal(2, await context.TaxPercentageMasterRVs.CountAsync());
    }

    [Fact]
    public async Task Save_UpdatesAnExistingRow_RatherThanDuplicatingIt()
    {
        var (service, context) = Create();
        using var _ = context;
        context.TaxPercentageMasterRVs.Add(Pct(1, typeOfUseId: 1, percentage: 2m));
        await context.SaveChangesAsync();

        await service.SaveAsync(Request("RV", (1, 1, 7.5m)));

        var row = await context.TaxPercentageMasterRVs.SingleAsync();
        Assert.Equal(7.5m, row.TaxPercentage);
    }

    [Fact]
    public async Task Save_RewritesBaseType_AcrossRowsTheClientNeverSent()
    {
        // Server-side pagination means Rows is usually just the visible page. Base Type is one
        // shared RV/ALV choice for the whole tax+year, so a save from page 1 must move page 2 too —
        // otherwise the same tax would be billed on two different bases.
        var (service, context) = Create();
        using var _ = context;
        context.TaxPercentageMasterRVs.AddRange(
            Pct(1, typeOfUseId: 1, percentage: 2m, baseType: "RV"),
            Pct(2, typeOfUseId: 2, percentage: 3m, baseType: "RV"),   // "another page"
            Pct(3, typeOfUseId: 3, percentage: 4m, baseType: "RV"));  // "another page"
        await context.SaveChangesAsync();

        await service.SaveAsync(Request("ALV", (1, 1, 2m))); // only row 1 is sent

        var bases = await context.TaxPercentageMasterRVs.Select(p => p.BaseType).ToListAsync();
        Assert.All(bases, b => Assert.Equal("ALV", b));
    }

    [Fact]
    public async Task Save_DoesNotTouchAnotherYearsBaseType()
    {
        // The setting is scoped to tax + YEAR, not the whole tax.
        var (service, context) = Create();
        using var _ = context;
        context.TaxPercentageMasterRVs.AddRange(
            Pct(1, typeOfUseId: 1, percentage: 2m, baseType: "RV", yearRangeId: YearRangeId),
            Pct(2, typeOfUseId: 1, percentage: 2m, baseType: "RV", yearRangeId: 99));
        await context.SaveChangesAsync();

        await service.SaveAsync(Request("ALV", (1, 1, 2m)));

        var otherYear = await context.TaxPercentageMasterRVs.SingleAsync(p => p.YearRangeRVId == 99);
        Assert.Equal("RV", otherYear.BaseType);
    }

    [Fact]
    public async Task Save_RejectsTheSameTypeOfUseTwiceInOneRequest()
    {
        // Reaching the unique index would roll back every valid row with an opaque DB error.
        var (service, context) = Create();
        using var _ = context;

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(Request("RV", (0, 1, 2m), (0, 1, 3m))));
        Assert.Contains("Duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPercentages_ReturnsOnlyTheRequestedTaxAndYear()
    {
        var (service, context) = Create();
        using var _ = context;
        context.TaxPercentageMasterRVs.AddRange(
            Pct(1, typeOfUseId: 1, percentage: 2m),
            Pct(2, typeOfUseId: 2, percentage: 3m, yearRangeId: 99));
        await context.SaveChangesAsync();

        var page = await service.GetPercentagesAsync(TaxId, YearRangeId, userGroup: null, pageNumber: 1, pageSize: 10);

        Assert.Equal(1, page.TotalCount);
    }
}

/// <summary>
/// <see cref="MasterBasedTaxService"/>. Its mapping rows are keyed by (year, master key), so the
/// validation that matters is rejecting a repeated key inside one request before it reaches the
/// unique index and takes the whole batch down with it.
/// </summary>
public class MasterBasedTaxServiceTests
{
    private const int TaxId = 20;
    private const int YearRangeId = 1;
    private const int RuleDefinitionId = 3;

    private static (MasterBasedTaxService Service, ApplicationDbContext Context) Create()
    {
        var context = DynamicTaxTestContext.Create();
        context.TaxCalculationModeMaster.AddRange(DynamicTaxTestContext.SeededModes());
        context.TaxMaster.Add(DynamicTaxTestContext.Tax(TaxId, "MST", calculationModeId: 3, ruleDefinitionId: RuleDefinitionId));
        context.SaveChanges();
        return (new MasterBasedTaxService(context), context);
    }

    private static SaveMasterMappingRequest Request(params TaxMasterMappingDto[] rows) => new()
    {
        TaxId = TaxId,
        RuleDefinitionId = RuleDefinitionId,
        Rows = [.. rows],
    };

    private static TaxMasterMappingDto Row(
        string masterKey = "12",
        string resultMode = "FIXED",
        string resultBase = "NONE",
        decimal resultValue = 500m,
        int assessmentYearRangeId = YearRangeId) => new()
        {
            TaxId = TaxId,
            MasterKey = masterKey,
            DisplayValue = $"Display {masterKey}",
            ResultMode = resultMode,
            ResultBase = resultBase,
            ResultValue = resultValue,
            AssessmentYearRangeId = assessmentYearRangeId,
        };

    [Theory]
    [InlineData("FIXED", "NONE")]
    [InlineData("PERCENT", "RV")]
    [InlineData("PERCENT", "ALV")]
    public async Task ValidModeAndBaseCombinations_AreAccepted(string resultMode, string resultBase)
    {
        var (service, context) = Create();
        using var _ = context;

        Assert.Equal(1, await service.SaveAsync(Request(Row(resultMode: resultMode, resultBase: resultBase))));
    }

    [Theory]
    [InlineData("PER_UNIT")]  // valid for condition rules, but NOT for master mappings
    [InlineData("SLAB")]
    public async Task InvalidResultMode_IsRejected(string resultMode)
    {
        var (service, context) = Create();
        using var _ = context;

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(Request(Row(resultMode: resultMode))));
        Assert.Contains("ResultMode", ex.Message);
    }

    [Fact]
    public async Task OtherTaxBase_IsRejected_BecauseMappingsCannotReferenceAnotherTax()
    {
        var (service, context) = Create();
        using var _ = context;

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(Request(Row(resultMode: "PERCENT", resultBase: "OTHER_TAX"))));
        Assert.Contains("ResultBase", ex.Message);
    }

    [Fact]
    public async Task TheSameMasterKeyTwiceInOneYear_IsRejected()
    {
        var (service, context) = Create();
        using var _ = context;

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(Request(Row(masterKey: "12"), Row(masterKey: "12"))));
        Assert.Contains("12", ex.Message);
    }

    [Fact]
    public async Task TheSameMasterKeyIsCaseInsensitiveForDuplicateDetection()
    {
        var (service, context) = Create();
        using var _ = context;

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SaveAsync(Request(Row(masterKey: "abc"), Row(masterKey: "ABC"))));
    }

    [Fact]
    public async Task TheSameMasterKeyInDifferentYears_IsAllowed()
    {
        // The natural key is (year, master key) — the same property type legitimately has a
        // different charge in a different assessment year.
        var (service, context) = Create();
        using var _ = context;

        var affected = await service.SaveAsync(Request(
            Row(masterKey: "12", assessmentYearRangeId: 1),
            Row(masterKey: "12", assessmentYearRangeId: 2)));

        Assert.Equal(2, affected);
    }

    [Fact]
    public async Task Save_UpsertsOnTheNaturalKey_RatherThanDuplicating()
    {
        var (service, context) = Create();
        using var _ = context;

        await service.SaveAsync(Request(Row(masterKey: "12", resultValue: 500m)));
        await service.SaveAsync(Request(Row(masterKey: "12", resultValue: 750m)));

        var row = await context.TaxMasterMappings.SingleAsync();
        Assert.Equal(750m, row.ResultValue);
    }

    [Fact]
    public async Task ARejectedSave_PersistsNothing()
    {
        var (service, context) = Create();
        using var _ = context;

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync(Request(
            Row(masterKey: "12"),
            Row(masterKey: "13", resultMode: "NOT_A_MODE"))));

        Assert.Empty(await context.TaxMasterMappings.ToListAsync());
    }

    [Fact]
    public async Task GetMappings_ReturnsOnlyTheRequestedTaxsRows()
    {
        var (service, context) = Create();
        using var _ = context;
        context.TaxMasterMappings.AddRange(
            new TaxMasterMappingEntity
            {
                Id = 1, TaxId = TaxId, RuleDefinitionId = RuleDefinitionId, MasterKey = "12",
                AssessmentYearRangeId = YearRangeId, ResultMode = "FIXED", ResultBase = "NONE", ResultValue = 100m,
            },
            new TaxMasterMappingEntity
            {
                Id = 2, TaxId = 999, RuleDefinitionId = RuleDefinitionId, MasterKey = "12",
                AssessmentYearRangeId = YearRangeId, ResultMode = "FIXED", ResultBase = "NONE", ResultValue = 100m,
            });
        await context.SaveChangesAsync();

        var page = await service.GetMappingsAsync(
            TaxId, assessmentYearRangeId: null, pageNumber: 1, pageSize: 10, ruleDefinitionId: RuleDefinitionId);

        Assert.Equal(1, page.TotalCount);
        Assert.All(page.Items, r => Assert.Equal(TaxId, r.TaxId));
    }
}
