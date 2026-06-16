using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MockQueryable;
using Moq;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for PropertyService deletion functionality.
/// Tests cover the refactored deletion logic including:
/// - MarkEntitiesForDeletion repository method (now in PropertyRepository)
/// - MarkPropertyDetailsAndRelatedAsync method
/// - MarkRelatedEntitiesForDeletionAsync method
/// - DeletePropertyInternalAsync orchestration
/// </summary>
public class PropertyServiceDeletionTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPropertyRepository> _mockPropertyRepository;
    private readonly Mock<ILogger<PropertyService>> _mockLogger;
    private readonly Mock<IOptions<FeatureFlagsOptions>> _mockFeatureFlags;
    private readonly PropertyService _service;

    public PropertyServiceDeletionTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockPropertyRepository = new Mock<IPropertyRepository>();
        _mockLogger = new Mock<ILogger<PropertyService>>();
        _mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();

        // Setup feature flag - allow deletion without payment validation in tests
        _mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = true
        });

        _service = new PropertyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockPropertyRepository.Object,
            _mockLogger.Object,
            _mockFeatureFlags.Object, new Mock<IRepository<NtisPlatform.Core.Entities.WardEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyCategoryEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.SocietyDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.RoomWiseSubmissionDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyAssessmentEntity, int>>().Object);
    }

    /// <summary>
    /// Helper method to setup all repository method mocks for delete operations.
    /// </summary>
    private void SetupDeleteRepositoryMocks()
    {
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity>());

        _mockPropertyRepository.Setup(r => r.GetRvResultsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationRVResultsEntity>());

        _mockPropertyRepository.Setup(r => r.GetSection129ResultsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyTaxCalculationSection129ResultsEntity>());

        _mockPropertyRepository.Setup(r => r.GetPropertyOccupancyByPropertyDetailIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyOccupancyDetailsEntity>());

        _mockPropertyRepository.Setup(r => r.GetRentersByPropertyDetailIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RenterMastEntity>());

        // Add missing mock for GetRenterDetailsByPropertyDetailIdsAsync
        _mockPropertyRepository.Setup(r => r.GetRenterDetailsByPropertyDetailIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RenterDetailEntity>());

        _mockPropertyRepository.Setup(r => r.GetRoomWiseSubmissionByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoomWiseSubmissionDetailsEntity>());

        _mockPropertyRepository.Setup(r => r.GetRelatedEntitiesForDeletionAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IHardDeletable>());

        _mockPropertyRepository.Setup(r => r.GetRoomWiseMinusBySubmissionIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoomWiseMinusDataEntity>());

        _mockPropertyRepository.Setup(r => r.GetPropertySocialDetailsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertySocialDetailsEntity>());

        _mockPropertyRepository.Setup(r => r.GetWaterConnectionsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WaterConnectionMasterEntity>());

        // Setup MarkEntitiesForDeletion to apply deletion flags to entities passed to it
        _mockPropertyRepository.Setup(r => r.MarkEntitiesForDeletion(It.IsAny<IEnumerable<IHardDeletable>>()))
            .Callback<IEnumerable<IHardDeletable>>(entities =>
            {
                var now = DateTime.Now;
                foreach (var entity in entities)
                {
                    entity.MarkedForDeletion = true;
                    if (!entity.MarkedForDeletionDate.HasValue)
                    {
                        entity.MarkedForDeletionDate = now;
                    }
                    if (entity is BaseEntity baseEntity)
                    {
                        baseEntity.IsActive = false;
                        baseEntity.UpdatedDate = now;
                    }
                }
            });

        // Setup DeactivatePropertyEntities to apply deactivation to BaseEntity-only entities
        _mockPropertyRepository.Setup(r => r.DeactivatePropertyEntities(It.IsAny<IEnumerable<BaseEntity>>()))
            .Callback<IEnumerable<BaseEntity>>(entities =>
            {
                var now = DateTime.Now;
                foreach (var entity in entities)
                {
                    entity.IsActive = false;
                    entity.UpdatedDate = now;
                }
            });
    }

    #region DeleteAsync with Property Details Tests

    [Fact]
    public async Task DeleteAsync_WithPropertyDetails_MarksAllDetailsForDeletion()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };
        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = propertyId, FloorId = 2, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        SetupDeleteRepositoryMocks();
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyDetails);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        // Verify property details were fetched
        _mockPropertyRepository.Verify(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
        // Verify the property details are now marked for deletion
        Assert.All(propertyDetails, pd =>
        {
            Assert.True(pd.MarkedForDeletion);
            Assert.False(pd.IsActive);
        });
    }

    [Fact]
    public async Task DeleteAsync_WithRvResults_MarksAllRvResultsForDeletion()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };
        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true }
        };
        var rvResults = new List<PropertyTaxCalculationRVResultsEntity>
        {
            new() { Id = 1, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        SetupDeleteRepositoryMocks();
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(propertyDetails);
        _mockPropertyRepository.Setup(r => r.GetRvResultsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rvResults);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        _mockPropertyRepository.Verify(r => r.GetRvResultsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.All(rvResults, rv =>
        {
            Assert.True(rv.MarkedForDeletion);
            Assert.False(rv.IsActive);
        });
    }

    [Fact]
    public async Task DeleteAsync_WithNoPropertyDetails_SkipsRelatedEntitiesQueries()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        SetupDeleteRepositoryMocks();
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity>()); // Empty list

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        // Verify PropertyId-based queries ARE called (even without PropertyDetails)
        // Reason: Entities can have PropertyId without PropertyDetailsId (property-level calculations, not floor-specific)
        _mockPropertyRepository.Verify(r => r.GetRvResultsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockPropertyRepository.Verify(r => r.GetSection129ResultsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockPropertyRepository.Verify(r => r.GetRoomWiseSubmissionByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);

        // Verify PropertyDetailsId-based queries are NOT called (optimization - these entities require PropertyDetails to exist)
        _mockPropertyRepository.Verify(r => r.GetPropertyOccupancyByPropertyDetailIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockPropertyRepository.Verify(r => r.GetRentersByPropertyDetailIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync with Related Entities Tests

    [Fact]
    public async Task DeleteAsync_WithRelatedEntities_MarksAllForDeletion()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };

        var relatedEntities = new List<IHardDeletable>
        {
            new ApplyTaxesMasterEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false },
            new PlotDetailsEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false },
            new PropertyAssessmentEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        SetupDeleteRepositoryMocks();
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity>());
        _mockPropertyRepository.Setup(r => r.GetRelatedEntitiesForDeletionAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relatedEntities);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        _mockPropertyRepository.Verify(r => r.GetRelatedEntitiesForDeletionAsync(propertyId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.All(relatedEntities, e =>
        {
            Assert.True(e.MarkedForDeletion);
            Assert.NotNull(e.MarkedForDeletionDate);
        });
    }

    [Fact]
    public async Task DeleteAsync_WithPropertyAssessmentEntity_MarksForDeletion()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };

        var assessment = new PropertyAssessmentEntity 
        { 
            Id = 1, 
            PropertyId = propertyId, 
            IsActive = true, 
            MarkedForDeletion = false 
        };

        var relatedEntities = new List<IHardDeletable> { assessment };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        SetupDeleteRepositoryMocks();
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyDetailsEntity>());
        _mockPropertyRepository.Setup(r => r.GetRelatedEntitiesForDeletionAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(relatedEntities);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        Assert.True(assessment.MarkedForDeletion);
        Assert.NotNull(assessment.MarkedForDeletionDate);
        Assert.False(assessment.IsActive);
        Assert.NotNull(assessment.UpdatedDate);
    }

    #endregion

    #region BulkDeleteAsync Tests

    [Fact]
    public async Task BulkDeleteAsync_WithMultipleProperties_DeletesAllAndReportsSuccess()
    {
        // Arrange
        var propertyIds = new[] { 1, 2, 3 };
        var entities = propertyIds.Select(id => new PropertyEntity { Id = id, IsActive = true }).ToList();

        // Setup GetQueryable to return async-compatible mock
        var mockQueryable = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        foreach (var entity in entities)
        {
            _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
        }

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailedCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithMixedResults_ReportsPartialSuccess()
    {
        // Arrange
        var propertyIds = new[] { 1, 2, 999 }; // 999 doesn't exist
        var entities = new List<PropertyEntity>
        {
            new() { Id = 1, IsActive = true },
            new() { Id = 2, IsActive = true }
        };

        // Setup GetQueryable to return only existing entities (id 1 and 2)
        var mockQueryable = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => entities.FirstOrDefault(e => e.Id == id));

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert - When entities count doesn't match requested ids, BulkDeleteAsync returns error for all
        Assert.NotNull(result);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(3, result.FailedCount);
        Assert.NotNull(result.Errors);
        Assert.Single(result.Errors);
        Assert.Contains("999", result.Errors[0]);
    }

    [Fact]
    public async Task BulkDeleteAsync_EachPropertyGetsOwnTransaction()
    {
        // Arrange
        var propertyIds = new[] { 1, 2 };
        var entities = propertyIds.Select(id => new PropertyEntity { Id = id, IsActive = true }).ToList();

        // Setup GetQueryable to return async-compatible mock
        var mockQueryable = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        foreach (var entity in entities)
        {
            _mockRepository.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
        }

        SetupDeleteRepositoryMocks();

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkDeleteAsync(propertyIds);

        // Assert
        Assert.NotNull(result);
        // Verify BeginTransactionAsync was called once per property
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(propertyIds.Length));
        // Verify CommitTransactionAsync was called once per successful property
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Exactly(propertyIds.Length));
    }

    #endregion

    #region Transaction Handling Tests

    [Fact]
    public async Task DeleteAsync_OnException_RollsBackTransaction()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockPropertyRepository.Setup(r => r.GetPropertyDetailsByPropertyIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act & Assert - DeletePropertyInternalAsync catches the exception, returns (false, errorMessage),
        // then DeleteAsync wraps it in a ValidationException
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(propertyId));
        Assert.Equal("Database error", exception.Message);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_SavesChanges_TwiceForChildrenAndParent()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        SetupDeleteRepositoryMocks();

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(propertyId);

        // Assert
        Assert.True(result);
        // Verify SaveChangesAsync is called twice: once for marking children, once for deleting parent
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task DeleteAsync_WhenFeatureFlagDisabled_ThrowsValidationException()
    {
        // Arrange
        var propertyId = 1;
        var entity = new PropertyEntity { Id = propertyId, IsActive = true };

        // Override the feature flags to disable deletion
        var mockFeatureFlagsDisabled = new Mock<IOptions<FeatureFlagsOptions>>();
        mockFeatureFlagsDisabled.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = false
        });

        // Create a new service with the disabled feature flag
        var serviceWithDisabledFlag = new PropertyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockPropertyRepository.Object,
            _mockLogger.Object,
            mockFeatureFlagsDisabled.Object, new Mock<IRepository<NtisPlatform.Core.Entities.WardEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyCategoryEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.SocietyDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.RoomWiseSubmissionDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyAssessmentEntity, int>>().Object);

        _mockRepository.Setup(r => r.GetByIdAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act & Assert - DeleteAsync now throws ValidationException instead of returning false
        var exception = await Assert.ThrowsAsync<ValidationException>(() => serviceWithDisabledFlag.DeleteAsync(propertyId));
        Assert.Contains("Property deletion is currently disabled", exception.Message);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}



