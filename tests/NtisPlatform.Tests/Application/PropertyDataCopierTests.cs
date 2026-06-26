using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class PropertyDataCopierTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockPropertyRepository;
    private readonly Mock<IRepository<PropertyDetailsEntity, int>> _mockPropertyDetailsRepository;
    private readonly Mock<IRepository<PropertyAssessmentEntity, int>> _mockPropertyAssessmentRepository;
    private readonly Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>> _mockRoomWiseSubmissionRepository;
    private readonly Mock<IRepository<RoomWiseMinusDataEntity, int>> _mockRoomWiseMinusDataRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PropertyDataCopier>> _mockLogger;
    private readonly PropertyDataCopier _copier;

    public PropertyDataCopierTests()
    {
        _mockPropertyRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockPropertyDetailsRepository = new Mock<IRepository<PropertyDetailsEntity, int>>();
        _mockPropertyAssessmentRepository = new Mock<IRepository<PropertyAssessmentEntity, int>>();
        _mockRoomWiseSubmissionRepository = new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();
        _mockRoomWiseMinusDataRepository = new Mock<IRepository<RoomWiseMinusDataEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PropertyDataCopier>>();

        // Setup unit of work saves
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Setup empty queries for secondary repositories to prevent failures
        _mockPropertyDetailsRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyDetailsEntity>().BuildMock());
        _mockPropertyAssessmentRepository.Setup(r => r.GetQueryable())
            .Returns(new List<PropertyAssessmentEntity>().BuildMock());
        _mockRoomWiseSubmissionRepository.Setup(r => r.GetQueryable())
            .Returns(new List<RoomWiseSubmissionDetailsEntity>().BuildMock());
        _mockRoomWiseMinusDataRepository.Setup(r => r.GetQueryable())
            .Returns(new List<RoomWiseMinusDataEntity>().BuildMock());

        _copier = new PropertyDataCopier(
            _mockPropertyRepository.Object,
            _mockPropertyDetailsRepository.Object,
            _mockPropertyAssessmentRepository.Object,
            _mockRoomWiseSubmissionRepository.Object,
            _mockRoomWiseMinusDataRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CopyPropertyDataAsync_BothNullFlatOrShopNo_KeepsNull()
    {
        // Arrange
        int mainPropertyId = 1;
        var combineIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity { Id = mainPropertyId, FlatOrShopNo = null, IsActive = true };
        var combinedProperties = new List<PropertyEntity>
        {
            new() { Id = 2, FlatOrShopNo = null, IsActive = true, MarkedForDeletion = false },
            new() { Id = 3, FlatOrShopNo = "", IsActive = true, MarkedForDeletion = false }
        };

        _mockPropertyRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);
        _mockPropertyRepository.Setup(r => r.GetQueryable())
            .Returns(combinedProperties.BuildMock());

        // Act
        await _copier.CopyPropertyDataAsync(mainPropertyId, combineIds, createdBy: 1, mergeOwnerNames: false, propertyTypeId: null, default);

        // Assert
        Assert.Null(mainProperty.FlatOrShopNo);
        _mockPropertyRepository.Verify(r => r.UpdateAsync(mainProperty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CopyPropertyDataAsync_MainNullCombinedHasValues_UpdatesMain()
    {
        // Arrange
        int mainPropertyId = 1;
        var combineIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity { Id = mainPropertyId, FlatOrShopNo = null, IsActive = true };
        var combinedProperties = new List<PropertyEntity>
        {
            new() { Id = 2, FlatOrShopNo = "101", IsActive = true, MarkedForDeletion = false },
            new() { Id = 3, FlatOrShopNo = "102", IsActive = true, MarkedForDeletion = false }
        };

        _mockPropertyRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);
        _mockPropertyRepository.Setup(r => r.GetQueryable())
            .Returns(combinedProperties.BuildMock());

        // Act
        await _copier.CopyPropertyDataAsync(mainPropertyId, combineIds, createdBy: 1, mergeOwnerNames: false, propertyTypeId: null, default);

        // Assert
        Assert.Equal("101, 102", mainProperty.FlatOrShopNo);
        _mockPropertyRepository.Verify(r => r.UpdateAsync(mainProperty, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CopyPropertyDataAsync_MainHasValueCombinedNull_KeepsMain()
    {
        // Arrange
        int mainPropertyId = 1;
        var combineIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity { Id = mainPropertyId, FlatOrShopNo = "101", IsActive = true };
        var combinedProperties = new List<PropertyEntity>
        {
            new() { Id = 2, FlatOrShopNo = null, IsActive = true, MarkedForDeletion = false },
            new() { Id = 3, FlatOrShopNo = " ", IsActive = true, MarkedForDeletion = false }
        };

        _mockPropertyRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);
        _mockPropertyRepository.Setup(r => r.GetQueryable())
            .Returns(combinedProperties.BuildMock());

        // Act
        await _copier.CopyPropertyDataAsync(mainPropertyId, combineIds, createdBy: 1, mergeOwnerNames: false, propertyTypeId: null, default);

        // Assert
        Assert.Equal("101", mainProperty.FlatOrShopNo);
    }

    [Fact]
    public async Task CopyPropertyDataAsync_MainAndCombinedHaveValues_MergesDistinctCommaSeparated()
    {
        // Arrange
        int mainPropertyId = 1;
        var combineIds = new List<int> { 2, 3 };

        var mainProperty = new PropertyEntity { Id = mainPropertyId, FlatOrShopNo = "101, 102", IsActive = true };
        var combinedProperties = new List<PropertyEntity>
        {
            new() { Id = 2, FlatOrShopNo = "102, 103", IsActive = true, MarkedForDeletion = false },
            new() { Id = 3, FlatOrShopNo = "104", IsActive = true, MarkedForDeletion = false }
        };

        _mockPropertyRepository.Setup(r => r.GetByIdAsync(mainPropertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mainProperty);
        _mockPropertyRepository.Setup(r => r.GetQueryable())
            .Returns(combinedProperties.BuildMock());

        // Act
        await _copier.CopyPropertyDataAsync(mainPropertyId, combineIds, createdBy: 1, mergeOwnerNames: false, propertyTypeId: null, default);

        // Assert
        Assert.Equal("101, 102, 103, 104", mainProperty.FlatOrShopNo);
    }
}
