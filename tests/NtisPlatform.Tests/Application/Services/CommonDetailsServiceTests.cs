using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Service-level tests for the Application-layer <see cref="CommonDetailsService.BulkUpdateAsync"/>.
/// The service is wired exactly as DI wires it — real repositories, <see cref="UnitOfWork"/> and
/// <see cref="DynamicEntityLoader"/> over a single in-memory <see cref="ApplicationDbContext"/>.
/// These cover the behaviors that had to be preserved through the layering refactor: dynamic table
/// resolution via the registry, multi-row updates, value coercion, rollback result semantics, the
/// non-existent-property quirk, and the unmapped-field guard.
///
/// The in-memory provider has no transactions, so the warning raised by <c>BeginTransactionAsync</c>
/// is ignored (it becomes a no-op) — matching the pattern used elsewhere in this project.
/// </summary>
public class CommonDetailsServiceTests
{
    private static ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static CommonDetailsService CreateService(ApplicationDbContext ctx) =>
        new(
            new Repository<BulkUpdateMasterEntity>(ctx),
            new Repository<BulkUpdateFieldConfigEntity>(ctx),
            new Repository<BulkUpdateHistoryEntity>(ctx),
            new Repository<PropertyEntity>(ctx),
            new Repository<WardEntity>(ctx),
            new Repository<SocietyDetailsEntity>(ctx),
            new UnitOfWork(ctx),
            new DynamicEntityLoader(ctx),
            Mock.Of<ILogger<CommonDetailsService>>());

    /// <summary>
    /// Seeds the BulkUpdateMaster + (active) BulkUpdateFieldConfig rows the service reads to learn
    /// which table and fields a given update code targets.
    /// </summary>
    private static async Task SeedConfigAsync(
        ApplicationDbContext ctx, string updateCode, string referenceTable, params string[] fieldNames)
    {
        var master = new BulkUpdateMasterEntity
        {
            Id = 1,
            UpdateCode = updateCode,
            UpdateName = updateCode,
            ReferenceTableName = referenceTable,
            IsActive = true,
            DisplaySequence = 1,
        };
        ctx.BulkUpdateMasters.Add(master);

        var seq = 1;
        foreach (var field in fieldNames)
        {
            ctx.BulkUpdateFieldConfigs.Add(new BulkUpdateFieldConfigEntity
            {
                BulkUpdateMasterId = master.Id,
                Master = master,
                FieldName = field,
                DisplayName = field,
                IsActive = true,
                IsRequired = false,
                SequenceNo = seq++,
            });
        }

        await ctx.SaveChangesAsync();
    }

    private static BulkUpdateRequestDto Request(string updateCode, IEnumerable<long> ids, params (string Key, object? Value)[] data)
        => new()
        {
            UpdateCode = updateCode,
            PropertyIds = ids.ToList(),
            UpdateData = data.ToDictionary(d => d.Key, d => d.Value),
        };

    [Fact]
    public async Task BulkUpdateAsync_UpdatesPropertyMast_AndWritesHistory()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast", "Address");
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_BASIC", new long[] { 100 }, ("Address", "New Address")),
            updatedBy: 42, ipAddress: "1.2.3.4", CancellationToken.None);

        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);

        var updated = await ctx.PropertyMast.FindAsync(100);
        Assert.Equal("New Address", updated!.Address);
        Assert.Equal(42, updated.UpdatedBy);
        Assert.NotNull(updated.UpdatedDate);

        var hist = await ctx.BulkUpdateHistory.SingleAsync();
        Assert.Equal(100, hist.PropertyId);
        Assert.Equal(42, hist.UpdatedBy);
        Assert.Equal("Address", hist.UpdatedColumns);
        Assert.Equal("1.2.3.4", hist.IpAddress);
        Assert.Contains("New Address", hist.NewValue);
        Assert.Contains("Address", hist.OldValue); // old value snapshot keyed by field name
    }

    [Fact]
    public async Task BulkUpdateAsync_CoercesStringInputToPropertyClrType()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PD", "PTIS.PropertyDetails", "CarpetAreaSqMeter");
        ctx.PropertyDetails.Add(new PropertyDetailsEntity { Id = 1, PropertyId = 100, IsActive = true });
        await ctx.SaveChangesAsync();

        // EF needs an exact CLR type, so CoerceToPropertyType must turn the string into a double for
        // the double? property (the conversion SQL Server used to do implicitly).
        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PD", new long[] { 100 }, ("CarpetAreaSqMeter", "12.5")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var row = await ctx.PropertyDetails.FindAsync(1);
        Assert.Equal(12.5, row!.CarpetAreaSqMeter);
    }

    [Fact]
    public async Task BulkUpdateAsync_UpdatesEveryRowForPropertyIdKeyedTable()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PD", "PTIS.PropertyDetails", "ConstructionYear");
        ctx.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = 1, PropertyId = 100, IsActive = true },
            new PropertyDetailsEntity { Id = 2, PropertyId = 100, IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PD", new long[] { 100 }, ("ConstructionYear", "2020")),
            updatedBy: 7, ipAddress: null, CancellationToken.None);

        // One property processed → one success and one history row, but BOTH physical rows updated
        // (a PropertyId-keyed table can have many rows per property).
        Assert.Equal(1, result.SuccessCount);
        var rows = await ctx.PropertyDetails.Where(d => d.PropertyId == 100).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal("2020", r.ConstructionYear));
        Assert.Single(await ctx.BulkUpdateHistory.ToListAsync());
    }

    [Fact]
    public async Task BulkUpdateAsync_NonExistentProperty_CountsSuccess_WithNullOldValue()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast", "Address");
        // No PropertyEntity seeded → the row to update does not exist.
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_BASIC", new long[] { 999 }, ("Address", "X")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        // Matches the original behavior: missing row is still a "success" and still logs history.
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        var hist = await ctx.BulkUpdateHistory.SingleAsync();
        Assert.Equal(999, hist.PropertyId);
        Assert.Null(hist.OldValue);
    }

    [Fact]
    public async Task BulkUpdateAsync_OnFailure_RollsBackAndResetsSuccessCount()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PD", "PTIS.PropertyDetails", "CarpetAreaSqMeter");
        ctx.PropertyDetails.Add(new PropertyDetailsEntity { Id = 1, PropertyId = 100, IsActive = true });
        await ctx.SaveChangesAsync();

        // "not-a-number" cannot be coerced to double → the property fails.
        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PD", new long[] { 100 }, ("CarpetAreaSqMeter", "not-a-number")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(1, result.FailedCount);
        Assert.Equal(0, result.SuccessCount);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("rolled back", result.Errors[0]);
    }

    [Fact]
    public async Task BulkUpdateAsync_Throws_WhenFieldNotMappedToEntity()
    {
        using var ctx = CreateContext();
        // "NotARealColumn" is whitelisted (it is a configured field) but is not a property on
        // PropertyEntity → the mapping guard rejects it.
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast", "NotARealColumn");
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_BASIC", new long[] { 100 }, ("NotARealColumn", "x")),
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("NotARealColumn", ex.Message);
    }

    [Fact]
    public async Task BulkUpdateAsync_Throws_WhenUpdateCodeUnknown()
    {
        using var ctx = CreateContext();

        // No master seeded → the update code cannot be resolved.
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("DOES_NOT_EXIST", new long[] { 1 }, ("Address", "x")),
                updatedBy: 1, ipAddress: null, CancellationToken.None));
    }
}
