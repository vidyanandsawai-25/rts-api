using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Tests for PropertyRepository.GetMaxPartition - the data-access half of the
/// `getmaxpartition` endpoint. Exercises filtering (ward, property no, active,
/// soft-delete) and the natural-order selection of the highest partition number.
/// </summary>
public class PropertyRepositoryMaxPartitionTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static PropertyRepository CreateRepository(ApplicationDbContext context)
        => new(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

    private static async Task SeedMastersAsync(
        ApplicationDbContext context,
        bool wardActive = true,
        bool categoryActive = true)
    {
        context.ZoneMaster.Add(new ZoneEntity { Id = 1, ZoneNo = "Z1", Description = "Zone 1", IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, Description = "Ward 1", IsActive = wardActive });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, Description = "Ward 2", IsActive = true });
        context.PropertyCategoryMaster.Add(new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential", IsActive = categoryActive });
        context.PropertyCategoryMaster.Add(new PropertyCategoryEntity { Id = 2, PropertyCategoryName = "Commercial", IsActive = true });
        await context.SaveChangesAsync();
    }

    private static PropertyEntity Property(
        int id,
        string partitionNo,
        int wardId = 1,
        string propertyNo = "P001",
        int categoryId = 1,
        bool isActive = true,
        bool markedForDeletion = false) => new()
        {
            Id = id,
            WardId = wardId,
            CategoryId = categoryId,
            PropertyNo = propertyNo,
            PartitionNo = partitionNo,
            IsActive = isActive,
            MarkedForDeletion = markedForDeletion
        };

    #region Happy Path

    [Fact]
    public async Task GetMaxPartition_WithSingleProperty_ReturnsThatPartitionWithMasterData()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.Add(Property(1, "1"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("W001", result.WardNo);
        Assert.Equal("P001", result.PropertyNo);
        Assert.Equal("Residential", result.Category);
        Assert.Equal("1", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_WithNumericPartitions_ReturnsNaturalOrderMaximum()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        // "9" must win over "10" only if compared lexicographically - natural order must pick "10".
        context.PropertyMast.AddRange(
            Property(1, "1"),
            Property(2, "9"),
            Property(3, "10"),
            Property(4, "2"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("10", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_WithAlphaNumericPartitions_ReturnsNaturalOrderMaximum()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, "A1"),
            Property(2, "A2"),
            Property(3, "A10"),
            Property(4, "A9"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("A10", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_WithMixedPrefixes_ReturnsHighestByPrefixThenNumber()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, "A10"),
            Property(2, "B1"),
            Property(3, "B2"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("B2", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_WithLeadingZeroPartitions_ComparesNumericValue()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, "007"),
            Property(2, "08"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("08", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_WithNullPartitionAmongOthers_IgnoresNullAndReturnsHighest()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, null!),
            Property(2, "3"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("3", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_WithOnlyNullPartition_ReturnsRowWithNullPartition()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.Add(Property(1, null!));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("P001", result.PropertyNo);
        Assert.Null(result.MaxPartitionNo);
    }

    #endregion

    #region Filtering

    [Fact]
    public async Task GetMaxPartition_WithNoMatchingProperty_ReturnsNull()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMaxPartition_IgnoresPropertiesFromOtherWards()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, "5", wardId: 1),
            Property(2, "99", wardId: 2));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("5", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_IgnoresOtherPropertyNumbers()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, "5", propertyNo: "P001"),
            Property(2, "99", propertyNo: "P002"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("5", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_IgnoresInactiveProperties()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, "5"),
            Property(2, "99", isActive: false));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("5", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_IgnoresPropertiesMarkedForDeletion()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, "5"),
            Property(2, "99", markedForDeletion: true));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("5", result.MaxPartitionNo);
    }

    [Fact]
    public async Task GetMaxPartition_WithInactiveWard_ReturnsNull()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context, wardActive: false);
        context.PropertyMast.Add(Property(1, "5"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMaxPartition_WithInactiveCategory_ReturnsNull()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context, categoryActive: false);
        context.PropertyMast.Add(Property(1, "5"));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMaxPartition_WithUnknownCategoryId_ReturnsNull()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.Add(Property(1, "5", categoryId: 999));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMaxPartition_ReturnsCategoryOfTheWinningRow()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.AddRange(
            Property(1, "2", categoryId: 1),
            Property(2, "10", categoryId: 2));
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).GetMaxPartition(1, "P001");

        Assert.NotNull(result);
        Assert.Equal("10", result.MaxPartitionNo);
        Assert.Equal("Commercial", result.Category);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task GetMaxPartition_WithCancelledToken_Throws()
    {
        using var context = CreateContext();
        await SeedMastersAsync(context);
        context.PropertyMast.Add(Property(1, "1"));
        await context.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateRepository(context).GetMaxPartition(1, "P001", cts.Token));
    }

    #endregion
}
