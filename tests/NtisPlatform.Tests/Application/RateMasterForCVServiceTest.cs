using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application    
{
    public class RateMasterForCVServiceTest
    {
        private readonly Mock<IRepository<RateMasterForCVEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly RateMasterForCVService _service;

        public RateMasterForCVServiceTest()
        {
            _mockRepository = new Mock<IRepository<RateMasterForCVEntity, int>>();
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

            _service = new RateMasterForCVService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            var entity = new RateMasterForCVEntity
            {
                Id = 1,
                SubZoneId = 1,
                TypeOfUseGroupId = 1,
                FloorGroupId = null,
                RateAmount = 1500.50m,
                AssessmentYearRangeId = 1,
                IsActive = true
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    RateMasterCVId = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupId = e.TypeOfUseGroupId,
                    FloorGroupId = e.FloorGroupId,
                    RateAmount = e.RateAmount,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    SubZoneNo = "SZ001",
                    SubZoneName = "Zone A",
                    TypeOfUseGroupName = "Residential",
                    IsActive = e.IsActive
                });

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.RateMasterCVId);
            Assert.Equal(1, result.SubZoneId);
            Assert.Equal(1, result.TypeOfUseGroupId);
            Assert.Equal(1500.50m, result.RateAmount);
            Assert.Equal(1, result.AssessmentYearRangeId);
            Assert.Equal("SZ001", result.SubZoneNo);
            Assert.Equal("Zone A", result.SubZoneName);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateMasterForCVEntity?)null);

            var result = await _service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            var entities = new List<RateMasterForCVEntity>
            {
                new() { Id = 1, SubZoneId = 1, TypeOfUseGroupId = 1, FloorGroupId = null, RateAmount = 1500.50m, AssessmentYearRangeId = 1, IsActive = true },
                new() { Id = 2, SubZoneId = 2, TypeOfUseGroupId = 2, FloorGroupId = 1, RateAmount = 2500.75m, AssessmentYearRangeId = 1, IsActive = true }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            // Use shared AutoMapper configuration
            var mapper = AutoMapperTestHelper.CreateRateMasterForCVMapper();
            var service = new RateMasterForCVService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var qp = new RateMasterForCVQueryParameters
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
            Assert.Contains(items, x => x.RateMasterCVId == 1);
            Assert.Contains(items, x => x.RateMasterCVId == 2);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            var createDto = new CreateRateMasterForCVDto
            {
                SubZoneId = 2,
                TypeOfUseGroupId = 1,
                FloorGroupId = null,
                RateAmount = 1800.00m,
                AssessmentYearRangeId = 1,
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVEntity>(It.IsAny<CreateRateMasterForCVDto>()))
                .Returns((CreateRateMasterForCVDto dto) => new RateMasterForCVEntity
                {
                    Id = 1,
                    SubZoneId = dto.SubZoneId,
                    TypeOfUseGroupId = dto.TypeOfUseGroupId,
                    FloorGroupId = dto.FloorGroupId,
                    RateAmount = dto.RateAmount,
                    AssessmentYearRangeId = dto.AssessmentYearRangeId,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy,
                    CreatedDate = DateTime.Now
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateMasterForCVEntity e, CancellationToken _) => e);

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    RateMasterCVId = e.Id,
                    SubZoneId = e.SubZoneId,
                    TypeOfUseGroupId = e.TypeOfUseGroupId,
                    FloorGroupId = e.FloorGroupId,
                    RateAmount = e.RateAmount,
                    AssessmentYearRangeId = e.AssessmentYearRangeId,
                    SubZoneNo = "SZ002",
                    SubZoneName = "Zone B",
                    TypeOfUseGroupName = "Residential",
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate
                });

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.RateMasterCVId);
            Assert.Equal(2, result.SubZoneId);
            Assert.Equal(1, result.TypeOfUseGroupId);
            Assert.Equal(1800.00m, result.RateAmount);
            Assert.Equal(1, result.AssessmentYearRangeId);
            Assert.Equal("SZ002", result.SubZoneNo);
            Assert.Equal("Zone B", result.SubZoneName);
            Assert.True(result.IsActive);
            Assert.NotNull(result.CreatedDate);

            _mockRepository.Verify(r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
        {
            var updateDto = new UpdateRateMasterForCVDto
            {
                SubZoneId = 3,
                TypeOfUseGroupId = 2,
                FloorGroupId = 1,
                RateAmount = 2200.50m,
                AssessmentYearRangeId = 1,
                IsActive = true,
                UpdatedBy = 2
            };

            var existingEntity = new RateMasterForCVEntity
            {
                Id = 1,
                SubZoneId = 1,
                TypeOfUseGroupId = 1,
                FloorGroupId = null,
                RateAmount = 1500.00m,
                AssessmentYearRangeId = 1,
                IsActive = true,
                CreatedBy = 1
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRateMasterForCVDto>(), It.IsAny<RateMasterForCVEntity>()))
                .Callback((UpdateRateMasterForCVDto src, RateMasterForCVEntity dest) =>
                {
                    dest.SubZoneId = src.SubZoneId;
                    dest.TypeOfUseGroupId = src.TypeOfUseGroupId;
                    dest.FloorGroupId = src.FloorGroupId;
                    dest.RateAmount = src.RateAmount;
                    dest.AssessmentYearRangeId = src.AssessmentYearRangeId;
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;
                    dest.UpdatedDate = DateTime.Now;
                });

            await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(1, existingEntity.Id);
            Assert.Equal(3, existingEntity.SubZoneId);
            Assert.Equal(2, existingEntity.TypeOfUseGroupId);
            Assert.Equal(1, existingEntity.FloorGroupId);
            Assert.Equal(2200.50m, existingEntity.RateAmount);
            Assert.Equal(1, existingEntity.AssessmentYearRangeId);
            Assert.True(existingEntity.IsActive);
            Assert.Equal(2, existingEntity.UpdatedBy);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
        {
            var updateDto = new UpdateRateMasterForCVDto 
            { 
                SubZoneId = 999,
                TypeOfUseGroupId = 1,
                RateAmount = 1000.00m,
                AssessmentYearRangeId = 1
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateMasterForCVEntity?)null);

            await _service.UpdateAsync(99, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
        {
            var idToDelete = 999;

            _mockRepository
                .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateMasterForCVEntity?)null);

            var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

            Assert.False(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
        {
            var idToDelete = 1;
            var existingEntity = new RateMasterForCVEntity { Id = idToDelete };

            _mockRepository
                .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

            Assert.True(result);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
