using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
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
                ConstructionTypeId = "A",
                MinYear = 1,
                MaxYear = 5,
                Rate = 2.5m,
                Year = 2020,
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
                    Year = e.Year,
                    IsActive = e.IsActive
                });

            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("A", result.ConstructionTypeId);
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
                new() { Id = 1, ConstructionTypeId = "A", Rate = 1.1m,MinYear=2020,MaxYear=2025,Year=2026 },
                new() { Id = 2, ConstructionTypeId = "B", Rate = 2.2m,MinYear=2020,MaxYear=2025,Year=2026 }
            };

            var mockQuery = entities.BuildMock(); // async IQueryable
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<DepreciationMasterEntity, DepreciationDtos>();
            });

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
            Assert.Contains(items, x => x.ConstructionTypeId == "A");
            Assert.Contains(items, x => x.ConstructionTypeId == "B");
        }


        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            var createDto = new CreateDepreciationDto
            {
                Id = 1,
                ConstructionTypeId = "A",
                MinYear = 1,
                MaxYear = 5,
                Rate = 3.3m,
                Year = 2021,
                IsActive = true,
                CreatedBy = 10
            };

            _mockMapper
                .Setup(m => m.Map<DepreciationMasterEntity>(It.IsAny<CreateDepreciationDto>()))
                .Returns((CreateDepreciationDto dto) => new DepreciationMasterEntity
                {
                    Id = dto.Id,
                    ConstructionTypeId = dto.ConstructionTypeId,
                    MinYear = dto.MinYear,
                    MaxYear = dto.MaxYear,
                    Rate = dto.Rate,
                    Year = dto.Year,
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
                    Year = e.Year
                });

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(createDto.Id, result.Id);
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
                ConstructionTypeId = "A",
                MinYear = 2,
                MaxYear = 6,
                Rate = 4.4m,
                Year = 2022,
                IsActive = true,
                UpdatedBy = 20
            };

            var existing = new DepreciationMasterEntity
            {
                Id = 1,
                ConstructionTypeId = "A",
                MinYear = 1,
                MaxYear = 5,
                Rate = 3.3m,
                Year = 2021
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockMapper.Setup(m => m.Map(It.IsAny<UpdateDepreciationDto>(), It.IsAny<DepreciationMasterEntity>()))
                .Callback((UpdateDepreciationDto src, DepreciationMasterEntity dest) =>
                {
                    dest.MinYear = src.MinYear;
                    dest.MaxYear = src.MaxYear;
                    dest.Rate = src.Rate;
                    dest.Year = src.Year;
                });

            await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<DepreciationMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(2, existing.MinYear);
            Assert.Equal(6, existing.MaxYear);
            Assert.Equal(4.4m, existing.Rate);
            Assert.Equal(2022, existing.Year);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
        {
            var updateDto = new UpdateDepreciationDto { Id = 99, ConstructionTypeId = "X" };
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
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
        {
            var existing = new DepreciationMasterEntity { Id = 1 };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
            _mockRepository.Setup(r => r.DeleteAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(1, CancellationToken.None);

            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
