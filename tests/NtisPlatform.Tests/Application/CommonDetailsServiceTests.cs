using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Services;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Tests for <see cref="CommonDetailsService"/>. All dependencies except <see cref="IPropertySearchService"/>
/// are real implementations backed by one shared EF Core InMemory <see cref="ApplicationDbContext"/> - this
/// service's query logic (LINQ joins, dynamic reflection over entities via <see cref="IDynamicEntityLoader"/>,
/// transactions via <see cref="IUnitOfWork"/>) needs a real, queryable DbContext to exercise correctly;
/// mocking IRepository&lt;T&gt;.GetQueryable() with a plain List&lt;T&gt;.AsQueryable() would not support the
/// async EF operators (ToListAsync/CountAsync/etc.) the service relies on.
///
/// Seed data map (see SeedTestData):
/// Wards:      1="001", 2="002"
/// Properties: 1..5 in ward 1 (P001..P005 / A..E), 6 in ward 2 (X001/Z)
/// Assessment: rows for property 1 (BHK=2BHK, has remark) and 2 (BHK=3BHK, no remark) - PropertyMastDetails
/// Society:    one row for property 2, WingName="WingA"
/// Users:      100=alice.user, 101=bob.user
/// Masters:    1=OWNER_UPDATE (PropertyMast, active, seq 2, CreatedBy=100), 2=ASSESSMENT_UPDATE
///             (PropertyMastDetails, active, seq 1, UpdatedBy=101), 3=INACTIVE_UPDATE (inactive)
/// FieldConfigs: master 1 -> OwnerName(required), MobileNo, RetiredField(inactive);
///               master 2 -> BHK, AssessmentRemark; master 3 -> OwnerName(required)
/// History:    5 rows (H1..H5) with distinct Updated/CreatedDate combinations - see inline comments.
///             H1/H2/H4/H5 set their own UpdatedBy (100=alice, 101=bob) - GetUpdateHistoryAsync's
///             DoneBy filter joins via the history row's own CreatedBy ?? UpdatedBy, not the master's.
/// </summary>
public class CommonDetailsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPropertySearchService> _mockPropertySearchService;
    private readonly Mock<ILogger<CommonDetailsService>> _mockLogger;
    private readonly CommonDetailsService _service;

    public CommonDetailsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new ApplicationDbContext(options);

        var masterRepo = new Repository<BulkUpdateMasterEntity>(_context);
        var fieldConfigRepo = new Repository<BulkUpdateFieldConfigEntity>(_context);
        var historyRepo = new Repository<BulkUpdateHistoryEntity>(_context);
        var activityRepo = new Repository<BulkUpdateActivityEntity, int>(_context);
        var propertyRepo = new Repository<PropertyEntity>(_context);
        var wardRepo = new Repository<WardEntity>(_context);
        var societyRepo = new Repository<SocietyDetailsEntity>(_context);
        var userRepo = new Repository<UserEntity>(_context);
        var sourceTableRepo = new Repository<SourceTableEntity>(_context);
        var sourceTableDetailsRepo = new Repository<SourceTableDetailsEntity>(_context);
        var moduleRepo = new Repository<ModuleMasterEntity>(_context);
        var unitOfWork = new UnitOfWork(_context);
        var entityLoader = new DynamicEntityLoader(_context);
        _mockPropertySearchService = new Mock<IPropertySearchService>();
        _mockLogger = new Mock<ILogger<CommonDetailsService>>();

        _service = new CommonDetailsService(
            masterRepo, fieldConfigRepo, historyRepo, activityRepo, propertyRepo, wardRepo, societyRepo, userRepo,
            sourceTableRepo, sourceTableDetailsRepo, moduleRepo,
            unitOfWork, entityLoader, _mockPropertySearchService.Object, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        _context.WardMaster.AddRange(
            new WardEntity { Id = 1, WardNo = "001", ZoneId = 1, IsActive = true },
            new WardEntity { Id = 2, WardNo = "002", ZoneId = 1, IsActive = true });

        _context.PropertyMast.AddRange(
            new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "P001", PartitionNo = "A", OwnerName = "Alice", MobileNo = "9000000001", IsActive = true },
            new PropertyEntity { Id = 2, WardId = 1, PropertyNo = "P002", PartitionNo = "B", OwnerName = "Bob", MobileNo = "9000000002", IsActive = true },
            new PropertyEntity { Id = 3, WardId = 1, PropertyNo = "P003", PartitionNo = "C", OwnerName = "Charlie", MobileNo = "9000000003", IsActive = true },
            new PropertyEntity { Id = 4, WardId = 1, PropertyNo = "P004", PartitionNo = "D", OwnerName = "Dave", MobileNo = "9000000004", IsActive = true },
            new PropertyEntity { Id = 5, WardId = 1, PropertyNo = "P005", PartitionNo = "E", OwnerName = "Eve", MobileNo = "9000000005", IsActive = true },
            new PropertyEntity { Id = 6, WardId = 2, PropertyNo = "X001", PartitionNo = "Z", OwnerName = "Zed", MobileNo = "9000000006", IsActive = true },
            // Main property (blank PartitionNo) sharing WardId+PropertyNo with property 6's partition "Z" -
            // exercises IdentityKey disambiguation between a Main row and a Partition row of the same property.
            // Placed in ward 2 (not ward 1) so it doesn't shift the property-count assertions several
            // FilterPropertiesAsync tests make against ward 1's fixed set of 5 properties.
            new PropertyEntity { Id = 7, WardId = 2, PropertyNo = "X001", PartitionNo = null, OwnerName = "MainOwner", MobileNo = "9000000007", IsActive = true });

        _context.PropertyMastDetails.AddRange(
            new PropertyAssessmentEntity { Id = 1, PropertyId = 1, BHK = "2BHK", AssessmentRemark = "Good" },
            new PropertyAssessmentEntity { Id = 2, PropertyId = 2, BHK = "3BHK" });

        _context.SocietyDetailsMast.Add(
            new SocietyDetailsEntity { Id = 1, PropertyId = 2, WingName = "WingA" });

        _context.UserMasters.AddRange(
            new UserEntity { Id = 100, UserName = "alice.user" },
            new UserEntity { Id = 101, UserName = "bob.user" });

        _context.ModuleMasters.Add(
            new ModuleMasterEntity { Id = 1, ModuleCode = "PROP", ModuleName = "Property", DepartmentId = 1, IsActive = true });

        _context.SourceTables.AddRange(
            new SourceTableEntity { Id = 1, ModuleId = 1, TableName = "PTIS.PropertyMast", TableAliasName = "Property Master", IsActive = true },
            new SourceTableEntity { Id = 2, ModuleId = 1, TableName = "PTIS.InactiveTable", TableAliasName = "Inactive Table", IsActive = false },
            new SourceTableEntity { Id = 3, ModuleId = 999, TableName = "PTIS.OrphanModuleTable", TableAliasName = "Orphan Module Table", IsActive = true });

        _context.SourceTableDetails.AddRange(
            new SourceTableDetailsEntity { Id = 21, SourceTableId = 1, FieldName = "OwnerName", DisplayName = "Owner Name", ControlType = "text", DataType = "string", IsRequired = true, SequenceNo = 1, IsActive = true },
            new SourceTableDetailsEntity { Id = 22, SourceTableId = 1, FieldName = "MobileNo", DisplayName = "Mobile No", ControlType = "text", DataType = "string", IsRequired = false, SequenceNo = 2, IsActive = true },
            new SourceTableDetailsEntity { Id = 23, SourceTableId = 1, FieldName = "PlotArea", DisplayName = null, ControlType = null, DataType = null, IsRequired = false, SequenceNo = 3, IsActive = true },
            new SourceTableDetailsEntity { Id = 24, SourceTableId = 1, FieldName = "RetiredField", DisplayName = "Retired Field", ControlType = "text", DataType = "string", IsRequired = false, SequenceNo = 4, IsActive = false });

        _context.BulkUpdateMasters.AddRange(
            new BulkUpdateMasterEntity
            {
                Id = 1, UpdateCode = "OWNER_UPDATE", UpdateName = "Owner Update",
                ReferenceTableName = "PTIS.PropertyMast", IsActive = true,
                CreatedBy = 100, UpdatedBy = null, UpdatedDate = new DateTime(2099, 1, 1),
            },
            new BulkUpdateMasterEntity
            {
                Id = 2, UpdateCode = "ASSESSMENT_UPDATE", UpdateName = "Assessment Update",
                ReferenceTableName = "PTIS.PropertyMastDetails", IsActive = true,
                CreatedBy = null, UpdatedBy = 101,
            },
            new BulkUpdateMasterEntity
            {
                Id = 3, UpdateCode = "INACTIVE_UPDATE", UpdateName = "Inactive Update",
                ReferenceTableName = "PTIS.PropertyMast", IsActive = false,
            });

        _context.BulkUpdateFieldConfigs.AddRange(
            new BulkUpdateFieldConfigEntity { Id = 1, BulkUpdateMasterId = 1, FieldName = "OwnerName", DisplayName = "Owner Name", IsRequired = true, MaxLength = 100, SequenceNo = 1, IsActive = true, ControlType = "text", DataType = "string" },
            new BulkUpdateFieldConfigEntity { Id = 2, BulkUpdateMasterId = 1, FieldName = "MobileNo", DisplayName = "Mobile No", IsRequired = false, ValidationRegex = "^[0-9]{10}$", SequenceNo = 2, IsActive = true, ControlType = "text", DataType = "string" },
            new BulkUpdateFieldConfigEntity { Id = 3, BulkUpdateMasterId = 1, FieldName = "RetiredField", DisplayName = "Retired Field", IsRequired = false, SequenceNo = 3, IsActive = false, ControlType = "text", DataType = "string" },
            new BulkUpdateFieldConfigEntity { Id = 4, BulkUpdateMasterId = 2, FieldName = "BHK", DisplayName = "BHK", IsRequired = false, SequenceNo = 1, IsActive = true, ControlType = "text", DataType = "string" },
            new BulkUpdateFieldConfigEntity { Id = 5, BulkUpdateMasterId = 2, FieldName = "AssessmentRemark", DisplayName = "Assessment Remark", IsRequired = false, SequenceNo = 2, IsActive = true, ControlType = "text", DataType = "string" },
            new BulkUpdateFieldConfigEntity { Id = 6, BulkUpdateMasterId = 3, FieldName = "OwnerName", DisplayName = "Owner Name", IsRequired = true, SequenceNo = 1, IsActive = true, ControlType = "text", DataType = "string" });

        // A1/A2 back H1/H2 respectively (carry the IPAddress/Remarks that used to live on History).
        // H3/H4/H5 reference Activity ids (901/902/903) with no matching Activity row, proving the left join on Activity.
        _context.BulkUpdateActivity.AddRange(
            new BulkUpdateActivityEntity
            {
                Id = 1, ActivityType = "Screen", ActivityStatus = "Success",
                DateAndTime = new DateTime(2026, 7, 1), Records = 1, IPAddress = "10.0.0.1", Remarks = "Owner correction",
                ActivityRemark = "[OWNER_UPDATE] Updated 1 of 1 property successfully.",
                UpdateName = "Owner Update", DoneBy = "alice.user",
                StartTime = new DateTime(2026, 7, 1, 12, 0, 0), EndTime = new DateTime(2026, 7, 1, 12, 1, 0), Duration = 1,
            },
            new BulkUpdateActivityEntity
            {
                Id = 2, ActivityType = "Screen", ActivityStatus = "Success",
                DateAndTime = new DateTime(2026, 7, 2), Records = 1, IPAddress = "10.0.0.2",
                UpdateName = "Assessment Update", DoneBy = "bob.user",
                StartTime = new DateTime(2026, 7, 2, 12, 0, 0), EndTime = new DateTime(2026, 7, 2, 12, 1, 0), Duration = 1,
            });

        // H3 = master invalid (999) but property valid (6, ward 2) -> proves left join on master.
        // H4 = master valid (1) but property invalid (9999) -> proves left join on property/ward.
        // CreatedDate/UpdatedDate below are the "recency" value (CreatedDate ?? UpdatedDate) and its
        // fallback, respectively. CreatedDate is a required (non-null) column in the real schema, so
        // every row must set it - the UpdatedDate-fallback branch is defensive C# only, not reachable
        // via valid seed data.
        _context.BulkUpdateHistory.AddRange(
            new BulkUpdateHistoryEntity
            {
                Id = 1, ActivityId = 1, BulkUpdateMasterId = 1, PropertyId = 1,
                OldValue = "{\"OwnerName\":\"Alice\"}", NewValue = "{\"OwnerName\":\"NewOwner\"}",
                UpdatedColumns = "OwnerName,MobileNo", UpdatedBy = 100,
                CreatedDate = new DateTime(2026, 7, 20), UpdatedDate = new DateTime(2026, 7, 1),
            },
            new BulkUpdateHistoryEntity
            {
                Id = 2, ActivityId = 2, BulkUpdateMasterId = 2, PropertyId = 2,
                UpdatedColumns = "BHK", UpdatedBy = 101,
                CreatedDate = new DateTime(2026, 7, 22), UpdatedDate = new DateTime(2026, 7, 2),
            },
            new BulkUpdateHistoryEntity
            {
                Id = 3, ActivityId = 901, BulkUpdateMasterId = 999, PropertyId = 6,
                UpdatedColumns = "Foo", CreatedDate = new DateTime(2026, 7, 25), UpdatedDate = null,
            },
            new BulkUpdateHistoryEntity
            {
                Id = 4, ActivityId = 902, BulkUpdateMasterId = 1, PropertyId = 9999, UpdatedBy = 100,
                UpdatedColumns = "Bar", CreatedDate = new DateTime(2026, 7, 18), UpdatedDate = new DateTime(2026, 7, 3),
            },
            new BulkUpdateHistoryEntity
            {
                Id = 5, ActivityId = 903, BulkUpdateMasterId = 1, PropertyId = 3, UpdatedBy = 100,
                UpdatedColumns = "PlotArea,OwnerName", CreatedDate = new DateTime(2026, 7, 24), UpdatedDate = new DateTime(2026, 7, 4),
            });

        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    // ---- small helpers ---------------------------------------------------

    private static Stream BuildWorkbookStream(string[] headers, IEnumerable<string?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        for (var c = 0; c < headers.Length; c++)
            worksheet.Cell(1, c + 1).Value = headers[c];

        var r = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length; c++)
                if (row[c] != null)
                    worksheet.Cell(r, c + 1).Value = row[c];
            r++;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    #region GetMenuAsync Tests

    [Fact]
    public async Task GetMenuAsync_ReturnsOnlyActiveMasters_OrderedByUpdateName()
    {
        var result = await _service.GetMenuAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "ASSESSMENT_UPDATE", "OWNER_UPDATE" }, result.Select(m => m.UpdateCode));
        Assert.DoesNotContain(result, m => m.UpdateCode == "INACTIVE_UPDATE");
    }

    [Fact]
    public async Task GetMenuAsync_MapsDtoFieldsCorrectly()
    {
        var result = await _service.GetMenuAsync(CancellationToken.None);

        var owner = result.Single(m => m.UpdateCode == "OWNER_UPDATE");
        Assert.Equal("Owner Update", owner.UpdateName);
        Assert.Equal("PTIS.PropertyMast", owner.ReferenceTableName);
        Assert.True(owner.IsActive);
    }

    #endregion

    #region GetFormFieldsAsync Tests

    [Fact]
    public async Task GetFormFieldsAsync_ReturnsOnlyActiveFieldsForActiveMaster_OrderedBySequence()
    {
        var result = await _service.GetFormFieldsAsync("OWNER_UPDATE", CancellationToken.None);

        Assert.Equal(new[] { "OwnerName", "MobileNo" }, result.Select(f => f.FieldName));
        Assert.DoesNotContain(result, f => f.FieldName == "RetiredField");
    }

    [Fact]
    public async Task GetFormFieldsAsync_ReturnsEmpty_WhenMasterIsInactive()
    {
        var result = await _service.GetFormFieldsAsync("INACTIVE_UPDATE", CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFormFieldsAsync_ReturnsEmpty_WhenUpdateCodeUnknown()
    {
        var result = await _service.GetFormFieldsAsync("NO_SUCH_CODE", CancellationToken.None);

        Assert.Empty(result);
    }

    #endregion

    #region GetGridColumnsAsync Tests

    [Fact]
    public async Task GetGridColumnsAsync_IncludesFixedColumnsPlusConfiguredFields()
    {
        var result = await _service.GetGridColumnsAsync("OWNER_UPDATE", CancellationToken.None);

        Assert.Equal(5, result.Count); // 3 fixed + OwnerName + MobileNo
        Assert.Equal(new[] { "wardNo", "propertyNo", "partitionNo", "OwnerName", "MobileNo" }, result.Select(c => c.Key));
    }

    [Fact]
    public async Task GetGridColumnsAsync_ReturnsOnlyFixedColumns_WhenNoFieldsConfigured()
    {
        var result = await _service.GetGridColumnsAsync("NO_SUCH_CODE", CancellationToken.None);

        Assert.Equal(new[] { "wardNo", "propertyNo", "partitionNo" }, result.Select(c => c.Key));
    }

    #endregion

    #region GetSourceTablesAsync Tests

    [Fact]
    public async Task GetSourceTablesAsync_ExcludesInactiveSourceTables()
    {
        var result = await _service.GetSourceTablesAsync(CancellationToken.None);

        Assert.DoesNotContain(result, t => t.Id == 2);
    }

    [Fact]
    public async Task GetSourceTablesAsync_CombinesModuleNameAndTableAliasName_WhenModuleExists()
    {
        var result = await _service.GetSourceTablesAsync(CancellationToken.None);

        var item = result.Single(t => t.Id == 1);
        Assert.Equal("Property Property Master", item.TableName);
        Assert.Equal("PTIS.PropertyMast", item.ReferenceTableName);
    }

    [Fact]
    public async Task GetSourceTablesAsync_FallsBackToTableAliasName_WhenModuleIsMissing()
    {
        // SourceTable 3 references ModuleId=999, which doesn't exist - the left join's `mm` is null.
        var result = await _service.GetSourceTablesAsync(CancellationToken.None);

        var item = result.Single(t => t.Id == 3);
        Assert.Equal("Orphan Module Table", item.TableName);
    }

    #endregion

    #region GetSourceTableFieldsAsync Tests

    [Fact]
    public async Task GetSourceTableFieldsAsync_ExcludesInactiveFields()
    {
        var result = await _service.GetSourceTableFieldsAsync(1, CancellationToken.None);

        Assert.DoesNotContain(result, f => f.Id == 24);
    }

    [Fact]
    public async Task GetSourceTableFieldsAsync_FallsBackToFieldName_WhenDisplayNameIsNullOrEmpty()
    {
        // Field 23 (PlotArea) has a null DisplayName.
        var result = await _service.GetSourceTableFieldsAsync(1, CancellationToken.None);

        var item = result.Single(f => f.Id == 23);
        Assert.Equal("PlotArea", item.TableFieldName);
    }

    [Fact]
    public async Task GetSourceTableFieldsAsync_UsesDisplayName_WhenPresent()
    {
        var result = await _service.GetSourceTableFieldsAsync(1, CancellationToken.None);

        var item = result.Single(f => f.Id == 21);
        Assert.Equal("Owner Name", item.TableFieldName);
        Assert.Equal("OwnerName", item.FieldName);
    }

    #endregion

    #region CreateFromSourceTableAsync Tests

    [Fact]
    public async Task CreateFromSourceTableAsync_CreatesMasterAndFieldConfigs_OnHappyPath()
    {
        var request = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = "FlatOrShop No Name",
            TableId = 1,
            TableFieldIds = [21, 22],
        };

        var result = await _service.CreateFromSourceTableAsync(request, createdBy: 100, CancellationToken.None);

        Assert.Equal("FLATORSHOP_NO_NAME", result.Master.UpdateCode);
        Assert.Equal("FlatOrShop No Name", result.Master.UpdateName);
        Assert.Equal("PTIS.PropertyMast", result.Master.ReferenceTableName);
        Assert.Equal(2, result.FieldConfigs.Count);
        Assert.Equal(new[] { "OwnerName", "MobileNo" }, result.FieldConfigs.Select(f => f.FieldName));
        Assert.True(await _context.BulkUpdateMasters.AnyAsync(m => m.UpdateCode == "FLATORSHOP_NO_NAME"));
        Assert.Equal(2, await _context.BulkUpdateFieldConfigs.CountAsync(f => f.BulkUpdateMasterId == result.Master.Id));
    }

    [Fact]
    public async Task CreateFromSourceTableAsync_CollapsesNonAlphanumericRuns_IntoSingleUnderscore()
    {
        var request = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = "Flat/Shop  No.Name!!",
            TableId = 1,
            TableFieldIds = [21]
        };

        var result = await _service.CreateFromSourceTableAsync(request, createdBy: 100, CancellationToken.None);

        Assert.Equal("FLAT_SHOP_NO_NAME", result.Master.UpdateCode);
    }

    [Fact]
    public async Task CreateFromSourceTableAsync_ThrowsArgumentException_WhenUpdateNameHasNoAlphanumericCharacters()
    {
        var request = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = "@@@ !!!",
            TableId = 1,
            TableFieldIds = [21]
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFromSourceTableAsync(request, createdBy: 100, CancellationToken.None));
    }

    [Fact]
    public async Task CreateFromSourceTableAsync_ThrowsArgumentException_WhenDerivedUpdateCodeExceeds100Characters()
    {
        var request = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = new string('A', 101),
            TableId = 1,
            TableFieldIds = [21]
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFromSourceTableAsync(request, createdBy: 100, CancellationToken.None));
        Assert.Contains("exceeds 100 characters", ex.Message);
    }

    [Fact]
    public async Task CreateFromSourceTableAsync_FillsDefaults_WhenSourceFieldMetadataIsMissing()
    {
        var request = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = "Plot Area Update",
            TableId = 1,
            TableFieldIds = [23]
        };

        var result = await _service.CreateFromSourceTableAsync(request, createdBy: 100, CancellationToken.None);

        var field = result.FieldConfigs.Single();
        Assert.Equal("PlotArea", field.DisplayName);
        Assert.Equal("text", field.ControlType);
        Assert.Equal("string", field.DataType);
    }

    [Fact]
    public async Task CreateFromSourceTableAsync_ThrowsArgumentException_WhenUpdateCodeAlreadyExists()
    {
        var request = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = "Owner Update", // derives to OWNER_UPDATE, which already exists (Id=1)
            TableId = 1,
            TableFieldIds = [21]
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFromSourceTableAsync(request, createdBy: 100, CancellationToken.None));

        Assert.Equal(3, await _context.BulkUpdateMasters.CountAsync());
    }

    [Fact]
    public async Task CreateFromSourceTableAsync_ThrowsArgumentException_WhenFieldIdNotFound()
    {
        var request = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = "New Definition",
            TableId = 1,
            TableFieldIds = [21, 9999]
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFromSourceTableAsync(request, createdBy: 100, CancellationToken.None));

        Assert.Contains("9999", ex.Message);
        Assert.False(await _context.BulkUpdateMasters.AnyAsync(m => m.UpdateName == "New Definition"));
    }

    [Fact]
    public async Task CreateFromSourceTableAsync_ThrowsArgumentException_WhenSourceTableMissingOrInactive()
    {
        var missingTableRequest = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = "New Definition", TableId = 999, TableFieldIds = [21]
        };
        var inactiveTableRequest = new CreateBulkUpdateDefinitionFromSourceDto
        {
            UpdateName = "Another Definition", TableId = 2, TableFieldIds = [21]
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFromSourceTableAsync(missingTableRequest, createdBy: 100, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateFromSourceTableAsync(inactiveTableRequest, createdBy: 100, CancellationToken.None));
    }

    #endregion

    #region FilterPropertiesAsync Tests

    [Fact]
    public async Task FilterPropertiesAsync_ThrowsArgumentException_WhenUpdateCodeUnknown()
    {
        var request = new FilterPropertiesRequestDto { UpdateCode = ["NO_SUCH_CODE"], WardId = 1 };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.FilterPropertiesAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task FilterPropertiesAsync_FiltersByWardId()
    {
        var request = new FilterPropertiesRequestDto { UpdateCode = ["OWNER_UPDATE"], WardId = 1, PageSize = -1 };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.All(result.Items, p => Assert.Equal("001", p.WardNo));
        Assert.DoesNotContain(result.Items, p => p.PropertyNo == "X001");
    }

    [Fact]
    public async Task FilterPropertiesAsync_FiltersByExactPropertyNo()
    {
        var request = new FilterPropertiesRequestDto { UpdateCode = ["OWNER_UPDATE"], WardId = 1, PropertyNo = "P002" };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("P002", result.Items.First().PropertyNo);
    }

    [Fact]
    public async Task FilterPropertiesAsync_FiltersByPropertyNoRange()
    {
        var request = new FilterPropertiesRequestDto
        {
            UpdateCode = ["OWNER_UPDATE"], WardId = 1, FromPropertyNo = "P002", ToPropertyNo = "P004", PageSize = -1
        };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        Assert.Equal(new[] { "P002", "P003", "P004" }, result.Items.Select(p => p.PropertyNo));
    }

    [Fact]
    public async Task FilterPropertiesAsync_FiltersByWing()
    {
        var request = new FilterPropertiesRequestDto { UpdateCode = ["OWNER_UPDATE"], WardId = 1, Wing = "WingA" };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("P002", result.Items.First().PropertyNo);
    }

    [Fact]
    public async Task FilterPropertiesAsync_ReturnsAllResults_WhenPageSizeIsMinusOne()
    {
        var request = new FilterPropertiesRequestDto { UpdateCode = ["OWNER_UPDATE"], WardId = 1, PageSize = -1 };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Items.Count());
    }

    [Fact]
    public async Task FilterPropertiesAsync_ReturnsCorrectPage()
    {
        var request = new FilterPropertiesRequestDto { UpdateCode = ["OWNER_UPDATE"], WardId = 1, PageNumber = 2, PageSize = 2 };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(new[] { "P003", "P004" }, result.Items.Select(p => p.PropertyNo));
    }

    [Fact]
    public async Task FilterPropertiesAsync_PopulatesCurrentValuesFromPropertyItself_WhenTargetIsPropertyMast()
    {
        var request = new FilterPropertiesRequestDto { UpdateCode = ["OWNER_UPDATE"], WardId = 1, PropertyNo = "P001" };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        var item = result.Items.Single();
        Assert.Equal("Alice", item.CurrentValues["OwnerName"]);
        Assert.Equal("9000000001", item.CurrentValues["MobileNo"]);
    }

    [Fact]
    public async Task FilterPropertiesAsync_PopulatesCurrentValuesFromRelatedEntity_WhenTargetIsNotPropertyMast()
    {
        var request = new FilterPropertiesRequestDto { UpdateCode = ["ASSESSMENT_UPDATE"], WardId = 1, PageSize = -1 };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        var withAssessment = result.Items.Single(p => p.PropertyNo == "P001");
        Assert.Equal("2BHK", withAssessment.CurrentValues["BHK"]);
        Assert.Equal("Good", withAssessment.CurrentValues["AssessmentRemark"]);

        // Property 3 has no PropertyMastDetails row -> source is null -> CurrentValues stays empty.
        var withoutAssessment = result.Items.Single(p => p.PropertyNo == "P003");
        Assert.Empty(withoutAssessment.CurrentValues);
    }

    [Fact]
    public async Task FilterPropertiesAsync_MergesCurrentValues_FromMultipleUpdateCodes()
    {
        var request = new FilterPropertiesRequestDto
        {
            UpdateCode = ["OWNER_UPDATE", "ASSESSMENT_UPDATE"], WardId = 1, PropertyNo = "P001"
        };

        var result = await _service.FilterPropertiesAsync(request, CancellationToken.None);

        var item = result.Items.Single();
        Assert.Equal("Alice", item.CurrentValues["OwnerName"]);
        Assert.Equal("9000000001", item.CurrentValues["MobileNo"]);
        Assert.Equal("2BHK", item.CurrentValues["BHK"]);
        Assert.Equal("Good", item.CurrentValues["AssessmentRemark"]);
    }

    [Fact]
    public async Task FilterPropertiesAsync_ThrowsArgumentException_WhenAnyUpdateCodeInListIsUnknown()
    {
        var request = new FilterPropertiesRequestDto
        {
            UpdateCode = ["OWNER_UPDATE", "NO_SUCH_CODE"], WardId = 1
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.FilterPropertiesAsync(request, CancellationToken.None));
    }

    #endregion

    #region FilterPropertiesByCategoryAsync Tests

    [Fact]
    public async Task FilterPropertiesByCategoryAsync_EnrichesResultsWithCurrentValues_FromPropertySearchService()
    {
        var request = new FilterPropertiesByCategoryRequestDto
        {
            UpdateCode = ["OWNER_UPDATE"], SearchCategory = PropertySearchCategory.WardWise, WardId = 1,
        };
        var searchItems = new List<PropertySearchByCategoryResponseDto>
        {
            new() { PropertyId = 1, WardId = 1, WardNo = "001", PropertyNo = "P001", PartitionNo = "A" },
            new() { PropertyId = 2, WardId = 1, WardNo = "001", PropertyNo = "P002", PartitionNo = "B" },
        };
        _mockPropertySearchService
            .Setup(s => s.SearchByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PropertySearchByCategoryResponseDto>(searchItems, totalCount: 2, pageNumber: 1, pageSize: 10));

        var result = await _service.FilterPropertiesByCategoryAsync(request, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        var item1 = result.Items.Single(i => i.PropertyNo == "P001");
        Assert.Equal("Alice", item1.CurrentValues["OwnerName"]);
        var item2 = result.Items.Single(i => i.PropertyNo == "P002");
        Assert.Equal("Bob", item2.CurrentValues["OwnerName"]);
    }

    [Fact]
    public async Task FilterPropertiesByCategoryAsync_MergesCurrentValues_FromMultipleUpdateCodes()
    {
        var request = new FilterPropertiesByCategoryRequestDto
        {
            UpdateCode = ["OWNER_UPDATE", "ASSESSMENT_UPDATE"], SearchCategory = PropertySearchCategory.WardWise, WardId = 1,
        };
        var searchItems = new List<PropertySearchByCategoryResponseDto>
        {
            new() { PropertyId = 1, WardId = 1, WardNo = "001", PropertyNo = "P001", PartitionNo = "A" },
        };
        _mockPropertySearchService
            .Setup(s => s.SearchByCategoryAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PropertySearchByCategoryResponseDto>(searchItems, totalCount: 1, pageNumber: 1, pageSize: 10));

        var result = await _service.FilterPropertiesByCategoryAsync(request, CancellationToken.None);

        var item = result.Items.Single();
        Assert.Equal("Alice", item.CurrentValues["OwnerName"]);
        Assert.Equal("9000000001", item.CurrentValues["MobileNo"]);
        Assert.Equal("2BHK", item.CurrentValues["BHK"]);
        Assert.Equal("Good", item.CurrentValues["AssessmentRemark"]);
    }

    [Fact]
    public async Task FilterPropertiesByCategoryAsync_ThrowsArgumentException_WhenAnyUpdateCodeInListIsUnknown()
    {
        var request = new FilterPropertiesByCategoryRequestDto
        {
            UpdateCode = ["OWNER_UPDATE", "NO_SUCH_CODE"], SearchCategory = PropertySearchCategory.WardWise, WardId = 1,
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.FilterPropertiesByCategoryAsync(request, CancellationToken.None));
    }

    #endregion

    #region BulkUpdateAsync Tests

    [Fact]
    public async Task BulkUpdateAsync_ThrowsArgumentException_WhenUpdateCodeUnknown()
    {
        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "NO_SUCH_CODE", PropertyIds = [1], UpdateData = new() { ["OwnerName"] = "X" },
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkUpdateAsync(request, 1, "1.1.1.1", CancellationToken.None));
    }

    [Fact]
    public async Task BulkUpdateAsync_ThrowsArgumentException_WhenRequiredFieldMissing()
    {
        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "OWNER_UPDATE", PropertyIds = [1], UpdateData = new() { ["MobileNo"] = "1234567890" },
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkUpdateAsync(request, 1, "1.1.1.1", CancellationToken.None));
        Assert.Contains("Owner Name is required", ex.Message);
    }

    [Fact]
    public async Task BulkUpdateAsync_ThrowsArgumentException_WhenNoValidFieldsPresent()
    {
        // ASSESSMENT_UPDATE has no required fields, so validation passes; but the only key supplied
        // isn't one of its configured fields, so nothing is left to update.
        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "ASSESSMENT_UPDATE", PropertyIds = [1], UpdateData = new() { ["UnknownField"] = "x" },
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkUpdateAsync(request, 1, "1.1.1.1", CancellationToken.None));
        Assert.Equal("No valid fields to update.", ex.Message);
    }

    [Fact]
    public async Task BulkUpdateAsync_HappyPath_UpdatesEntitiesAndWritesHistory()
    {
        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "OWNER_UPDATE",
            PropertyIds = [1, 2],
            UpdateData = new() { ["OwnerName"] = "UpdatedOwner", ["MobileNo"] = "9999999999" },
        };

        var result = await _service.BulkUpdateAsync(request, 7, "3.3.3.3", CancellationToken.None);

        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);

        var p1 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 1);
        var p2 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 2);
        Assert.Equal("UpdatedOwner", p1.OwnerName);
        Assert.Equal("9999999999", p1.MobileNo);
        Assert.Equal("UpdatedOwner", p2.OwnerName);
        Assert.Equal(7, p1.UpdatedBy);

        var newHistory = await _context.BulkUpdateHistory.AsNoTracking()
            .Where(h => h.BulkUpdateMasterId == 1 && (h.PropertyId == 1 || h.PropertyId == 2) && h.Id > 5)
            .ToListAsync();
        Assert.Equal(2, newHistory.Count);

        var h1 = newHistory.Single(h => h.PropertyId == 1);
        Assert.Equal("OwnerName,MobileNo", h1.UpdatedColumns);
        using (var oldDoc = JsonDocument.Parse(h1.OldValue!))
            Assert.Equal("Alice", oldDoc.RootElement.GetProperty("OwnerName").GetString());
        using (var newDoc = JsonDocument.Parse(h1.NewValue!))
            Assert.Equal("UpdatedOwner", newDoc.RootElement.GetProperty("OwnerName").GetString());

        var activity = await _context.BulkUpdateActivity.AsNoTracking()
            .SingleAsync(a => a.Id == h1.ActivityId);
        Assert.Equal("Screen", activity.ActivityType);
        Assert.Equal("Success", activity.ActivityStatus);
        Assert.Equal("3.3.3.3", activity.IPAddress);
        Assert.Equal(2, activity.Records);
        Assert.Equal("Owner Update", activity.UpdateName);
        Assert.Equal("[OWNER_UPDATE] Updated 2 of 2 properties successfully.", activity.ActivityRemark);
    }

    [Fact]
    public async Task BulkUpdateAsync_ThrowsAndRollsBackTransaction_WhenPropertyIdIsOutOfInt32Range()
    {
        // NOTE ON COVERAGE: the per-property try/catch inside BulkUpdateAsync exists to let some
        // properties in a batch succeed while others fail, then roll back the whole batch. In this
        // codebase, every plausible per-property failure mode (bad key, bad value coercion) is driven by
        // data that's identical for the WHOLE batch: the same UpdateData is applied to every property,
        // and all PropertyIds are validated together in a single batched LoadByKeyAsync preload BEFORE
        // the per-property loop even starts (CommonDetailsService.cs line ~370). So a clean "2 succeed,
        // 1 fails" scenario that lands inside the per-property catch isn't reachable here - the same way
        // BulkApplyAsync's raw-SQL MERGE path isn't reachable against the InMemory provider in
        // LockUnlockServiceTests. What IS reachable and covered here is the whole-batch failure path:
        // an out-of-range PropertyId (long.MaxValue can't narrow to the int Id key) blows up the batched
        // preload (Convert.ToInt32 inside DynamicEntityLoader.BuildKeyPredicate) before the per-property
        // try/catch is ever entered, so it's caught only by the OUTER catch - which still rolls back the
        // transaction before rethrowing, leaving no partial changes persisted.
        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "OWNER_UPDATE",
            PropertyIds = [1, 2, long.MaxValue],
            UpdateData = new() { ["OwnerName"] = "ShouldRollback" },
        };

        await Assert.ThrowsAsync<OverflowException>(
            () => _service.BulkUpdateAsync(request, 1, null, CancellationToken.None));

        // Nothing was persisted - property 1 must still show its original, pre-batch value.
        var p1 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 1);
        Assert.Equal("Alice", p1.OwnerName);
        Assert.Equal(5, await _context.BulkUpdateHistory.CountAsync()); // unchanged from seed

        // Even though the exception was thrown before any per-property iteration completed, a Failed
        // Activity row still survives - it's written outside/after the rolled-back transaction.
        var activity = await _context.BulkUpdateActivity.AsNoTracking()
            .Where(a => a.UpdateName == "Owner Update" && a.Records == 3)
            .OrderByDescending(a => a.Id).FirstAsync();
        Assert.Equal("Failed", activity.ActivityStatus);
        Assert.StartsWith("[OWNER_UPDATE] ", activity.ActivityRemark);
    }

    [Fact]
    public async Task BulkUpdateAsync_RecordsFailedActivityWithRemark_WhenFieldValidationFails()
    {
        // Validation runs before the properties transaction even begins, but an Activity row must
        // still be written - the whole point of ActivityRemark is to explain *why* an attempt failed,
        // including rejections that never touch BulkUpdateHistory at all.
        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "OWNER_UPDATE",
            PropertyIds = [1],
            UpdateData = new() { ["OwnerName"] = "Alice", ["MobileNo"] = "not-a-number" },
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.BulkUpdateAsync(request, 1, null, CancellationToken.None));
        Assert.Equal("Mobile No has invalid format.", ex.Message);

        var activity = await _context.BulkUpdateActivity.AsNoTracking()
            .Where(a => a.UpdateName == "Owner Update" && a.Records == 1)
            .OrderByDescending(a => a.Id).FirstAsync();
        Assert.Equal("Failed", activity.ActivityStatus);
        Assert.Equal("[OWNER_UPDATE] Mobile No has invalid format.", activity.ActivityRemark);

        // The failure happened before the transaction started - nothing was ever written to History.
        Assert.Equal(5, await _context.BulkUpdateHistory.CountAsync());
    }

    [Fact]
    public async Task BulkUpdateAsync_CountsPropertyWithNoMatchingTargetRows_AsSuccess()
    {
        // Documents observed behavior rather than asserting it's desirable: a PropertyId with zero
        // matching target rows (e.g. a property nobody has an assessment row for yet) is NOT treated as
        // a per-property failure - the loop finds an empty `targets` list, writes a history row with a
        // null OldValue, and still unconditionally increments SuccessCount (CommonDetailsService.cs,
        // the `targets.Count > 0 ? ... : null` branch followed by `result.SuccessCount++`). Worth
        // flagging as a possible reporting gap: callers can't tell "actually updated" from "matched
        // nothing" from SuccessCount alone.
        var request = new BulkUpdateRequestDto
        {
            UpdateCode = "ASSESSMENT_UPDATE",
            PropertyIds = [4], // property 4 has no PropertyMastDetails (PropertyAssessmentEntity) row
            UpdateData = new() { ["BHK"] = "1BHK" },
        };

        var result = await _service.BulkUpdateAsync(request, 1, null, CancellationToken.None);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        var history = await _context.BulkUpdateHistory.AsNoTracking()
            .Where(h => h.BulkUpdateMasterId == 2 && h.PropertyId == 4).SingleAsync();
        Assert.Null(history.OldValue);
    }

    #endregion

    #region BulkUpdateBatchAsync Tests

    [Fact]
    public async Task BulkUpdateBatchAsync_HappyPath_ProcessesEachItemIndependently()
    {
        var requests = new List<BulkUpdateRequestDto>
        {
            new() { UpdateCode = "OWNER_UPDATE", PropertyIds = [3], UpdateData = new() { ["OwnerName"] = "CharlieUpdated" } },
            new() { UpdateCode = "ASSESSMENT_UPDATE", PropertyIds = [1], UpdateData = new() { ["BHK"] = "4BHK" } },
        };

        var results = await _service.BulkUpdateBatchAsync(requests, 1, "1.1.1.1", CancellationToken.None);

        Assert.Equal(2, results.Count);

        var r0 = results[0];
        Assert.Equal("OWNER_UPDATE", r0.UpdateCode);
        Assert.Equal(1, r0.SuccessCount);
        Assert.Equal(0, r0.FailedCount);

        var r1 = results[1];
        Assert.Equal("ASSESSMENT_UPDATE", r1.UpdateCode);
        Assert.Equal(1, r1.SuccessCount);
        Assert.Equal(0, r1.FailedCount);

        var p3 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 3);
        Assert.Equal("CharlieUpdated", p3.OwnerName);
        var assessment1 = await _context.PropertyMastDetails.AsNoTracking().SingleAsync(a => a.PropertyId == 1);
        Assert.Equal("4BHK", assessment1.BHK);
    }

    [Fact]
    public async Task BulkUpdateBatchAsync_MixedBatch_BadItemFailsWithoutAffectingGoodItem()
    {
        var requests = new List<BulkUpdateRequestDto>
        {
            new() { UpdateCode = "NO_SUCH_CODE", PropertyIds = [4], UpdateData = new() { ["OwnerName"] = "X" } },
            new() { UpdateCode = "OWNER_UPDATE", PropertyIds = [4], UpdateData = new() { ["OwnerName"] = "DaveUpdated" } },
        };

        var results = await _service.BulkUpdateBatchAsync(requests, 1, null, CancellationToken.None);

        Assert.Equal(2, results.Count);

        var bad = results[0];
        Assert.Equal("NO_SUCH_CODE", bad.UpdateCode);
        Assert.Equal(0, bad.SuccessCount);
        Assert.Equal(1, bad.FailedCount);
        Assert.Single(bad.Errors);

        var good = results[1];
        Assert.Equal("OWNER_UPDATE", good.UpdateCode);
        Assert.Equal(1, good.SuccessCount);
        Assert.Equal(0, good.FailedCount);

        var p4 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 4);
        Assert.Equal("DaveUpdated", p4.OwnerName);
    }

    [Fact]
    public async Task BulkUpdateBatchAsync_ItemThatThrowsOverflow_RollsBackOnlyThatItem()
    {
        var requests = new List<BulkUpdateRequestDto>
        {
            new()
            {
                UpdateCode = "OWNER_UPDATE",
                PropertyIds = [1, 2, long.MaxValue],
                UpdateData = new() { ["OwnerName"] = "ShouldRollback" },
            },
            new() { UpdateCode = "OWNER_UPDATE", PropertyIds = [5], UpdateData = new() { ["OwnerName"] = "EveUpdated" } },
        };

        var results = await _service.BulkUpdateBatchAsync(requests, 1, null, CancellationToken.None);

        Assert.Equal(2, results.Count);

        var bad = results[0];
        Assert.Equal(0, bad.SuccessCount);
        Assert.Equal(3, bad.FailedCount);
        Assert.Single(bad.Errors);

        var good = results[1];
        Assert.Equal(1, good.SuccessCount);
        Assert.Equal(0, good.FailedCount);

        // Bad item rolled back - properties 1/2 unchanged.
        var p1 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 1);
        var p2 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 2);
        Assert.Equal("Alice", p1.OwnerName);
        Assert.Equal("Bob", p2.OwnerName);

        // Good item committed independently.
        var p5 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 5);
        Assert.Equal("EveUpdated", p5.OwnerName);
    }

    #endregion

    #region ExportPropertiesToExcelAsync Tests

    [Fact]
    public async Task ExportPropertiesToExcelAsync_ProducesValidWorkbook_IgnoringPaging()
    {
        var request = new ExportPropertiesRequestDto { UpdateCode = "OWNER_UPDATE", WardId = 1 };

        var bytes = await _service.ExportPropertiesToExcelAsync(request, CancellationToken.None);

        Assert.NotEmpty(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        var headerRow = ws.Row(1);
        Assert.Equal("wardNo", headerRow.Cell(1).GetString());
        Assert.Equal("propertyNo", headerRow.Cell(2).GetString());
        Assert.Equal("partitionNo", headerRow.Cell(3).GetString());
        Assert.Equal("Owner Name", headerRow.Cell(4).GetString());
        Assert.Equal("Mobile No", headerRow.Cell(5).GetString());

        var usedRange = ws.RangeUsed()!;
        Assert.Equal(6, usedRange.RowCount()); // header + 5 properties
        Assert.Equal("P001", ws.Cell(2, 2).GetString());
        Assert.Equal("Alice", ws.Cell(2, 4).GetString());
        Assert.Equal("P005", ws.Cell(6, 2).GetString());
    }

    [Fact]
    public async Task ExportPropertiesToExcelAsync_HeaderOnly_WhenWardIdOmitted()
    {
        var request = new ExportPropertiesRequestDto { UpdateCode = "OWNER_UPDATE" };

        var bytes = await _service.ExportPropertiesToExcelAsync(request, CancellationToken.None);

        Assert.NotEmpty(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        var headerRow = ws.Row(1);
        Assert.Equal("wardNo", headerRow.Cell(1).GetString());
        Assert.Equal("propertyNo", headerRow.Cell(2).GetString());
        Assert.Equal("partitionNo", headerRow.Cell(3).GetString());
        Assert.Equal("Owner Name", headerRow.Cell(4).GetString());
        Assert.Equal("Mobile No", headerRow.Cell(5).GetString());

        var usedRange = ws.RangeUsed()!;
        Assert.Equal(1, usedRange.RowCount()); // header only, no data rows
    }

    [Fact]
    public async Task ExportPropertiesToExcelAsync_ThrowsArgumentException_WhenUpdateCodeUnknown_EvenWithoutWardId()
    {
        var request = new ExportPropertiesRequestDto { UpdateCode = "NO_SUCH_CODE" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ExportPropertiesToExcelAsync(request, CancellationToken.None));
    }

    #endregion

    #region ImportPropertiesFromExcelAsync Tests

    [Fact]
    public async Task ImportPropertiesFromExcelAsync_HappyPath_UpdatesMatchingProperty()
    {
        using var stream = BuildWorkbookStream(
            ["wardNo", "propertyNo", "partitionNo", "OwnerName"],
            [["001", "P001", "A", "ExcelOwner1"]]);

        var result = await _service.ImportPropertiesFromExcelAsync("OWNER_UPDATE", stream, 9, "4.4.4.4", null, CancellationToken.None);

        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);

        var p1 = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 1);
        Assert.Equal("ExcelOwner1", p1.OwnerName);

        var activity = await _context.BulkUpdateActivity.AsNoTracking()
            .Where(a => a.ActivityType == "Excel").OrderByDescending(a => a.Id).FirstAsync();
        Assert.Equal("Success", activity.ActivityStatus);
        Assert.Equal("4.4.4.4", activity.IPAddress);
        Assert.Equal(1, activity.Records);
        Assert.Equal("[OWNER_UPDATE] Updated 1 of 1 property successfully.", activity.ActivityRemark);
    }

    [Fact]
    public async Task ImportPropertiesFromExcelAsync_UpdatesBothMainAndPartitionRows_InSameUpload()
    {
        // Row 1 targets the Main property (blank partitionNo, property 7); row 2 targets the
        // Partition property (partitionNo "Z", property 6) - same ward+propertyNo, different partition.
        // Regression coverage for the candidate-narrowing filter incorrectly excluding blank-partition
        // rows once any row in the file has a non-blank partitionNo.
        using var stream = BuildWorkbookStream(
            ["wardNo", "propertyNo", "partitionNo", "OwnerName"],
            [
                ["002", "X001", null, "MainOwnerUpdated"],
                ["002", "X001", "Z", "PartitionOwnerUpdated"],
            ]);

        var result = await _service.ImportPropertiesFromExcelAsync("OWNER_UPDATE", stream, 9, "4.4.4.4", null, CancellationToken.None);

        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Errors);

        var mainProperty = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 7);
        var partitionProperty = await _context.PropertyMast.AsNoTracking().SingleAsync(p => p.Id == 6);
        Assert.Equal("MainOwnerUpdated", mainProperty.OwnerName);
        Assert.Equal("PartitionOwnerUpdated", partitionProperty.OwnerName);
    }

    [Fact]
    public async Task ImportPropertiesFromExcelAsync_ReturnsErrors_WhenNoMatchingPropertyFound()
    {
        using var stream = BuildWorkbookStream(
            ["wardNo", "propertyNo", "partitionNo", "OwnerName"],
            [["999", "P999", "Z", "NoSuchOwner"]]);

        var result = await _service.ImportPropertiesFromExcelAsync("OWNER_UPDATE", stream, 9, "4.4.4.4", null, CancellationToken.None);

        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains(result.Errors, e => e.Contains("no property found"));
    }

    [Fact]
    public async Task ImportPropertiesFromExcelAsync_ThrowsArgumentException_WhenIdentityColumnMissing()
    {
        using var stream = BuildWorkbookStream(
            ["wardNo", "propertyNo", "OwnerName"], // partitionNo missing
            [["001", "P001", "ExcelOwner1"]]);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ImportPropertiesFromExcelAsync("OWNER_UPDATE", stream, 9, "4.4.4.4", null, CancellationToken.None));
        Assert.Contains("partitionNo", ex.Message);
    }

    #endregion

    #region ValidateImportExcelAsync Tests

    [Fact]
    public async Task ValidateImportExcelAsync_ReturnsNoRows_WhenAllRowsAreClean()
    {
        using var stream = BuildWorkbookStream(
            ["wardNo", "propertyNo", "partitionNo", "MobileNo"],
            [["001", "P001", "A", "9999999999"]]);

        var result = await _service.ValidateImportExcelAsync("OWNER_UPDATE", stream, CancellationToken.None);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.FlaggedRowCount);
        Assert.Empty(result.Rows);
        Assert.Equal(["wardNo", "propertyNo", "partitionNo", "Mobile No", "ValidationRemark"], result.Columns);
    }

    [Fact]
    public async Task ValidateImportExcelAsync_ReturnsOnlyProblemRows_WithValidationRemarks()
    {
        using var stream = BuildWorkbookStream(
            ["wardNo", "propertyNo", "partitionNo", "MobileNo"],
            [
                ["001", "P001", "A", "9999999999"],   // clean - must NOT appear in the report
                ["999", "P999", "Z", "9999999999"],   // no matching property
                ["001", "P002", "B", "notanumber"],   // fails MobileNo's regex
            ]);

        var result = await _service.ValidateImportExcelAsync("OWNER_UPDATE", stream, CancellationToken.None);

        Assert.Equal(3, result.TotalRows);
        Assert.Equal(2, result.FlaggedRowCount);
        Assert.Equal(2, result.Rows.Count);

        var noPropertyRow = result.Rows[0];
        Assert.Equal("999", noPropertyRow["wardNo"]);
        Assert.Contains("No property found", (string)noPropertyRow["ValidationRemark"]!);

        var badFormatRow = result.Rows[1];
        Assert.Equal("P002", badFormatRow["propertyNo"]);
        Assert.Contains("Mobile No has invalid format", (string)badFormatRow["ValidationRemark"]!);
    }

    [Fact]
    public async Task ValidateImportExcelAsync_ThrowsArgumentException_WhenIdentityColumnMissing()
    {
        using var stream = BuildWorkbookStream(
            ["wardNo", "propertyNo", "OwnerName"], // partitionNo missing
            [["001", "P001", "ExcelOwner1"]]);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ValidateImportExcelAsync("OWNER_UPDATE", stream, CancellationToken.None));
        Assert.Contains("partitionNo", ex.Message);
    }

    [Fact]
    public async Task ValidateImportExcelAsync_MakesNoDatabaseChanges()
    {
        var historyCountBefore = await _context.BulkUpdateHistory.CountAsync();
        var activityCountBefore = await _context.BulkUpdateActivity.CountAsync();

        using var stream = BuildWorkbookStream(
            ["wardNo", "propertyNo", "partitionNo", "MobileNo"],
            [["999", "P999", "Z", "notanumber"]]); // fails both identity and format checks

        var result = await _service.ValidateImportExcelAsync("OWNER_UPDATE", stream, CancellationToken.None);

        Assert.Equal(1, result.FlaggedRowCount);
        Assert.Equal(historyCountBefore, await _context.BulkUpdateHistory.CountAsync());
        Assert.Equal(activityCountBefore, await _context.BulkUpdateActivity.CountAsync());
    }

    #endregion

    #region GetUpdateHistoryAsync Tests

    [Fact]
    public async Task GetUpdateHistoryAsync_NoFilters_ReturnsAllOrderedByCreatedDateThenIdDescending()
    {
        var request = new UpdateHistoryQueryParameters { PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        // H3 (07-25), H5 (07-24), H2 (07-22), H1 (07-20), H4 (07-18)
        Assert.Equal(new[] { 3, 5, 2, 1, 4 }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_CreatedDateComesFromHistoryRow_NotFromMaster()
    {
        // Master 1's own UpdatedDate is sentinel-far-future (2099); history row 1's CreatedDate is 2026-07-20.
        var request = new UpdateHistoryQueryParameters { PropertyNo = "P001", PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        var item = result.Items.Single();
        Assert.Equal(new DateTime(2026, 7, 20), item.CreatedDate);
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByUpdateName()
    {
        var request = new UpdateHistoryQueryParameters { UpdateName = "Owner Update", PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        Assert.Equal(new[] { 5, 1, 4 }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByWardNo()
    {
        var request = new UpdateHistoryQueryParameters { WardNo = "001", PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        Assert.Equal(new[] { 5, 2, 1 }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByPropertyNo()
    {
        var request = new UpdateHistoryQueryParameters { PropertyNo = "P001", PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByPartitionNo()
    {
        var request = new UpdateHistoryQueryParameters { PartitionNo = "C", PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        Assert.Equal([5], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByUpdatedColumns_UsingContainsSemantics()
    {
        var request = new UpdateHistoryQueryParameters { UpdatedColumns = "PlotArea", PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        Assert.Equal([5], result.Items.Select(i => i.Id));
        Assert.Equal("PlotArea,OwnerName", result.Items.Single().UpdatedColumns);
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByDoneBy_ViaHistoryRowsOwnCreatedByOrUpdatedBy()
    {
        var aliceResult = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { DoneBy = "alice.user", PageSize = -1 }, CancellationToken.None);
        Assert.Equal(new[] { 5, 1, 4 }, aliceResult.Items.Select(i => i.Id));

        var bobResult = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { DoneBy = "bob.user", PageSize = -1 }, CancellationToken.None);
        Assert.Equal([2], bobResult.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_CombinesMultipleFilters_WithAndSemantics()
    {
        var request = new UpdateHistoryQueryParameters { UpdateName = "Owner Update", WardNo = "001", PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        // Master 1's history rows are 1, 4, 5 - only 1 and 5 also have a ward-1 property (4's property is missing).
        Assert.Equal(new[] { 5, 1 }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_LeftJoin_IncludesRowWithUnmatchedMaster()
    {
        var request = new UpdateHistoryQueryParameters { PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        var orphanMasterRow = result.Items.Single(i => i.Id == 3);
        Assert.Null(orphanMasterRow.UpdateName);
        Assert.Null(orphanMasterRow.DoneBy);
        // Its property (6, ward 2) is valid, so those fields ARE populated - proving this is a
        // per-join left join, not one big inner join that would have dropped the whole row.
        Assert.Equal("002", orphanMasterRow.WardNo);
        Assert.Equal("X001", orphanMasterRow.PropertyNo);
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_LeftJoin_IncludesRowWithUnmatchedProperty()
    {
        var request = new UpdateHistoryQueryParameters { PageSize = -1 };

        var result = await _service.GetUpdateHistoryAsync(request, CancellationToken.None);

        var orphanPropertyRow = result.Items.Single(i => i.Id == 4);
        Assert.Null(orphanPropertyRow.WardNo);
        Assert.Null(orphanPropertyRow.PropertyNo);
        Assert.Null(orphanPropertyRow.PartitionNo);
        // Its master (1) is valid, so UpdateName IS populated.
        Assert.Equal("Owner Update", orphanPropertyRow.UpdateName);
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_ReturnsAllResults_WhenPageSizeIsMinusOne()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { PageSize = -1 }, CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Items.Count());
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_ReturnsCorrectPage()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(new[] { 2, 1 }, result.Items.Select(i => i.Id)); // page 2 of [3,5,2,1,4]
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_ComputesPropertyField_AsWardPropertyPartitionCombination()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { PageSize = -1 }, CancellationToken.None);

        Assert.Equal("001-P001-A", result.Items.Single(i => i.Id == 1).Property);

        // History row 4 has no matching property (left join) - all three parts are null, so
        // Property drops every empty segment rather than rendering "--".
        Assert.Equal(string.Empty, result.Items.Single(i => i.Id == 4).Property);
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersBySearchTerm_MatchingCombinedPropertyField()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { SearchTerm = "P001", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersBySearchTerm_MatchingDoneBy_AcrossMultipleRows()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { SearchTerm = "alice", PageSize = -1 }, CancellationToken.None);

        Assert.Equal(new[] { 5, 1, 4 }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersBySearchTerm_MatchingUpdatedColumns()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { SearchTerm = "PlotArea", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([5], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersBySearchTerm_MatchingUpdateName()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { SearchTerm = "Owner Update", PageSize = -1 }, CancellationToken.None);

        Assert.Equal(new[] { 5, 1, 4 }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_IncludesJoinedActivityFields()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { PropertyNo = "P001", PageSize = -1 }, CancellationToken.None);

        var item = result.Items.Single();
        Assert.Equal("Screen", item.ActivityType);
        Assert.Equal("Success", item.ActivityStatus);
        Assert.Equal("alice.user", item.ActivityDoneBy);
        Assert.Equal(1, item.Records);
        Assert.Equal("10.0.0.1", item.IPAddress);
        Assert.Equal("Owner correction", item.Remarks);
        Assert.Equal("[OWNER_UPDATE] Updated 1 of 1 property successfully.", item.ActivityRemark);
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_LeftJoin_ActivityFieldsNullWhenNoMatchingActivity()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { PageSize = -1 }, CancellationToken.None);

        var orphanActivityRow = result.Items.Single(i => i.Id == 5);
        Assert.Null(orphanActivityRow.ActivityType);
        Assert.Null(orphanActivityRow.ActivityStatus);
        Assert.Null(orphanActivityRow.ActivityDoneBy);
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByActivityStatus()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { ActivityStatus = "Success", PageSize = -1 }, CancellationToken.None);

        Assert.Equal(new[] { 2, 1 }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersById()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { Id = 1, PageSize = -1 }, CancellationToken.None);

        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByActivityId()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { ActivityId = 2, PageSize = -1 }, CancellationToken.None);

        Assert.Equal([2], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByProperty_UsingContainsSemantics()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { Property = "001-P001", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([1], result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_FiltersByIsActive()
    {
        _context.BulkUpdateHistory.Add(new BulkUpdateHistoryEntity
        {
            Id = 6, ActivityId = 904, BulkUpdateMasterId = 1, PropertyId = 4,
            UpdatedColumns = "OwnerName", IsActive = false, CreatedDate = new DateTime(2026, 7, 5),
        });
        await _context.SaveChangesAsync();

        var activeResult = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { IsActive = true, PageSize = -1 }, CancellationToken.None);
        Assert.DoesNotContain(activeResult.Items, i => i.Id == 6);

        var inactiveResult = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { IsActive = false, PageSize = -1 }, CancellationToken.None);
        Assert.Equal([6], inactiveResult.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_IncludesPropertyIdAndIsActiveInProjection()
    {
        var result = await _service.GetUpdateHistoryAsync(
            new UpdateHistoryQueryParameters { Id = 1, PageSize = -1 }, CancellationToken.None);

        var item = result.Items.Single();
        Assert.Equal(1, item.PropertyId);
        Assert.True(item.IsActive);
    }

    #endregion

    #region ExportUpdateHistoryToExcelAsync Tests

    [Fact]
    public async Task ExportUpdateHistoryToExcelAsync_ProducesValidWorkbook_WithExpectedHeaders()
    {
        var request = new UpdateHistoryQueryParameters { PageSize = -1 };

        var bytes = await _service.ExportUpdateHistoryToExcelAsync(request, CancellationToken.None);

        Assert.NotEmpty(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        var expectedHeaders = new[]
        {
            "Id", "UpdateName", "PropertyId", "WardNo", "PropertyNo", "PartitionNo", "Property",
            "OldValue", "NewValue", "UpdatedColumns", "IsActive", "Remarks", "IPAddress", "DoneBy", "CreatedDate",
            "ActivityId", "ActivityType", "ActivityStatus", "ActivityDoneBy", "Records", "StartTime", "EndTime", "Duration",
            "ActivityRemark"
        };
        for (var c = 0; c < expectedHeaders.Length; c++)
            Assert.Equal(expectedHeaders[c], ws.Cell(1, c + 1).GetString());
    }

    [Fact]
    public async Task ExportUpdateHistoryToExcelAsync_IgnoresCallerPaging_ExportsAllMatchingRows()
    {
        // 3 rows match WardNo="001" (ids 1, 2, 5), but PageSize asks for only 1 per page.
        var request = new UpdateHistoryQueryParameters { WardNo = "001", PageNumber = 1, PageSize = 1 };

        var bytes = await _service.ExportUpdateHistoryToExcelAsync(request, CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        var usedRange = ws.RangeUsed()!;
        Assert.Equal(4, usedRange.RowCount()); // header + 3 matching rows, paging ignored

        var idColumnValues = new List<int>();
        for (var r = 2; r <= usedRange.RowCount(); r++)
            idColumnValues.Add(ws.Cell(r, 1).GetValue<int>());
        Assert.Equal(new[] { 5, 2, 1 }, idColumnValues);
    }

    #endregion

    #region GetUpdateActivityAsync Tests

    [Fact]
    public async Task GetUpdateActivityAsync_NoFilters_ReturnsAllOrderedByDateAndTimeDescending()
    {
        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { PageSize = -1 }, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { 2, 1 }, result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_FiltersById()
    {
        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { Id = 1, PageSize = -1 }, CancellationToken.None);

        Assert.Equal([1], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_FiltersByActivityType()
    {
        _context.BulkUpdateActivity.Add(new BulkUpdateActivityEntity
        {
            Id = 3, ActivityType = "Excel", ActivityStatus = "Success",
            DateAndTime = new DateTime(2026, 7, 3),
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { ActivityType = "Excel", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([3], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_FiltersByActivityStatus()
    {
        _context.BulkUpdateActivity.Add(new BulkUpdateActivityEntity
        {
            Id = 3, ActivityType = "Screen", ActivityStatus = "Failed",
            DateAndTime = new DateTime(2026, 7, 3),
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { ActivityStatus = "Failed", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([3], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_FiltersByCreatedDateRange()
    {
        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters
            {
                CreatedDateFrom = new DateTime(2026, 7, 2),
                CreatedDateTo = new DateTime(2026, 7, 2),
                PageSize = -1
            }, CancellationToken.None);

        Assert.Equal([2], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_FiltersByDoneBy()
    {
        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { DoneBy = "bob.user", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([2], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_FiltersByRemarks_UsingContainsSemantics()
    {
        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { Remarks = "correction", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([1], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_FiltersByActivityRemark_UsingContainsSemantics()
    {
        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { ActivityRemark = "Updated 1 of 1", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([1], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_FiltersBySearchTerm_MatchingDoneBy()
    {
        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { SearchTerm = "bob", PageSize = -1 }, CancellationToken.None);

        Assert.Equal([2], result.Items.Select(a => a.Id));
    }

    [Fact]
    public async Task GetUpdateActivityAsync_ReturnsCorrectPage()
    {
        var result = await _service.GetUpdateActivityAsync(
            new UpdateActivityQueryParameters { PageNumber = 1, PageSize = 1 }, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal([2], result.Items.Select(a => a.Id));
    }

    #endregion

    #region ExportUpdateActivityToExcelAsync Tests

    [Fact]
    public async Task ExportUpdateActivityToExcelAsync_ProducesValidWorkbook_WithExpectedHeaders()
    {
        var bytes = await _service.ExportUpdateActivityToExcelAsync(
            new UpdateActivityQueryParameters { PageSize = -1 }, CancellationToken.None);

        Assert.NotEmpty(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        var expectedHeaders = new[]
        {
            "Id", "ActivityType", "ActivityStatus", "CreatedDate", "Records", "IPAddress",
            "Remarks", "UpdateName", "DoneBy", "StartTime", "EndTime", "Duration", "ActivityRemark"
        };
        for (var c = 0; c < expectedHeaders.Length; c++)
            Assert.Equal(expectedHeaders[c], ws.Cell(1, c + 1).GetString());
    }

    [Fact]
    public async Task ExportUpdateActivityToExcelAsync_IgnoresCallerPaging_ExportsAllMatchingRows()
    {
        var bytes = await _service.ExportUpdateActivityToExcelAsync(
            new UpdateActivityQueryParameters { PageNumber = 1, PageSize = 1 }, CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        var usedRange = ws.RangeUsed()!;
        Assert.Equal(3, usedRange.RowCount()); // header + 2 matching rows, paging ignored

        var idColumnValues = new List<int>();
        for (var r = 2; r <= usedRange.RowCount(); r++)
            idColumnValues.Add(ws.Cell(r, 1).GetValue<int>());
        Assert.Equal(new[] { 2, 1 }, idColumnValues);
    }

    [Fact]
    public async Task GetUpdateHistoryAsync_EnrichesOldAndNewValues_WithDynamicLookupDescriptions()
    {
        // Arrange: Seed PropertyCategoryMaster, BulkUpdateFieldConfig for CategoryId, and BulkUpdateHistory Entity
        _context.BulkUpdateFieldConfigs.Add(new BulkUpdateFieldConfigEntity
        {
            Id = 99,
            BulkUpdateMasterId = 1,
            FieldName = "CategoryId",
            DisplayName = "Category",
            ControlType = "select",
            DataType = "int",
            BindApi = "/api/master/property-category",
            ApiResponse = "id,propertyCategoryName",
            SequenceNo = 10,
            IsActive = true
        });

        _context.PropertyCategoryMaster.Add(new PropertyCategoryEntity
        {
            Id = 10,
            PropertyCategoryName = "Commercial Test Category",
            IsActive = true
        });
        _context.PropertyCategoryMaster.Add(new PropertyCategoryEntity
        {
            Id = 20,
            PropertyCategoryName = "Residential Test Category",
            IsActive = true
        });

        _context.BulkUpdateHistory.Add(new BulkUpdateHistoryEntity
        {
            Id = 999,
            BulkUpdateMasterId = 1,
            PropertyId = 1,
            OldValue = "{\"CategoryId\":10}",
            NewValue = "{\"CategoryId\":\"20\"}",
            UpdatedColumns = "CategoryId",
            IsActive = true,
            CreatedDate = DateTime.Now
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetUpdateHistoryAsync(new UpdateHistoryQueryParameters { Id = 999 }, CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        var historyItem = result.Items.First();
        Assert.Contains("Commercial Test Category", historyItem.OldValue);
        Assert.Contains("Residential Test Category", historyItem.NewValue);
    }

    [Fact]
    public async Task ExportPropertiesToExcelAsync_UsesDisplayNameHeader_AndResolvesLookupDescription()
    {
        // Arrange
        _context.BulkUpdateMasters.Add(new BulkUpdateMasterEntity
        {
            Id = 4,
            UpdateCode = "CATEGORY",
            UpdateName = "Category Update",
            ReferenceTableName = "PTIS.PropertyMast",
            IsActive = true
        });

        _context.BulkUpdateFieldConfigs.Add(new BulkUpdateFieldConfigEntity
        {
            Id = 100,
            BulkUpdateMasterId = 4,
            FieldName = "CategoryId",
            DisplayName = "Category",
            ControlType = "select",
            DataType = "int",
            BindApi = "/api/master/property-category",
            ApiResponse = "id,propertyCategoryName",
            SequenceNo = 1,
            IsActive = true
        });

        _context.PropertyCategoryMaster.Add(new PropertyCategoryEntity
        {
            Id = 55,
            PropertyCategoryName = "Export Category Test",
            IsActive = true
        });

        var prop1 = _context.PropertyMast.First(p => p.Id == 1);
        prop1.CategoryId = 55;
        await _context.SaveChangesAsync();

        var request = new ExportPropertiesRequestDto
        {
            UpdateCode = "CATEGORY",
            WardId = 1
        };

        // Act
        var bytes = await _service.ExportPropertiesToExcelAsync(request, CancellationToken.None);

        // Assert
        Assert.NotEmpty(bytes);
        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);
        Assert.NotNull(ws);
        var headerCell = ws.Cell(1, 4).GetString();
        Assert.Equal("Category", headerCell);

        var dataCell = ws.Cell(2, 4).GetString();
        Assert.Equal("Export Category Test", dataCell);
    }

    [Fact]
    public async Task DynamicEntityLoader_LoadByKeyAsync_EmptyKeys_ReturnsEmptyList_WithoutQueryingDb()
    {
        // Arrange
        var loader = new DynamicEntityLoader(_context);

        // Act
        var emptyResult = await loader.LoadByKeyAsync(typeof(PropertyEntity), "Id", Array.Empty<long>(), asNoTracking: true);
        var nullResult = await loader.LoadByKeyAsync(typeof(PropertyEntity), "Id", null!, asNoTracking: true);

        // Assert
        Assert.Empty(emptyResult);
        Assert.Empty(nullResult);
    }

    [Fact]
    public async Task DynamicEntityLoader_LoadAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var loader = new DynamicEntityLoader(_context);

        // Act
        var allProps = await loader.LoadAllAsync(typeof(PropertyEntity), asNoTracking: true);

        // Assert
        Assert.NotEmpty(allProps);
        Assert.Equal(_context.PropertyMast.Count(), allProps.Count);
    }

    [Fact]
    public void ValidateFieldValues_YearValidation_EnforcesFourDigits_FutureYear_AndAssessmentLessThanConstruction()
    {
        // Arrange
        var configs = new List<BulkUpdateFieldConfigDto>
        {
            new BulkUpdateFieldConfigDto { FieldName = "ConstructionYear", DisplayName = "Construction Year", ControlType = "year", IsActive = true },
            new BulkUpdateFieldConfigDto { FieldName = "AssessmentYear", DisplayName = "Assessment Year", ControlType = "year", IsActive = true },
            new BulkUpdateFieldConfigDto { FieldName = "TestYear", DisplayName = "Test Year", ControlType = "year", IsActive = true }
        };

        // Scenario 1: Invalid 4-digit format & Future Year for AssessmentYear, plus future year 2027 for TestYear
        var data1 = new Dictionary<string, object?>
        {
            ["ConstructionYear"] = "25",
            ["AssessmentYear"] = "2030",
            ["TestYear"] = "2027"
        };

        // Act 1
        var methodInfo = typeof(CommonDetailsService).GetMethod("ValidateFieldValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var errors1 = (List<string>)methodInfo.Invoke(null, new object?[] { configs, data1, null })!;

        // Assert 1
        Assert.Contains("Construction Year must be a valid 4-digit year.", errors1);
        Assert.Contains($"Assessment Year cannot exceed current year ({DateTime.Now.Year}).", errors1);
        // TestYear is a generic year field (like TestYear = 2027), so it MUST NOT get "cannot exceed current year" error
        Assert.DoesNotContain(errors1, e => e.Contains("Test Year cannot exceed current year"));

        // Scenario 2: Both fields updated together -> AssessmentYear < ConstructionYear
        var data2 = new Dictionary<string, object?>
        {
            ["ConstructionYear"] = "2025",
            ["AssessmentYear"] = "2020"
        };

        // Act 2
        var errors2 = (List<string>)methodInfo.Invoke(null, new object?[] { configs, data2, null })!;

        // Assert 2: Common message when both fields are updated together
        Assert.Contains("Assessment Year must be greater than or equal to Construction Year.", errors2);
    }

    [Fact]
    public void ValidateFieldValues_SeparateYearUpdate_ValidatesAgainstDatabaseEntity()
    {
        // Arrange
        var assessmentConfig = new List<BulkUpdateFieldConfigDto>
        {
            new BulkUpdateFieldConfigDto { FieldName = "AssessmentYear", DisplayName = "Assessment Year", ControlType = "year", IsActive = true }
        };

        var constructionConfig = new List<BulkUpdateFieldConfigDto>
        {
            new BulkUpdateFieldConfigDto { FieldName = "ConstructionYear", DisplayName = "Construction Year", ControlType = "year", IsActive = true }
        };

        var existingEntity = new PropertyDetailsEntity
        {
            ConstructionYear = "2025",
            AssessmentYear = "2024"
        };

        var methodInfo = typeof(CommonDetailsService).GetMethod("ValidateFieldValues", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        // Act & Assert 1: Updating only AssessmentYear = 2020 (less than DB ConstructionYear 2025)
        var dataAssessOnly = new Dictionary<string, object?> { ["AssessmentYear"] = "2020" };
        var errorsAssessOnly = (List<string>)methodInfo.Invoke(null, new object?[] { assessmentConfig, dataAssessOnly, existingEntity })!;
        Assert.Contains("Assessment Year cannot be less than Construction Year.", errorsAssessOnly);

        // Act & Assert 2: Updating only ConstructionYear = 2025 (greater than DB AssessmentYear 2024)
        var dataConstOnly = new Dictionary<string, object?> { ["ConstructionYear"] = "2025" };
        var errorsConstOnly = (List<string>)methodInfo.Invoke(null, new object?[] { constructionConfig, dataConstOnly, existingEntity })!;
        Assert.Contains("Construction Year cannot be greater than Assessment Year.", errorsConstOnly);
    }

    #endregion
}
