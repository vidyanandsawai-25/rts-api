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
                MoujaId = 10,
                SubZoneNo = "SZ-1",
                SubZoneName = "SubZone A",
                CSN = "CSN-001",
                OpenPlotRate = 1000,
                ResidentialRate = 2000,
                OfficeRate = 3000,
                ShopRate = 4000,
                IndustrialRate = 5000
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    Id = e.Id,
                    MoujaId = e.MoujaId,
                    SubZoneNo = e.SubZoneNo,
                    SubZoneName = e.SubZoneName,
                    CSN = e.CSN,
                    OpenPlotRate = e.OpenPlotRate,
                    ResidentialRate = e.ResidentialRate,
                    OfficeRate = e.OfficeRate,
                    ShopRate = e.ShopRate,
                    IndustrialRate = e.IndustrialRate
                });

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(10, result.MoujaId);
            Assert.Equal("SZ-1", result.SubZoneNo);
            Assert.Equal("SubZone A", result.SubZoneName);
            Assert.Equal("CSN-001", result.CSN);
            Assert.Equal(1000, result.OpenPlotRate);
            Assert.Equal(2000, result.ResidentialRate);
            Assert.Equal(3000, result.OfficeRate);
            Assert.Equal(4000, result.ShopRate);
            Assert.Equal(5000, result.IndustrialRate);
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
                new() { Id = 1, MoujaId = 1, SubZoneNo = "SZ-1",SubZoneName="SubZone A",CSN="CSN-001",OpenPlotRate =1000,ResidentialRate=2000,OfficeRate=3000,ShopRate=4000,IndustrialRate=5000},
                new() { Id = 2, MoujaId = 2, SubZoneNo = "SZ-2",SubZoneName="SubZone B",CSN="CSN-002",OpenPlotRate =1100,ResidentialRate=2200,OfficeRate=3300,ShopRate=4400,IndustrialRate=5500 }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RateMasterForCVEntity, RateMasterForCVDto>();
            });

            mapperConfig.AssertConfigurationIsValid();
            IMapper mapper = mapperConfig.CreateMapper();

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
            Assert.Contains(items, x => x.Id == 1);
            Assert.Contains(items, x => x.Id == 2);
        }

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            var createDto = new CreateRateMasterForCVDto
            {
                MoujaId = 11,
                SubZoneNo = "SZ-11",
                SubZoneName = "SubZone A1",
                CSN = "CSN-0011",
                OpenPlotRate = 1100,
                ResidentialRate = 2100,
                OfficeRate = 3100,
                ShopRate = 4100,
                IndustrialRate = 51000,
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVEntity>(It.IsAny<CreateRateMasterForCVDto>()))
                .Returns((CreateRateMasterForCVDto dto) => new RateMasterForCVEntity
                {
                    Id = dto.Id,
                    MoujaId = dto.MoujaId,
                    SubZoneNo = dto.SubZoneNo,
                    SubZoneName = dto.SubZoneName,
                    CSN = dto.CSN,
                    OpenPlotRate = dto.OpenPlotRate,
                    ResidentialRate = dto.ResidentialRate,
                    OfficeRate = dto.OfficeRate,
                    ShopRate = dto.ShopRate,
                    IndustrialRate = dto.IndustrialRate,
                    IsActive = dto.IsActive,
                    CreatedDate = DateTime.Now
                    });

            
            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RateMasterForCVEntity e, CancellationToken _) => e);

            _mockMapper
                .Setup(m => m.Map<RateMasterForCVDto>(It.IsAny<RateMasterForCVEntity>()))
                .Returns((RateMasterForCVEntity e) => new RateMasterForCVDto
                {
                    Id = e.Id,
                    MoujaId = e.MoujaId,                    
                    SubZoneNo = e.SubZoneNo,
                    SubZoneName = e.SubZoneName,
                    CSN = e.CSN,
                    OpenPlotRate = e.OpenPlotRate,
                    ResidentialRate = e.ResidentialRate,
                    OfficeRate = e.OfficeRate,
                    ShopRate = e.ShopRate,
                    IndustrialRate = e.IndustrialRate,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate

                });

            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(11, result.MoujaId);
            Assert.Equal("SZ-11", result.SubZoneNo);
            Assert.Equal("SubZone A1", result.SubZoneName);
            Assert.Equal("CSN-0011", result.CSN);
            Assert.Equal(1100, result.OpenPlotRate);
            Assert.Equal(2100, result.ResidentialRate);
            Assert.Equal(3100, result.OfficeRate);
            Assert.Equal(4100, result.ShopRate);
            Assert.Equal(51000, result.IndustrialRate);
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
                Id = 1,
                MoujaId = 1,
                SubZoneNo = "SZ-11",
                SubZoneName = "SubZone Z",
                CSN = "CSN-0010",
                OpenPlotRate = 1200,
                ResidentialRate = 2200,
                OfficeRate = 3300,
                ShopRate = 4300,
                IndustrialRate = 5300,
                IsActive = true,
                UpdatedBy = 2
            };

            var existingEntity = new RateMasterForCVEntity
            {
                Id = 1,
                MoujaId = 10,
                SubZoneNo = "SZ-5",
                SubZoneName = "SubZone A",
                CSN = "CSN-001",
                OpenPlotRate = 1000,
                ResidentialRate = 2500,
                OfficeRate = 3500,
                ShopRate = 4000,
                IndustrialRate = 5500,
                IsActive = true,
                UpdatedBy = 2

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
                    dest.MoujaId = src.MoujaId;
                    dest.SubZoneNo = src.SubZoneNo;
                    dest.SubZoneName = src.SubZoneName;
                    dest.CSN = src.CSN;
                    dest.OpenPlotRate = src.OpenPlotRate;
                    dest.ResidentialRate = src.ResidentialRate;
                    dest.OfficeRate = src.OfficeRate;
                    dest.ShopRate = src.ShopRate;
                    dest.IndustrialRate = src.IndustrialRate;
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;

                });

            await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RateMasterForCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            
            Assert.Equal(1, existingEntity.Id);
            Assert.Equal(1, existingEntity.MoujaId);
            Assert.Equal("SZ-11", existingEntity.SubZoneNo);
            Assert.Equal("SubZone Z", existingEntity.SubZoneName);
            Assert.Equal("CSN-0010", existingEntity.CSN);
            Assert.Equal(1200, existingEntity.OpenPlotRate);
            Assert.Equal(2200, existingEntity.ResidentialRate);
            Assert.Equal(3300, existingEntity.OfficeRate);
            Assert.Equal(4300, existingEntity.ShopRate);
            Assert.Equal(5300, existingEntity.IndustrialRate);
            Assert.True(existingEntity.IsActive);
            Assert.Equal(2, existingEntity.UpdatedBy);

        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
        {
            var updateDto = new UpdateRateMasterForCVDto { Id = 99, MoujaId = 99 };

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
            var existingEntity = new RateMasterForCVEntity { Id = idToDelete, MoujaId = 10 };

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
