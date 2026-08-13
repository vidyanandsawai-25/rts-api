using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Application.DynamicTaxRegister;

/// <summary>
/// Save-path validation for condition rules. This is the last line of defence before a
/// misconfigured rule becomes a wrong charge, and it also NORMALISES rows — clearing fields that
/// belong to a different result mode — so that the server's duplicate check sees the same shape
/// the client does.
/// </summary>
public class TaxConditionRuleServiceValidationTests
{
    private const int TaxId = 10;
    private const int OtherTaxId = 11;

    private sealed class Harness : IDisposable
    {
        public ApplicationDbContext Context { get; }
        public TaxConditionRuleService Service { get; }

        public Harness()
        {
            Context = DynamicTaxTestContext.Create();
            Context.TaxMaster.Add(DynamicTaxTestContext.Tax(TaxId, "COND", calculationModeId: 2));
            Context.TaxMaster.Add(DynamicTaxTestContext.Tax(OtherTaxId, "OTHER", calculationModeId: 1));
            Context.SaveChanges();

            var rulesFieldRepo = new Mock<IRepository<RulesFieldEntity, int>>();
            rulesFieldRepo.Setup(r => r.GetQueryable())
                .Returns(new List<RulesFieldEntity>
                {
                    new() { Id = 1, FieldName = "NoOfToilets", DatabaseColumnName = "NoOfToilets", IsActive = true },
                }.BuildMock());

            Service = new TaxConditionRuleService(
                Context,
                rulesFieldRepo.Object,
                Mock.Of<IPropertyContextLoaderService>(),
                Mock.Of<IPropertyFieldFlattenerService>(),
                Mock.Of<IConditionRuleEvaluator>(),
                Mock.Of<ITaxMasterDataService>(),
                Mock.Of<IFinanceYearProvider>());
        }

        public void Dispose() => Context.Dispose();
    }

    private static TaxConditionRuleDto Row(
        string resultMode = "FIXED",
        string resultBase = "NONE",
        decimal resultValue = 100m,
        int? referenceTaxId = null,
        string? unitFieldId = null,
        string assessmentBasis = "PROPERTY_BASED",
        int id = 0,
        int sortOrder = 1,
        List<TaxConditionItemDto>? conditions = null) => new()
        {
            Id = id,
            TaxId = TaxId,
            SortOrder = sortOrder,
            Conditions = conditions ?? [],
            ResultMode = resultMode,
            ResultBase = resultBase,
            ResultValue = resultValue,
            ReferenceTaxId = referenceTaxId,
            UnitFieldId = unitFieldId,
            AssessmentBasis = assessmentBasis,
            IsActive = true,
        };

    private static SaveTaxConditionRuleRequest Request(params TaxConditionRuleDto[] rows) =>
        new() { TaxId = TaxId, Rows = [.. rows] };

    // ── result mode / base vocabulary ───────────────────────────────────────────

    [Theory]
    [InlineData("FIXED")]
    [InlineData("PERCENT")]
    [InlineData("PER_UNIT")]
    public async Task ValidResultModes_AreAccepted(string resultMode)
    {
        using var h = new Harness();
        var row = Row(resultMode, resultBase: resultMode == "PERCENT" ? "RV" : "NONE",
            resultValue: 10m, unitFieldId: resultMode == "PER_UNIT" ? "NoOfToilets" : null);

        Assert.Equal(1, await h.Service.SaveAsync(Request(row)));
    }

