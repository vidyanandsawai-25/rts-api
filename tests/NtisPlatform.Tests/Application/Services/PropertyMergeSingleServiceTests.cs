using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyMergeSingle;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertyMergeSingleServiceTests
{
    private readonly Mock<IRepository<PropertyMapMasterEntity, int>> _mockPropertyMapMasterRepository;
    private readonly Mock<IRepository<PropertyMastOldEntity, int>> _mockPropertyOldRepository;
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockPropertyMapDetailRepository;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IRepository<WardEntity, int>> _mockWardRepository;
    private readonly Mock<IRepository<SocietyDetailsEntity, int>> _mockSocietyRepository;
    private readonly Mock<IRepository<MergeDetailEntity, int>> _mockMergeDetailRepository;
    private readonly Mock<IRepository<PropertyTypeMasterEntity, int>> _mockPropertyTypeRepository;
    private readonly Mock<IRepository<WingEntity, int>> _mockWingMasterRepository;
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _mockAssessmentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PropertyMergeSingleService>> _mockLogger;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyMergeSingleService _service;

    public PropertyMergeSingleServiceTests()
    {
        _mockPropertyMapMasterRepository = new Mock<IRepository<PropertyMapMasterEntity, int>>();
        _mockPropertyOldRepository = new Mock<IRepository<PropertyMastOldEntity, int>>();
        _mockPropertyMapDetailRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockWardRepository = new Mock<IRepository<WardEntity, int>>();
        _mockSocietyRepository = new Mock<IRepository<SocietyDetailsEntity, int>>();
        _mockMergeDetailRepository = new Mock<IRepository<MergeDetailEntity, int>>();
        _mockPropertyTypeRepository = new Mock<IRepository<PropertyTypeMasterEntity, int>>();
        _mockWingMasterRepository = new Mock<IRepository<WingEntity, int>>();
        _mockAssessmentRepository = new Mock<IRepository<PropertyAssessmentEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PropertyMergeSingleService>>();
        _mockMapper = new Mock<IMapper>();

        _service = new PropertyMergeSingleService(
            _mockPropertyMapMasterRepository.Object,
            _mockPropertyOldRepository.Object,
            _mockPropertyMapDetailRepository.Object,
            _mockRepository.Object,
            _mockWardRepository.Object,
            _mockSocietyRepository.Object,
            _mockMergeDetailRepository.Object,
            _mockPropertyTypeRepository.Object,
            _mockWingMasterRepository.Object,
            _mockAssessmentRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPropertyNotFound_WhenPropertyKeyIsMissing()
    {
        // Arrange
        var queryParams = new PropertyMergeSingleQueryParameters { PropertyId = 10, PageNumber = 1, PageSize = 10 };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(new List<SocietyDetailsEntity>().BuildMock());

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var dto = result.Items.First();
        Assert.False(dto.Success);
        Assert.Equal("Property not found", dto.Message);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRecords_WhenDataMatches()
    {
        // Arrange
        var queryParams = new PropertyMergeSingleQueryParameters { PropertyId = 10, PageNumber = 1, PageSize = 10 };

        var properties = new List<PropertyEntity>
        {
            new PropertyEntity { Id = 10, WardId = 1, PropertyNo = "101", PartitionNo = "A", IsActive = true, MarkedForDeletion = false, SocietyDetailId = 1 },
            new PropertyEntity { Id = 11, WardId = 1, PropertyNo = "101", PartitionNo = "B", IsActive = true, MarkedForDeletion = false, SocietyDetailId = 1 }
        };

        var societies = new List<SocietyDetailsEntity>
        {
            new SocietyDetailsEntity { Id = 1, SocietyName = "Sunrise", IsActive = true }
        };

        var wards = new List<WardEntity>
        {
            new WardEntity { Id = 1, WardNo = "W1", IsActive = true }
        };

        _mockRepository.Setup(r => r.GetQueryable()).Returns(properties.BuildMock());
        _mockSocietyRepository.Setup(r => r.GetQueryable()).Returns(societies.BuildMock());
        _mockWardRepository.Setup(r => r.GetQueryable()).Returns(wards.BuildMock());
        _mockPropertyTypeRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyTypeMasterEntity>().BuildMock());
        _mockWingMasterRepository.Setup(r => r.GetQueryable()).Returns(new List<WingEntity>().BuildMock());
        _mockPropertyMapDetailRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyMapDetailEntity>().BuildMock());
        _mockAssessmentRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyAssessmentEntity>().BuildMock());

        // Act
        var result = await _service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        var dto = result.Items.First();
        Assert.True(dto.Success);
        Assert.NotNull(dto.Data);
        var data = dto.Data as List<PropertyDetailsDto>;
        Assert.NotNull(data);
        Assert.Equal(2, data.Count);
        Assert.Contains(data, x => x.PropertyId == 10);
        Assert.Contains(data, x => x.PropertyId == 11);
    }
}
