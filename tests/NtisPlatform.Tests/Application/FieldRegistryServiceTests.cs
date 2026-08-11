using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NtisPlatform.Application.DTOs.FieldRegistry;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;

namespace NtisPlatform.Tests.Application;

public class FieldRegistryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FieldRegistryService _service;

    public FieldRegistryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new FieldRegistryService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var masters = new List<BulkUpdateMasterEntity>
        {
            new()
            {
                Id = 1,
                UpdateCode = "UPD001",
                UpdateName = "Update Owner Name",
                ReferenceTableName = "PropertyMast",
                IsApprovalRequired = true,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
            },
            new()
            {
                Id = 2,
                UpdateCode = "UPD002",
                UpdateName = "Update Tax Rate",
                ReferenceTableName = "TaxMast",
                IsApprovalRequired = false,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
            },
            new()
            {
                Id = 3,
                UpdateCode = "UPD003",
                UpdateName = "Update Address",
                ReferenceTableName = "PropertyMast",
                IsApprovalRequired = false,
                IsActive = false,
                CreatedBy = 2,
                CreatedDate = DateTime.Now,
            },
        };
        _context.BulkUpdateMasters.AddRange(masters);

        var fieldConfigs = new List<BulkUpdateFieldConfigEntity>
        {
            new()
            {
                Id = 1,
                BulkUpdateMasterId = 1,
                FieldName = "OwnerName",
                DisplayName = "Owner Name",
                ControlType = "text",
                DataType = "string",
                IsRequired = true,
                SequenceNo = 1,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
            },
            new()
            {
                Id = 2,
                BulkUpdateMasterId = 1,
                FieldName = "OwnerMobile",
                DisplayName = "Owner Mobile",
                ControlType = "text",
                DataType = "string",
                IsRequired = false,
                SequenceNo = 2,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
            },
            new()
            {
                Id = 3,
                BulkUpdateMasterId = 2,
                FieldName = "TaxRate",
                DisplayName = "Tax Rate",
                ControlType = "number",
                DataType = "decimal",
                IsRequired = true,
                SequenceNo = 1,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now,
            },
        };
        _context.BulkUpdateFieldConfigs.AddRange(fieldConfigs);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsNonEmptyOrderedListOfSchemas()
    {
        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var schemaNames = result.Select(r => r.SchemaName).ToList();
        var sorted = schemaNames.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(sorted, schemaNames);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDistinctSchemaNames()
    {
        // Act
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        var schemaNames = result.Select(r => r.SchemaName).ToList();
        var distinctCount = schemaNames.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        Assert.Equal(distinctCount, schemaNames.Count);
    }

    [Fact]
    public async Task GetAllAsync_IncludesPtisSchema()
    {
        // Act - BulkUpdateMasterEntity (and many others) are mapped to the "PTIS" schema.
        var result = await _service.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Contains(result, r => string.Equals(r.SchemaName, "PTIS", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region GetDetailsBySchemaAsync Tests

    [Fact]
    public async Task GetDetailsBySchemaAsync_FiltersBySchemaName()
    {
        // Act
        var result = await _service.GetDetailsBySchemaAsync(
            new FieldRegistryDetailsQueryParameters { SchemaName = "PTIS", PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, i => Assert.Equal("PTIS", i.SchemaName, ignoreCase: true));
    }

    [Fact]
    public async Task GetDetailsBySchemaAsync_ReturnsEmpty_WhenSchemaDoesNotExist()
    {
        // Act
        var result = await _service.GetDetailsBySchemaAsync(
            new FieldRegistryDetailsQueryParameters { SchemaName = "NoSuchSchema", PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetDetailsBySchemaAsync_FiltersBySearchTerm()
    {
        // Arrange - find a real table name in the PTIS schema first, then search for a substring of it.
        var all = await _service.GetDetailsBySchemaAsync(
            new FieldRegistryDetailsQueryParameters { SchemaName = "PTIS", PageNumber = 1, PageSize = -1 },
            CancellationToken.None);
        var sampleTable = all.Items.First(i => i.TableName.Contains("BulkUpdate", StringComparison.OrdinalIgnoreCase));

        // Act
        var result = await _service.GetDetailsBySchemaAsync(
            new FieldRegistryDetailsQueryParameters { SchemaName = "PTIS", SearchTerm = "BulkUpdate", PageNumber = 1, PageSize = 100 },
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, i => Assert.Contains("BulkUpdate", i.TableName, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Items, i => i.TableName == sampleTable.TableName);
    }

    [Fact]
    public async Task GetDetailsBySchemaAsync_PageSizeMinusOne_ReturnsAllRows()
    {
        // Act
        var result = await _service.GetDetailsBySchemaAsync(
            new FieldRegistryDetailsQueryParameters { SchemaName = "PTIS", PageNumber = 1, PageSize = -1 },
            CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(result.TotalCount, result.PageSize);
        Assert.Equal(result.TotalCount, result.Items.Count());
        Assert.True(result.TotalCount > 0);
    }

    [Fact]
    public async Task GetDetailsBySchemaAsync_NormalPagination_ReturnsCorrectSlice()
    {
        // Arrange - page size 1, page number 2 should return the second table alphabetically.
        var all = await _service.GetDetailsBySchemaAsync(
            new FieldRegistryDetailsQueryParameters { SchemaName = "PTIS", PageNumber = 1, PageSize = -1 },
            CancellationToken.None);
        var expectedSecond = all.Items.OrderBy(i => i.TableName, StringComparer.OrdinalIgnoreCase).Skip(1).First();

        // Act
        var result = await _service.GetDetailsBySchemaAsync(
            new FieldRegistryDetailsQueryParameters { SchemaName = "PTIS", PageNumber = 2, PageSize = 1 },
            CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(expectedSecond.TableName, result.Items.First().TableName);
    }

    #endregion

    #region GetDetailsByTableAsync Tests

    [Fact]
    public async Task GetDetailsByTableAsync_ReturnsColumnsForTable()
    {
        // Act
        var result = await _service.GetDetailsByTableAsync(
            new FieldRegistryTableDetailsQueryParameters { SchemaName = "PTIS", TableName = "BulkUpdateMaster", PageNumber = 1, PageSize = -1 },
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, c => string.Equals(c.ColumnName, "UpdateCode", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetDetailsByTableAsync_ReturnsEmpty_WhenTableDoesNotExist()
    {
        // Act
        var result = await _service.GetDetailsByTableAsync(
            new FieldRegistryTableDetailsQueryParameters { SchemaName = "PTIS", TableName = "NoSuchTable", PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetDetailsByTableAsync_FiltersBySearchTerm()
    {
        // Act
        var result = await _service.GetDetailsByTableAsync(
            new FieldRegistryTableDetailsQueryParameters { SchemaName = "PTIS", TableName = "BulkUpdateMaster", SearchTerm = "UpdateCode", PageNumber = 1, PageSize = 10 },
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, c => Assert.Contains("UpdateCode", c.ColumnName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetDetailsByTableAsync_PageSizeMinusOne_ReturnsAllColumns()
    {
        // Act
        var result = await _service.GetDetailsByTableAsync(
            new FieldRegistryTableDetailsQueryParameters { SchemaName = "PTIS", TableName = "BulkUpdateMaster", PageNumber = 1, PageSize = -1 },
            CancellationToken.None);

        // Assert
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(result.TotalCount, result.PageSize);
        Assert.Equal(result.TotalCount, result.Items.Count());
    }

    #endregion

    #region AddFieldRegistryAsync Tests

    [Fact]
    public async Task AddFieldRegistryAsync_CreatesMasterAndFieldConfigsWithSequentialSequenceNo()
    {
        // Arrange
        var createDto = new CreateFieldRegistryDto
        {
            UpdateCode = "NEWCODE",
            UpdateName = "New Update",
            ReferenceTableName = "SomeTable",
            IsApprovalRequired = false,
            IsActive = true,
            CreatedBy = 10,
            FieldConfigs = new List<FieldRegistryFieldConfigDto>
            {
                new() { FieldName = "Field1", DisplayName = "Field 1", ControlType = "text", DataType = "string" },
                new() { FieldName = "Field2", DisplayName = "Field 2", ControlType = "number", DataType = "int" },
            },
        };

        // Act
        var result = await _service.AddFieldRegistryAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("NEWCODE", result.UpdateCode);
        Assert.Equal("New Update", result.UpdateName);
        Assert.Equal("SomeTable", result.ReferenceTableName);
        Assert.Equal(2, result.FieldConfigs.Count);
        Assert.Equal(1, result.FieldConfigs[0].SequenceNo);
        Assert.Equal("Field1", result.FieldConfigs[0].FieldName);
        Assert.Equal(2, result.FieldConfigs[1].SequenceNo);
        Assert.Equal("Field2", result.FieldConfigs[1].FieldName);

        var savedMaster = await _context.BulkUpdateMasters.FirstOrDefaultAsync(m => m.UpdateCode == "NEWCODE");
        Assert.NotNull(savedMaster);
        var savedConfigs = await _context.BulkUpdateFieldConfigs.Where(fc => fc.BulkUpdateMasterId == savedMaster.Id).ToListAsync();
        Assert.Equal(2, savedConfigs.Count);
    }

    // Note: BulkUpdateMasterEntity.UpdateCode has a unique index configured via Fluent API in
    // ApplicationDbContext, but the EF Core InMemory provider does not enforce unique-index / unique
    // constraints from model configuration, so a duplicate-UpdateCode insert does NOT throw here.
    // A test asserting a throw on duplicate UpdateCode would therefore be asserting a false
    // expectation against this provider and is intentionally omitted.

    #endregion

    #region GetFieldRegistriesAsync Tests

    [Fact]
    public async Task GetFieldRegistriesAsync_NoFilters_ReturnsFirstPageOrderedByUpdateNameThenId()
    {
        // Act
        var result = await _service.GetFieldRegistriesAsync(new FieldRegistryQueryParameters { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        // Assert - alphabetically: "Update Address" (UPD003), "Update Owner Name" (UPD001), "Update Tax Rate" (UPD002).
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new[] { "UPD003", "UPD001", "UPD002" }, result.Items.Select(i => i.UpdateCode));
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_PageSizeMinusOne_ReturnsAllRows()
    {
        // Act
        var result = await _service.GetFieldRegistriesAsync(new FieldRegistryQueryParameters { PageNumber = 1, PageSize = -1 }, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(3, result.PageSize);
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_PageSizeMinusOne_WithZeroMatchingRows_DoesNotDivideByZero()
    {
        // Arrange - filter that matches nothing.
        var queryParameters = new FieldRegistryQueryParameters
        {
            UpdateCode = "NO_SUCH_CODE",
            PageNumber = 1,
            PageSize = -1,
        };

        // Act
        var result = await _service.GetFieldRegistriesAsync(queryParameters, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize); // clamped to 1 per totalCount > 0 ? totalCount : 1
        Assert.Equal(0, result.TotalPages); // Ceiling(0 / 1.0) == 0 - no div-by-zero exception is thrown
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_FiltersByUpdateCode()
    {
        // Act
        var result = await _service.GetFieldRegistriesAsync(new FieldRegistryQueryParameters { UpdateCode = "UPD002" }, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("UPD002", result.Items.First().UpdateCode);
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_FiltersByUpdateName()
    {
        // Act
        var result = await _service.GetFieldRegistriesAsync(new FieldRegistryQueryParameters { UpdateName = "Update Tax Rate" }, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("UPD002", result.Items.First().UpdateCode);
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_FiltersByReferenceTableName()
    {
        // Act
        var result = await _service.GetFieldRegistriesAsync(new FieldRegistryQueryParameters { ReferenceTableName = "PropertyMast" }, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal("PropertyMast", i.ReferenceTableName));
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_FiltersByFieldName_MatchesOnlyMastersContainingThatField()
    {
        // Act - "OwnerMobile" only exists on master 1's field configs.
        var result = await _service.GetFieldRegistriesAsync(new FieldRegistryQueryParameters { FieldName = "OwnerMobile" }, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("UPD001", result.Items.First().UpdateCode);
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_CombinesFilters_ReturnsEmpty_WhenNoOverlap()
    {
        // Act - ReferenceTableName=PropertyMast matches UPD001/UPD003, UpdateCode=UPD002 does not overlap.
        var result = await _service.GetFieldRegistriesAsync(
            new FieldRegistryQueryParameters { ReferenceTableName = "PropertyMast", UpdateCode = "UPD002" },
            CancellationToken.None);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_SecondPagePagination_ReturnsCorrectSlice()
    {
        // Act
        var result = await _service.GetFieldRegistriesAsync(new FieldRegistryQueryParameters { PageNumber = 2, PageSize = 1 }, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Equal("UPD001", result.Items.First().UpdateCode); // second by UpdateName order (Address, Owner Name, Tax Rate)
    }

    [Fact]
    public async Task GetFieldRegistriesAsync_ReturnsFieldConfigsForEachMaster()
    {
        // Act
        var result = await _service.GetFieldRegistriesAsync(new FieldRegistryQueryParameters { UpdateCode = "UPD001" }, CancellationToken.None);

        // Assert
        var item = result.Items.First();
        Assert.Equal(2, item.FieldConfigs.Count);
        Assert.Equal(new[] { "OwnerName", "OwnerMobile" }, item.FieldConfigs.Select(fc => fc.FieldName));
    }

    #endregion

    #region SetActiveStatusAsync Tests

    [Fact]
    public async Task SetActiveStatusAsync_ReturnsFalse_WhenUpdateCodeNotFound()
    {
        // Act
        var result = await _service.SetActiveStatusAsync("NO_SUCH_CODE", false, 1, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetActiveStatusAsync_FlipsIsActiveOnMasterAndAllFieldConfigs()
    {
        // Act - master 1 (currently active) has 2 field configs (both currently active).
        var result = await _service.SetActiveStatusAsync("UPD001", false, 99, CancellationToken.None);

        // Assert
        Assert.True(result);
        var master = await _context.BulkUpdateMasters.Include(m => m.FieldConfigs).FirstAsync(m => m.UpdateCode == "UPD001");
        Assert.False(master.IsActive);
        Assert.All(master.FieldConfigs, fc => Assert.False(fc.IsActive));
    }

    [Fact]
    public async Task SetActiveStatusAsync_SetsUpdatedByAndUpdatedDate_WhenUpdatedByProvided()
    {
        // Act
        var result = await _service.SetActiveStatusAsync("UPD001", false, 42, CancellationToken.None);

        // Assert
        Assert.True(result);
        var master = await _context.BulkUpdateMasters.Include(m => m.FieldConfigs).FirstAsync(m => m.UpdateCode == "UPD001");
        Assert.Equal(42, master.UpdatedBy);
        Assert.NotNull(master.UpdatedDate);
        Assert.All(master.FieldConfigs, fc =>
        {
            Assert.Equal(42, fc.UpdatedBy);
            Assert.NotNull(fc.UpdatedDate);
        });
    }

    [Fact]
    public async Task SetActiveStatusAsync_LeavesUpdatedByUnchanged_WhenUpdatedByIsNull()
    {
        // Arrange - pre-set an UpdatedBy value that should survive a null updatedBy call.
        var master = await _context.BulkUpdateMasters.FirstAsync(m => m.UpdateCode == "UPD001");
        master.UpdatedBy = 7;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SetActiveStatusAsync("UPD001", false, null, CancellationToken.None);

        // Assert
        Assert.True(result);
        var refetched = await _context.BulkUpdateMasters.FirstAsync(m => m.UpdateCode == "UPD001");
        Assert.Equal(7, refetched.UpdatedBy); // unchanged
        Assert.NotNull(refetched.UpdatedDate); // UpdatedDate is always set regardless
        Assert.False(refetched.IsActive);
    }

    #endregion

    #region UpdateFieldRegistryAsync Tests

    [Fact]
    public async Task UpdateFieldRegistryAsync_ReturnsNull_WhenUpdateCodeNotFound()
    {
        // Act
        var result = await _service.UpdateFieldRegistryAsync(
            "NO_SUCH_CODE",
            new UpdateFieldRegistryDto { UpdateName = "X", ReferenceTableName = "Y", FieldConfigs = new List<UpdateFieldRegistryFieldConfigDto> { new() { FieldName = "F", DisplayName = "F", ControlType = "text", DataType = "string" } } },
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateFieldRegistryAsync_UpdatesMasterFields()
    {
        // Arrange
        var updateDto = new UpdateFieldRegistryDto
        {
            UpdateName = "Renamed Update",
            ReferenceTableName = "NewTable",
            IsApprovalRequired = true,
            IsActive = false,
            UpdatedBy = 5,
            FieldConfigs = new List<UpdateFieldRegistryFieldConfigDto>
            {
                new() { Id = 1, FieldName = "OwnerName", DisplayName = "Owner Name", ControlType = "text", DataType = "string" },
                new() { Id = 2, FieldName = "OwnerMobile", DisplayName = "Owner Mobile", ControlType = "text", DataType = "string" },
            },
        };

        // Act
        var result = await _service.UpdateFieldRegistryAsync("UPD001", updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Renamed Update", result.UpdateName);
        Assert.Equal("NewTable", result.ReferenceTableName);
        Assert.True(result.IsApprovalRequired);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task UpdateFieldRegistryAsync_ReconcilesFieldConfigs_UpdatesRemovesAndAdds()
    {
        // Arrange - master UPD001 currently has field configs Id=1 (OwnerName) and Id=2 (OwnerMobile).
        // Incoming request: update Id=1, omit Id=2 (should be removed), add a brand-new config (no Id).
        var updateDto = new UpdateFieldRegistryDto
        {
            UpdateName = "Update Owner Name",
            ReferenceTableName = "PropertyMast",
            IsActive = true,
            UpdatedBy = 3,
            FieldConfigs = new List<UpdateFieldRegistryFieldConfigDto>
            {
                new() { Id = 1, FieldName = "OwnerNameUpdated", DisplayName = "Owner Name Updated", ControlType = "text", DataType = "string" },
                new() { FieldName = "OwnerEmail", DisplayName = "Owner Email", ControlType = "text", DataType = "string" },
            },
        };

        // Act
        var result = await _service.UpdateFieldRegistryAsync("UPD001", updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.FieldConfigs.Count);

        var updatedConfig = result.FieldConfigs.Single(fc => fc.Id == 1);
        Assert.Equal("OwnerNameUpdated", updatedConfig.FieldName);
        Assert.Equal(1, updatedConfig.SequenceNo);

        var newConfig = result.FieldConfigs.Single(fc => fc.FieldName == "OwnerEmail");
        Assert.Equal(2, newConfig.SequenceNo);
        Assert.True(newConfig.Id > 0); // persisted, got a real Id

        // Removed config (Id=2, OwnerMobile) should no longer exist.
        Assert.DoesNotContain(result.FieldConfigs, fc => fc.FieldName == "OwnerMobile");
        var removedFromDb = await _context.BulkUpdateFieldConfigs.FirstOrDefaultAsync(fc => fc.Id == 2);
        Assert.Null(removedFromDb);

        // Confirm DB state matches (2 configs total for this master).
        var dbConfigs = await _context.BulkUpdateFieldConfigs.Where(fc => fc.BulkUpdateMasterId == 1).ToListAsync();
        Assert.Equal(2, dbConfigs.Count);
    }

    #endregion

    #region PurgeFieldRegistryAsync Tests

    [Fact]
    public async Task PurgeFieldRegistryAsync_ThrowsArgumentException_WhenBothUpdateCodeAndFieldConfigIdAreNullOrWhitespace()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.PurgeFieldRegistryAsync(null, "   ", CancellationToken.None));
    }

    [Fact]
    public async Task PurgeFieldRegistryAsync_ThrowsArgumentException_WhenFieldConfigIdHasNoValidIntegers()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.PurgeFieldRegistryAsync(null, "abc,xyz", CancellationToken.None));
    }

    // Note: every non-empty branch of PurgeFieldRegistryAsync's delete phase (both the by-field-config-id
    // case and the by-update-code case) issues ExecuteDeleteAsync, which the EF Core InMemory provider
    // used by these tests does not support - it throws InvalidOperationException ("The methods
    // 'ExecuteDelete' and 'ExecuteDeleteAsync' are not supported by the current database provider").
    // This mirrors the precedent set in LockUnlockServiceTests for BulkApplyAsync/ExecuteUpdateAsync:
    // those code paths aren't exercised here. The validation tests above and the not-found/no-op test
    // below don't reach ExecuteDeleteAsync (validation throws first, or the update-code lookup finds
    // zero master ids so the delete calls are skipped entirely), so they remain covered.

    [Fact]
    public async Task PurgeFieldRegistryAsync_ByUpdateCode_NotFound_IsIdempotentNoOp()
    {
        // Act
        var result = await _service.PurgeFieldRegistryAsync("NO_SUCH_CODE", null, CancellationToken.None);

        // Assert - no throw, all-zero counts.
        Assert.Equal(0, result.DeletedFieldConfigCount);
        Assert.Equal(0, result.DeletedHistoryCount);
        Assert.Equal(0, result.DeletedMasterCount);

        // Nothing was touched.
        Assert.Equal(3, await _context.BulkUpdateMasters.CountAsync());
        Assert.Equal(3, await _context.BulkUpdateFieldConfigs.CountAsync());
    }

    #endregion
}
