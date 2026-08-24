using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class TaxZoningRangeServiceTests
{
    private readonly Mock<IRepository<TaxZoningRangeEntity, int>> _rangeRepo = new();
    private readonly Mock<IRepository<PropertyEntity, int>> _propertyRepo = new();
    private readonly Mock<IRepository<WardEntity, int>> _wardRepo = new();
    private readonly Mock<IRepository<TaxZoneEntity, int>> _taxZoneRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<TaxZoningRangeService>> _logger = new();
    private readonly Mock<ILocalizationService> _localizationService = new();
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    private readonly IMapper _mapper;

    private readonly List<TaxZoningRangeEntity> _ranges = new();
    private readonly List<PropertyEntity> _properties = new();
    private readonly List<WardEntity> _wards = new();
    private readonly List<TaxZoneEntity> _taxZones = new();
    private int _nextRangeId = 1;

    public TaxZoningRangeServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxZoningRangeMappingProfile>();
        }, NullLoggerFactory.Instance);
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();

        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _wardRepo.Setup(r => r.GetQueryable()).Returns(() => _wards.BuildMock());
        _wardRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => _wards.FirstOrDefault(w => w.Id == id));

        _taxZoneRepo.Setup(r => r.GetQueryable()).Returns(() => _taxZones.BuildMock());
        _taxZoneRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => _taxZones.FirstOrDefault(z => z.Id == id));

        _propertyRepo.Setup(r => r.GetQueryable()).Returns(() => _properties.BuildMock());

        _rangeRepo.Setup(r => r.GetQueryable()).Returns(() => _ranges.BuildMock());
        _rangeRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => _ranges.FirstOrDefault(r => r.Id == id));
        _rangeRepo.Setup(r => r.AddAsync(It.IsAny<TaxZoningRangeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoningRangeEntity e, CancellationToken _) =>
            {
                e.Id = _nextRangeId++;
                _ranges.Add(e);
                return e;
            });
        _rangeRepo.Setup(r => r.UpdateAsync(It.IsAny<TaxZoningRangeEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        SetLanguage("en");
        _localizationService
            .Setup(s => s.GetTranslations(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns((string _, string _, IEnumerable<string> keys) => keys.ToDictionary(k => k, k => k));
    }

    /// <summary>Sets the language the mocked <see cref="IHttpContextAccessor"/> reports via HttpContext.Items, mirroring what LanguageMiddleware sets in production.</summary>
    private void SetLanguage(string language)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[HttpContextKeys.CurrentLanguage] = language;
        _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
    }

    private TaxZoningRangeService CreateService(int currentUserId = 99)
    {
        _currentUserService.Setup(s => s.GetCurrentUserId()).Returns(currentUserId);
        return new(
            _rangeRepo.Object,
            _propertyRepo.Object,
            _wardRepo.Object,
            _taxZoneRepo.Object,
            _unitOfWork.Object,
            _mapper,
            _currentUserService.Object,
            _logger.Object,
            _localizationService.Object,
            _httpContextAccessor.Object);
    }

    private void SeedWardsAndZone()
    {
        _wards.Add(new WardEntity { Id = 1, WardNo = "W1", ZoneId = 1, IsActive = true });
        _wards.Add(new WardEntity { Id = 2, WardNo = "W2", ZoneId = 1, IsActive = true });
        _taxZones.Add(new TaxZoneEntity { Id = 10, TaxZoneNo = "TZ10", Remark = "Zone 10", IsActive = true });
    }

    private void SeedProperties(int wardId, params string[] propertyNos)
    {
        foreach (var no in propertyNos)
        {
            _properties.Add(new PropertyEntity { Id = _properties.Count + 1, WardId = wardId, PropertyNo = no, TaxZoneId = 0, IsActive = true });
        }
    }

    [Fact]
    public async Task CreateAsync_WithValidRange_CreatesRangeAndAssignsProperties()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2", "A3", "A4", "A5");
        var service = CreateService();

        var dto = new CreateTaxZoningRangeDto
        {
            WardIds = new List<int> { 1 },
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A5",
            ZoneDescription = "Full ward A1 to A5 zoning assignment",
            IsActive = true
        };

        var result = await service.CreateAsync(dto);

        Assert.Single(result);
        Assert.Equal(1, result[0].WardId);
        Assert.Equal("W1", result[0].WardNo);
        Assert.Equal(10, result[0].TaxZoneId);
        Assert.False(result[0].AssignEntireWard);
        Assert.Equal("A1", result[0].FromPropertyNo);
        Assert.Equal("A5", result[0].ToPropertyNo);
        Assert.Single(_ranges);
        _unitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LeavingAGapAgainstExistingRange_SucceedsAndLeavesGapPending()
    {
        // Gaps between ranges are allowed — a ward is not required to be fully tiled at all times;
        // untouched property numbers simply show up as pending until a later range covers them.
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2", "A3", "A4", "A5");
        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A2",
            AssignEntireWard = false,
            ZoneDescription = "Existing assignment for A1 to A2",
            IsActive = true
        });

        var service = CreateService();
        var dto = new CreateTaxZoningRangeDto
        {
            WardIds = new List<int> { 1 },
            TaxZoneId = 10,
            FromPropertyNo = "A4",
            ToPropertyNo = "A5",
            ZoneDescription = "New assignment for A4 to A5 skipping A3",
            IsActive = true
        };

        var result = await service.CreateAsync(dto);

        Assert.Single(result);
        Assert.Equal("A4", result[0].FromPropertyNo);
        Assert.Equal("A5", result[0].ToPropertyNo);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleWards_ForcesWholeWardModeForEachWard()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2");
        SeedProperties(2, "B1", "B2");
        var service = CreateService();

        var dto = new CreateTaxZoningRangeDto
        {
            WardIds = new List<int> { 1, 2 },
            TaxZoneId = 10,
            AssignEntireWard = false, // should be forced to true because 2 wards are selected
            ZoneDescription = "Whole-ward zoning for two selected wards",
            IsActive = true
        };

        var result = await service.CreateAsync(dto);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.True(r.AssignEntireWard));
        Assert.Equal("A1", result.Single(r => r.WardId == 1).FromPropertyNo);
        Assert.Equal("B1", result.Single(r => r.WardId == 2).FromPropertyNo);
        Assert.Equal(new[] { 1, 2 }, result.Select(r => r.WardId).OrderBy(x => x));
        Assert.Equal(2, _ranges.Count);
        Assert.All(_ranges, r => Assert.True(r.AssignEntireWard));
    }

    [Fact]
    public async Task UpdateAsync_WithValidChanges_UpdatesRangeAndReassignsProperties()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2", "A3");
        var existing = new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A2",
            AssignEntireWard = false,
            ZoneDescription = "Original description text here",
            IsActive = true
        };
        _ranges.Add(existing);

        var service = CreateService(5);
        var dto = new UpdateTaxZoningRangeDto
        {
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A3",
            ZoneDescription = "Updated description covering full ward",
            IsActive = true
        };

        var result = await service.UpdateAsync(existing.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("A1", result!.FromPropertyNo);
        Assert.Equal("A3", result.ToPropertyNo);
        Assert.Equal("Updated description covering full ward", existing.ZoneDescription);
        Assert.Equal(5, existing.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNull()
    {
        SeedWardsAndZone();
        var service = CreateService(5);

        var result = await service.UpdateAsync(999, new UpdateTaxZoningRangeDto
        {
            WardId = 1,
            TaxZoneId = 10,
            AssignEntireWard = true,
            ZoneDescription = "Does not matter for this test case"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task BulkUpsertAsync_WithOneInvalidRow_PartiallySucceeds()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2");
        SeedProperties(2, "B1", "B2");
        var service = CreateService(3);

        var request = new BulkTaxZoningRangeRequest
        {
            Items = new List<CreateTaxZoningRangeDto>
            {
                new()
                {
                    WardIds = new List<int> { 1 },
                    TaxZoneId = 10,
                    FromPropertyNo = "A1",
                    ToPropertyNo = "A2",
                    ZoneDescription = "Valid row covering whole ward one",
                    IsActive = true
                },
                new()
                {
                    WardIds = new List<int> { 999 }, // non-existent ward -> invalid
                    TaxZoneId = 10,
                    FromPropertyNo = "X1",
                    ToPropertyNo = "X2",
                    ZoneDescription = "Invalid row referencing a bad ward",
                    IsActive = true
                }
            }
        };

        var result = await service.BulkUpsertAsync(request);

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Results);
    }

    [Fact]
    public async Task GetCoverageAsync_ReturnsCorrectCounts()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2", "A3");
        // Coverage is derived from active TaxZoningRange bounds, not PropertyMast.TaxZoneId directly.
        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A2",
            AssignEntireWard = false,
            ZoneDescription = "Covers A1 to A2 only",
            IsActive = true
        });
        // A3 left uncovered — no range touches it.

        var service = CreateService();
        var result = await service.GetCoverageAsync();

        Assert.Equal(3, result.TotalProperties);
        Assert.Equal(2, result.CoveredProperties);
        Assert.Equal(1, result.PendingProperties);
    }

    [Fact]
    public async Task GetWardAbstractAsync_ReturnsPerWardStats()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2");
        SeedProperties(2, "B1");
        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A1",
            AssignEntireWard = false,
            ZoneDescription = "Covers A1 only",
            IsActive = true
        });
        // Ward 2 (B1) has no range — stays uncovered.

        var service = CreateService();
        var result = await service.GetWardAbstractAsync(new WardAbstractQueryParameters { PageSize = -1 });

        Assert.Equal(2, result.TotalCount);
        var ward1 = result.Items.Single(w => w.WardId == 1);
        Assert.Equal(2, ward1.TotalProperties);
        Assert.Equal(1, ward1.CoveredProperties);
        Assert.Equal(1, ward1.PendingProperties);

        var ward2 = result.Items.Single(w => w.WardId == 2);
        Assert.Equal(1, ward2.TotalProperties);
        Assert.Equal(0, ward2.CoveredProperties);
    }

    [Fact]
    public async Task GetAllAsync_ForRangeRow_ReportsTotalPropertiesWithinBoundsIncludingPartitions()
    {
        SeedWardsAndZone();
        // A2 has two partitioned sub-records — both must count towards TotalProperties.
        _properties.Add(new PropertyEntity { Id = 1, WardId = 1, PropertyNo = "A1", TaxZoneId = 0, IsActive = true });
        _properties.Add(new PropertyEntity { Id = 2, WardId = 1, PropertyNo = "A2", PartitionNo = "1", TaxZoneId = 0, IsActive = true });
        _properties.Add(new PropertyEntity { Id = 3, WardId = 1, PropertyNo = "A2", PartitionNo = "2", TaxZoneId = 0, IsActive = true });
        _properties.Add(new PropertyEntity { Id = 4, WardId = 1, PropertyNo = "A3", TaxZoneId = 0, IsActive = true });
        _properties.Add(new PropertyEntity { Id = 5, WardId = 1, PropertyNo = "A4", TaxZoneId = 0, IsActive = true });
        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A2",
            ToPropertyNo = "A3",
            AssignEntireWard = false,
            ZoneDescription = "Covers A2 (both partitions) through A3",
            IsActive = true
        });

        var service = CreateService();
        var result = await service.GetAllAsync(new TaxZoningRangeQueryParameters());

        var row = Assert.Single(result.Items);
        Assert.Equal(3, row.TotalProperties); // A2 partition 1 + A2 partition 2 + A3
    }

    [Fact]
    public async Task GetAllAsync_ForEntireWardRow_ReportsTotalPropertiesAsEveryPropertyInWard()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2", "A3");
        _properties.Add(new PropertyEntity { Id = 100, WardId = 1, PropertyNo = "A3", PartitionNo = "1", TaxZoneId = 0, IsActive = true });
        SeedProperties(2, "B1"); // different ward — must not be counted
        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            AssignEntireWard = true,
            ZoneDescription = "Whole-ward assignment for ward 1",
            IsActive = true
        });

        var service = CreateService();
        var result = await service.GetAllAsync(new TaxZoningRangeQueryParameters());

        var row = Assert.Single(result.Items);
        Assert.True(row.AssignEntireWard);
        Assert.Equal(4, row.TotalProperties); // A1 + A2 + A3 + A3's partition, excluding ward 2
    }

    [Fact]
    public async Task ExportRangesToExcelAsync_ResolvesColumnHeadersFromLocalizationService()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1");
        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A1",
            AssignEntireWard = false,
            ZoneDescription = "Covers A1 only",
            IsActive = true
        });

        SetLanguage("hi");
        _localizationService
            .Setup(s => s.GetTranslations(
                "TaxZoningRangeExport",
                "hi",
                It.Is<IEnumerable<string>>(keys => keys.Contains("TaxZoningReport_Col_SrNo"))))
            .Returns(new Dictionary<string, string>
            {
                ["TaxZoningReport_Col_SrNo"] = "क्र.",
                ["TaxZoningReport_Col_PropertyNo"] = "संपत्ति क्र.",
                ["TaxZoningReport_Col_TotalProperties"] = "कुल संपत्ति",
                ["TaxZoningReport_Col_TaxZone"] = "वस्तीचा प्रकार",
                ["TaxZoningReport_Col_Address"] = "पता",
            });

        var service = CreateService();
        var bytes = await service.ExportRangesToExcelAsync(new TaxZoningRangeQueryParameters(), "Test ULB");

        using var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);

        // Row 4 = ward heading, row 5 = column header row 1 of 2 (A/D/E/F merged across two rows).
        Assert.Equal("क्र.", ws.Cell(5, 1).GetString());
        Assert.Equal("संपत्ति क्र.", ws.Cell(5, 2).GetString());
        Assert.Equal("कुल संपत्ति", ws.Cell(5, 4).GetString());
        Assert.Equal("वस्तीचा प्रकार", ws.Cell(5, 5).GetString());
        Assert.Equal("पता", ws.Cell(5, 6).GetString());

        _localizationService.Verify(s => s.GetTranslations(
            "TaxZoningRangeExport",
            "hi",
            It.Is<IEnumerable<string>>(keys => keys.Count() == 7)), Times.Once);
    }

    [Fact]
    public async Task ExportRangesToExcelAsync_WritesWardTotalAndGrandTotalRows()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1", "A2", "A3");
        SeedProperties(2, "B1", "B2");

        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A3",
            AssignEntireWard = false,
            ZoneDescription = "Ward 1 range",
            IsActive = true
        });
        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 2,
            TaxZoneId = 10,
            FromPropertyNo = "B1",
            ToPropertyNo = "B2",
            AssignEntireWard = false,
            ZoneDescription = "Ward 2 range",
            IsActive = true
        });

        var service = CreateService();
        var bytes = await service.ExportRangesToExcelAsync(new TaxZoningRangeQueryParameters(), "Test ULB");

        using var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);

        // Ward W1 (sorted first): heading row4, header rows 5-6, one data row 7, "total" row 8.
        Assert.Equal("total", ws.Cell(8, 1).GetString());
        Assert.Equal(3, ws.Cell(8, 4).GetDouble());

        // Blank spacer row 9, then ward W2: heading row 10, header rows 11-12, data row 13, "total" row 14.
        Assert.Equal("total", ws.Cell(14, 1).GetString());
        Assert.Equal(2, ws.Cell(14, 4).GetDouble());

        // Blank spacer row 15, then the grand total row 16 - properties covered across every ward.
        Assert.Equal("Grand Total", ws.Cell(16, 1).GetString());
        Assert.Equal(5, ws.Cell(16, 4).GetDouble());
    }

    [Fact]
    public async Task ExportRangesToExcelAsync_LocalizesTotalAndGrandTotalLabels()
    {
        SeedWardsAndZone();
        SeedProperties(1, "A1");
        _ranges.Add(new TaxZoningRangeEntity
        {
            Id = _nextRangeId++,
            WardId = 1,
            TaxZoneId = 10,
            FromPropertyNo = "A1",
            ToPropertyNo = "A1",
            AssignEntireWard = false,
            ZoneDescription = "Covers A1 only",
            IsActive = true
        });

        SetLanguage("mr");
        _localizationService
            .Setup(s => s.GetTranslations(
                "TaxZoningRangeExport",
                "mr",
                It.Is<IEnumerable<string>>(keys => keys.Contains("TaxZoningReport_Col_Total") && keys.Contains("TaxZoningReport_Col_GrandTotal"))))
            .Returns(new Dictionary<string, string>
            {
                ["TaxZoningReport_Col_Total"] = "कुल",
                ["TaxZoningReport_Col_GrandTotal"] = "कुल संख्या",
            });

        var service = CreateService();
        var bytes = await service.ExportRangesToExcelAsync(new TaxZoningRangeQueryParameters(), "Test ULB");

        using var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(bytes));
        var ws = workbook.Worksheet(1);

        // Single ward, single range: heading row4, header rows 5-6, data row 7, ward "total" row 8.
        Assert.Equal("कुल", ws.Cell(8, 1).GetString());
        // Blank spacer row 9, grand total row 10.
        Assert.Equal("कुल संख्या", ws.Cell(10, 1).GetString());
    }
}
