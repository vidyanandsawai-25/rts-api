using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
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
            _logger.Object);
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
}
