using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertySplit;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertySplitServiceTests
{
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockPropertyMapMasterRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyOldRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository;
    private readonly Mock<IRepository<MergeDetailEntity, int>> _mockMergeDetailRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PropertySplitService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertySplitService _service;

    public PropertySplitServiceTests()
    {
        _mockPropertyMapMasterRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockPropertyOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockSocietyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockMergeDetailRepository = new Mock<IRepository<MergeDetailEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PropertySplitService>>();
        _mockMapper = new Mock<IMapper>();

        _service = new PropertySplitService(
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyOldRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockRepository.Object,
            _mockWardRepository.Object,
            _mockSocietyRepository.Object,
            _mockMergeDetailRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRecords_WhenDataMatches()
    {
        // Arrange
        var queryParams = new PropertySplitQueryParameters { PropertyId = 10, PageNumber = 1, PageSize = 10 };

        var maps = new List<PropertyMapDetailEntity>
        {
            new PropertyMapDetailEntity { PropertyIdOld = 1, PropertyIdNew = 10, IsActive = true, Status = PropertyMapStatus.Draft }
        };

        var olds = new List<PropertyMastOldEntity>
        {
            new PropertyMastOldEntity { Id = 1, OldSocietyName = "Sunrise", OldPropertyNo = "101", OldWardNo = "W1" }
        };

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(maps.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(olds.BuildMock());

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var dto = result.Items.First();
        Assert.True(dto.Success);
        Assert.NotNull(dto.Data);
        var data = dto.Data as List<PropertyDetailsOldDto>;
        Assert.NotNull(data);
        Assert.Single(data);
        Assert.Equal(1, data.First().PropertyOldId);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoMatch()
    {
        // Arrange
        var queryParams = new PropertySplitQueryParameters { PropertyId = 99, PageNumber = 1, PageSize = 10 };

        var maps = new List<PropertyMapDetailEntity>();
        var olds = new List<PropertyMastOldEntity>();

        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(maps.BuildMock());
        _mockPropertyOldRepository.Setup(r => r.GetQueryable()).Returns(olds.BuildMock());

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var dto = result.Items.First();
        Assert.False(dto.Success);
        var data = dto.Data as List<PropertyDetailsOldDto>;
        Assert.NotNull(data);
        Assert.Empty(data!);
    }
}
