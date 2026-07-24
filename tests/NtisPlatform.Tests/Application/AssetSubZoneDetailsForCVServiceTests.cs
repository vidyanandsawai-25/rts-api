using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using Xunit;
using SystemValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace NtisPlatform.Tests.Application
{
    public class AssetSubZoneDetailsForCVServiceTests
    {
        private readonly Mock<IRepository<AssetSubZoneDetailsForCVEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IMapper _mapper;
        private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
        private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
        private readonly AssetSubZoneDetailsForCVService _service;

        public AssetSubZoneDetailsForCVServiceTests()
        {
            _mockRepository = new Mock<IRepository<AssetSubZoneDetailsForCVEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockReferenceValidator = new Mock<IReferenceValidationService>();
            _mockCleanupService = new Mock<IHardDeleteCleanupService>();

            _mockReferenceValidator
                .Setup(x => x.ValidateReferencesAsync<AssetSubZoneDetailsForCVEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AssetSubZoneDetailsForCVMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _service = new AssetSubZoneDetailsForCVService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);
        }

        #region Entity & DTO Tests

        [Fact]
        public void Entity_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var entity = new AssetSubZoneDetailsForCVEntity
            {
                Id = 1,
                MoujaId = 10,
                SubZoneNo = "SZ1",
                SubZoneName = "SubZone 1",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = date,
                UpdatedBy = 2,
                UpdatedDate = date,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(10, entity.MoujaId);
            Assert.Equal("SZ1", entity.SubZoneNo);
            Assert.Equal("SubZone 1", entity.SubZoneName);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void Dto_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var dto = new AssetSubZoneDetailsForCVDto
            {
                Id = 1,
                MoujaId = 10,
                SubZoneNo = "SZ1",
                SubZoneName = "SubZone 1",
                IsActive = true,
                CreatedDate = date
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal(10, dto.MoujaId);
            Assert.Equal("SZ1", dto.SubZoneNo);
        }

        [Fact]
        public void CreateDto_Validation()
        {
            var dto = new CreateAssetSubZoneDetailsForCVDto
            {
                MoujaId = 10,
                SubZoneNo = "SZ1",
                SubZoneName = "SubZone 1",
                IsActive = true
            };

            var results = new List<SystemValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.True(isValid);
        }

        #endregion

        #region Service Tests

        [Fact]
        public async Task Service_GetAllAsync_ReturnsPagedResult()
        {
            var list = new List<AssetSubZoneDetailsForCVEntity>
            {
                new AssetSubZoneDetailsForCVEntity { Id = 1, MoujaId = 10, SubZoneNo = "SZ1", SubZoneName = "SubZone 1", IsActive = true }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var result = await _service.GetAllAsync(new AssetSubZoneDetailsForCVQueryParameters { MoujaId = 10 }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task Service_GetByIdAsync_Existing_ReturnsDto()
        {
            var entity = new AssetSubZoneDetailsForCVEntity { Id = 1, MoujaId = 10, SubZoneNo = "SZ1", SubZoneName = "SubZone 1", IsActive = true };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("SZ1", result.SubZoneNo);
        }

        [Fact]
        public async Task Service_CreateAsync_Valid_CreatesSuccessfully()
        {
            var list = new List<AssetSubZoneDetailsForCVEntity>();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<AssetSubZoneDetailsForCVEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AssetSubZoneDetailsForCVEntity e, CancellationToken ct) => e);

            var createDto = new CreateAssetSubZoneDetailsForCVDto { MoujaId = 10, SubZoneNo = "SZ1", SubZoneName = "SubZone 1", IsActive = true };
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("SZ1", result.SubZoneNo);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_UpdateAsync_Valid_UpdatesSuccessfully()
        {
            var existingEntity = new AssetSubZoneDetailsForCVEntity { Id = 1, MoujaId = 10, SubZoneNo = "SZ1", SubZoneName = "Old", IsActive = true };
            var list = new List<AssetSubZoneDetailsForCVEntity> { existingEntity };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var updateDto = new UpdateAssetSubZoneDetailsForCVDto { MoujaId = 10, SubZoneNo = "SZ1", SubZoneName = "Updated", IsActive = true };
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Updated", result.SubZoneName);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_DeleteAsync_Existing_DeletesSuccessfully()
        {
            var entity = new AssetSubZoneDetailsForCVEntity { Id = 1, MoujaId = 10, SubZoneNo = "SZ1" };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.DeleteAsync(1, CancellationToken.None);

            Assert.True(result);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

    }
}
