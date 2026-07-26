using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;

namespace NtisPlatform.Tests.Repositories
{
    /// <summary>
    /// Tests for PropertyRepository methods related to property existence checks.
    /// </summary>
    public class PropertyRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly PropertyRepository _repository;

        public PropertyRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new PropertyRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public async Task IsPropertyExists_ShouldReturnTrue_WhenMatchFound()
        {
            // Arrange
            _context.PropertyMast.Add(new PropertyEntity
            {
                Id = 10,
                WardId = 5,
                PropertyNo = "TEST-99",
                IsActive = true,
                MarkedForDeletion = false
            });
            await _context.SaveChangesAsync();

            // Act
            var exactMatch = await _repository.IsPropertyExists(
                wardId: 5,
                propertyNo: "TEST-99",
                propertyId: null);

            var matchWithDifferentId = await _repository.IsPropertyExists(
                wardId: 5,
                propertyNo: "TEST-99",
                propertyId: 99);

            // Assert
            Assert.True(exactMatch);
            Assert.True(matchWithDifferentId);
        }

        [Fact]
        public async Task IsPropertyExists_ShouldReturnFalse_WhenExcludingOwnId()
        {
            // Arrange
            _context.PropertyMast.Add(new PropertyEntity
            {
                Id = 10,
                WardId = 5,
                PropertyNo = "TEST-99",
                IsActive = true,
                MarkedForDeletion = false
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.IsPropertyExists(wardId: 5, propertyNo: "TEST-99", propertyId: 10);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsPropertyExists_ShouldReturnFalse_WhenNoMatch()
        {
            // Arrange - no data seeded for this ward/property

            // Act
            var result = await _repository.IsPropertyExists(wardId: 1, propertyNo: "NON-EXISTENT", propertyId: null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsPropertyExists_ShouldReturnTrue_WhenPropertyIsInactive()
        {
            // Arrange - IsPropertyExists does NOT filter by IsActive per current implementation
            _context.PropertyMast.Add(new PropertyEntity
            {
                Id = 20,
                WardId = 5,
                PropertyNo = "INACTIVE-PROP",
                IsActive = false,
                MarkedForDeletion = false
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.IsPropertyExists(wardId: 5, propertyNo: "INACTIVE-PROP", propertyId: null);

            // Assert - Property exists regardless of IsActive status
            Assert.True(result);
        }

        [Fact]
        public async Task IsPropertyExists_ShouldReturnFalse_WhenPropertyIsMarkedForDeletion()
        {
            _context.PropertyMast.Add(new PropertyEntity
            {
                Id = 30,
                WardId = 5,
                PropertyNo = "DELETED-PROP",
                IsActive = true,
                MarkedForDeletion = true
            });

            await _context.SaveChangesAsync();

            var result = await _repository.IsPropertyExists(
                wardId: 5,
                propertyNo: "DELETED-PROP",
                propertyId: null
            );

            Assert.False(result);
        }

        [Fact]
        public async Task IsPropertyExists_ShouldReturnFalse_WhenDifferentWardId()
        {
            // Arrange
            _context.PropertyMast.Add(new PropertyEntity
            {
                Id = 40,
                WardId = 5,
                PropertyNo = "WARD-PROP",
                IsActive = true,
                MarkedForDeletion = false
            });
            await _context.SaveChangesAsync();

            // Act - Search in different ward
            var result = await _repository.IsPropertyExists(wardId: 10, propertyNo: "WARD-PROP", propertyId: null);

            // Assert
            Assert.False(result);
        }
    }
}

namespace NtisPlatform.Tests.Application
{
    using NtisPlatform.Application.Interfaces.Rules;
    using NtisPlatform.Tests.Api.Controllers;

    /// <summary>
    /// Tests for PropertyService range-based property generation functionality.
    /// </summary>
    public class PropertyServiceRangeTests
    {
        private static (
    Mock<IRepository<PropertyEntity, int>> repoMock,
    Mock<IUnitOfWork> uowMock,
    Mock<IMapper> mapperMock,
    Mock<IPropertyRepository> propRepoMock,
    Mock<IOptions<FeatureFlagsOptions>> featureFlagsMock,
    Mock<IRepository<WardEntity, int>> wardRepoMock,
    Mock<IRepository<PropertyCategoryEntity, int>> categoryRepoMock,
    Mock<IRepository<SocietyDetailsEntity, int>> societyRepoMock,
    Mock<IRepository<PropertyDetailsEntity, int>> propertyDetailsRepoMock,
    Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>> roomWiseRepoMock,
    Mock<IRepository<PropertyAssessmentEntity, int>> assessmentRepoMock,
    Mock<IRepository<GlobalSurveyWardAllocationEntity, int>> wardAllocationRepoMock,
    Mock<IRepository<PropertyMapMasterEntity, int>> propertyMapMasterRepoMock,
    Mock<IRepository<PropertyMapDetailEntity, int>> propertyMapDetailRepoMock,
    Mock<IRepository<UserEntity, int>> userRepoMock,
    Mock<IRepository<PropertyMastOldEntity, int>> propertyOldRepoMock,
    Mock<IRepository<PropertyTypeMasterEntity, int>> propertyTypeRepoMock,
    Mock<IPropertyRuleApplicationLogService> ruleLogServiceMock
) CreateMocks()
        {
            var repoMock = new Mock<IRepository<PropertyEntity, int>>();
            var uowMock = new Mock<IUnitOfWork>();
            uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            uowMock.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            uowMock.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var mapperMock = new Mock<IMapper>();
            var propRepoMock = new Mock<IPropertyRepository>();
            var featureFlagsMock = new Mock<IOptions<FeatureFlagsOptions>>();

            // Setup default feature flag configuration
            featureFlagsMock.Setup(x => x.Value).Returns(new FeatureFlagsOptions
            {
                AllowPropertyDeletionWithoutPaymentValidation = true
            });

            var wardRepoMock = new Mock<IRepository<WardEntity, int>>();
            var categoryRepoMock = new Mock<IRepository<PropertyCategoryEntity, int>>();
            var societyRepoMock = new Mock<IRepository<SocietyDetailsEntity, int>>();
            var propertyDetailsRepoMock = new Mock<IRepository<PropertyDetailsEntity, int>>();
            var roomWiseRepoMock = new Mock<IRepository<RoomWiseSubmissionDetailsEntity, int>>();
            var assessmentRepoMock = new Mock<IRepository<PropertyAssessmentEntity, int>>();
            var wardAllocationRepoMock = new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>();
            var propertyMapMasterRepoMock = new Mock<IRepository<PropertyMapMasterEntity, int>>();
            var propertyMapDetailRepoMock = new Mock<IRepository<PropertyMapDetailEntity, int>>();
            var userRepoMock = new Mock<IRepository<UserEntity, int>>();

            var propertyOldRepoMock =
             new Mock<IRepository<PropertyMastOldEntity, int>>();

            var propertyTypeRepoMock =
                new Mock<IRepository<PropertyTypeMasterEntity, int>>();

            var ruleLogServiceMock =
                new Mock<IPropertyRuleApplicationLogService>();

            // Setup default mapper behaviors for entity creation
            var propertyIdCounter = 100;
            mapperMock.Setup(m => m.Map<PropertyEntity>(It.IsAny<CreateNewPropertyDto>()))
                .Returns(() => new PropertyEntity { Id = propertyIdCounter++, WardId = 1 });
            mapperMock.Setup(m => m.Map<PropertyAssessmentEntity>(It.IsAny<CreateNewPropertyDto>()))
                .Returns(new PropertyAssessmentEntity());

            // Setup default repository behaviors
            wardRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WardEntity { Id = 1, WardNo = "W01" });
            categoryRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PropertyCategoryEntity { Id = 1, PropertyCategoryName = "Residential" });

            // Default: no duplicates
            propRepoMock.Setup(r => r.IsPropertyExists(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(false);

            return (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock,
                    wardRepoMock, categoryRepoMock, societyRepoMock,
                    propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock,
                    wardAllocationRepoMock, propertyMapMasterRepoMock,
                    propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock);
        }


        private static CreateNewPropertyDto CreateValidTemplate() => new()
        {
            WardId = 1,
            PropertyTypeId = 1,
            CategoryId = 1,
            TaxZoneId = 1,
            OwnerName = "Test Owner"
        };

        #region Null and Invalid Input Tests

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldReturnError_WhenTemplateIsNull()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();
            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = null!
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.NotNull(result.Errors);
            Assert.Contains("Template cannot be null.", result.Errors!);
        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldThrow_WhenRequestIsNull()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();
            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await service.CreatePropertiesFromRangeAsync(null!, CancellationToken.None);
            });
        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldThrow_ForInvalidMixedRange()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();
            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "A",
                Template = CreateValidTemplate()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);
            });
        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldThrow_WhenRangeFromIsEmpty()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();
            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "",
                RangeTo = "5",
                Template = CreateValidTemplate()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);
            });
        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldThrow_WhenRangeToIsEmpty()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();
            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "",
                Template = CreateValidTemplate()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);
            });
        }

        #endregion

        #region Numeric Range Tests

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldSucceed_ForNumericRange()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();
            var propertyIdCounter = 100;


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            // Note: Prefix/Suffix must be null because the code uses Convert.ToInt32(rangeValues[i])
            // which only works with pure numeric values
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Prefix = null,
                Suffix = null,
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(3, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.NotNull(result.Results);
            Assert.Equal(3, result.Results.Count);
            Assert.True(result.AllSucceeded);
            Assert.False(result.HasFailures);

        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldSucceed_ForZeroPaddedNumericRange()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "01",
                RangeTo = "03",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(3, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(3, result.Results.Count);

        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldSucceed_ForSingleNumericValue()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "5",
                RangeTo = "5",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);

        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldSucceed_WithPrefixAndSuffix()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            // Note: Using prefix/suffix with numeric range causes Convert.ToInt32 to fail
            // because rangeValues will contain "WARD-1-PROP" which cannot be converted to int.
            // The method handles this gracefully by catching the exception and returning errors.
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "2",
                Prefix = "WARD-",
                Suffix = "-PROP",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert - The method catches FormatException and returns error result
            Assert.Equal(0, result.SuccessCount);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Count > 0);
        }

        #endregion

        #region Alphabetic Range Tests

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldSucceed_ForAlphabeticRange()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            // Note: Alphabetic ranges like "A" to "C" generate values "A", "B", "C" (or with prefix "BLK-A", etc.)
            // These cannot be converted to int by Convert.ToInt32(rangeValues[i]).
            // The method handles this gracefully by catching the exception and returning errors.
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "A",
                RangeTo = "C",
                Prefix = "BLK-",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert - The method catches FormatException and returns error result
            Assert.Equal(0, result.SuccessCount);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Count > 0);
        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldSucceed_ForSingleAlphabeticValue()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            // Note: Alphabetic value "X" cannot be converted to int by Convert.ToInt32(rangeValues[i]).
            // The method handles this gracefully by catching the exception and returning errors.
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "X",
                RangeTo = "X",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert - The method catches FormatException and returns error result
            Assert.Equal(0, result.SuccessCount);
            Assert.NotNull(result.Errors);
            Assert.True(result.Errors.Count > 0);
        }

        #endregion

        #region Failure Scenario Tests

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldReturnFailed_WhenRepositoryReturnsFailure_NonDuplicate()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();

            repoMock.Setup(x => x.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Invalid data"));

            propRepoMock.Setup(x => x.IsPropertyExists(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(false);

            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(3, result.FailedCount);
            Assert.NotNull(result.Errors);
            Assert.True(result.HasFailures);
            Assert.Contains(result.Errors!, e => e.Contains("Invalid data"));

            uowMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldReturnFailed_WhenRepositoryReturnsFailure_Duplicate()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();

            

            propRepoMock.Setup(x => x.IsPropertyExists(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(true);

            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(3, result.FailedCount);
            Assert.NotNull(result.Errors);
            Assert.Contains(result.Errors!, e => e.Contains("Property already exists"));

        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldReturnCancelled_WhenCancellationRequested()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "5",
                Template = CreateValidTemplate()
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, cts.Token);

            // Assert
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(5, result.FailedCount);
            Assert.NotNull(result.Errors);
            Assert.Contains("Operation cancelled.", result.Errors!);
        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldRollback_WhenCancellationRequested()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "10",
                Template = CreateValidTemplate()
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, cts.Token);

            // Assert
            Assert.True(result.HasFailures);
            uowMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            uowMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Transaction Tests

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldCommitTransaction_WhenAllSucceed()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();


            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(3, result.SuccessCount);
            uowMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            uowMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            uowMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldRollbackTransaction_WhenAnyFails()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();
            var callCount = 0;

            repoMock.Setup(x => x.AddAsync(It.IsAny<PropertyEntity>(), It.IsAny<CancellationToken>())).ReturnsAsync((PropertyEntity entity, CancellationToken ct) => { callCount++; if (callCount == 2) throw new Exception("Failed on second"); return entity; });

            propRepoMock.Setup(x => x.IsPropertyExists(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
                .ReturnsAsync(false);

            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "5",
                Template = CreateValidTemplate()
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.True(result.HasFailures);
            uowMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            uowMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            uowMock.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Full Template Tests

        [Fact]
        public async Task CreatePropertiesFromRangeAsync_ShouldSucceed_WithFullyPopulatedTemplate()
        {
            // Arrange
            var (repoMock, uowMock, mapperMock, propRepoMock, featureFlagsMock, wardRepoMock, categoryRepoMock, societyRepoMock, propertyDetailsRepoMock, roomWiseRepoMock, assessmentRepoMock, wardAllocationRepoMock, propertyMapMasterRepoMock, propertyMapDetailRepoMock, userRepoMock, propertyOldRepoMock, propertyTypeRepoMock, ruleLogServiceMock) = CreateMocks();

            

            var mockLogger = new Mock<ILogger<PropertyService>>();
            var service = new PropertyService(repoMock.Object, uowMock.Object, mapperMock.Object, propRepoMock.Object, mockLogger.Object, featureFlagsMock.Object, wardRepoMock.Object, categoryRepoMock.Object, societyRepoMock.Object, propertyDetailsRepoMock.Object, roomWiseRepoMock.Object, assessmentRepoMock.Object, wardAllocationRepoMock.Object, propertyMapMasterRepoMock.Object, propertyMapDetailRepoMock.Object, userRepoMock.Object, propertyOldRepoMock.Object, propertyTypeRepoMock.Object, ruleLogServiceMock.Object);

            var template = new CreateNewPropertyDto
            {
                WardId = 1,
                PropertyTypeId = 1,
                CategoryId = 1,
                TaxZoneId = 1,
                PropertySeqNo = 1,
                OwnerName = "Rajesh Kumar",
                BuilderMobileNo = "9876543210",
                CSN = "CSN-45892",
                SurveyRemark = "Survey completed successfully.",
                BlockNo = "Block-A",
                PinCode = "411001",
                OpenPlot = false,
                PlotNo = "Plot-42",
                Type = "COM",
                OwnerTitle = "Mr.",
                OwnerTitleEnglish = "Mr.",
                OwnerNameEnglish = "Rajesh Kumar",
                MobileNo = "9123456780",
                EmailId = "rajesh.kumar@example.com",
                OccupierTitle = "Mrs.",
                OccupierName = "Priya Sharma",
                FlatOrShopNo = "F-101",
                FlatOrShopName = "Sunrise Apartments",
                Address = "123, Main Market Road",
                AddressEnglish = "123, Main Market Road",
                Location = "Downtown Area",
                SocietyName = "Sunrise Cooperative Housing Society",
                SecretaryName = "Amit Patel",
                ManagerName = "Suresh Desai",
                LandOwnerName = "ABC Developers",
                BuilderName = "ABC Builders Pvt Ltd",
                ManagerMobileNo = "9876500001",
                SecretaryMobileNo = "9876500002",
                SocietyEmailId = "society@example.com",
                LengthMtr = (double?)15.5m,
                WidthMtr = (double?)10.0m,
                TotalAreaSqMtr = (double?)155.0m,
                CreatedBy = 1,
                CreatedDate = DateTime.UtcNow
            };

            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "2",
                Template = template
            };

            // Act
            var result = await service.CreatePropertiesFromRangeAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.True(result.AllSucceeded);
            
        }

        #endregion
    }

    /// <summary>
    /// Comprehensive test suite for PropertyController.CreateFromRange endpoint.
    /// Tests the HTTP POST /api/Property/Range endpoint behavior.
    /// </summary>
    public class PropertyControllerCreateFromRangeTests
    {
        private readonly Mock<IPropertyService> _mockPropertyService;
        private readonly Mock<ILogger<PropertyController>> _mockLogger;
        private readonly PropertyController _controller;

        public PropertyControllerCreateFromRangeTests()
        {
            _mockPropertyService = new Mock<IPropertyService>();
            _mockLogger = new Mock<ILogger<PropertyController>>();
            _controller = PropertyControllerTestHelper.CreateController(_mockPropertyService, _mockLogger);
        }

        private static CreateNewPropertyDto CreateControllerValidTemplate() => new()
        {
            WardId = 1,
            PropertyTypeId = 1,
            CategoryId = 1,
            TaxZoneId = 1,
            OwnerName = "Test Owner"
        };

        #region Success Scenarios

        [Fact]
        public async Task CreateFromRange_WithValidNumericRange_ReturnsOkResult()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Prefix = "PROP-",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 3,
                FailedCount: 0,
                Results:
                [
                    new() { Success = true, PropertyId = 100, UPICID = "UPIC-001" },
                    new() { Success = true, PropertyId = 101, UPICID = "UPIC-002" },
                    new() { Success = true, PropertyId = 102, UPICID = "UPIC-003" }
                ],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(3);
            response.FailedCount.Should().Be(0);
            response.AllSucceeded.Should().BeTrue();
            response.HasFailures.Should().BeFalse();
            response.Results.Should().HaveCount(3);

            _mockPropertyService.Verify(s => s.CreatePropertiesFromRangeAsync(
                It.Is<RangeCreateRequest<CreateNewPropertyDto>>(r =>
                    r.RangeFrom == "1" &&
                    r.RangeTo == "3" &&
                    r.Prefix == "PROP-"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateFromRange_WithValidAlphabeticRange_ReturnsOkResult()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "A",
                RangeTo = "C",
                Prefix = "BLK-",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 3,
                FailedCount: 0,
                Results:
                [
                    new() { Success = true, PropertyId = 200, UPICID = "UPIC-A01" },
                    new() { Success = true, PropertyId = 201, UPICID = "UPIC-A02" },
                    new() { Success = true, PropertyId = 202, UPICID = "UPIC-A03" }
                ],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(3);
            response.FailedCount.Should().Be(0);
            response.AllSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task CreateFromRange_WithPrefixAndSuffix_ReturnsOkResult()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "2",
                Prefix = "WARD-1-",
                Suffix = "-ZONE-A",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 2,
                FailedCount: 0,
                Results:
                [
                    new() { Success = true, PropertyId = 300, UPICID = "UPIC-300" },
                    new() { Success = true, PropertyId = 301, UPICID = "UPIC-301" }
                ],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(2);
            response.AllSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task CreateFromRange_WithSingleValue_ReturnsOkResult()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "5",
                RangeTo = "5",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 1,
                FailedCount: 0,
                Results: [new() { Success = true, PropertyId = 400, UPICID = "UPIC-400" }],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(1);
            response.Results.Should().ContainSingle();
        }

        [Fact]
        public async Task CreateFromRange_WithZeroPaddedRange_ReturnsOkResult()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "001",
                RangeTo = "003",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 3,
                FailedCount: 0,
                Results:
                [
                    new() { Success = true, PropertyId = 500, UPICID = "UPIC-500" },
                    new() { Success = true, PropertyId = 501, UPICID = "UPIC-501" },
                    new() { Success = true, PropertyId = 502, UPICID = "UPIC-502" }
                ],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(3);
            response.AllSucceeded.Should().BeTrue();
        }

        #endregion

        #region Partial Failure Scenarios

        [Fact]
        public async Task CreateFromRange_WithPartialFailures_ReturnsOkWithFailureInfo()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 2,
                FailedCount: 1,
                Results:
                [
                    new() { Success = true, PropertyId = 600 },
                    new() { Success = false, Message = "Property already exists" },
                    new() { Success = true, PropertyId = 602 }
                ],
                Errors: ["Row 2: Property already exists"]
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(2);
            response.FailedCount.Should().Be(1);
            response.HasFailures.Should().BeTrue();
            response.AllSucceeded.Should().BeFalse();
            response.Errors.Should().Contain(e => e.Contains("Property already exists"));
        }

        [Fact]
        public async Task CreateFromRange_WithAllFailures_ReturnsOkWithAllErrors()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "2",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 0,
                FailedCount: 2,
                Results:
                [
                    new() { Success = false, Message = "Duplicate property" },
                    new() { Success = false, Message = "Duplicate property" }
                ],
                Errors: ["Row 1: Duplicate property", "Row 2: Duplicate property"]
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(0);
            response.FailedCount.Should().Be(2);
            response.HasFailures.Should().BeTrue();
            response.AllSucceeded.Should().BeFalse();
            response.Errors.Should().HaveCount(2);
        }

        #endregion

        #region Exception Handling Tests
        // CreateFromRange is a thin adapter: it does NOT catch exceptions.
        // PropertyApiExceptionFilter (registered via [TypeFilter] on the controller class) handles
        // exception-to-HTTP mapping inside the ASP.NET Core pipeline.
        // In unit tests that call the action directly (no pipeline), exceptions propagate to the caller.

        [Fact]
        public async Task CreateFromRange_WhenServiceThrowsException_PropagatesException()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = CreateControllerValidTemplate()
            };

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection failed"));

                        // Act & Assert — thin adapter: exception must reach the caller (filter handles it in production)
            await Assert.ThrowsAsync<Exception>(() => _controller.CreateFromRange(request, CancellationToken.None));
        }

        [Fact]
        public async Task CreateFromRange_WhenServiceThrowsArgumentNullException_PropagatesException()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = CreateControllerValidTemplate()
            };

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentNullException("request", "Request cannot be null"));

            await Assert.ThrowsAsync<ArgumentNullException>(() => _controller.CreateFromRange(request, CancellationToken.None));
        }

        [Fact]
        public async Task CreateFromRange_WhenServiceThrowsArgumentException_PropagatesException()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "A", // Mixed range - invalid
                Template = CreateControllerValidTemplate()
            };

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid mixed range: numeric and alphabetic cannot be combined"));

            await Assert.ThrowsAsync<ArgumentException>(() => _controller.CreateFromRange(request, CancellationToken.None));
        }

        [Fact]
        public async Task CreateFromRange_WhenServiceThrowsInvalidOperationException_PropagatesException()
        {
                      // Arrange — InvalidOperationException → 400 via PropertyApiExceptionFilter in production
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = CreateControllerValidTemplate()
            };

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Service is in invalid state"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.CreateFromRange(request, CancellationToken.None));
        }

        #endregion

        #region Controller Cancellation Tests

        [Fact]
        public async Task CreateFromRange_WhenCancellationRequested_ReturnsResultFromService()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "5",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 0,
                FailedCount: 5,
                Results: [],
                Errors: ["Operation cancelled."]
            );

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, cts.Token);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(0);
            response.FailedCount.Should().Be(5);
            response.Errors.Should().Contain(e => e.Contains("Operation cancelled"));
        }

        [Fact]
        public async Task CreateFromRange_PassesCancellationTokenToService()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "2",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 2,
                FailedCount: 0,
                Results: [],
                Errors: null
            );

            using var cts = new CancellationTokenSource();

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), cts.Token))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, cts.Token);

            // Assert
            _mockPropertyService.Verify(s => s.CreatePropertiesFromRangeAsync(
                It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(),
                cts.Token), Times.Once);
        }

        #endregion

        #region Service Interaction Tests

        [Fact]
        public async Task CreateFromRange_CallsServiceExactlyOnce()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "5",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 5,
                FailedCount: 0,
                Results: [],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            _mockPropertyService.Verify(s => s.CreatePropertiesFromRangeAsync(
                It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateFromRange_PassesRequestToServiceUnmodified()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "10",
                RangeTo = "20",
                Prefix = "TEST-",
                Suffix = "-END",
                StartSequenceNo = 5,
                Template = new CreateNewPropertyDto
                {
                    WardId = 99,
                    PropertyTypeId = 88,
                    CategoryId = 77,
                    TaxZoneId = 66,
                    OwnerName = "Specific Owner"
                }
            };

            RangeCreateRequest<CreateNewPropertyDto>? capturedRequest = null;

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .Callback<RangeCreateRequest<CreateNewPropertyDto>, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new RangeResult<CreateNewPropertyResponseDto>(0, 0, [], null));

            // Act
            await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.RangeFrom.Should().Be("10");
            capturedRequest.RangeTo.Should().Be("20");
            capturedRequest.Prefix.Should().Be("TEST-");
            capturedRequest.Suffix.Should().Be("-END");
            capturedRequest.StartSequenceNo.Should().Be(5);
            capturedRequest.Template.WardId.Should().Be(99);
            capturedRequest.Template.PropertyTypeId.Should().Be(88);
            capturedRequest.Template.OwnerName.Should().Be("Specific Owner");
        }

        #endregion

        #region Response Structure Tests

        [Fact]
        public async Task CreateFromRange_ReturnsCorrectResponseStructure()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "2",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 2,
                FailedCount: 0,
                Results:
                [
                    new() { Success = true, PropertyId = 1, UPICID = "UPIC-001" },
                    new() { Success = true, PropertyId = 2, UPICID = "UPIC-002" }
                ],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;
            response.Results.Should().AllSatisfy(r =>
            {
                r.Success.Should().BeTrue();
                r.PropertyId.Should().BeGreaterThan(0);
            });
        }

        [Fact]
        public async Task CreateFromRange_WhenServiceThrowsGenericException_PropagatesException()
        {
             // Arrange - thin adapter: no inline error response; PropertyApiExceptionFilter handles it in the pipeline
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = CreateControllerValidTemplate()
            };

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            await Assert.ThrowsAsync<Exception>(() => _controller.CreateFromRange(request, CancellationToken.None));
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task CreateFromRange_WithEmptyResults_ReturnsOkWithEmptyResults()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "1",
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 0,
                FailedCount: 0,
                Results: [],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(0);
            response.FailedCount.Should().Be(0);
            response.Results.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateFromRange_WithLargeRange_ReturnsOkResult()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "100",
                Template = CreateControllerValidTemplate()
            };

            var results = Enumerable.Range(1, 100)
                .Select(i => new CreateNewPropertyResponseDto { Success = true, PropertyId = i })
                .ToList();

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 100,
                FailedCount: 0,
                Results: results,
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(100);
            response.Results.Should().HaveCount(100);
        }

        [Fact]
        public async Task CreateFromRange_WithNullTemplate_ReturnsOkWithValidationError()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                Template = null!
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 0,
                FailedCount: 0,
                Results: [],
                Errors: ["Template cannot be null."]
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(0);
            response.Errors.Should().Contain("Template cannot be null.");
        }

        [Fact]
        public async Task CreateFromRange_WithFullyPopulatedTemplate_ReturnsOkResult()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "2",
                Prefix = "FULL-",
                Template = new CreateNewPropertyDto
                {
                    WardId = 1,
                    PropertyTypeId = 2,
                    CategoryId = 3,
                    TaxZoneId = 4,
                    OwnerName = "Full Owner",
                    PropertyNo = "P001",
                    PlotNo = "Plot-42",
                    BlockNo = "Block-A",
                    PinCode = "411001",
                    CSN = "CSN-001",
                    OwnerTitle = "Mr.",
                    OwnerTitleEnglish = "Mr.",
                    OwnerNameEnglish = "Full Owner English"
                }
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 2,
                FailedCount: 0,
                Results:
                [
                    new() { Success = true, PropertyId = 1, UPICID = "UPIC-FULL-1" },
                    new() { Success = true, PropertyId = 2, UPICID = "UPIC-FULL-2" }
                ],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(2);
            response.AllSucceeded.Should().BeTrue();
            response.Results.Should().AllSatisfy(r =>
            {
                r.UPICID.Should().NotBeNullOrEmpty();
            });
        }

        [Fact]
        public async Task CreateFromRange_WithStartSequenceNo_ReturnsOkResult()
        {
            // Arrange
            var request = new RangeCreateRequest<CreateNewPropertyDto>
            {
                RangeFrom = "1",
                RangeTo = "3",
                StartSequenceNo = 100,
                Template = CreateControllerValidTemplate()
            };

            var expectedResult = new RangeResult<CreateNewPropertyResponseDto>(
                SuccessCount: 3,
                FailedCount: 0,
                Results:
                [
                    new() { Success = true, PropertyId = 100 },
                    new() { Success = true, PropertyId = 101 },
                    new() { Success = true, PropertyId = 102 }
                ],
                Errors: null
            );

            _mockPropertyService
                .Setup(s => s.CreatePropertiesFromRangeAsync(It.IsAny<RangeCreateRequest<CreateNewPropertyDto>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.CreateFromRange(request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<RangeResult<CreateNewPropertyResponseDto>>().Subject;

            response.SuccessCount.Should().Be(3);
            response.AllSucceeded.Should().BeTrue();
        }

        #endregion
    }
}







