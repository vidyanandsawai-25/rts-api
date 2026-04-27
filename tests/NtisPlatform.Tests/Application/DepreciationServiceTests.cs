using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;


namespace NtisPlatform.Tests.Application
{
    public class DepreciationServiceTests
    {
        private readonly Mock<IRepository<DepreciationMasterEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly DepreciationService _service;

        public DepreciationServiceTests()
        {
            _mockRepository = new Mock<IRepository<DepreciationMasterEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _service = new DepreciationService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            var entity = new DepreciationMasterEntity
            {
                Id = 1,
                ConstructionTypeId = 1,
                MinYear = 1,
                MaxYear = 5,
                Rate = 2.5m,
                YearRangeRVId = 1,
                IsActive = true
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<DepreciationDtos>(It.IsAny<DepreciationMasterEntity>()))
                .Returns((DepreciationMasterEntity e) => new DepreciationDtos
                {
                    Id = e.Id,
                    ConstructionTypeId = e.ConstructionTypeId,
                    MinYear = e.MinYear,
                    MaxYear = e.MaxYear,
                    Rate = e.Rate,
                    YearRangeRVId = e.YearRangeRVId,
                    IsActive = e.IsActive
                });

            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(1, result.ConstructionTypeId);
            Assert.Equal(2.5m, result.Rate);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((DepreciationMasterEntity?)null);

            var result = await _service.GetByIdAsync(999, CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            var entities = new List<DepreciationMasterEntity>
            {
                new() { Id = 1, ConstructionTypeId = 1, Rate = 1.1m,MinYear=2020,MaxYear=2025,YearRangeRVId=1 },
                new() { Id = 2, ConstructionTypeId = 2, Rate = 2.2m,MinYear=2020,MaxYear=2025,YearRangeRVId=2 }
            };

            var mockQuery = entities.BuildMock(); // async IQueryable
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<DepreciationMasterEntity, DepreciationDtos>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            mapperConfig.AssertConfigurationIsValid();
            IMapper mapper = mapperConfig.CreateMapper();

            var service = new DepreciationService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                mapper);

            var qp = new DepreciationQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
                SearchTerm = null!,
                SortBy = null!
            };

            // Act
            var result = await service.GetAllAsync(qp, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);

            var items = result.Items.ToList();
            Assert.Equal(2, items.Count);
            Assert.Contains(items, x => x.ConstructionTypeId == 1);
            Assert.Contains(items, x => x.ConstructionTypeId == 2);
        }


        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            var createDto = new CreateDepreciationDto
            {
                ConstructionTypeId = 1,
                MinYear = 1,
                MaxYear = 5,
                Rate = 3.3m,
                YearRangeRVId = 1,
                IsActive = true,
                CreatedBy = 10
            };

