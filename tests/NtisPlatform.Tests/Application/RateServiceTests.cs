using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
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

            _service = new RateService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            var entity = new RateEntity
            {
                ID = 1,
                Year = 2023,
                TaxZoneNo = "TZ1",
                RateSquareMeter = 100m
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<RateDto>(It.IsAny<RateEntity>()))
                .Returns((RateEntity e) => new RateDto
                {
                    ID = e.ID,
                    Year = e.Year,
                    TaxZoneNo = e.TaxZoneNo,
                    RateSquareMeter = e.RateSquareMeter
                });

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.ID);
            Assert.Equal(2023, result.Year);
            Assert.Equal("TZ1", result.TaxZoneNo);
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
                new() { ID = 1, Year = 2020, TaxZoneNo = "A" },
                new() { ID = 2, Year = 2021, TaxZoneNo = "B" }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RateEntity, RateDto>();
            });

            mapperConfig.AssertConfigurationIsValid();
            IMapper mapper = mapperConfig.CreateMapper();

            var service = new RateService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

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
            Assert.Contains(items, x => x.ID == 1);
            Assert.Contains(items, x => x.ID == 2);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            var createDto = new CreateRateDto
            {
                Year = 2022,
                TaxZoneNo = "TZ",
                RateSquareMeter = 200m,
                FloorID = "F1",
                ConstructionID = "C1",
                TypeOfUseGroupID = "U1",
                MinYear = 2000,
                MaxYear = 2022,
                RateSquareFeet = 185.8m,
                RateSectionNo = "RS1",
                RateRemark = "Test Rate",
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RateEntity>(It.IsAny<CreateRateDto>()))
                .Returns((CreateRateDto dto) => new RateEntity
                {
                    ID = dto.ID ?? 0,
                    Year = dto.Year,
                    TaxZoneNo = dto.TaxZoneNo,
                    RateSquareMeter = dto.RateSquareMeter,
                    FloorID = dto.FloorID,
                    ConstructionID = dto.ConstructionID,
                    TypeOfUseGroupID = dto.TypeOfUseGroupID,
                    MinYear = dto.MinYear,
                    MaxYear = dto.MaxYear,
                    RateSquareFeet = dto.RateSquareFeet,
                    RateSectionNo = dto.RateSectionNo,
                    RateRemark = dto.RateRemark,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy,
                    CreatedDate = DateTime.Now
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateEntity e, CancellationToken _) => e);

            _mockMapper
                .Setup(m => m.Map<RateDto>(It.IsAny<RateEntity>()))
                .Returns((RateEntity e) => new RateDto
                {
                    ID = e.ID,
                    Year = e.Year,
                    TaxZoneNo = e.TaxZoneNo,
                    RateSquareMeter = e.RateSquareMeter,
                    FloorID = e.FloorID,
                    ConstructionID = e.ConstructionID,
                    TypeOfUseGroupID = e.TypeOfUseGroupID,
                    MinYear = e.MinYear,
                    MaxYear = e.MaxYear,
                    RateSquareFeet = e.RateSquareFeet,
                    RateSectionNo = e.RateSectionNo,
                    RateRemark = e.RateRemark,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate
                });

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(2022, result.Year);
            Assert.Equal("TZ", result.TaxZoneNo);
            Assert.Equal(200m, result.RateSquareMeter);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
        {
            var updateDto = new UpdateRateDto
            {
                ID = 1,
                Year = 2025,
                RateSquareMeter = 300m,
                TaxZoneNo = "TZ-Updated",
                FloorID = "F1-Updated",
                ConstructionID = "C1-Updated",
                TypeOfUseGroupID = "U1-Updated",
                MinYear = 2010,
                MaxYear = 2025,
                RateSquareFeet = 278.7m,
                RateSectionNo = "RS1-Updated",
                RateRemark = "Updated Test Rate",
                IsActive = false,
                UpdatedBy = 2
            };

            var existingEntity = new RateEntity
            {
                ID = 1,
                Year = 2020,
                RateSquareMeter = 100m,
                TaxZoneNo = "TZ",
                FloorID = "F1",
                ConstructionID = "C1",
                TypeOfUseGroupID = "U1",
                MinYear = 2000,
                MaxYear = 2022,
                RateSquareFeet = 185.8m,
                RateSectionNo = "RS1",
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
                    dest.Year = src.Year;
                    dest.RateSquareMeter = src.RateSquareMeter;
                });

            await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(2025, existingEntity.Year);
            Assert.Equal(300m, existingEntity.RateSquareMeter);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
        {
            var updateDto = new UpdateRateDto { ID = 99, Year = 2030 };

            _mockRepository
                .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateEntity?)null);

            await _service.UpdateAsync(99, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateEntity>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
            var existingEntity = new RateEntity { ID = idToDelete, Year = 2020 };

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
