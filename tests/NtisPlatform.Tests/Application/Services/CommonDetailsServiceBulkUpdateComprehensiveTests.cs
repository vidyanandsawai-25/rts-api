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
/// Comprehensive test suite for CommonDetailsService BulkUpdate functionality
/// covering validation scenarios, edge cases, and error handling.
/// </summary>
public class CommonDetailsServiceBulkUpdateComprehensiveTests
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

    private static async Task SeedConfigAsync(
        ApplicationDbContext ctx, string updateCode, string referenceTable, params (string FieldName, bool IsRequired, int? MaxLength, string? ValidationRegex)[] fieldConfigs)
    {
        var master = new BulkUpdateMasterEntity
        {
            Id = 1,
            UpdateCode = updateCode,
            UpdateName = updateCode,
            ReferenceTableName = referenceTable,
            IsActive = true,
            DisplaySequence = 1,
            ApiRoute = "/x",
        };
        ctx.BulkUpdateMasters.Add(master);

        var seq = 1;
        foreach (var (fieldName, isRequired, maxLength, validationRegex) in fieldConfigs)
        {
            ctx.BulkUpdateFieldConfigs.Add(new BulkUpdateFieldConfigEntity
            {
                BulkUpdateMasterId = master.Id,
                Master = master,
                FieldName = fieldName,
                DisplayName = fieldName,
                IsActive = true,
                IsRequired = isRequired,
                MaxLength = maxLength,
                ValidationRegex = validationRegex,
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

    #region Required Field Validation Tests

    [Fact]
    public async Task BulkUpdateAsync_WithMissingRequiredField_ThrowsArgumentException()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_REQ", "PTIS.PropertyMast",
            ("Address", IsRequired: true, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_REQ", new long[] { 100 }, ("Address", "")),
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithNullRequiredField_ThrowsArgumentException()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_REQ", "PTIS.PropertyMast",
            ("Address", IsRequired: true, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_REQ", new long[] { 100 }, ("Address", null)),
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithWhitespaceRequiredField_ThrowsArgumentException()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_REQ", "PTIS.PropertyMast",
            ("Address", IsRequired: true, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_REQ", new long[] { 100 }, ("Address", "   ")),
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region MaxLength Validation Tests

    [Fact]
    public async Task BulkUpdateAsync_WithExceedingMaxLength_ThrowsArgumentException()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_MAX", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: 10, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_MAX", new long[] { 100 }, ("Address", "This is a very long address exceeding max length")),
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("max length", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithExactMaxLength_Succeeds()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_MAX", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: 10, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_MAX", new long[] { 100 }, ("Address", "1234567890")), // Exactly 10 chars
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var updated = await ctx.PropertyMast.FindAsync(100);
        Assert.Equal("1234567890", updated!.Address);
    }

    #endregion

    #region Regex Validation Tests

    [Fact]
    public async Task BulkUpdateAsync_WithInvalidRegexPattern_ThrowsArgumentException()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_REGEX", "PTIS.PropertyMast",
            ("PropertyNo", IsRequired: false, MaxLength: null, ValidationRegex: @"^\d{3}$")); // Must be 3 digits
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_REGEX", new long[] { 100 }, ("PropertyNo", "ABC")), // Invalid: not digits
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("invalid format", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithValidRegexPattern_Succeeds()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_REGEX", "PTIS.PropertyMast",
            ("PropertyNo", IsRequired: false, MaxLength: null, ValidationRegex: @"^\d{3}$")); // Must be 3 digits
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_REGEX", new long[] { 100 }, ("PropertyNo", "123")), // Valid: 3 digits
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var updated = await ctx.PropertyMast.FindAsync(100);
        Assert.Equal("123", updated!.PropertyNo);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithPhoneRegex_ValidatesCorrectly()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_PHONE", "PTIS.PropertyMast",
            ("PropertyNo", IsRequired: false, MaxLength: null, ValidationRegex: @"^\d{10}$"));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_PHONE", new long[] { 100 }, ("PropertyNo", "1234567890")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var updated = await ctx.PropertyMast.FindAsync(100);
        Assert.Equal("1234567890", updated!.PropertyNo);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithInvalidPhoneNumber_ThrowsArgumentException()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_PHONE", "PTIS.PropertyMast",
            ("PropertyNo", IsRequired: false, MaxLength: null, ValidationRegex: @"^\d{10}$"));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_PHONE", new long[] { 100 }, ("PropertyNo", "123")),
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("invalid format", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Multiple Validation Errors Tests

    [Fact]
    public async Task BulkUpdateAsync_WithMultipleValidationErrors_ReturnsAllErrors()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_MULTI", "PTIS.PropertyMast",
            ("Address", IsRequired: true, MaxLength: 10, ValidationRegex: null),
            ("PropertyNo", IsRequired: true, MaxLength: null, ValidationRegex: @"^\d{3}$"));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_MULTI", new long[] { 100 },
                    ("Address", ""), // Required field empty
                    ("PropertyNo", "ABC")), // Invalid regex
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("required", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("invalid format", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region No Valid Fields Tests

    [Fact]
    public async Task BulkUpdateAsync_WithNoValidFields_ThrowsArgumentException()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_BASIC", new long[] { 100 }, ("NonExistentField", "value")),
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("No valid fields", ex.Message);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithEmptyUpdateData_ThrowsArgumentException()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("PROP_BASIC", new long[] { 100 }), // No data
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("No valid fields", ex.Message);
    }

    #endregion

    #region Multiple Properties Update Tests

    [Fact]
    public async Task BulkUpdateAsync_WithMultipleProperties_UpdatesAll()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.AddRange(
            new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true },
            new PropertyEntity { Id = 101, PropertyNo = "002", IsActive = true },
            new PropertyEntity { Id = 102, PropertyNo = "003", IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_BASIC", new long[] { 100, 101, 102 }, ("Address", "Updated Address")),
            updatedBy: 42, ipAddress: "1.2.3.4", CancellationToken.None);

        Assert.Equal(3, result.TotalRequested);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);

        var updated1 = await ctx.PropertyMast.FindAsync(100);
        var updated2 = await ctx.PropertyMast.FindAsync(101);
        var updated3 = await ctx.PropertyMast.FindAsync(102);
        Assert.Equal("Updated Address", updated1!.Address);
        Assert.Equal("Updated Address", updated2!.Address);
        Assert.Equal("Updated Address", updated3!.Address);

        var histories = await ctx.BulkUpdateHistory.ToListAsync();
        Assert.Equal(3, histories.Count);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithOneFailureInBatch_RollsBackAll()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PD", "PTIS.PropertyDetails",
            ("CarpetAreaSqMeter", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyDetails.AddRange(
            new PropertyDetailsEntity { Id = 1, PropertyId = 100, IsActive = true },
            new PropertyDetailsEntity { Id = 2, PropertyId = 101, IsActive = true });
        await ctx.SaveChangesAsync();

        // Both properties will fail (invalid number format)
        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PD", new long[] { 100, 101 }, ("CarpetAreaSqMeter", "invalid")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(2, result.FailedCount);
        Assert.Equal(0, result.SuccessCount); // Reset to 0 after rollback
        Assert.NotEmpty(result.Errors);
        Assert.Contains("rolled back", result.Errors[0]);

        // Verify no changes were saved
        var prop1 = await ctx.PropertyDetails.FindAsync(1);
        var prop2 = await ctx.PropertyDetails.FindAsync(2);
        Assert.Null(prop1!.CarpetAreaSqMeter); // Should remain null
        Assert.Null(prop2!.CarpetAreaSqMeter); // Should remain null
    }

    #endregion

    #region History Tracking Tests

    [Fact]
    public async Task BulkUpdateAsync_RecordsHistoryWithIPAddress()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", Address = "Old Address", IsActive = true });
        await ctx.SaveChangesAsync();

        await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_BASIC", new long[] { 100 }, ("Address", "New Address")),
            updatedBy: 42, ipAddress: "192.168.1.1", CancellationToken.None);

        var history = await ctx.BulkUpdateHistory.SingleAsync();
        Assert.Equal(100, history.PropertyId);
        Assert.Equal(42, history.UpdatedBy);
        Assert.Equal("192.168.1.1", history.IpAddress);
        Assert.Equal("Address", history.UpdatedColumns);
        Assert.Contains("Old Address", history.OldValue);
        Assert.Contains("New Address", history.NewValue);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithMultipleFields_RecordsAllInHistory()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_MULTI", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: null, ValidationRegex: null),
            ("PropertyNo", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity
        {
            Id = 100,
            PropertyNo = "001",
            Address = "Old Address",
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_MULTI", new long[] { 100 },
                ("Address", "New Address"),
                ("PropertyNo", "002")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        var history = await ctx.BulkUpdateHistory.SingleAsync();
        Assert.Contains("Address", history.UpdatedColumns);
        Assert.Contains("PropertyNo", history.UpdatedColumns);
        Assert.Contains("New Address", history.NewValue);
        Assert.Contains("002", history.NewValue);
    }

    #endregion

    #region Case Insensitive Field Names Tests

    [Fact]
    public async Task BulkUpdateAsync_WithMixedCaseFieldNames_WorksCorrectly()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        // Use lowercase "address" instead of "Address"
        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_BASIC", new long[] { 100 }, ("address", "New Address")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var updated = await ctx.PropertyMast.FindAsync(100);
        Assert.Equal("New Address", updated!.Address);
    }

    #endregion

    #region UpdatedBy and UpdatedDate Tests

    [Fact]
    public async Task BulkUpdateAsync_SetsUpdatedByAndUpdatedDate()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PROP_BASIC", "PTIS.PropertyMast",
            ("Address", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyMast.Add(new PropertyEntity { Id = 100, PropertyNo = "001", IsActive = true });
        await ctx.SaveChangesAsync();

        var beforeUpdate = DateTime.Now;

        await CreateService(ctx).BulkUpdateAsync(
            Request("PROP_BASIC", new long[] { 100 }, ("Address", "New Address")),
            updatedBy: 99, ipAddress: null, CancellationToken.None);

        var updated = await ctx.PropertyMast.FindAsync(100);
        Assert.Equal(99, updated!.UpdatedBy);
        Assert.NotNull(updated.UpdatedDate);
        Assert.True(updated.UpdatedDate >= beforeUpdate);
    }

    #endregion

    #region Numeric Type Coercion Tests

    [Fact]
    public async Task BulkUpdateAsync_CoercesIntegerString_ToIntegerProperty()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PD", "PTIS.PropertyDetails",
            ("ConstructionYear", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyDetails.Add(new PropertyDetailsEntity { Id = 1, PropertyId = 100, IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PD", new long[] { 100 }, ("ConstructionYear", "2020")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        var row = await ctx.PropertyDetails.FindAsync(1);
        Assert.Equal("2020", row!.ConstructionYear);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithInvalidNumericString_Fails()
    {
        using var ctx = CreateContext();
        await SeedConfigAsync(ctx, "PD", "PTIS.PropertyDetails",
            ("CarpetAreaSqMeter", IsRequired: false, MaxLength: null, ValidationRegex: null));
        ctx.PropertyDetails.Add(new PropertyDetailsEntity { Id = 1, PropertyId = 100, IsActive = true });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).BulkUpdateAsync(
            Request("PD", new long[] { 100 }, ("CarpetAreaSqMeter", "not-a-number")),
            updatedBy: 1, ipAddress: null, CancellationToken.None);

        Assert.Equal(1, result.FailedCount);
        Assert.Equal(0, result.SuccessCount);
    }

    #endregion

    #region Unrecognized Reference Table Tests

    [Fact]
    public async Task BulkUpdateAsync_WithUnrecognizedReferenceTable_Throws()
    {
        using var ctx = CreateContext();
        var master = new BulkUpdateMasterEntity
        {
            Id = 1,
            UpdateCode = "UNKNOWN_TABLE",
            UpdateName = "Unknown Table",
            ReferenceTableName = "UnknownTable", // Not in registry
            IsActive = true,
            DisplaySequence = 1,
            ApiRoute = "/x",
        };
        ctx.BulkUpdateMasters.Add(master);
        ctx.BulkUpdateFieldConfigs.Add(new BulkUpdateFieldConfigEntity
        {
            BulkUpdateMasterId = 1,
            Master = master,
            FieldName = "SomeField",
            DisplayName = "Some Field",
            IsActive = true,
            SequenceNo = 1
        });
        await ctx.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(ctx).BulkUpdateAsync(
                Request("UNKNOWN_TABLE", new long[] { 100 }, ("SomeField", "value")),
                updatedBy: 1, ipAddress: null, CancellationToken.None));

        Assert.Contains("unrecognized table", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetMenuAsync Tests

    [Fact]
    public async Task GetMenuAsync_ReturnsOnlyActiveItems_OrderedByDisplaySequence()
    {
        using var ctx = CreateContext();
        ctx.BulkUpdateMasters.AddRange(
            new BulkUpdateMasterEntity
            {
                Id = 1,
                UpdateCode = "SECOND",
                UpdateName = "Second",
                ReferenceTableName = "Table2",
                DisplaySequence = 2,
                ApiRoute = "/api/2",
                IsActive = true
            },
            new BulkUpdateMasterEntity
            {
                Id = 2,
                UpdateCode = "FIRST",
                UpdateName = "First",
                ReferenceTableName = "Table1",
                DisplaySequence = 1,
                ApiRoute = "/api/1",
                IsActive = true
            },
            new BulkUpdateMasterEntity
            {
                Id = 3,
                UpdateCode = "INACTIVE",
                UpdateName = "Inactive",
                ReferenceTableName = "Table3",
                DisplaySequence = 0,
                ApiRoute = "/api/3",
                IsActive = false // Should not be returned
            });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).GetMenuAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("FIRST", result[0].UpdateCode);
        Assert.Equal("SECOND", result[1].UpdateCode);
    }

    #endregion

    #region GetFormFieldsAsync Tests

    [Fact]
    public async Task GetFormFieldsAsync_ReturnsOnlyActiveFields_OrderedBySequence()
    {
        using var ctx = CreateContext();
        var master = new BulkUpdateMasterEntity
        {
            Id = 1,
            UpdateCode = "TEST",
            UpdateName = "Test",
            ReferenceTableName = "TestTable",
            IsActive = true,
            DisplaySequence = 1,
            ApiRoute = "/x"
        };
        ctx.BulkUpdateMasters.Add(master);
        ctx.BulkUpdateFieldConfigs.AddRange(
            new BulkUpdateFieldConfigEntity
            {
                BulkUpdateMasterId = 1,
                Master = master,
                FieldName = "Field2",
                DisplayName = "Field 2",
                IsActive = true,
                SequenceNo = 2
            },
            new BulkUpdateFieldConfigEntity
            {
                BulkUpdateMasterId = 1,
                Master = master,
                FieldName = "Field1",
                DisplayName = "Field 1",
                IsActive = true,
                SequenceNo = 1
            },
            new BulkUpdateFieldConfigEntity
            {
                BulkUpdateMasterId = 1,
                Master = master,
                FieldName = "InactiveField",
                DisplayName = "Inactive",
                IsActive = false,
                SequenceNo = 3
            });
        await ctx.SaveChangesAsync();

        var result = await CreateService(ctx).GetFormFieldsAsync("TEST", CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("Field1", result[0].FieldName);
        Assert.Equal("Field2", result[1].FieldName);
    }

    #endregion
}
