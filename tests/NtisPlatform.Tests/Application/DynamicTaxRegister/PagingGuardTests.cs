using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.DynamicTaxRegister;

/// <summary>
/// <see cref="PagingGuard"/> is the floor and ceiling under every Dynamic Tax Register paged read.
/// The values it clamps used to reach SQL Server's OFFSET/FETCH directly, so the cases below are
/// the ones that previously threw, overflowed, or let a caller pull an entire table.
/// </summary>
public class PagingGuardTests
{
    // Mirrors the private constants in PagingGuard; duplicated deliberately so a change to either
    // has to be made consciously in both places rather than the test silently following along.
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;
    private const int MaxUnboundedPageSize = 5000;

    [Fact]
    public void OrdinaryPage_IsPassedThroughUnchanged()
    {
        var (pageNumber, pageSize, skip) = PagingGuard.Normalize(pageNumber: 3, pageSize: 10, totalCount: 500);

        Assert.Equal(3, pageNumber);
        Assert.Equal(10, pageSize);
        Assert.Equal(20, skip);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(int.MinValue)]
    public void PageNumberBelowOne_IsFlooredAtOne(int requested)
    {
        var (pageNumber, _, skip) = PagingGuard.Normalize(requested, pageSize: 10, totalCount: 100);

        Assert.Equal(1, pageNumber);
        Assert.Equal(0, skip);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]      // negative but NOT the -1 sentinel
    [InlineData(int.MinValue)]
    public void InvalidPageSize_FallsBackToTheDefault_NotZero(int requested)
    {
        // A zero/negative Take reached SQL Server directly before this guard existed and threw.
        var (_, pageSize, _) = PagingGuard.Normalize(pageNumber: 1, requested, totalCount: 100);

        Assert.Equal(DefaultPageSize, pageSize);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void ExplicitPageSize_IsCappedAtTheMaximum(int requested)
    {
        var (_, pageSize, _) = PagingGuard.Normalize(pageNumber: 1, requested, totalCount: 100_000);

        Assert.Equal(MaxPageSize, pageSize);
    }

    [Fact]
    public void PageSizeExactlyAtTheCap_IsNotReduced()
    {
        var (_, pageSize, _) = PagingGuard.Normalize(pageNumber: 1, MaxPageSize, totalCount: 100_000);

        Assert.Equal(MaxPageSize, pageSize);
    }

    // ── the "-1 means everything" convention ────────────────────────────────────

    [Fact]
    public void MinusOne_ReturnsEverything_FromTheFirstRow()
    {
        // Several call sites rely on -1 to populate reference dropdowns from small master tables,
        // so the convention itself must survive the guard.
        var (pageNumber, pageSize, skip) = PagingGuard.Normalize(pageNumber: 1, pageSize: -1, totalCount: 342);

        Assert.Equal(1, pageNumber);
        Assert.Equal(342, pageSize);
        Assert.Equal(0, skip);
    }

    [Fact]
    public void MinusOne_IsStillBounded_SoOneCallerCannotPullAnEntireTable()
    {
        var (_, pageSize, _) = PagingGuard.Normalize(pageNumber: 1, pageSize: -1, totalCount: 10_000_000);

        Assert.Equal(MaxUnboundedPageSize, pageSize);
    }

    [Fact]
    public void MinusOne_OnAnEmptyTable_StillTakesAtLeastOne()
    {
        // Take(0) is not a valid page size to hand downstream; the result set is empty either way.
        var (_, pageSize, skip) = PagingGuard.Normalize(pageNumber: 1, pageSize: -1, totalCount: 0);

        Assert.Equal(1, pageSize);
        Assert.Equal(0, skip);
    }

    [Fact]
    public void MinusOne_IgnoresPageNumber_ForSkip()
    {
        // "Everything" has no second page — skipping would silently drop rows.
        var (pageNumber, _, skip) = PagingGuard.Normalize(pageNumber: 7, pageSize: -1, totalCount: 50);

        Assert.Equal(7, pageNumber); // echoed back for the caller's response envelope
        Assert.Equal(0, skip);
    }

    // ── overflow / out-of-range paging ──────────────────────────────────────────

    [Fact]
    public void AbsurdPageNumber_DoesNotOverflow_AndClampsToTotalCount()
    {
        // (300000000 - 1) * 100 overflows int; the guard does this arithmetic in long.
        var (_, _, skip) = PagingGuard.Normalize(pageNumber: 300_000_000, pageSize: 100, totalCount: 500);

        Assert.Equal(500, skip);
    }

    [Fact]
    public void PageBeyondTheEnd_SkipsExactlyTotalCount_YieldingAnEmptyPage()
    {
        var (_, _, skip) = PagingGuard.Normalize(pageNumber: 99, pageSize: 10, totalCount: 25);

        Assert.Equal(25, skip);
    }

    [Fact]
    public void LastPartialPage_SkipsToItsStart()
    {
        var (_, pageSize, skip) = PagingGuard.Normalize(pageNumber: 3, pageSize: 10, totalCount: 25);

        Assert.Equal(10, pageSize);
        Assert.Equal(20, skip);
    }

    [Fact]
    public void EmptyTable_WithAnOrdinaryPageSize_SkipsNothing()
    {
        var (pageNumber, pageSize, skip) = PagingGuard.Normalize(pageNumber: 1, pageSize: 10, totalCount: 0);

        Assert.Equal(1, pageNumber);
        Assert.Equal(10, pageSize);
        Assert.Equal(0, skip);
    }
}
