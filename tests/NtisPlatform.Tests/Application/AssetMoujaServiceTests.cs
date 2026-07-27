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
    public class AssetMoujaServiceTests
    {
        private readonly Mock<IRepository<AssetMoujaMasterEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IMapper _mapper;
        private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
        private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
        private readonly AssetMoujaService _service;

        public AssetMoujaServiceTests()
        {
            _mockRepository = new Mock<IRepository<AssetMoujaMasterEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockReferenceValidator = new Mock<IReferenceValidationService>();
            _mockCleanupService = new Mock<IHardDeleteCleanupService>();

            _mockReferenceValidator
                .Setup(x => x.ValidateReferencesAsync<AssetMoujaMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AssetMoujaMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _service = new AssetMoujaService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);
        }

        #region Entity & DTO Tests

        [Fact]
        public void Entity_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var entity = new AssetMoujaMasterEntity
            {
                Id = 1,
                MoujaNo = "M100",
                MoujaName = "Test Mouja",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = date,
                UpdatedBy = 2,
                UpdatedDate = date,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal("M100", entity.MoujaNo);
            Assert.Equal("Test Mouja", entity.MoujaName);
            Assert.True(entity.IsActive);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(date, entity.CreatedDate);
            Assert.Equal(2, entity.UpdatedBy);
            Assert.Equal(date, entity.UpdatedDate);
            Assert.False(entity.MarkedForDeletion);
            Assert.Null(entity.MarkedForDeletionDate);
        }

        [Fact]
        public void Dto_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var dto = new AssetMoujaDto
            {
                Id = 1,
                MoujaNo = "M100",
                MoujaName = "Test Mouja",
                IsActive = true,
                CreatedDate = date,
                UpdatedDate = date,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal("M100", dto.MoujaNo);
            Assert.Equal("Test Mouja", dto.MoujaName);
            Assert.True(dto.IsActive);
            Assert.Equal(date, dto.CreatedDate);
            Assert.Equal(date, dto.UpdatedDate);
        }

        [Fact]
        public void CreateDto_Validation()
        {
            var dto = new CreateAssetMoujaDto
            {
                MoujaNo = "M100",
                MoujaName = "Test Mouja",
                IsActive = true,
                CreatedBy = 1
            };

            var results = new List<SystemValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.True(isValid);
        }

        [Fact]
        public void UpdateDto_Validation()
        {
            var dto = new UpdateAssetMoujaDto
            {
                MoujaNo = "M100",
                MoujaName = "Updated Mouja",
                IsActive = true,
                UpdatedBy = 2
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
            var list = new List<AssetMoujaMasterEntity>
            {
                new AssetMoujaMasterEntity { Id = 1, MoujaNo = "M1", MoujaName = "Mouja 1", IsActive = true }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var result = await _service.GetAllAsync(new AssetMoujaQueryParameters { SearchTerm = "Mouja" }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task Service_GetByIdAsync_Existing_ReturnsDto()
        {
            var entity = new AssetMoujaMasterEntity { Id = 1, MoujaNo = "M1", MoujaName = "Mouja 1", IsActive = true };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("M1", result.MoujaNo);
        }

        [Fact]
        public async Task Service_CreateAsync_Valid_CreatesSuccessfully()
        {
            var list = new List<AssetMoujaMasterEntity>();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<AssetMoujaMasterEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AssetMoujaMasterEntity e, CancellationToken ct) => e);

            var createDto = new CreateAssetMoujaDto { MoujaNo = "M100", MoujaName = "New Mouja", IsActive = true };
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("M100", result.MoujaNo);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_CreateAsync_DuplicateMoujaNo_ThrowsValidationException()
        {
            var list = new List<AssetMoujaMasterEntity>
            {
                new AssetMoujaMasterEntity { Id = 1, MoujaNo = "M100", MoujaName = "Existing" }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var createDto = new CreateAssetMoujaDto { MoujaNo = "M100", MoujaName = "New Mouja" };

            await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
        }

        [Fact]
        public async Task Service_UpdateAsync_Valid_UpdatesSuccessfully()
        {
            var existingEntity = new AssetMoujaMasterEntity { Id = 1, MoujaNo = "M100", MoujaName = "Old Mouja", IsActive = true };
            var list = new List<AssetMoujaMasterEntity> { existingEntity };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var updateDto = new UpdateAssetMoujaDto { MoujaNo = "M100", MoujaName = "Updated Mouja", IsActive = true };
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Updated Mouja", result.MoujaName);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_DeleteAsync_Existing_DeletesSuccessfully()
        {
            var entity = new AssetMoujaMasterEntity { Id = 1, MoujaNo = "M100" };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.DeleteAsync(1, CancellationToken.None);

            Assert.True(result);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

    }
}
