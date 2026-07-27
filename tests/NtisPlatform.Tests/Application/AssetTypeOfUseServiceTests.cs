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
    public class AssetTypeOfUseServiceTests
    {
        private readonly Mock<IRepository<AssetTypeOfUseMasterEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IMapper _mapper;
        private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
        private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
        private readonly AssetTypeOfUseService _service;

        public AssetTypeOfUseServiceTests()
        {
            _mockRepository = new Mock<IRepository<AssetTypeOfUseMasterEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockReferenceValidator = new Mock<IReferenceValidationService>();
            _mockCleanupService = new Mock<IHardDeleteCleanupService>();

            _mockReferenceValidator
                .Setup(x => x.ValidateReferencesAsync<AssetTypeOfUseMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AssetTypeOfUseMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _service = new AssetTypeOfUseService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                _mapper,
                _mockReferenceValidator.Object);
        }

        #region Entity & DTO Tests

        [Fact]
        public void Entity_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var entity = new AssetTypeOfUseMasterEntity
            {
                Id = 1,
                AssetCategoryId = 1,
                AssetTypeId = 11,
                TypeOfUseCode = "U1",
                Description = "Use 1",
                Type = "R",
                TypeOfUseGroupId = 1,
                SearchSequence = 1,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = date,
                UpdatedBy = 2,
                UpdatedDate = date,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(1, entity.AssetCategoryId);
            Assert.Equal(11, entity.AssetTypeId);
            Assert.Equal("U1", entity.TypeOfUseCode);
            Assert.Equal("Use 1", entity.Description);
            Assert.Equal("R", entity.Type);
            Assert.Equal(1, entity.TypeOfUseGroupId);
            Assert.Equal(1, entity.SearchSequence);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void Dto_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var dto = new AssetTypeOfUseDto
            {
                Id = 1,
                AssetCategoryId = 1,
                AssetCategoryName = "Cat 1",
                AssetTypeId = 11,
                AssetTypeName = "Type 11",
                TypeOfUseGroupId = 1,
                TypeOfUseGroupName = "Group 1",
                TypeOfUseCode = "U1",
                Description = "Use 1",
                Type = "R",
                SearchSequence = 1,
                IsActive = true,
                CreatedDate = date
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal("Cat 1", dto.AssetCategoryName);
            Assert.Equal("Type 11", dto.AssetTypeName);
            Assert.Equal("Group 1", dto.TypeOfUseGroupName);
            Assert.Equal("U1", dto.TypeOfUseCode);
            Assert.Equal("R", dto.Type);
        }

        [Fact]
        public void CreateDto_Validation()
        {
            var dto = new CreateAssetTypeOfUseDto
            {
                AssetCategoryId = 1,
                AssetTypeId = 11,
                TypeOfUseGroupId = 1,
                TypeOfUseCode = "U1",
                Description = "Use 1",
                Type = "R",
                SearchSequence = 1,
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
            var list = new List<AssetTypeOfUseMasterEntity>
            {
                new AssetTypeOfUseMasterEntity { Id = 1, AssetCategoryId = 1, AssetTypeId = 11, TypeOfUseGroupId = 1, TypeOfUseCode = "U1", Description = "Use 1", IsActive = true }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var result = await _service.GetAllAsync(new AssetTypeOfUseQueryParameters { TypeOfUseGroupId = 1 }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task Service_GetByIdAsync_Existing_ReturnsDto()
        {
            var entity = new AssetTypeOfUseMasterEntity { Id = 1, AssetCategoryId = 1, AssetTypeId = 11, TypeOfUseGroupId = 1, TypeOfUseCode = "U1", Description = "Use 1", IsActive = true };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("U1", result.TypeOfUseCode);
        }

        [Fact]
        public async Task Service_CreateAsync_Valid_CreatesSuccessfully()
        {
            var list = new List<AssetTypeOfUseMasterEntity>();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<AssetTypeOfUseMasterEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AssetTypeOfUseMasterEntity e, CancellationToken ct) => e);

            var createDto = new CreateAssetTypeOfUseDto
            {
                AssetCategoryId = 1,
                AssetTypeId = 11,
                TypeOfUseGroupId = 1,
                TypeOfUseCode = "U1",
                Description = "Use 1",
                Type = "R",
                SearchSequence = 1,
                IsActive = true
            };
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("U1", result.TypeOfUseCode);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_UpdateAsync_Valid_UpdatesSuccessfully()
        {
            var existingEntity = new AssetTypeOfUseMasterEntity { Id = 1, AssetCategoryId = 1, AssetTypeId = 11, TypeOfUseGroupId = 1, TypeOfUseCode = "U1", Description = "Old", IsActive = true };
            var list = new List<AssetTypeOfUseMasterEntity> { existingEntity };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var updateDto = new UpdateAssetTypeOfUseDto
            {
                AssetCategoryId = 1,
                AssetTypeId = 11,
                TypeOfUseGroupId = 1,
                TypeOfUseCode = "U1",
                Description = "Updated",
                Type = "I",
                SearchSequence = 2,
                IsActive = true
            };
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Updated", result.Description);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_DeleteAsync_Existing_DeletesSuccessfully()
        {
            var entity = new AssetTypeOfUseMasterEntity { Id = 1, AssetCategoryId = 1, AssetTypeId = 11, TypeOfUseCode = "U1" };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.DeleteAsync(1, CancellationToken.None);

            Assert.True(result);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

    }
}
