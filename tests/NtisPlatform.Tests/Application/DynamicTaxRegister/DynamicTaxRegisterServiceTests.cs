using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Application.DynamicTaxRegister;

/// <summary>
/// <see cref="DynamicTaxRegisterService"/> — creation guards, the stats total, and category
/// filtering.
///
/// <para>
/// NOT covered here: <c>UpdateSettingsAsync</c>. It reads the tax with a raw
/// <c>WITH (UPDLOCK, ROWLOCK)</c> statement to serialise concurrent mode changes, and the EF
/// InMemory provider cannot execute raw SQL. Its guards are pinned indirectly through
/// <see cref="TaxModeChangeConflictExceptionTests"/>; end-to-end coverage of that path needs a
/// real SQL Server.
/// </para>
/// </summary>
public class DynamicTaxRegisterServiceTests
{
    private const int CategoryId = 1;
    private const int RuleId = 3;

    private static (DynamicTaxRegisterService Service, ApplicationDbContext Context) Create()
    {
        var context = DynamicTaxTestContext.Create();
        context.TaxCalculationModeMaster.AddRange(DynamicTaxTestContext.SeededModes());
        context.TaxCategoryMaster.AddRange(
            new TaxCategoryMasterEntity { Id = CategoryId, CategoryCode = "GEN", CategoryName = "General", IsActive = true },
            new TaxCategoryMasterEntity { Id = 2, CategoryCode = "EDU", CategoryName = "Education", IsActive = true },
            new TaxCategoryMasterEntity { Id = 3, CategoryCode = "EMP", CategoryName = "Employment", IsActive = true });
        context.DynamicTaxRuleMaster.Add(new DynamicTaxRuleEntity
        {
            Id = RuleId,
            DisplayName = "PropertyType rule",
            RuleType = "MASTER_BASED",
            AttachedReference = "PropertyType",
            IsActive = true,
        });
        context.SaveChanges();

        return (new DynamicTaxRegisterService(context, new TaxCalculationModeService(context)), context);
    }

    private static CreateTaxRegisterRequest Request(
        string calculationMode = "VALUE_BASED",
        string status = "ACTIVE",
        int taxCategoryId = CategoryId,
        int? ruleDefinitionId = null,
        string taxName = "New Tax",
        string taxCode = "NEW") => new()
        {
            TaxName = taxName,
            TaxCode = taxCode,
            TaxCategoryId = taxCategoryId,
            CalculationMode = calculationMode,
            RuleDefinitionId = ruleDefinitionId,
            Status = status,
        };

    // ── status parsing ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("ACTIVE", true)]
    [InlineData("DEACTIVE", false)]
    [InlineData("active", true)]
    [InlineData("  ACTIVE  ", true)]
    public async Task Status_IsParsedCaseInsensitivelyAndTrimmed(string status, bool expectedIsActive)
    {
        var (service, context) = Create();
        using var _ = context;

        var id = await service.CreateAsync(Request(status: status));

        var tax = await context.TaxMaster.SingleAsync(t => t.Id == id);
        Assert.Equal(expectedIsActive, tax.IsActive);
    }