            _mockMapper
                .Setup(m => m.Map<DepreciationMasterEntity>(It.IsAny<CreateDepreciationDto>()))
                .Returns((CreateDepreciationDto dto) => new DepreciationMasterEntity
                {
                    ConstructionTypeId = dto.ConstructionTypeId,
                    MinYear = dto.MinYear,
                    MaxYear = dto.MaxYear,
                    Rate = dto.Rate,
                    YearRangeRVId = dto.YearRangeRVId,
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((DepreciationMasterEntity e, CancellationToken _) => e);

            _mockMapper
                .Setup(m => m.Map<DepreciationDtos>(It.IsAny<DepreciationMasterEntity>()))
                .Returns((DepreciationMasterEntity e) => new DepreciationDtos
                {
                    Id = e.Id,
                    ConstructionTypeId = e.ConstructionTypeId,
                    MinYear = e.MinYear,
                    MaxYear = e.MaxYear,
                    Rate = e.Rate,
                    YearRangeRVId = e.YearRangeRVId
                });

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(createDto.ConstructionTypeId, result.ConstructionTypeId);
            Assert.Equal(createDto.Rate, result.Rate);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
        {
            var updateDto = new UpdateDepreciationDto
            {
                Id = 1,
                ConstructionTypeId = 1,
                MinYear = 2,
                MaxYear = 6,
                Rate = 4.4m,
                YearRangeRVId = 1,
                IsActive = true,
                UpdatedBy = 20
            };

            var existing = new DepreciationMasterEntity
            {
                Id = 1,
                ConstructionTypeId = 1,
                MinYear = 1,
                MaxYear = 5,
                Rate = 3.3m,
                YearRangeRVId = 1
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockMapper.Setup(m => m.Map(It.IsAny<UpdateDepreciationDto>(), It.IsAny<DepreciationMasterEntity>()))
                .Callback((UpdateDepreciationDto src, DepreciationMasterEntity dest) =>
                {
                    dest.MinYear = src.MinYear;
                    dest.MaxYear = src.MaxYear;
                    dest.Rate = src.Rate;
                    dest.YearRangeRVId = src.YearRangeRVId;
                });

            await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(2, existing.MinYear);
            Assert.Equal(6, existing.MaxYear);
            Assert.Equal(4.4m, existing.Rate);
            Assert.Equal(1, existing.YearRangeRVId);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
        {
            var updateDto = new UpdateDepreciationDto { Id = 99, ConstructionTypeId = 1 };
            _mockRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((DepreciationMasterEntity?)null);

            await _service.UpdateAsync(99, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((DepreciationMasterEntity?)null);

            var result = await _service.DeleteAsync(999, CancellationToken.None);

            Assert.False(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
        {
            var existing = new DepreciationMasterEntity { Id = 1 };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
            _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(1, CancellationToken.None);

            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #region Bulk Operations Tests

        [Fact]
        public async Task BulkCreateAsync_EmptyArray_ReturnsEmptyResult()
        {
            // Arrange
            var items = Array.Empty<CreateDepreciationDto>();

            // Act
            var result = await _service.BulkCreateAsync(items, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.SuccessCount);
            Assert.Equal(0, result.FailedCount);
            Assert.Empty(result.Results);
            Assert.True(result.AllSucceeded);

            _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<DepreciationMasterEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task BulkCreateAsync_ValidItems_CreatesAllAndReturnsSuccessResult()
        {
            // Arrange
            _mockUnitOfWork
                .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
                .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var createDtos = new[]
            {
                new CreateDepreciationDto { ConstructionTypeId = 1, MinYear = 1, MaxYear = 5, Rate = 2.5m, YearRangeRVId = 1, IsActive = true },
                new CreateDepreciationDto { ConstructionTypeId = 2, MinYear = 6, MaxYear = 10, Rate = 3.5m, YearRangeRVId = 2, IsActive = true },
                new CreateDepreciationDto { ConstructionTypeId = 3, MinYear = 11, MaxYear = 15, Rate = 4.5m, YearRangeRVId = 3, IsActive = true }
            };

            _mockMapper
                .Setup(m => m.Map<DepreciationMasterEntity[]>(It.IsAny<CreateDepreciationDto[]>()))
                .Returns((CreateDepreciationDto[] dtos) => dtos.Select((dto, idx) => new DepreciationMasterEntity
                {
                    Id = idx + 1,
                    ConstructionTypeId = dto.ConstructionTypeId,
                    MinYear = dto.MinYear,
                    MaxYear = dto.MaxYear,
                    Rate = dto.Rate,
                    YearRangeRVId = dto.YearRangeRVId,
                    IsActive = dto.IsActive
                }).ToArray());

            _mockRepository
                .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<DepreciationMasterEntity>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map<DepreciationDtos[]>(It.IsAny<DepreciationMasterEntity[]>()))
                .Returns((DepreciationMasterEntity[] entities) => entities.Select(e => new DepreciationDtos
                {
                    Id = e.Id,
                    ConstructionTypeId = e.ConstructionTypeId,
                    MinYear = e.MinYear,
                    MaxYear = e.MaxYear,
                    Rate = e.Rate,
                    YearRangeRVId = e.YearRangeRVId,
                    IsActive = e.IsActive
                }).ToArray());

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

            Assert.Contains(result.Results, r => r.ConstructionTypeId == 1);
            Assert.Contains(result.Results, r => r.ConstructionTypeId == 2);
            Assert.Contains(result.Results, r => r.ConstructionTypeId == 3);

            _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<DepreciationMasterEntity>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BulkUpdateAsync_EmptyArray_ReturnsEmptyResult()
        {
            // Arrange
            var items = Array.Empty<BulkUpdateItem<int, UpdateDepreciationDto>>();

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
                new BulkUpdateItem<int, UpdateDepreciationDto>(1, new UpdateDepreciationDto { ConstructionTypeId = 1, MinYear = 2, MaxYear = 6, Rate = 5.5m, YearRangeRVId = 1, IsActive = true }),
                new BulkUpdateItem<int, UpdateDepreciationDto>(2, new UpdateDepreciationDto { ConstructionTypeId = 2, MinYear = 7, MaxYear = 12, Rate = 6.5m, YearRangeRVId = 2, IsActive = true })
            };

            var existingEntities = new Dictionary<int, DepreciationMasterEntity>
            {
                { 1, new DepreciationMasterEntity { Id = 1, ConstructionTypeId = 1, MinYear = 1, MaxYear = 5, Rate = 2.5m, YearRangeRVId = 1, IsActive = true } },
                { 2, new DepreciationMasterEntity { Id = 2, ConstructionTypeId = 2, MinYear = 6, MaxYear = 10, Rate = 3.5m, YearRangeRVId = 2, IsActive = true } }
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateDepreciationDto>(), It.IsAny<DepreciationMasterEntity>()))
                .Callback((UpdateDepreciationDto src, DepreciationMasterEntity dest) =>
                {
                    dest.ConstructionTypeId = src.ConstructionTypeId;
                    dest.MinYear = src.MinYear;
                    dest.MaxYear = src.MaxYear;
                    dest.Rate = src.Rate;
                    dest.YearRangeRVId = src.YearRangeRVId;
                    dest.IsActive = src.IsActive;
                });

            _mockMapper
                .Setup(m => m.Map<List<DepreciationDtos>>(It.IsAny<List<DepreciationMasterEntity>>()))
                .Returns((List<DepreciationMasterEntity> entities) => entities.Select(e => new DepreciationDtos
                {
                    Id = e.Id,
                    ConstructionTypeId = e.ConstructionTypeId,
                    MinYear = e.MinYear,
                    MaxYear = e.MaxYear,
                    Rate = e.Rate,
                    YearRangeRVId = e.YearRangeRVId,
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
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BulkUpdateAsync_MixedExistingAndNonExisting_ReturnsPartialSuccess()
        {
            // Arrange
            var updateItems = new[]
            {
                new BulkUpdateItem<int, UpdateDepreciationDto>(1, new UpdateDepreciationDto { ConstructionTypeId = 1, MinYear = 2, MaxYear = 6, Rate = 5.5m, YearRangeRVId = 1, IsActive = true }),
                new BulkUpdateItem<int, UpdateDepreciationDto>(9999, new UpdateDepreciationDto { ConstructionTypeId = 9999, MinYear = 99, MaxYear = 99, Rate = 99m, YearRangeRVId = 9999, IsActive = true }),
                new BulkUpdateItem<int, UpdateDepreciationDto>(2, new UpdateDepreciationDto { ConstructionTypeId = 2, MinYear = 7, MaxYear = 12, Rate = 6.5m, YearRangeRVId = 2, IsActive = true })
            };

            var existingEntities = new Dictionary<int, DepreciationMasterEntity>
            {
                { 1, new DepreciationMasterEntity { Id = 1, ConstructionTypeId = 1, MinYear = 1, MaxYear = 5, Rate = 2.5m, YearRangeRVId = 1, IsActive = true } },
                { 2, new DepreciationMasterEntity { Id = 2, ConstructionTypeId = 2, MinYear = 6, MaxYear = 10, Rate = 3.5m, YearRangeRVId = 2, IsActive = true } }
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateDepreciationDto>(), It.IsAny<DepreciationMasterEntity>()))
                .Callback((UpdateDepreciationDto src, DepreciationMasterEntity dest) =>
                {
                    dest.ConstructionTypeId = src.ConstructionTypeId;
                });

            _mockMapper
                .Setup(m => m.Map<List<DepreciationDtos>>(It.IsAny<List<DepreciationMasterEntity>>()))
                .Returns((List<DepreciationMasterEntity> entities) => entities.Select(e => new DepreciationDtos
                {
                    Id = e.Id,
                    ConstructionTypeId = e.ConstructionTypeId
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
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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

            var existingEntities = new Dictionary<int, DepreciationMasterEntity>
            {
                { 1, new DepreciationMasterEntity { Id = 1, ConstructionTypeId = 1 } },
                { 2, new DepreciationMasterEntity { Id = 2, ConstructionTypeId = 2 } },
                { 3, new DepreciationMasterEntity { Id = 3, ConstructionTypeId = 3 } }
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()))
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
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BulkDeleteAsync_MixedExistingAndNonExisting_ReturnsPartialSuccess()
        {
            // Arrange
            var idsToDelete = new[] { 1, 9999, 2 };

            var existingEntities = new Dictionary<int, DepreciationMasterEntity>
            {
                { 1, new DepreciationMasterEntity { Id = 1, ConstructionTypeId = 1 } },
                { 2, new DepreciationMasterEntity { Id = 2, ConstructionTypeId = 2 } }
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => existingEntities.GetValueOrDefault(id));

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()))
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
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion
    }
}
