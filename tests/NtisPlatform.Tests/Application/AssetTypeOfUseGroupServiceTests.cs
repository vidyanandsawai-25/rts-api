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
    public class AssetTypeOfUseGroupServiceTests
    {
        private readonly Mock<IRepository<AssetTypeOfUseGroupEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IMapper _mapper;
        private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
        private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
        private readonly AssetTypeOfUseGroupService _service;

        public AssetTypeOfUseGroupServiceTests()
        {
            _mockRepository = new Mock<IRepository<AssetTypeOfUseGroupEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockReferenceValidator = new Mock<IReferenceValidationService>();
            _mockCleanupService = new Mock<IHardDeleteCleanupService>();

            _mockReferenceValidator
                .Setup(x => x.ValidateReferencesAsync<AssetTypeOfUseGroupEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AssetTypeOfUseGroupMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _service = new AssetTypeOfUseGroupService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);
        }

        #region Entity & DTO Tests

        [Fact]
        public void Entity_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var entity = new AssetTypeOfUseGroupEntity
            {
                Id = 1,
                TypeOfUseGroupCode = "G1",
                GroupName = "Group 1",
                GroupIcon = "icon1.png",
                IsFloorWiseRateApplicable = true,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = date,
                UpdatedBy = 2,
                UpdatedDate = date,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal("G1", entity.TypeOfUseGroupCode);
            Assert.Equal("Group 1", entity.GroupName);
            Assert.Equal("icon1.png", entity.GroupIcon);
            Assert.True(entity.IsFloorWiseRateApplicable);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void Dto_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var dto = new AssetTypeOfUseGroupDto
            {
                Id = 1,
                TypeOfUseGroupCode = "G1",
                GroupName = "Group 1",
                GroupIcon = "icon1.png",
                IsFloorWiseRateApplicable = true,
                IsActive = true,
                CreatedDate = date
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal("G1", dto.TypeOfUseGroupCode);
            Assert.Equal("Group 1", dto.GroupName);
            Assert.True(dto.IsFloorWiseRateApplicable);
        }

        [Fact]
        public void CreateDto_Validation()
        {
            var dto = new CreateAssetTypeOfUseGroupDto
            {
                TypeOfUseGroupCode = "G1",
                GroupName = "Group 1",
                GroupIcon = "icon1.png",
                IsFloorWiseRateApplicable = false,
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
            var list = new List<AssetTypeOfUseGroupEntity>
            {
                new AssetTypeOfUseGroupEntity { Id = 1, TypeOfUseGroupCode = "G1", GroupName = "Group 1", GroupIcon = "icon1", IsActive = true }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var result = await _service.GetAllAsync(new AssetTypeOfUseGroupQueryParameters { SearchTerm = "Group" }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task Service_GetByIdAsync_Existing_ReturnsDto()
        {
            var entity = new AssetTypeOfUseGroupEntity { Id = 1, TypeOfUseGroupCode = "G1", GroupName = "Group 1", GroupIcon = "icon1", IsActive = true };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("G1", result.TypeOfUseGroupCode);
        }

        [Fact]
        public async Task Service_CreateAsync_Valid_CreatesSuccessfully()
        {
            var list = new List<AssetTypeOfUseGroupEntity>();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<AssetTypeOfUseGroupEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AssetTypeOfUseGroupEntity e, CancellationToken ct) => e);

            var createDto = new CreateAssetTypeOfUseGroupDto { TypeOfUseGroupCode = "G1", GroupName = "Group 1", GroupIcon = "icon1", IsActive = true };
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("G1", result.TypeOfUseGroupCode);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_CreateAsync_DuplicateCode_ThrowsValidationException()
        {
            var list = new List<AssetTypeOfUseGroupEntity>
            {
                new AssetTypeOfUseGroupEntity { Id = 1, TypeOfUseGroupCode = "G1", GroupName = "Group 1", GroupIcon = "icon1" }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var createDto = new CreateAssetTypeOfUseGroupDto { TypeOfUseGroupCode = "G1", GroupName = "Group Distinct", GroupIcon = "icon1" };

            await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
        }

        [Fact]
        public async Task Service_UpdateAsync_Valid_UpdatesSuccessfully()
        {
            var existingEntity = new AssetTypeOfUseGroupEntity { Id = 1, TypeOfUseGroupCode = "G1", GroupName = "Old", GroupIcon = "icon1", IsActive = true };
            var list = new List<AssetTypeOfUseGroupEntity> { existingEntity };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var updateDto = new UpdateAssetTypeOfUseGroupDto { TypeOfUseGroupCode = "G1", GroupName = "Updated", GroupIcon = "icon1", IsActive = true };
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Updated", result.GroupName);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_DeleteAsync_Existing_DeletesSuccessfully()
        {
            var entity = new AssetTypeOfUseGroupEntity { Id = 1, TypeOfUseGroupCode = "G1", GroupName = "G1", GroupIcon = "i" };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.DeleteAsync(1, CancellationToken.None);

            Assert.True(result);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

    }
}