    [Theory]
    [InlineData("Activ")]      // a typo used to silently DEACTIVATE the tax with a 200 OK
    [InlineData("ENABLED")]
    [InlineData("")]
    [InlineData(null)]
    public async Task InvalidStatus_IsRejected_RatherThanSilentlyDeactivating(string? status)
    {
        var (service, context) = Create();
        using var _ = context;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Request(status: status!)));
        Assert.Contains("ACTIVE, DEACTIVE", ex.Message);
    }

    // ── creation guards ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UnknownTaxCategory_IsRejected()
    {
        var (service, context) = Create();
        using var _ = context;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Request(taxCategoryId: 999)));
        Assert.Contains("999", ex.Message);
    }

    [Fact]
    public async Task UnknownRuleDefinition_IsRejected()
    {
        var (service, context) = Create();
        using var _ = context;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Request(ruleDefinitionId: 999)));
        Assert.Contains("999", ex.Message);
    }

    [Theory]
    [InlineData("NOT_A_MODE")]
    [InlineData("")]
    public async Task UnknownCalculationMode_IsRejected_AndNamesTheValidOnes(string mode)
    {
        // A new tax must never be created in an unknown or retired mode.
        var (service, context) = Create();
        using var _ = context;

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Request(calculationMode: mode)));
        Assert.Contains("VALUE_BASED", ex.Message);
    }

    [Fact]
    public async Task Create_ResolvesTheModeCodeToItsForeignKey()
    {
        var (service, context) = Create();
        using var _ = context;

        var id = await service.CreateAsync(Request(calculationMode: "HYBRID"));

        var tax = await context.TaxMaster.SingleAsync(t => t.Id == id);
        Assert.Equal(4, tax.CalculationModeId); // the seeded HYBRID row
    }

    [Fact]
    public async Task Create_TrimsNameAndCode_AndNormalisesABlankAliasToNull()
    {
        var (service, context) = Create();
        using var _ = context;

        var id = await service.CreateAsync(new CreateTaxRegisterRequest
        {
            TaxName = "  Padded Name  ",
            TaxNameAlias = "   ",
            TaxCode = "  PAD  ",
            TaxCategoryId = CategoryId,
            CalculationMode = "VALUE_BASED",
            Status = "ACTIVE",
        });

        var tax = await context.TaxMaster.SingleAsync(t => t.Id == id);
        Assert.Equal("Padded Name", tax.TaxName);
        Assert.Equal("PAD", tax.TaxCode);
        Assert.Null(tax.TaxNameAlias);
    }

    [Fact]
    public async Task ARejectedCreate_PersistsNothing()
    {
        var (service, context) = Create();
        using var _ = context;

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Request(status: "NONSENSE")));

        Assert.Empty(await context.TaxMaster.ToListAsync());
    }

    // ── stats ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Stats_TotalSumsEveryMode_IncludingOneWithNoHeroCard()
    {
        // A mode added to the DB later has no card of its own, but its taxes must still be counted
        // in the total — otherwise the total silently disagrees with the grid's own row count.
        var (service, context) = Create();
        using var _ = context;
        context.TaxCalculationModeMaster.Add(
            DynamicTaxTestContext.Mode(5, "FUTURE_MODE", value: true, displayOrder: 5));
        context.TaxMaster.AddRange(
            DynamicTaxTestContext.Tax(1, "A", calculationModeId: 1),
            DynamicTaxTestContext.Tax(2, "B", calculationModeId: 2),
            DynamicTaxTestContext.Tax(3, "C", calculationModeId: 3),
            DynamicTaxTestContext.Tax(4, "D", calculationModeId: 4),
            DynamicTaxTestContext.Tax(5, "E", calculationModeId: 5)); // the mode with no card
        await context.SaveChangesAsync();

        var stats = await service.GetStatsAsync();

        Assert.Equal(1, stats.ValueBased);
        Assert.Equal(1, stats.ConditionBased);
        Assert.Equal(1, stats.MasterBased);
        Assert.Equal(1, stats.Hybrid);
        Assert.Equal(5, stats.Total); // NOT 4
    }

    [Fact]
    public async Task Stats_OnAnEmptyRegister_AreAllZero()
    {
        var (service, context) = Create();
        using var _ = context;

        var stats = await service.GetStatsAsync();

        Assert.Equal(0, stats.Total);
    }

    // ── category options ────────────────────────────────────────────────────────

    [Fact]
    public async Task TaxCategories_ExcludeEducationAndEmployment()
    {
        // EDU/EMP are slab-computed on a separate path and are not configurable here, so offering
        // them in the Add-Tax picker would let an admin create a tax this screen cannot configure.
        var (service, context) = Create();
        using var _ = context;

        var categories = await service.GetTaxCategoriesAsync();

        Assert.DoesNotContain(categories, c => c.Code is "EDU" or "EMP");
        Assert.Contains(categories, c => c.Code == "GEN");
    }

    [Fact]
    public async Task TaxCategories_ExcludeInactiveOnes()
    {
        var (service, context) = Create();
        using var _ = context;
        context.TaxCategoryMaster.Add(new TaxCategoryMasterEntity
        {
            Id = 4, CategoryCode = "OLD", CategoryName = "Retired", IsActive = false,
        });
        await context.SaveChangesAsync();

        var categories = await service.GetTaxCategoriesAsync();

        Assert.DoesNotContain(categories, c => c.Code == "OLD");
    }
}

/// <summary>
/// The three mode-change conflict signals. Each carries a distinct error code that the API surfaces
/// as a 409 and the UI branches on, so the codes are part of the contract, not just a message.
/// </summary>
public class TaxModeChangeConflictExceptionTests
{
    [Fact]
    public void ExpectedModeRequired_CarriesItsOwnCode_AndNamesBothModes()
    {
        var ex = TaxModeChangeConflictException.ExpectedModeRequired("VALUE_BASED", "HYBRID");

        Assert.Equal("DTR_MODE_CHANGE_EXPECTED_MODE_REQUIRED", ex.ErrorCode);
        Assert.Equal("VALUE_BASED", ex.CurrentMode);
        Assert.Equal("HYBRID", ex.RequestedMode);
    }

    [Fact]
    public void ConfirmationRequired_SaysWhatWillBeDeleted()
    {
        // Deletion of the abandoned mode's configuration is never implicit.
        var ex = TaxModeChangeConflictException.ConfirmationRequired("MASTER_BASED", "CONDITION_BASED");

        Assert.Equal("DTR_MODE_CHANGE_CONFIRMATION_REQUIRED", ex.ErrorCode);
        Assert.Contains("deletes the configuration", ex.Message);
        Assert.Contains("ConfirmModeChangeCleanup=true", ex.Message);
    }

    [Fact]
    public void StaleClient_ReportsWhatWasActuallyStored()
    {
        // Raised when the caller's view of the tax is out of date — it may have been about to
        // destroy configuration it never warned the user about.
        var ex = TaxModeChangeConflictException.StaleClient("HYBRID", "VALUE_BASED");

        Assert.Equal("DTR_MODE_CHANGE_STALE_CLIENT", ex.ErrorCode);
        Assert.Contains("HYBRID", ex.Message);
        Assert.Contains("Reload", ex.Message);
    }

    [Fact]
    public void EveryFactory_ProducesADistinctErrorCode()
    {
        string[] codes =
        [
            TaxModeChangeConflictException.ExpectedModeRequired("A", "B").ErrorCode,
            TaxModeChangeConflictException.ConfirmationRequired("A", "B").ErrorCode,
            TaxModeChangeConflictException.StaleClient("A", "B").ErrorCode,
        ];

        Assert.Equal(codes.Length, codes.Distinct().Count());
    }
}
