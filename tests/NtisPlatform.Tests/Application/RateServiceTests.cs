using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application
{
    public class RateServiceTests
    {
        private readonly Mock<IRepository<RateEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly RateService _service;

        public RateServiceTests()
        {
            _mockRepository = new Mock<IRepository<RateEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockUnitOfWork
                .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockTaxZoneRepository = new Mock<IRepository<TaxZoneEntity>>();
            var mockFloorRepository = new Mock<IRepository<FloorEntity>>();
            var mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity>>();
            var mockTypeOfUseGroupRepository = new Mock<IRepository<TypeOfUseGroupEntity>>();
            var mockAssessmentYearRangeRepository = new Mock<IRepository<AssessmentYearRangeEntity>>();
            var mockRateSectionRepository = new Mock<IRepository<RateSectionEntity>>();

            _service = new RateService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                mockTaxZoneRepository.Object,
                mockFloorRepository.Object,
                mockConstructionTypeRepository.Object,
                mockTypeOfUseGroupRepository.Object,
                mockAssessmentYearRangeRepository.Object,
                mockRateSectionRepository.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            var entity = new RateEntity
            {
                Id = 1,
                YearRangeRVId = 2023,
                TaxZoneId = 1,
                RateSquareMeter = 100m
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<RateDto>(It.IsAny<RateEntity>()))
                .Returns((RateEntity e) => new RateDto
                {
                    Id = e.Id,
                    TaxZoneId = e.TaxZoneId,
                    RateSquareMeter = e.RateSquareMeter
                });

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(1, result.TaxZoneId);
            Assert.Equal(100m, result.RateSquareMeter);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateEntity?)null);

            var result = await _service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            var entities = new List<RateEntity>
            {
                new() { Id = 1, YearRangeRVId = 2020, TaxZoneId = 1 },
                new() { Id = 2, YearRangeRVId = 2021, TaxZoneId = 2 }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RateEntity, RateDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            mapperConfig.AssertConfigurationIsValid();
            IMapper mapper = mapperConfig.CreateMapper();

            var mockTaxZoneRepository = new Mock<IRepository<TaxZoneEntity>>();
            var mockFloorRepository = new Mock<IRepository<FloorEntity>>();
            var mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity>>();
            var mockTypeOfUseGroupRepository = new Mock<IRepository<TypeOfUseGroupEntity>>();
            var mockAssessmentYearRangeRepository = new Mock<IRepository<AssessmentYearRangeEntity>>();
            var mockRateSectionRepository = new Mock<IRepository<RateSectionEntity>>();

            var service = new RateService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                mapper,
                mockTaxZoneRepository.Object,
                mockFloorRepository.Object,
                mockConstructionTypeRepository.Object,
                mockTypeOfUseGroupRepository.Object,
                mockAssessmentYearRangeRepository.Object,
                mockRateSectionRepository.Object);

            var qp = new RateQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
                SearchTerm = null!,
                SortBy = null!
            };

            var result = await service.GetAllAsync(qp, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);

            var items = result.Items.ToList();
            Assert.Equal(2, items.Count);
            Assert.Contains(items, x => x.Id == 1);
            Assert.Contains(items, x => x.Id == 2);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            var createDto = new CreateRateDto
            {
                TaxZoneId = 1,
                RateSquareMeter = 200m,
                FloorId = 1,
                ConstructionTypeId = 1,
                TypeOfUseGroupId = 1,
                YearRangeRVId = 1,
                RateSquareFeet = 185.8m,
                RateSectionId = 1,
                RateRemark = "Test Rate",
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RateEntity>(It.IsAny<CreateRateDto>()))
                .Returns((CreateRateDto dto) => new RateEntity
                {
                    Id = 0,
                    TaxZoneId = dto.TaxZoneId,
                    RateSquareMeter = dto.RateSquareMeter,
                    FloorId = dto.FloorId,
                    ConstructionTypeId = dto.ConstructionTypeId,
                    TypeOfUseGroupId = dto.TypeOfUseGroupId,
                    YearRangeRVId = dto.YearRangeRVId ?? 0,
                    RateSquareFeet = dto.RateSquareFeet,
                    RateSectionId = dto.RateSectionId,
                    RateRemark = dto.RateRemark ?? string.Empty,
                    IsActive = true,
                    CreatedBy = dto.CreatedBy,
                    CreatedDate = DateTime.Now
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateEntity e, CancellationToken _) =>
                {
                    e.Id = 1;
                    return e;
                });

            _mockMapper
                .Setup(m => m.Map<RateDto>(It.IsAny<RateEntity>()))
                .Returns((RateEntity e) => new RateDto
                {
                    Id = e.Id,
                    TaxZoneId = e.TaxZoneId,
                    RateSquareMeter = e.RateSquareMeter,
                    FloorId = e.FloorId,
                    ConstructionTypeId = e.ConstructionTypeId,
                    TypeOfUseGroupId = e.TypeOfUseGroupId,
                    YearRangeRVId = e.YearRangeRVId,
                    RateSquareFeet = e.RateSquareFeet,
                    RateSectionId = e.RateSectionId,
                    RateRemark = e.RateRemark,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate
                });

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.TaxZoneId);
            Assert.Equal(200m, result.RateSquareMeter);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
        {
            var updateDto = new UpdateRateDto
            {
                RateSquareMeter = 300m,
                TaxZoneId = 2,
                FloorID = 2,
                ConstructionTypeId = 2,
                TypeOfUseGroupID = 2,
                YearRangeRVId = 2,
                RateSquareFeet = 278.7m,
                RateSectionId = 2,
                RateRemark = "Updated Test Rate",
                IsActive = false,
                UpdatedBy = 2
            };

            var existingEntity = new RateEntity
            {
                Id = 1,
                RateSquareMeter = 100m,
                TaxZoneId = 1,
                FloorId = 1,
                ConstructionTypeId = 1,
                TypeOfUseGroupId = 1,
                YearRangeRVId = 1,
                RateSquareFeet = 185.8m,
                RateSectionId = 1,
                RateRemark = "Test Rate",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRateDto>(), It.IsAny<RateEntity>()))
                .Callback((UpdateRateDto src, RateEntity dest) =>
                {
                    dest.RateSquareMeter = src.RateSquareMeter;
                });

            await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(300m, existingEntity.RateSquareMeter);
        }        

        [Fact]
        public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
        {
            var idToDelete = 999;

            _mockRepository
                .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateEntity?)null);

            var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

            Assert.False(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
        {
            var idToDelete = 1;
            var existingEntity = new RateEntity { Id = idToDelete, YearRangeRVId = 2020 };

            _mockRepository
                .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #region Bulk Operations Tests

        [Fact]
        public async Task BulkCreateAsync_EmptyArray_ReturnsEmptyResult()
        {
            // Arrange
            var items = Array.Empty<CreateRateDto>();

            // Act
            var result = await _service.BulkCreateAsync(items, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Empty(result.Results);
            Assert.True(result.AllSucceeded);

            _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<RateEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BulkCreateAsync_ValidItems_CreatesAllAndReturnsSuccessResult()
        {
            // Arrange
            var createDtos = new[]
            {
                new CreateRateDto { TaxZoneId = 1, FloorId = 1, ConstructionTypeId = 1, RateSquareMeter = 100m, IsActive = true },
                new CreateRateDto { TaxZoneId = 2, FloorId = 2, ConstructionTypeId = 2, RateSquareMeter = 200m, IsActive = true },
                new CreateRateDto { TaxZoneId = 3, FloorId = 3, ConstructionTypeId = 3, RateSquareMeter = 300m, IsActive = true }
            };

            // Setup mapping for each item (service maps each CreateRateDto to RateEntity individually)
            _mockMapper
                .Setup(m => m.Map<RateEntity>(It.IsAny<CreateRateDto>()))
                .Returns((CreateRateDto dto) => new RateEntity
                {
                    Id = 0,
                    TaxZoneId = dto.TaxZoneId,
                    FloorId = dto.FloorId,
                    ConstructionTypeId = dto.ConstructionTypeId,
                    RateSquareMeter = dto.RateSquareMeter,
                    IsActive = dto.IsActive
                });

            _mockRepository
                .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<RateEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Setup mapping from List<RateEntity> to List<RateDto> (service uses this after creation)
            _mockMapper
                .Setup(m => m.Map<List<RateDto>>(It.IsAny<List<RateEntity>>()))
                .Returns((List<RateEntity> entities) => entities.Select((e, idx) => new RateDto
                {
                    Id = idx + 1,
                    TaxZoneId = e.TaxZoneId,
                    FloorId = e.FloorId,
                    ConstructionTypeId = e.ConstructionTypeId,
                    RateSquareMeter = e.RateSquareMeter,
                    IsActive = e.IsActive
                }).ToList());

            // Act
            var result = await _service.BulkCreateAsync(createDtos, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(3, result.Results.Count);
            Assert.True(result.AllSucceeded);
            Assert.False(result.HasFailures);
            Assert.Null(result.Errors);

            _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<RateEntity>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BulkUpdateAsync_EmptyArray_ReturnsEmptyResult()
        {
            // Arrange
            var items = Array.Empty<BulkUpdateItem<int, UpdateRateDto>>();

            // Act
            var result = await _service.BulkUpdateAsync(items, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Empty(result.Results);
            Assert.True(result.AllSucceeded);

            _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BulkUpdateAsync_AllExistingEntities_UpdatesAllSuccessfully()
        {
            // Arrange
            var updateItems = new[]
            {
                new BulkUpdateItem<int, UpdateRateDto>(1, new UpdateRateDto { TaxZoneId = 10, RateSquareMeter = 500m, IsActive = true }),
                new BulkUpdateItem<int, UpdateRateDto>(2, new UpdateRateDto { TaxZoneId = 20, RateSquareMeter = 600m, IsActive = true })
            };

            var existingEntities = new Dictionary<int, RateEntity>
            {
                { 1, new RateEntity { Id = 1, TaxZoneId = 1, RateSquareMeter = 100m, IsActive = true } },
                { 2, new RateEntity { Id = 2, TaxZoneId = 2, RateSquareMeter = 200m, IsActive = true } }
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRateDto>(), It.IsAny<RateEntity>()))
                .Callback((UpdateRateDto src, RateEntity dest) =>
                {
                    dest.TaxZoneId = src.TaxZoneId ?? dest.TaxZoneId;
                    dest.RateSquareMeter = src.RateSquareMeter;
                    dest.IsActive = src.IsActive;
                });

            _mockMapper
                .Setup(m => m.Map<List<RateDto>>(It.IsAny<List<RateEntity>>()))
                .Returns((List<RateEntity> entities) => entities.Select(e => new RateDto
                {
                    Id = e.Id,
                    TaxZoneId = e.TaxZoneId,
                    RateSquareMeter = e.RateSquareMeter,
                    IsActive = e.IsActive
                }).ToList());

            // Act
            var result = await _service.BulkUpdateAsync(updateItems, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(2, result.Results.Count);
            Assert.True(result.AllSucceeded);
            Assert.False(result.HasFailures);
            Assert.Null(result.Errors);

            _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BulkUpdateAsync_MixedExistingAndNonExisting_ReturnsPartialSuccess()
        {
            // Arrange
            var updateItems = new[]
            {
                new BulkUpdateItem<int, UpdateRateDto>(1, new UpdateRateDto { TaxZoneId = 10, RateSquareMeter = 500m, IsActive = true }),
                new BulkUpdateItem<int, UpdateRateDto>(9999, new UpdateRateDto { TaxZoneId = 99, RateSquareMeter = 999m, IsActive = true }),
                new BulkUpdateItem<int, UpdateRateDto>(2, new UpdateRateDto { TaxZoneId = 20, RateSquareMeter = 600m, IsActive = true })
            };

            var existingEntities = new Dictionary<int, RateEntity>
            {
                { 1, new RateEntity { Id = 1, YearRangeRVId = 2020, TaxZoneId = 1, RateSquareMeter = 100m, IsActive = true } },
                { 2, new RateEntity { Id = 2, YearRangeRVId = 2021, TaxZoneId = 2, RateSquareMeter = 200m, IsActive = true } }
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRateDto>(), It.IsAny<RateEntity>()))
                .Callback((UpdateRateDto src, RateEntity dest) =>
                {
                    dest.RateSquareMeter = src.RateSquareMeter;
                    dest.IsActive = src.IsActive;
                });

            _mockMapper
                .Setup(m => m.Map<List<RateDto>>(It.IsAny<List<RateEntity>>()))
                .Returns((List<RateEntity> entities) => entities.Select(e => new RateDto
                {
                    Id = e.Id
                }).ToList());

            // Act
            var result = await _service.BulkUpdateAsync(updateItems, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(1, result.FailedCount);
            Assert.Equal(2, result.Results.Count);
            Assert.False(result.AllSucceeded);
            Assert.True(result.HasFailures);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Contains("9999", result.Errors[0]);

            _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BulkDeleteAsync_EmptyArray_ReturnsEmptyResult()
        {
            // Arrange
            var ids = Array.Empty<int>();

            // Act
            var result = await _service.BulkDeleteAsync(ids, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Empty(result.Results);
            Assert.True(result.AllSucceeded);

            _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BulkDeleteAsync_AllExistingEntities_DeletesAllSuccessfully()
        {
            // Arrange
            var idsToDelete = new[] { 1, 2, 3 };

            var existingEntities = new Dictionary<int, RateEntity>
            {
                { 1, new RateEntity { Id = 1, YearRangeRVId = 2020, TaxZoneId = 1 } },
                { 2, new RateEntity { Id = 2, YearRangeRVId = 2021, TaxZoneId = 2 } },
                { 3, new RateEntity { Id = 3, YearRangeRVId = 2022, TaxZoneId = 3 } }
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.BulkDeleteAsync(idsToDelete, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Equal(3, result.Results.Count);
            Assert.True(result.AllSucceeded);
            Assert.False(result.HasFailures);
            Assert.Null(result.Errors);

            Assert.Contains(1, result.Results);
            Assert.Contains(2, result.Results);
            Assert.Contains(3, result.Results);

            _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BulkDeleteAsync_MixedExistingAndNonExisting_ReturnsPartialSuccess()
        {
            // Arrange
            var idsToDelete = new[] { 1, 9999, 2 };

            var existingEntities = new Dictionary<int, RateEntity>
            {
                { 1, new RateEntity { Id = 1, YearRangeRVId = 2020, TaxZoneId = 1 } },
                { 2, new RateEntity { Id = 2, YearRangeRVId = 2021, TaxZoneId = 2 } }
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.BulkDeleteAsync(idsToDelete, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(1, result.FailedCount);
            Assert.Equal(2, result.Results.Count);
            Assert.False(result.AllSucceeded);
            Assert.True(result.HasFailures);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Contains("9999", result.Errors[0]);

            Assert.Contains(1, result.Results);
            Assert.Contains(2, result.Results);
            Assert.DoesNotContain(9999, result.Results);

            _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetDetailedAllAsync Tests

        [Fact]
        public async Task GetDetailedAllAsync_WithPagination_ReturnsPaginatedResults()
        {
            // Arrange
            var rateEntities = new List<RateEntity>
            {
                new() { Id = 1, TaxZoneId = 1, FloorId = 1, ConstructionTypeId = 1, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSectionId = 1 },
                new() { Id = 2, TaxZoneId = 2, FloorId = 2, ConstructionTypeId = 2, TypeOfUseGroupId = 2, YearRangeRVId = 2, RateSectionId = 2 },
                new() { Id = 3, TaxZoneId = 1, FloorId = 1, ConstructionTypeId = 1, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSectionId = 1 }
            };

            var taxZones = new List<TaxZoneEntity> { new() { Id = 1, TaxZoneNo = "TZ001" }, new() { Id = 2, TaxZoneNo = "TZ002" } };
            var floors = new List<FloorEntity> { new() { Id = 1, Description = "Floor 1" }, new() { Id = 2, Description = "Floor 2" } };
            var constructionTypes = new List<ConstructionTypeEntity> { new() { Id = 1, Description = "Type A" }, new() { Id = 2, Description = "Type B" } };
            var useGroups = new List<TypeOfUseGroupEntity> { new() { Id = 1, GroupName = "Group 1" }, new() { Id = 2, GroupName = "Group 2" } };
            var yearRanges = new List<AssessmentYearRangeEntity> { new() { Id = 1, FromYear = 2020, ToYear = 2025 }, new() { Id = 2, FromYear = 2026, ToYear = 2030 } };
            var sections = new List<RateSectionEntity> { new() { Id = 1, Description = "Section 1" }, new() { Id = 2, Description = "Section 2" } };

            var rateQuery = rateEntities.BuildMock();
            var taxZoneQuery = taxZones.BuildMock();
            var floorQuery = floors.BuildMock();
            var constructionTypeQuery = constructionTypes.BuildMock();
            var useGroupQuery = useGroups.BuildMock();
            var yearRangeQuery = yearRanges.BuildMock();
            var sectionQuery = sections.BuildMock();

            _mockRepository.Setup(r => r.GetQueryable()).Returns(rateQuery);

            var mockTaxZoneRepository = new Mock<IRepository<TaxZoneEntity>>();
            var mockFloorRepository = new Mock<IRepository<FloorEntity>>();
            var mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity>>();
            var mockTypeOfUseGroupRepository = new Mock<IRepository<TypeOfUseGroupEntity>>();
            var mockAssessmentYearRangeRepository = new Mock<IRepository<AssessmentYearRangeEntity>>();
            var mockRateSectionRepository = new Mock<IRepository<RateSectionEntity>>();

            mockTaxZoneRepository.Setup(r => r.GetQueryable()).Returns(taxZoneQuery);
            mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floorQuery);
            mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypeQuery);
            mockTypeOfUseGroupRepository.Setup(r => r.GetQueryable()).Returns(useGroupQuery);
            mockAssessmentYearRangeRepository.Setup(r => r.GetQueryable()).Returns(yearRangeQuery);
            mockRateSectionRepository.Setup(r => r.GetQueryable()).Returns(sectionQuery);

            var service = new RateService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                mockTaxZoneRepository.Object,
                mockFloorRepository.Object,
                mockConstructionTypeRepository.Object,
                mockTypeOfUseGroupRepository.Object,
                mockAssessmentYearRangeRepository.Object,
                mockRateSectionRepository.Object);

            var qp = new RateQueryParameters
            {
                PageNumber = 1,
                PageSize = 2,
                FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
                SearchTerm = null,
                SortBy = null
            };

            // Act
            var result = await service.GetDetailedAllAsync(qp, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(2, result.PageSize);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetDetailedAllAsync_WithPageSizeNegativeOne_NormalizesPageMetadata()
        {
            // Arrange
            var rateEntities = new List<RateEntity>
            {
                new() { Id = 1, TaxZoneId = 1, FloorId = 1, ConstructionTypeId = 1, TypeOfUseGroupId = 1, YearRangeRVId = 1, RateSectionId = 1 },
                new() { Id = 2, TaxZoneId = 2, FloorId = 2, ConstructionTypeId = 2, TypeOfUseGroupId = 2, YearRangeRVId = 2, RateSectionId = 2 }
            };

            var taxZones = new List<TaxZoneEntity> { new() { Id = 1, TaxZoneNo = "TZ001" }, new() { Id = 2, TaxZoneNo = "TZ002" } };
            var floors = new List<FloorEntity> { new() { Id = 1, Description = "Floor 1" }, new() { Id = 2, Description = "Floor 2" } };
            var constructionTypes = new List<ConstructionTypeEntity> { new() { Id = 1, Description = "Type A" }, new() { Id = 2, Description = "Type B" } };
            var useGroups = new List<TypeOfUseGroupEntity> { new() { Id = 1, GroupName = "Group 1" }, new() { Id = 2, GroupName = "Group 2" } };
            var yearRanges = new List<AssessmentYearRangeEntity> { new() { Id = 1, FromYear = 2020, ToYear = 2025 }, new() { Id = 2, FromYear = 2026, ToYear = 2030 } };
            var sections = new List<RateSectionEntity> { new() { Id = 1, Description = "Section 1" }, new() { Id = 2, Description = "Section 2" } };

            var rateQuery = rateEntities.BuildMock();
            var taxZoneQuery = taxZones.BuildMock();
            var floorQuery = floors.BuildMock();
            var constructionTypeQuery = constructionTypes.BuildMock();
            var useGroupQuery = useGroups.BuildMock();
            var yearRangeQuery = yearRanges.BuildMock();
            var sectionQuery = sections.BuildMock();

            _mockRepository.Setup(r => r.GetQueryable()).Returns(rateQuery);

            var mockTaxZoneRepository = new Mock<IRepository<TaxZoneEntity>>();
            var mockFloorRepository = new Mock<IRepository<FloorEntity>>();
            var mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity>>();
            var mockTypeOfUseGroupRepository = new Mock<IRepository<TypeOfUseGroupEntity>>();
            var mockAssessmentYearRangeRepository = new Mock<IRepository<AssessmentYearRangeEntity>>();
            var mockRateSectionRepository = new Mock<IRepository<RateSectionEntity>>();

            mockTaxZoneRepository.Setup(r => r.GetQueryable()).Returns(taxZoneQuery);
            mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floorQuery);
            mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypeQuery);
            mockTypeOfUseGroupRepository.Setup(r => r.GetQueryable()).Returns(useGroupQuery);
            mockAssessmentYearRangeRepository.Setup(r => r.GetQueryable()).Returns(yearRangeQuery);
            mockRateSectionRepository.Setup(r => r.GetQueryable()).Returns(sectionQuery);

            var service = new RateService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                mockTaxZoneRepository.Object,
                mockFloorRepository.Object,
                mockConstructionTypeRepository.Object,
                mockTypeOfUseGroupRepository.Object,
                mockAssessmentYearRangeRepository.Object,
                mockRateSectionRepository.Object);

            var qp = new RateQueryParameters
            {
                PageNumber = 1,
                PageSize = -1, // Unpaged
                FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
                SearchTerm = null,
                SortBy = null
            };

            // Act
            var result = await service.GetDetailedAllAsync(qp, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.PageNumber); // Should normalize to 1
            Assert.Equal(2, result.PageSize); // Should normalize to totalCount (2)
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task GetDetailedAllAsync_PopulatesRelatedEntityNames()
        {
            // Arrange
            var rateEntities = new List<RateEntity>
            {
                new()
                {
                    Id = 1,
                    TaxZoneId = 1,
                    FloorId = 1,
                    ConstructionTypeId = 1,
                    TypeOfUseGroupId = 1,
                    YearRangeRVId = 1,
                    RateSectionId = 1,
                    RateRemark = "Test Remark",
                    RateSquareFeet = 100.5m,
                    RateSquareMeter = 150.5m,
                    IsActive = true
                }
            };

            var taxZones = new List<TaxZoneEntity> { new() { Id = 1, TaxZoneNo = "TZ-001" } };
            var floors = new List<FloorEntity> { new() { Id = 1, Description = "Ground Floor" } };
            var constructionTypes = new List<ConstructionTypeEntity> { new() { Id = 1, Description = "Concrete" } };
            var useGroups = new List<TypeOfUseGroupEntity> { new() { Id = 1, GroupName = "Residential" } };
            var yearRanges = new List<AssessmentYearRangeEntity> { new() { Id = 1, FromYear = 2020, ToYear = 2025 } };
            var sections = new List<RateSectionEntity> { new() { Id = 1, Description = "Main Section" } };

            var rateQuery = rateEntities.BuildMock();
            var taxZoneQuery = taxZones.BuildMock();
            var floorQuery = floors.BuildMock();
            var constructionTypeQuery = constructionTypes.BuildMock();
            var useGroupQuery = useGroups.BuildMock();
            var yearRangeQuery = yearRanges.BuildMock();
            var sectionQuery = sections.BuildMock();

            _mockRepository.Setup(r => r.GetQueryable()).Returns(rateQuery);

            var mockTaxZoneRepository = new Mock<IRepository<TaxZoneEntity>>();
            var mockFloorRepository = new Mock<IRepository<FloorEntity>>();
            var mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity>>();
            var mockTypeOfUseGroupRepository = new Mock<IRepository<TypeOfUseGroupEntity>>();
            var mockAssessmentYearRangeRepository = new Mock<IRepository<AssessmentYearRangeEntity>>();
            var mockRateSectionRepository = new Mock<IRepository<RateSectionEntity>>();

            mockTaxZoneRepository.Setup(r => r.GetQueryable()).Returns(taxZoneQuery);
            mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floorQuery);
            mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypeQuery);
            mockTypeOfUseGroupRepository.Setup(r => r.GetQueryable()).Returns(useGroupQuery);
            mockAssessmentYearRangeRepository.Setup(r => r.GetQueryable()).Returns(yearRangeQuery);
            mockRateSectionRepository.Setup(r => r.GetQueryable()).Returns(sectionQuery);

            var service = new RateService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                mockTaxZoneRepository.Object,
                mockFloorRepository.Object,
                mockConstructionTypeRepository.Object,
                mockTypeOfUseGroupRepository.Object,
                mockAssessmentYearRangeRepository.Object,
                mockRateSectionRepository.Object);

            var qp = new RateQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
                SearchTerm = null,
                SortBy = null
            };

            // Act
            var result = await service.GetDetailedAllAsync(qp, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            var item = result.Items.First();
            Assert.Equal("TZ-001", item.TaxZone);
            Assert.Equal("Ground Floor", item.Floor);
            Assert.Equal("Concrete", item.ConstructionType);
            Assert.Equal("Residential", item.TypeOfUseGroup);
            Assert.Equal("2020-2025", item.YearRangeRV);
            Assert.Equal("Main Section", item.RateSection);
            Assert.Equal("Test Remark", item.RateRemark);
            Assert.Equal(100.5m, item.RateSquareFeet);
            Assert.Equal(150.5m, item.RateSquareMeter);
            Assert.True(item.IsActive);
        }

        [Fact]
        public async Task GetDetailedAllAsync_WithNullRelatedEntities_PopulatesEmptyStrings()
        {
            // Arrange
            var rateEntities = new List<RateEntity>
            {
                new()
                {
                    Id = 1,
                    TaxZoneId = 999, // Non-existent
                    FloorId = 999,   // Non-existent
                    ConstructionTypeId = 999, // Non-existent
                    TypeOfUseGroupId = 999, // Non-existent
                    YearRangeRVId = 999, // Non-existent
                    RateSectionId = 999 // Non-existent
                }
            };

            var taxZones = new List<TaxZoneEntity>();
            var floors = new List<FloorEntity>();
            var constructionTypes = new List<ConstructionTypeEntity>();
            var useGroups = new List<TypeOfUseGroupEntity>();
            var yearRanges = new List<AssessmentYearRangeEntity>();
            var sections = new List<RateSectionEntity>();

            var rateQuery = rateEntities.BuildMock();
            var taxZoneQuery = taxZones.BuildMock();
            var floorQuery = floors.BuildMock();
            var constructionTypeQuery = constructionTypes.BuildMock();
            var useGroupQuery = useGroups.BuildMock();
            var yearRangeQuery = yearRanges.BuildMock();
            var sectionQuery = sections.BuildMock();

            _mockRepository.Setup(r => r.GetQueryable()).Returns(rateQuery);

            var mockTaxZoneRepository = new Mock<IRepository<TaxZoneEntity>>();
            var mockFloorRepository = new Mock<IRepository<FloorEntity>>();
            var mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity>>();
            var mockTypeOfUseGroupRepository = new Mock<IRepository<TypeOfUseGroupEntity>>();
            var mockAssessmentYearRangeRepository = new Mock<IRepository<AssessmentYearRangeEntity>>();
            var mockRateSectionRepository = new Mock<IRepository<RateSectionEntity>>();

            mockTaxZoneRepository.Setup(r => r.GetQueryable()).Returns(taxZoneQuery);
            mockFloorRepository.Setup(r => r.GetQueryable()).Returns(floorQuery);
            mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypeQuery);
            mockTypeOfUseGroupRepository.Setup(r => r.GetQueryable()).Returns(useGroupQuery);
            mockAssessmentYearRangeRepository.Setup(r => r.GetQueryable()).Returns(yearRangeQuery);
            mockRateSectionRepository.Setup(r => r.GetQueryable()).Returns(sectionQuery);

            var service = new RateService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                mockTaxZoneRepository.Object,
                mockFloorRepository.Object,
                mockConstructionTypeRepository.Object,
                mockTypeOfUseGroupRepository.Object,
                mockAssessmentYearRangeRepository.Object,
                mockRateSectionRepository.Object);

            var qp = new RateQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
                SearchTerm = null,
                SortBy = null
            };

            // Act
            var result = await service.GetDetailedAllAsync(qp, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            var item = result.Items.First();
            Assert.Equal(string.Empty, item.TaxZone);
            Assert.Equal(string.Empty, item.Floor);
            Assert.Equal(string.Empty, item.ConstructionType);
            Assert.Equal(string.Empty, item.TypeOfUseGroup);
            Assert.Equal(string.Empty, item.YearRangeRV);
            Assert.Equal(string.Empty, item.RateSection);
        }

        #endregion
    }
}
