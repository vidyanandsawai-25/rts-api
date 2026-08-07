using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Application.DynamicTaxRegister;

/// <summary>
/// <see cref="HybridTaxService"/> stores one strategy row per HYBRID tax. Its enum validation is
/// the only thing standing between a typo in a request and a tax whose evaluation order is
/// meaningless, so each accepted and rejected value is pinned here.
/// </summary>
public class HybridTaxServiceTests
{
    private const int TaxId = 10;

    private static TaxHybridConfigDto Config(
        string priority = "MASTER_THEN_CONDITION",
        string fallback = "DEFAULT_ZERO",
        string resultBase = "NONE",
        int taxId = TaxId) => new()
        {
            TaxId = taxId,
            EvaluationPriority = priority,
            FallbackStrategy = fallback,
            ResultBase = resultBase,
        };

    private static async Task<ApplicationDbContextScope> SeededAsync()
    {
        var context = DynamicTaxTestContext.Create();
        context.TaxMaster.Add(DynamicTaxTestContext.Tax(TaxId, "HYB", calculationModeId: 4));
        await context.SaveChangesAsync();
        return new ApplicationDbContextScope(context);
    }

    private sealed class ApplicationDbContextScope(NtisPlatform.Infrastructure.Data.ApplicationDbContext context) : IDisposable
    {
        public NtisPlatform.Infrastructure.Data.ApplicationDbContext Context { get; } = context;
        public HybridTaxService Service { get; } = new(context);
        public void Dispose() => Context.Dispose();
    }

    // ── defaults ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetConfig_WhenUnconfigured_ReturnsDocumentedDefaults_NotNull()
    {
        // Callers (including the calculation engine) must not have to null-check this.
        using var scope = await SeededAsync();

        var config = await scope.Service.GetConfigAsync(TaxId);

        Assert.NotNull(config);
        Assert.Equal("MASTER_THEN_CONDITION", config.EvaluationPriority);
        Assert.Equal("DEFAULT_ZERO", config.FallbackStrategy);
        Assert.Equal("NONE", config.ResultBase);
    }

    // ── validation ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("MASTER_THEN_CONDITION")]
    [InlineData("CONDITION_THEN_MASTER")]
    public async Task ValidEvaluationPriority_IsAccepted(string priority)
    {
        using var scope = await SeededAsync();

        var saved = await scope.Service.SaveConfigAsync(Config(priority: priority));

        Assert.Equal(priority, saved.EvaluationPriority);
    }

    [Theory]
    [InlineData("MASTER_FIRST")]
    [InlineData("master_then_condition")] // validation is case-SENSITIVE
    [InlineData("")]
    public async Task InvalidEvaluationPriority_IsRejected(string priority)
    {
        using var scope = await SeededAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => scope.Service.SaveConfigAsync(Config(priority: priority)));
        Assert.Contains("EvaluationPriority", ex.Message);
    }

    [Theory]
    [InlineData("DEFAULT_ZERO")]
    [InlineData("CONDITION_RULE")]
    public async Task ValidFallbackStrategy_IsAccepted(string fallback)
    {
        using var scope = await SeededAsync();

        var saved = await scope.Service.SaveConfigAsync(Config(fallback: fallback));

        Assert.Equal(fallback, saved.FallbackStrategy);
    }

    [Theory]
    [InlineData("ZERO")]
    [InlineData("FALLBACK")]
    public async Task InvalidFallbackStrategy_IsRejected(string fallback)
    {
        using var scope = await SeededAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => scope.Service.SaveConfigAsync(Config(fallback: fallback)));
        Assert.Contains("FallbackStrategy", ex.Message);
    }

    [Theory]
    [InlineData("NONE")]
    [InlineData("RV")]
    [InlineData("ALV")]
    public async Task ValidResultBase_IsAccepted(string resultBase)
    {
        using var scope = await SeededAsync();

        var saved = await scope.Service.SaveConfigAsync(Config(resultBase: resultBase));

        Assert.Equal(resultBase, saved.ResultBase);
    }

    [Theory]
    [InlineData("OTHER_TAX")] // valid for condition rules, but NOT for the hybrid strategy row
    [InlineData("CV")]
    public async Task InvalidResultBase_IsRejected(string resultBase)
    {
        using var scope = await SeededAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => scope.Service.SaveConfigAsync(Config(resultBase: resultBase)));
        Assert.Contains("ResultBase", ex.Message);
    }

    [Fact]
    public async Task UnknownTaxId_IsRejected()
    {
        // Prevents orphan strategy rows that no tax will ever read.
        using var scope = await SeededAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => scope.Service.SaveConfigAsync(Config(taxId: 9999)));
        Assert.Contains("9999", ex.Message);
    }

    // ── persistence ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Save_ThenGet_RoundTripsEveryField()
    {
        using var scope = await SeededAsync();

        await scope.Service.SaveConfigAsync(Config("CONDITION_THEN_MASTER", "CONDITION_RULE", "ALV"));
        var reloaded = await scope.Service.GetConfigAsync(TaxId);

        Assert.Equal("CONDITION_THEN_MASTER", reloaded.EvaluationPriority);
        Assert.Equal("CONDITION_RULE", reloaded.FallbackStrategy);
        Assert.Equal("ALV", reloaded.ResultBase);
    }

    [Fact]
    public async Task SavingTwice_UpdatesInPlace_LeavingExactlyOneRowPerTax()
    {
        // The table is one-row-per-tax; a second save must not create a duplicate strategy.
        using var scope = await SeededAsync();

        await scope.Service.SaveConfigAsync(Config("MASTER_THEN_CONDITION", "DEFAULT_ZERO", "NONE"));
        await scope.Service.SaveConfigAsync(Config("CONDITION_THEN_MASTER", "CONDITION_RULE", "RV"));

        var rows = await scope.Context.TaxHybridConfigs.Where(c => c.TaxId == TaxId).ToListAsync();

        Assert.Single(rows);
        Assert.Equal("CONDITION_THEN_MASTER", rows[0].EvaluationPriority);
        Assert.Equal("RV", rows[0].ResultBase);
    }

    [Fact]
    public async Task ValidationRunsBeforePersistence_SoARejectedSaveLeavesNoRow()
    {
        using var scope = await SeededAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => scope.Service.SaveConfigAsync(Config(priority: "NONSENSE")));

        Assert.Empty(await scope.Context.TaxHybridConfigs.ToListAsync());
    }
}
