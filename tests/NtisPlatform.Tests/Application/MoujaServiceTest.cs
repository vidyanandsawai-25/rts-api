using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application
{
    public class MoujaServiceTest
    {
        private readonly Mock<IRepository<MoujaEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly MoujaService _service;

        public MoujaServiceTest()
        {
            _mockRepository = new Mock<IRepository<MoujaEntity, int>>();
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

            _service = new MoujaService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            var entity = new MoujaEntity
            {
                Id = 1,
                Year = 2024,
                MoujaName= "Mouja_A"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<MoujaDto>(It.IsAny<MoujaEntity>()))
                .Returns((MoujaEntity e) => new MoujaDto
                {
                    Id = e.Id,
                    Year = e.Year,
                    MoujaName = e.MoujaName
                });

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(2024, result.Year);
            Assert.Equal("Mouja_A", result.MoujaName);

        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MoujaEntity?)null);

            var result = await _service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            var entities = new List<MoujaEntity>
            {
                new() { Id = 1, Year = 2020, MoujaName = "Mouja-1"},
                new() { Id = 2, Year = 2021, MoujaName = "Mouja-2"}
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<MoujaEntity, MoujaDto>();
            });

            mapperConfig.AssertConfigurationIsValid();
            IMapper mapper = mapperConfig.CreateMapper();

            var service = new MoujaService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var qp = new MoujaQueryParameters
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
            var createDto = new CreateMoujaDto
            {
                Id = 2,
                Year = 2022,
                MoujaName = "Mouja_B",
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<MoujaEntity>(It.IsAny<CreateMoujaDto>()))
                .Returns((CreateMoujaDto dto) => new MoujaEntity
                {
                    Id=dto.Id,
                    Year=dto.Year,
                    MoujaName=dto.MoujaName,
                    IsActive = dto.IsActive,
                    CreatedDate = DateTime.Now
                });


            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<MoujaEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MoujaEntity e, CancellationToken _) => e);

            _mockMapper
                .Setup(m => m.Map<MoujaDto>(It.IsAny<MoujaEntity>()))
                .Returns((MoujaEntity e) => new MoujaDto
                {
                    Id = e.Id,
                    Year=e.Year,
                    MoujaName = e.MoujaName,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate

                });

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
            Assert.Equal(2022, result.Year);
            Assert.Equal("Mouja_B", result.MoujaName);
            Assert.True(result.IsActive);
            Assert.NotNull(result.CreatedDate);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<MoujaEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
        {
            var updateDto = new UpdateMoujaDto
            {
                Id = 1,
                Year =2025, 
                MoujaName ="Mouja_C",
                IsActive = true,
                UpdatedBy = 2
            };

            var existingEntity = new MoujaEntity
            {
                Id = 1,
                Year=2025, 
                MoujaName ="Mouja_C",
                IsActive = true,
                UpdatedBy = 2

            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<MoujaEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateMoujaDto>(), It.IsAny<MoujaEntity>()))
                .Callback((UpdateMoujaDto src, MoujaEntity dest) =>
                {

                    dest.Id = src.Id;
                    dest.Year = src.Year;
                    dest.MoujaName = src.MoujaName;
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;

                });

            await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<MoujaEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);


            Assert.Equal(1, existingEntity.Id);
            Assert.Equal(2025, existingEntity.Year);
            Assert.Equal("Mouja_C", existingEntity.MoujaName);

            Assert.True(existingEntity.IsActive);
            Assert.Equal(2, existingEntity.UpdatedBy);

        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
        {
            var updateDto = new UpdateMoujaDto { Id = 99, Year=2023, MoujaName="Mouja_D"};

            _mockRepository
                .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MoujaEntity?)null);

            await _service.UpdateAsync(99, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<MoujaEntity>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
        {
            var idToDelete = 999;

            _mockRepository
                .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MoujaEntity?)null);

            var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

            Assert.False(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
        {
            var idToDelete = 1;
            var existingEntity = new MoujaEntity { Id = idToDelete };

            _mockRepository
                .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