    [Theory]
    [InlineData("SLAB")]
    [InlineData("fixed")]  // case-sensitive
    [InlineData("")]
    public async Task InvalidResultMode_IsRejected(string resultMode)
    {
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(Row(resultMode))));
        Assert.Contains("ResultMode", ex.Message);
    }

    [Theory]
    [InlineData("CV")]
    [InlineData("rv")]
    public async Task InvalidResultBase_IsRejected(string resultBase)
    {
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(Row("PERCENT", resultBase))));
        Assert.Contains("ResultBase", ex.Message);
    }

    // ── normalisation: fields belonging to another mode are cleared ─────────────

    [Fact]
    public async Task NonPercentMode_HasItsResultBaseForcedToNone()
    {
        // A base only means anything for PERCENT. Leaving a stale "RV" behind would make two
        // identical-looking rows compare differently between client and server.
        using var h = new Harness();

        await h.Service.SaveAsync(Request(Row("FIXED", resultBase: "RV", resultValue: 500m)));

        var saved = await h.Context.TaxConditionRules.SingleAsync();
        Assert.Equal("NONE", saved.ResultBase);
    }

    [Fact]
    public async Task NonOtherTaxBase_HasItsReferenceTaxIdCleared()
    {
        using var h = new Harness();

        await h.Service.SaveAsync(Request(Row("PERCENT", "RV", 10m, referenceTaxId: OtherTaxId)));

        var saved = await h.Context.TaxConditionRules.SingleAsync();
        Assert.Null(saved.ReferenceTaxId);
    }

    [Fact]
    public async Task NonPerUnitMode_HasItsUnitFieldIdCleared()
    {
        using var h = new Harness();

        await h.Service.SaveAsync(Request(Row("FIXED", resultValue: 500m, unitFieldId: "NoOfToilets")));

        var saved = await h.Context.TaxConditionRules.SingleAsync();
        Assert.Null(saved.UnitFieldId);
    }

    // ── mode-specific required fields ───────────────────────────────────────────

    [Fact]
    public async Task OtherTaxBase_WithoutAReferenceTax_IsRejected()
    {
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(Row("PERCENT", "OTHER_TAX", 10m, referenceTaxId: null))));
        Assert.Contains("ReferenceTaxId", ex.Message);
    }

    [Fact]
    public async Task ReferencingItsOwnTax_IsRejected()
    {
        // A tax charging a percentage of itself is an unresolvable self-reference.
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(Row("PERCENT", "OTHER_TAX", 10m, referenceTaxId: TaxId))));
        Assert.Contains("own tax", ex.Message);
    }

    [Fact]
    public async Task ReferencingANonExistentTax_IsRejected()
    {
        // Caught at save time rather than evaluating to a silent ₹0 forever.
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(Row("PERCENT", "OTHER_TAX", 10m, referenceTaxId: 9999))));
        Assert.Contains("9999", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PerUnit_WithoutAUnitField_IsRejected(string? unitFieldId)
    {
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(Row("PER_UNIT", resultValue: 150m, unitFieldId: unitFieldId))));
        Assert.Contains("UnitFieldId", ex.Message);
    }

    [Fact]
    public async Task PerUnit_NamingAnUnknownField_IsRejected()
    {
        // A typo or a since-renamed field would otherwise save cleanly and evaluate to ₹0 forever.
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(Row("PER_UNIT", resultValue: 150m, unitFieldId: "NotAField"))));
        Assert.Contains("NotAField", ex.Message);
    }

    [Fact]
    public async Task PerUnit_UnitFieldIdIsTrimmed()
    {
        using var h = new Harness();

        await h.Service.SaveAsync(Request(Row("PER_UNIT", resultValue: 150m, unitFieldId: "  NoOfToilets  ")));

        var saved = await h.Context.TaxConditionRules.SingleAsync();
        Assert.Equal("NoOfToilets", saved.UnitFieldId);
    }

    // ── per-mode value ceilings ─────────────────────────────────────────────────

    [Theory]
    [InlineData("PERCENT", 100)]   // a percentage cannot exceed 100
    [InlineData("FIXED", 999)]
    [InlineData("PER_UNIT", 99999)] // a per-unit RATE is a currency amount, so it needs headroom
    public async Task ResultValue_AtTheCeiling_IsAccepted(string resultMode, decimal atCeiling)
    {
        using var h = new Harness();
        var row = Row(resultMode, resultMode == "PERCENT" ? "RV" : "NONE", atCeiling,
            unitFieldId: resultMode == "PER_UNIT" ? "NoOfToilets" : null);

        Assert.Equal(1, await h.Service.SaveAsync(Request(row)));
    }

    [Theory]
    [InlineData("PERCENT", 101)]
    [InlineData("FIXED", 1000)]
    [InlineData("PER_UNIT", 100000)]
    public async Task ResultValue_AboveTheCeiling_IsRejected(string resultMode, decimal aboveCeiling)
    {
        using var h = new Harness();
        var row = Row(resultMode, resultMode == "PERCENT" ? "RV" : "NONE", aboveCeiling,
            unitFieldId: resultMode == "PER_UNIT" ? "NoOfToilets" : null);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => h.Service.SaveAsync(Request(row)));
        Assert.Contains("exceeds the maximum", ex.Message);
    }

    // ── AssessmentBasis ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("PROPERTY_BASED", false)]
    [InlineData("BUILDING_BASED", true)]
    [InlineData("property_based", false)] // parsed case-insensitively
    public async Task AssessmentBasis_MapsOntoIsBuildingBased(string basis, bool expected)
    {
        using var h = new Harness();

        await h.Service.SaveAsync(Request(Row(resultValue: 100m, assessmentBasis: basis)));

        var saved = await h.Context.TaxConditionRules.SingleAsync();
        Assert.Equal(expected, saved.IsBuildingBased);
    }

    [Theory]
    [InlineData("LAND_BASED")]
    [InlineData("")]
    public async Task InvalidAssessmentBasis_IsRejected(string basis)
    {
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(Row(resultValue: 100m, assessmentBasis: basis))));
        Assert.Contains("AssessmentBasis", ex.Message);
    }

    // ── condition item shape + duplicates ───────────────────────────────────────

    [Fact]
    public async Task ConditionItem_WithBlankFieldOrOperator_IsRejected()
    {
        using var h = new Harness();
        var row = Row(conditions: [new TaxConditionItemDto { FieldId = "  ", Operator = "=", Value = "1" }]);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => h.Service.SaveAsync(Request(row)));
        Assert.Contains("FieldId", ex.Message);
    }

    [Fact]
    public async Task ConditionItem_LogicalOperator_IsNormalisedRatherThanRejected()
    {
        // An older client that sends no LogicalOperator must not fail the save; it defaults to AND,
        // matching the evaluator's own left-to-right fold.
        using var h = new Harness();
        var row = Row(conditions:
        [
            new TaxConditionItemDto { FieldId = "Floor", Operator = "=", Value = "1", LogicalOperator = "" },
            new TaxConditionItemDto { FieldId = "Area", Operator = "=", Value = "2", LogicalOperator = "or" },
        ]);

        await h.Service.SaveAsync(Request(row));

        var saved = await h.Context.TaxConditionRules.SingleAsync();
        Assert.Contains("\"LogicalOperator\":\"AND\"", saved.ConditionsJson);
        Assert.Contains("\"LogicalOperator\":\"OR\"", saved.ConditionsJson);
    }

    [Fact]
    public async Task TwoIdenticalRows_InOneRequest_AreRejected()
    {
        // Identical rows are redundant and would double-charge under sum-all-matches evaluation.
        using var h = new Harness();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => h.Service.SaveAsync(Request(
                Row(resultValue: 500m, sortOrder: 1),
                Row(resultValue: 500m, sortOrder: 2))));
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public async Task RowsDifferingOnlyByResultValue_AreNotDuplicates()
    {
        using var h = new Harness();

        var affected = await h.Service.SaveAsync(Request(
            Row(resultValue: 500m, sortOrder: 1),
            Row(resultValue: 600m, sortOrder: 2)));

        Assert.Equal(2, affected);
    }

    [Fact]
    public async Task ARejectedSave_PersistsNothing()
    {
        // Validation must run before any row is written, or a partial save would leave the tax in
        // a state the admin never asked for.
        using var h = new Harness();

        await Assert.ThrowsAsync<ArgumentException>(() => h.Service.SaveAsync(Request(
            Row(resultValue: 100m, sortOrder: 1),
            Row("NOT_A_MODE", sortOrder: 2))));

        Assert.Empty(await h.Context.TaxConditionRules.ToListAsync());
    }

    // ── read path ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTax_ReturnsRowsInEvaluationOrder()
    {
        using var h = new Harness();
        h.Context.TaxConditionRules.AddRange(
            DynamicTaxTestContext.ConditionRule(3, TaxId, sortOrder: 3),
            DynamicTaxTestContext.ConditionRule(1, TaxId, sortOrder: 1),
            DynamicTaxTestContext.ConditionRule(2, TaxId, sortOrder: 2));
        await h.Context.SaveChangesAsync();

        var page = await h.Service.GetByTaxAsync(TaxId, ruleDefinitionId: null, pageNumber: 1, pageSize: 10);

        Assert.Equal([1, 2, 3], page.Items.Select(r => r.SortOrder));
    }

    [Fact]
    public async Task GetByTax_ReturnsInactiveRowsToo_BecauseTheEditorMustShowThem()
    {
        // The drawer is a CRUD surface: an admin has to be able to see and re-enable a row they
        // previously deactivated.
        using var h = new Harness();
        h.Context.TaxConditionRules.AddRange(
            DynamicTaxTestContext.ConditionRule(1, TaxId, sortOrder: 1, isActive: true),
            DynamicTaxTestContext.ConditionRule(2, TaxId, sortOrder: 2, isActive: false));
        await h.Context.SaveChangesAsync();

        var page = await h.Service.GetByTaxAsync(TaxId, ruleDefinitionId: null, pageNumber: 1, pageSize: 10);

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetByTax_DoesNotLeakAnotherTaxsRows()
    {
        using var h = new Harness();
        h.Context.TaxConditionRules.AddRange(
            DynamicTaxTestContext.ConditionRule(1, TaxId, sortOrder: 1),
            DynamicTaxTestContext.ConditionRule(2, OtherTaxId, sortOrder: 1));
        await h.Context.SaveChangesAsync();

        var page = await h.Service.GetByTaxAsync(TaxId, ruleDefinitionId: null, pageNumber: 1, pageSize: 10);

        Assert.Equal(1, page.TotalCount);
        Assert.All(page.Items, r => Assert.Equal(TaxId, r.TaxId));
    }

    [Fact]
    public async Task Delete_RemovesOnlyTheNamedRow_AndOnlyFromItsOwnTax()
    {
        using var h = new Harness();
        h.Context.TaxConditionRules.AddRange(
            DynamicTaxTestContext.ConditionRule(1, TaxId, sortOrder: 1),
            DynamicTaxTestContext.ConditionRule(2, TaxId, sortOrder: 2));
        await h.Context.SaveChangesAsync();

        await h.Service.DeleteAsync(1, TaxId);

        var remaining = await h.Context.TaxConditionRules.Select(r => r.Id).ToListAsync();
        Assert.Equal([2], remaining);
    }

    [Fact]
    public async Task Delete_WithAMismatchedTaxId_IsRejected_SoOneTaxCannotDeleteAnothersRow()
    {
        using var h = new Harness();
        h.Context.TaxConditionRules.Add(DynamicTaxTestContext.ConditionRule(1, TaxId, sortOrder: 1));
        await h.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => h.Service.DeleteAsync(1, OtherTaxId));
        Assert.Single(await h.Context.TaxConditionRules.ToListAsync());
    }
}
