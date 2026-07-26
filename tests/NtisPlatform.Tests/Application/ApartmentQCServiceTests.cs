using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class ApartmentQCServiceTests
{
    private readonly Mock<IApartmentQCRepository> _repo;
    private readonly Mock<IUnitOfWork>            _uow;
    private readonly ApartmentQCOptions           _opts;
    private readonly ApartmentQCService           _service;

    public ApartmentQCServiceTests()
    {
        _repo = new Mock<IApartmentQCRepository>();
        _uow  = new Mock<IUnitOfWork>();
        _opts = new ApartmentQCOptions
        {
            MaxBulkUpdateBatchSize = 500,
            MaxUnpagedPageSize     = 1000,
            MaxExportRowCount      = 50_000
        };
        _service = new ApartmentQCService(
            _repo.Object,
            _uow.Object,
            Options.Create(_opts));
    }

    // ──────────────────────────── Helpers ─────────────────────────────────────

    private static ApartmentQCFetchedData EmptyFetched() => ApartmentQCFetchedData.Empty;

    private static ApartmentQCFetchedData FetchedWithOneProperty(int propId = 1, int wardId = 10) =>
        new()
        {
            Properties = [new ApartmentQCPropertyData
            {
                Id         = propId,
                WardId     = wardId,
                PropertyNo = "100",
            }],
            Details = [],
            WardZones = new Dictionary<int, ApartmentQCWardData>
            {
                [wardId] = new ApartmentQCWardData(wardId, "W1", "Z1")
            }
        };

    private static ApartmentQCFetchedData FetchedWithDetailAndPartition(int propId = 1, int wardId = 10) =>
        new()
        {
            Properties = [new ApartmentQCPropertyData
            {
                Id           = propId,
                WardId       = wardId,
                PropertyNo   = "100",
                PartitionNo  = "A"
            }],
            Details = [new ApartmentQCDetailData { Id = 5, PropertyId = propId }],
            WardZones = new Dictionary<int, ApartmentQCWardData>
            {
                [wardId] = new ApartmentQCWardData(wardId, "W2", "Z2")
            }
        };

    // ──────────────────────────── GetPagedAsync ────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ZeroCount_ReturnsEmptyPagedResult()
    {
        _repo.Setup(r => r.CountAsync(It.IsAny<ApartmentQCQueryParameters>(), default)).ReturnsAsync(0);
        _repo.Setup(r => r.FetchPagedDataAsync(It.IsAny<ApartmentQCQueryParameters>(), It.IsAny<int>(), It.IsAny<int>(), null, default))
             .ReturnsAsync(EmptyFetched());

        var result = await _service.GetPagedAsync(new ApartmentQCQueryParameters(), default);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetPagedAsync_WithData_ReturnsItems()
    {
        _repo.Setup(r => r.CountAsync(It.IsAny<ApartmentQCQueryParameters>(), default)).ReturnsAsync(1);
        _repo.Setup(r => r.FetchPagedDataAsync(It.IsAny<ApartmentQCQueryParameters>(), It.IsAny<int>(), It.IsAny<int>(), null, default))
             .ReturnsAsync(FetchedWithOneProperty());

        var result = await _service.GetPagedAsync(new ApartmentQCQueryParameters { PageNumber = 1, PageSize = 10 }, default);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetPagedAsync_FormatsPropertyNo_WithoutPartition()
    {
        _repo.Setup(r => r.CountAsync(It.IsAny<ApartmentQCQueryParameters>(), default)).ReturnsAsync(1);
        _repo.Setup(r => r.FetchPagedDataAsync(It.IsAny<ApartmentQCQueryParameters>(), It.IsAny<int>(), It.IsAny<int>(), null, default))
             .ReturnsAsync(FetchedWithOneProperty(wardId: 10));

        var result = await _service.GetPagedAsync(new ApartmentQCQueryParameters { PageNumber = 1, PageSize = 10 }, default);

        Assert.Equal("W1-100", result.Items.First().PropertyNo);
    }

    [Fact]
    public async Task GetPagedAsync_FormatsPropertyNo_WithPartition()
    {
        _repo.Setup(r => r.CountAsync(It.IsAny<ApartmentQCQueryParameters>(), default)).ReturnsAsync(1);
        _repo.Setup(r => r.FetchPagedDataAsync(It.IsAny<ApartmentQCQueryParameters>(), It.IsAny<int>(), It.IsAny<int>(), null, default))
             .ReturnsAsync(FetchedWithDetailAndPartition(wardId: 10));

        var result = await _service.GetPagedAsync(new ApartmentQCQueryParameters { PageNumber = 1, PageSize = 10 }, default);

        Assert.Equal("W2-100-A", result.Items.First().PropertyNo);
    }

    [Fact]
    public async Task GetPagedAsync_TaxTotals_AreSummedCorrectly()
    {
        var fetched = new ApartmentQCFetchedData
        {
            Properties = [new ApartmentQCPropertyData { Id = 1, WardId = 1 }],
            Details    = [],
            WardZones  = new Dictionary<int, ApartmentQCWardData> { [1] = new(1, "W", "Z") },
            Tm         = new Dictionary<int, ApartmentQCTransactionData>
                { [1] = new(1, null, TmTaxAmount: 100m) },
            Tp         = new Dictionary<int, ApartmentQCTaxPendingData>
                { [1] = new(1, PendingAmount: 50m) }
        };

        _repo.Setup(r => r.CountAsync(It.IsAny<ApartmentQCQueryParameters>(), default)).ReturnsAsync(1);
        _repo.Setup(r => r.FetchPagedDataAsync(It.IsAny<ApartmentQCQueryParameters>(), It.IsAny<int>(), It.IsAny<int>(), null, default))
             .ReturnsAsync(fetched);

        var result = await _service.GetPagedAsync(new ApartmentQCQueryParameters { PageNumber = 1, PageSize = 10 }, default);

        Assert.Equal(150m, result.Items.First().NewTaxTotal);
    }

    // ──────────────────────── GetByPropertyDetailAsync ────────────────────────

    [Fact]
    public async Task GetByPropertyDetailAsync_PropertyNotFound_ReturnsEmptyPaged()
    {
        _repo.Setup(r => r.FetchByPropertyDataAsync(99, ApartmentQCResultType.Dual, default))
             .ReturnsAsync(EmptyFetched());

        var result = await _service.GetByPropertyDetailAsync(99, ApartmentQCResultType.Dual, default);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetByPropertyDetailAsync_WithDetails_ReturnsOneItemPerDetail()
    {
        var fetched = new ApartmentQCFetchedData
        {
            Properties = [new ApartmentQCPropertyData { Id = 1, WardId = 1 }],
            Details    = [
                new ApartmentQCDetailData { Id = 10, PropertyId = 1 },
                new ApartmentQCDetailData { Id = 11, PropertyId = 1 }
            ],
            WardZones  = new Dictionary<int, ApartmentQCWardData> { [1] = new(1, "W", "Z") }
        };

        _repo.Setup(r => r.FetchByPropertyDataAsync(1, ApartmentQCResultType.Dual, default))
             .ReturnsAsync(fetched);

        var result = await _service.GetByPropertyDetailAsync(1, ApartmentQCResultType.Dual, default);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    // ──────────────────────── GetFilterOptionsAsync ────────────────────────────

    [Fact]
    public async Task GetFilterOptionsAsync_StripsColumnFilters_BeforeCallingRepo()
    {
        var query = new ApartmentQCQueryParameters
        {
            WardId       = 5,
            PropertyNo   = "P100",
            Wing         = "A",
            ApartmentType = "2BHK",
            FlatOrShopNo = "101",
            PropertyType = 1
        };

        ApartmentQCQueryParameters? capturedQuery = null;
        _repo.Setup(r => r.GetFilterOptionsAsync(It.IsAny<ApartmentQCQueryParameters>(), null, default))
             .Callback<ApartmentQCQueryParameters, ApartmentQCFilterColumn?, CancellationToken>(
                 (q, _, _) => capturedQuery = q)
             .ReturnsAsync(new ApartmentQCFilterOptionsDto());

        await _service.GetFilterOptionsAsync(query, null, default);

        Assert.NotNull(capturedQuery);
        Assert.Equal(5, capturedQuery!.WardId);
        Assert.Equal("P100", capturedQuery.PropertyNo);
        Assert.Null(capturedQuery.Wing);
        Assert.Null(capturedQuery.ApartmentType);
        Assert.Null(capturedQuery.FlatOrShopNo);
        Assert.Null(capturedQuery.PropertyType);
    }

    // ──────────────────────── GetOldPropertyDataAsync ──────────────────────────

    [Fact]
    public async Task GetOldPropertyDataAsync_ReturnsNull_WhenRepoReturnsNull()
    {
        _repo.Setup(r => r.GetOldPropertyDataByNoAsync("OLD-999", default))
             .ReturnsAsync((OldPropertyLookupDto?)null);

        var result = await _service.GetOldPropertyDataAsync("OLD-999", default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOldPropertyDataAsync_ReturnsDto_WhenFound()
    {
        var dto = new OldPropertyLookupDto { OldPropertyNo = "OLD-001", OldRV = 5000m };
        _repo.Setup(r => r.GetOldPropertyDataByNoAsync("OLD-001", default))
             .ReturnsAsync(dto);

        var result = await _service.GetOldPropertyDataAsync("OLD-001", default);

        Assert.NotNull(result);
        Assert.Equal(5000m, result!.OldRV);
    }

    // ──────────────────────── ExportToExcelAsync ───────────────────────────────

    [Fact]
    public async Task ExportToExcelAsync_ThrowsInvalidOperation_WhenCountExceedsCap()
    {
        _opts.MaxExportRowCount = 100;
        _repo.Setup(r => r.CountAsync(It.IsAny<ApartmentQCQueryParameters>(), default))
             .ReturnsAsync(101);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExportToExcelAsync(new ApartmentQCQueryParameters()));
    }

    [Fact]
    public async Task ExportToExcelAsync_ReturnsNonEmptyBytes_WhenZeroRows()
    {
        _repo.Setup(r => r.CountAsync(It.IsAny<ApartmentQCQueryParameters>(), default)).ReturnsAsync(0);

        var bytes = await _service.ExportToExcelAsync(new ApartmentQCQueryParameters());

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task ExportToExcelAsync_ReturnsBytes_WithRows()
    {
        _repo.Setup(r => r.CountAsync(It.IsAny<ApartmentQCQueryParameters>(), default)).ReturnsAsync(1);
        _repo.Setup(r => r.FetchPagedDataAsync(It.IsAny<ApartmentQCQueryParameters>(), 0, 1,
                         ApartmentQCResultType.Dual, default))
             .ReturnsAsync(FetchedWithOneProperty());

        var bytes = await _service.ExportToExcelAsync(new ApartmentQCQueryParameters(), ApartmentQCResultType.Dual);

        Assert.NotEmpty(bytes);
    }

    // ──────────────────────── UpdateDetailAsync ────────────────────────────────

    [Fact]
    public async Task UpdateDetailAsync_BatchTooLarge_ReturnsFailure()
    {
        _opts.MaxBulkUpdateBatchSize = 2;
        var dtos = Enumerable.Range(1, 3)
            .Select(i => new UpdateApartmentQCDetailsDto { DetailId = i, FloorId = 1 })
            .ToList();

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Single(result!.Failures);
        Assert.Contains("exceeds", result.Failures[0].Reason);
        _repo.Verify(r => r.PropertyExistsAsync(It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateDetailAsync_NullRowInList_ReturnsFailure()
    {
        var dtos = new List<UpdateApartmentQCDetailsDto> { null! };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Single(result!.Failures);
        Assert.Contains("null", result.Failures[0].Reason);
    }

    [Fact]
    public async Task UpdateDetailAsync_NoOpRow_ReturnsFailure()
    {
        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 1 } // no fields set
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Single(result!.Failures);
        Assert.Contains("at least one updatable field", result.Failures[0].Reason);
    }

    [Fact]
    public async Task UpdateDetailAsync_DuplicateDetailIds_ReturnsFailure()
    {
        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 5, FloorId = 1 },
            new() { DetailId = 5, FloorId = 2 }
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Contains(result!.Failures,failure => failure.Reason.Contains("Duplicate"));
    }

    [Fact]
    public async Task UpdateDetailAsync_PropertyNotFound_ReturnsNull()
    {
        _repo.Setup(r => r.PropertyExistsAsync(99, default)).ReturnsAsync(false);

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 1, FloorId = 1 }
        };

        var result = await _service.UpdateDetailAsync(99, dtos, updatedBy: 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateDetailAsync_InvalidFloorId_ReturnsFailure()
    {
        _repo.Setup(r => r.PropertyExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.GetExistingFloorIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());  // floor 999 does not exist

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 1, FloorId = 999 }
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Contains(result!.Failures,failure =>failure.Field == "FloorId" && failure.InvalidId == 999);
    }

    [Fact]
    public async Task UpdateDetailAsync_InvalidConstructionTypeId_ReturnsFailure()
    {
        _repo.Setup(r => r.PropertyExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.GetExistingFloorIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingConstructionTypeIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 1, ConstructionTypeId = 888 }
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
       Assert.Contains( result!.Failures,failure => failure.Field == "ConstructionTypeId");
    }

    [Fact]
    public async Task UpdateDetailAsync_InvalidTypeOfUseId_ReturnsFailure()
    {
        _repo.Setup(r => r.PropertyExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.GetExistingFloorIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingConstructionTypeIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 1, TypeOfUseId = 777 }
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Contains( result!.Failures,failure => failure.Field == "TypeOfUseId");
    }

    [Fact]
    public async Task UpdateDetailAsync_InvalidSubTypeOfUseId_ReturnsFailure()
    {
        _repo.Setup(r => r.PropertyExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.GetExistingFloorIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingConstructionTypeIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingSubTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 1, SubTypeOfUseId = 666 }
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Contains(result!.Failures,failure => failure.Field == "SubTypeOfUseId");
    }

    [Fact]
    public async Task UpdateDetailAsync_TypeOfUseIdWithoutSubTypeOfUseId_IsAcceptedAndClearsSubType()
    {
        // When TypeOfUseId is changed and no SubTypeOfUseId is provided, the sub-type is cleared.
        var entity = new PropertyDetailsEntity { Id = 10, PropertyId = 1, IsActive = true, SubTypeOfUseId = 5 };
        _repo.Setup(r => r.PropertyExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.GetExistingFloorIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingConstructionTypeIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int> { 3 });
        _repo.Setup(r => r.GetExistingSubTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetTrackedDetailsForUpdateAsync(1, It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new Dictionary<int, PropertyDetailsEntity> { [10] = entity });
        _repo.Setup(r => r.ApplyDetailPatches(It.IsAny<Dictionary<int, PropertyDetailsEntity>>(),
                         It.IsAny<IEnumerable<UpdateApartmentQCDetailsDto>>(), It.IsAny<int>()));
        _uow.Setup(u => u.BeginTransactionAsync(default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _uow.Setup(u => u.CommitTransactionAsync(default)).Returns(Task.CompletedTask);

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 10, TypeOfUseId = 3 }
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Empty(result!.Failures);
        Assert.Equal(1, result.Updated);
    }

    [Fact]
    public async Task UpdateDetailAsync_DetailIdNotFoundForProperty_ReturnsFailure()
    {
        _repo.Setup(r => r.PropertyExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.GetExistingFloorIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int> { 1 });
        _repo.Setup(r => r.GetExistingConstructionTypeIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingSubTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        // Tracked entities: DetailId 42 does NOT belong to property 1
        _repo.Setup(r => r.GetTrackedDetailsForUpdateAsync(1, It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new Dictionary<int, PropertyDetailsEntity>());

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 42, FloorId = 1 }
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 1);

        Assert.NotNull(result);
        Assert.Single(result!.Failures);
        Assert.Equal(42, result.Failures[0].DetailId);
    }

    [Fact]
    public async Task UpdateDetailAsync_Success_CallsTransactionAndReturnsResult()
    {
        var entity = new PropertyDetailsEntity { Id = 10, PropertyId = 1, IsActive = true };
        _repo.Setup(r => r.PropertyExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.GetExistingFloorIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int> { 2 });
        _repo.Setup(r => r.GetExistingConstructionTypeIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingSubTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetTrackedDetailsForUpdateAsync(1, It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new Dictionary<int, PropertyDetailsEntity> { [10] = entity });
        _repo.Setup(r => r.ApplyDetailPatches(It.IsAny<Dictionary<int, PropertyDetailsEntity>>(),
                         It.IsAny<IEnumerable<UpdateApartmentQCDetailsDto>>(), It.IsAny<int>()));
        _uow.Setup(u => u.BeginTransactionAsync(default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitTransactionAsync(default)).Returns(Task.CompletedTask);

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 10, FloorId = 2 }
        };

        var result = await _service.UpdateDetailAsync(1, dtos, updatedBy: 7);

        Assert.NotNull(result);
        Assert.Empty(result!.Failures);
        Assert.Equal(1, result.Updated);
        Assert.Equal(1, result.TotalRequested);
        Assert.Contains(10, result.UpdatedDetailIds);
        _uow.Verify(u => u.BeginTransactionAsync(default), Times.Once);
        _uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateDetailAsync_ExceptionDuringSave_RollsBack()
    {
        var entity = new PropertyDetailsEntity { Id = 10, PropertyId = 1, IsActive = true };
        _repo.Setup(r => r.PropertyExistsAsync(1, default)).ReturnsAsync(true);
        _repo.Setup(r => r.GetExistingFloorIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int> { 2 });
        _repo.Setup(r => r.GetExistingConstructionTypeIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetExistingSubTypeOfUseIdsAsync(It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new HashSet<int>());
        _repo.Setup(r => r.GetTrackedDetailsForUpdateAsync(1, It.IsAny<IEnumerable<int>>(), default))
             .ReturnsAsync(new Dictionary<int, PropertyDetailsEntity> { [10] = entity });
        _repo.Setup(r => r.ApplyDetailPatches(It.IsAny<Dictionary<int, PropertyDetailsEntity>>(),
                         It.IsAny<IEnumerable<UpdateApartmentQCDetailsDto>>(), It.IsAny<int>()));
        _uow.Setup(u => u.BeginTransactionAsync(default)).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitTransactionAsync(default)).ThrowsAsync(new Exception("DB error"));
        _uow.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var dtos = new List<UpdateApartmentQCDetailsDto> { new() { DetailId = 10, FloorId = 2 } };

        await Assert.ThrowsAsync<Exception>(() => _service.UpdateDetailAsync(1, dtos, updatedBy: 1));

        _uow.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────── SyncRoomAggregatesAsync ──────────────────────────

    [Fact]
    public async Task SyncRoomAggregatesAsync_PropertyNotFound_ReturnsFalseAndDoesNotSave()
    {
        _repo.Setup(r => r.GetRoomAggregatesAsync(5, default)).ReturnsAsync((10.0, 3));
        _repo.Setup(r => r.GetTrackedPropertyDetailsByIdAsync(5, default))
             .ReturnsAsync((PropertyDetailsEntity?)null);

        var result = await _service.SyncRoomAggregatesAsync(5, updatedBy: 1);

        Assert.False(result);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task SyncRoomAggregatesAsync_SetsAreaFieldsWithCorrectFactors()
    {
        const double sqMeter = 10.0;
        var entity = new PropertyDetailsEntity { Id = 5 };
        _repo.Setup(r => r.GetRoomAggregatesAsync(5, default)).ReturnsAsync((sqMeter, 4));
        _repo.Setup(r => r.GetTrackedPropertyDetailsByIdAsync(5, default)).ReturnsAsync(entity);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await _service.SyncRoomAggregatesAsync(5, updatedBy: null);

        Assert.Equal(sqMeter,                  entity.CarpetAreaSqMeter);
        Assert.Equal(sqMeter * 10.7639,        entity.CarpetAreaSqFeet!.Value,  4);
        Assert.Equal(sqMeter * 1.20,           entity.BuiltupAreaSqMeter!.Value, 4);
        Assert.Equal(sqMeter * 1.20 * 10.7639, entity.BuiltupAreaSqFeet!.Value, 4);
        Assert.Equal(4,                        entity.NoOfRooms);
    }

    [Fact]
    public async Task SyncRoomAggregatesAsync_StampsUpdatedBy_WhenProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 5 };
        _repo.Setup(r => r.GetRoomAggregatesAsync(5, default)).ReturnsAsync((5.0, 2));
        _repo.Setup(r => r.GetTrackedPropertyDetailsByIdAsync(5, default)).ReturnsAsync(entity);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await _service.SyncRoomAggregatesAsync(5, updatedBy: 99);

        Assert.Equal(99, entity.UpdatedBy);
    }

    [Fact]
    public async Task SyncRoomAggregatesAsync_DoesNotStampUpdatedBy_WhenNull()
    {
        var entity = new PropertyDetailsEntity { Id = 5, UpdatedBy = 0 };
        _repo.Setup(r => r.GetRoomAggregatesAsync(5, default)).ReturnsAsync((5.0, 2));
        _repo.Setup(r => r.GetTrackedPropertyDetailsByIdAsync(5, default)).ReturnsAsync(entity);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        await _service.SyncRoomAggregatesAsync(5, updatedBy: null);

        Assert.Equal(0, entity.UpdatedBy);
    }

    // ──────────────────────── UpdateBasicDetailsAsync ──────────────────────────

    [Fact]
    public async Task UpdateBasicDetailsAsync_NoFieldsProvided_ThrowsArgumentException()
    {
        var dto = new UpdateApartmentQCBasicDetailsDto(); // all null

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateBasicDetailsAsync(1, dto, updatedBy: 1));
    }

    [Fact]
    public async Task UpdateBasicDetailsAsync_PropertyNotFound_ReturnsPropertyNotFound()
    {
        _repo.Setup(r => r.PrepareBasicDetailsPatchAsync(99, It.IsAny<UpdateApartmentQCBasicDetailsDto>(), 1, default))
             .ReturnsAsync(BasicDetailsPatchOutcome.PropertyNotFound);

        var result = await _service.UpdateBasicDetailsAsync(99, new UpdateApartmentQCBasicDetailsDto { OwnerName = "Test" }, updatedBy: 1);

        Assert.Equal(BasicDetailsPatchOutcome.PropertyNotFound, result);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateBasicDetailsAsync_OldPropertyNoNotFound_ReturnsOldPropertyNoNotFound()
    {
        _repo.Setup(r => r.PrepareBasicDetailsPatchAsync(1, It.IsAny<UpdateApartmentQCBasicDetailsDto>(), 1, default))
             .ReturnsAsync(BasicDetailsPatchOutcome.OldPropertyNoNotFound);

        var result = await _service.UpdateBasicDetailsAsync(1, new UpdateApartmentQCBasicDetailsDto { OldPropertyNo = "INVALID" }, updatedBy: 1);

        Assert.Equal(BasicDetailsPatchOutcome.OldPropertyNoNotFound, result);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateBasicDetailsAsync_Success_SavesAndReturnsSuccess()
    {
        _repo.Setup(r => r.PrepareBasicDetailsPatchAsync(1, It.IsAny<UpdateApartmentQCBasicDetailsDto>(), 1, default))
             .ReturnsAsync(BasicDetailsPatchOutcome.Success);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var result = await _service.UpdateBasicDetailsAsync(1, new UpdateApartmentQCBasicDetailsDto { Wing = "B" }, updatedBy: 1);

        Assert.Equal(BasicDetailsPatchOutcome.Success, result);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
