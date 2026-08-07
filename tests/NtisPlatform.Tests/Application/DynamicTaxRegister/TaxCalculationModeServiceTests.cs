using System.Linq;
using System.Threading.Tasks;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Application.DynamicTaxRegister;

/// <summary>
/// Calculation modes are DB-driven rows, and every consumer is expected to branch on this service's
/// capability flags rather than on a mode code string. The asymmetry that matters most here:
/// <c>GetByCodeAsync</c> only sees ACTIVE modes (a retired mode must not be newly selectable) while
/// <c>GetByIdAsync</c> must still resolve INACTIVE ones (taxes already pointing at them must keep
/// rendering).
/// </summary>
public class TaxCalculationModeServiceTests
{
    private static (TaxCalculationModeService Service, NtisPlatform.Infrastructure.Data.ApplicationDbContext Context) Seeded(
        bool includeRetired = false)
    {
        var context = DynamicTaxTestContext.Create();
        context.TaxCalculationModeMaster.AddRange(DynamicTaxTestContext.SeededModes());
        if (includeRetired)
        {
            context.TaxCalculationModeMaster.Add(
                DynamicTaxTestContext.Mode(99, "RETIRED_MODE", value: true, isActive: false, displayOrder: 99));
        }
        context.SaveChanges();
        return (new TaxCalculationModeService(context), context);
    }

    [Fact]
    public async Task GetActive_ReturnsTheFourSeededModes_InDisplayOrder()
    {
        var (service, context) = Seeded();
        using var _ = context;

        var modes = await service.GetActiveAsync();

        Assert.Equal(
            ["VALUE_BASED", "CONDITION_BASED", "MASTER_BASED", "HYBRID"],
            modes.Select(m => m.ModeCode));
    }

    [Fact]
    public async Task GetActive_ExcludesRetiredModes()
    {
        var (service, context) = Seeded(includeRetired: true);
        using var _ = context;

        var modes = await service.GetActiveAsync();

        Assert.DoesNotContain(modes, m => m.ModeCode == "RETIRED_MODE");
    }

    [Fact]
    public async Task GetById_StillResolvesARetiredMode()
    {
        // A tax already assigned to a retired mode must keep resolving, or the register grid would
        // render it with a blank category.
        var (service, context) = Seeded(includeRetired: true);
        using var _ = context;

        var mode = await service.GetByIdAsync(99);

        Assert.NotNull(mode);
        Assert.Equal("RETIRED_MODE", mode!.ModeCode);
    }

    [Fact]
    public async Task GetByCode_DoesNotResolveARetiredMode()
    {
        // The inverse of the above: retired modes must not be newly selectable on a save.
        var (service, context) = Seeded(includeRetired: true);
        using var _ = context;

        Assert.Null(await service.GetByCodeAsync("RETIRED_MODE"));
    }

    [Theory]
    [InlineData("HYBRID")]
    [InlineData("hybrid")]
    [InlineData("  HYBRID  ")]
    public async Task GetByCode_IsCaseInsensitiveAndTrims(string code)
    {
        var (service, context) = Seeded();
        using var _ = context;

        var mode = await service.GetByCodeAsync(code);

        Assert.NotNull(mode);
        Assert.Equal("HYBRID", mode!.ModeCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NOT_A_MODE")]
    public async Task GetByCode_UnknownOrBlank_ReturnsNull(string? code)
    {
        // Callers translate null into a validation error naming the allowed values; it must not throw.
        var (service, context) = Seeded();
        using var _ = context;

        Assert.Null(await service.GetByCodeAsync(code));
    }

    [Fact]
    public async Task GetById_NullId_ReturnsNull()
    {
        var (service, context) = Seeded();
        using var _ = context;

        Assert.Null(await service.GetByIdAsync(null));
    }

    [Fact]
    public async Task CapabilityFlags_MatchTheSeededDefinition()
    {
        // These flags are how every consumer decides which configuration surface a tax uses, so a
        // seed drift here would mis-route calculation. HYBRID deliberately sets THREE flags.
        var (service, context) = Seeded();
        using var _ = context;

        var modes = (await service.GetActiveAsync()).ToDictionary(m => m.ModeCode);

        Assert.True(modes["VALUE_BASED"].UsesValueConfig);
        Assert.False(modes["VALUE_BASED"].UsesConditionConfig);

        Assert.True(modes["CONDITION_BASED"].UsesConditionConfig);
        Assert.False(modes["CONDITION_BASED"].UsesMasterConfig);

        Assert.True(modes["MASTER_BASED"].UsesMasterConfig);
        Assert.False(modes["MASTER_BASED"].UsesConditionConfig);

        var hybrid = modes["HYBRID"];
        Assert.True(hybrid.UsesHybridConfig);
        Assert.True(hybrid.UsesConditionConfig);
        Assert.True(hybrid.UsesMasterConfig);
        Assert.False(hybrid.UsesValueConfig);
    }

    [Fact]
    public async Task RepeatedReads_AreServedFromTheMemoisedLoad()
    {
        // The class memoises per scoped instance because a settings save reads it 2-3 times.
        // Verified behaviourally: rows added AFTER the first read are not observed by later reads.
        var (service, context) = Seeded();
        using var _ = context;

        var first = await service.GetActiveAsync();

        context.TaxCalculationModeMaster.Add(
            DynamicTaxTestContext.Mode(50, "ADDED_LATER", value: true, displayOrder: 50));
        await context.SaveChangesAsync();

        var second = await service.GetActiveAsync();

        Assert.Equal(first.Count, second.Count);
        Assert.DoesNotContain(second, m => m.ModeCode == "ADDED_LATER");
    }
}
