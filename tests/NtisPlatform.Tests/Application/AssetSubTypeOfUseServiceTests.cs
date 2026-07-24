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
    public class AssetSubTypeOfUseServiceTests
    {
        private readonly Mock<IRepository<AssetSubTypeOfUseEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IMapper _mapper;
        private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
        private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
        private readonly AssetSubTypeOfUseService _service;

        public AssetSubTypeOfUseServiceTests()
        {
            _mockRepository = new Mock<IRepository<AssetSubTypeOfUseEntity, int>>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockReferenceValidator = new Mock<IReferenceValidationService>();
            _mockCleanupService = new Mock<IHardDeleteCleanupService>();

            _mockReferenceValidator
                .Setup(x => x.ValidateReferencesAsync<AssetSubTypeOfUseEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AssetSubTypeOfUseMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _service = new AssetSubTypeOfUseService(
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
            var entity = new AssetSubTypeOfUseEntity
            {
                Id = 1,
                TypeOfUseId = 10,
                Description = "Sub Use 1",
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
            Assert.Equal(10, entity.TypeOfUseId);
            Assert.Equal("Sub Use 1", entity.Description);
            Assert.Equal(1, entity.SearchSequence);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void Dto_Properties_GetSet_WorksCorrectly()
        {
            var date = DateTime.Now;
            var dto = new AssetSubTypeOfUseDto
            {
                Id = 1,
                TypeOfUseId = 10,
                Description = "Sub Use 1",
                SearchSequence = 1,
                IsActive = true,
                CreatedDate = date,
                UpdatedDate = date,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal(10, dto.TypeOfUseId);
            Assert.Equal("Sub Use 1", dto.Description);
            Assert.Equal(1, dto.SearchSequence);
            Assert.True(dto.IsActive);
            Assert.Equal(date, dto.CreatedDate);
            Assert.Equal(date, dto.UpdatedDate);
            Assert.False(dto.MarkedForDeletion);
            Assert.Null(dto.MarkedForDeletionDate);
        }

        [Fact]
        public void CreateDto_Validation()
        {
            var dto = new CreateAssetSubTypeOfUseDto
            {
                TypeOfUseId = 10,
                Description = "Sub Use 1",
                SearchSequence = 1,
                IsActive = true,
                CreatedBy = 1
            };

            Assert.Equal(10, dto.TypeOfUseId);
            Assert.Equal("Sub Use 1", dto.Description);
            Assert.Equal(1, dto.SearchSequence);
            Assert.True(dto.IsActive);
            Assert.Equal(1, dto.CreatedBy);

            var results = new List<SystemValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.True(isValid);
        }

        [Fact]
        public void UpdateDto_Validation()
        {
            var dto = new UpdateAssetSubTypeOfUseDto
            {
                TypeOfUseId = 10,
                Description = "Sub Use 1 Updated",
                SearchSequence = 2,
                IsActive = true,
                UpdatedBy = 2
            };

            Assert.Equal(10, dto.TypeOfUseId);
            Assert.Equal("Sub Use 1 Updated", dto.Description);
            Assert.Equal(2, dto.SearchSequence);
            Assert.True(dto.IsActive);
            Assert.Equal(2, dto.UpdatedBy);

            var results = new List<SystemValidationResult>();
            var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
            Assert.True(isValid);
        }

        [Fact]
        public void QueryParameters_Properties_GetSet()
        {
            var qp = new AssetSubTypeOfUseQueryParameters
            {
                TypeOfUseId = 10,
                SearchTerm = "test",
                IsActive = true,
                MarkedForDeletion = false,
                PageNumber = 1,
                PageSize = 10,
                SortBy = "Description",
                SortOrder = "asc"
            };

            Assert.Equal(10, qp.TypeOfUseId);
            Assert.Equal("test", qp.SearchTerm);
            Assert.True(qp.IsActive);
            Assert.False(qp.MarkedForDeletion);
            Assert.Equal(1, qp.PageNumber);
            Assert.Equal(10, qp.PageSize);
            Assert.Equal("Description", qp.SortBy);
            Assert.Equal("asc", qp.SortOrder);
        }

        #endregion

        #region Service Tests

        [Fact]
        public async Task Service_GetAllAsync_ReturnsPagedResult()
        {
            var list = new List<AssetSubTypeOfUseEntity>
            {
                new AssetSubTypeOfUseEntity { Id = 1, TypeOfUseId = 10, Description = "Sub Use 1", IsActive = true }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var result = await _service.GetAllAsync(new AssetSubTypeOfUseQueryParameters { TypeOfUseId = 10 }, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task Service_GetByIdAsync_Existing_ReturnsDto()
        {
            var entity = new AssetSubTypeOfUseEntity { Id = 1, TypeOfUseId = 10, Description = "Sub Use 1", IsActive = true };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Sub Use 1", result.Description);
        }

        [Fact]
        public async Task Service_CreateAsync_Valid_CreatesSuccessfully()
        {
            var list = new List<AssetSubTypeOfUseEntity>();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);
            _mockRepository.Setup(r => r.AddAsync(It.IsAny<AssetSubTypeOfUseEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AssetSubTypeOfUseEntity e, CancellationToken ct) => e);

            var createDto = new CreateAssetSubTypeOfUseDto
            {
                TypeOfUseId = 10,
                Description = "Sub Use 1",
                SearchSequence = 1,
                IsActive = true
            };
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Sub Use 1", result.Description);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_CreateAsync_Duplicate_ThrowsValidationException()
        {
            var list = new List<AssetSubTypeOfUseEntity>
            {
                new AssetSubTypeOfUseEntity { Id = 1, TypeOfUseId = 10, Description = "Sub Use 1", MarkedForDeletion = false }
            };
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var createDto = new CreateAssetSubTypeOfUseDto { TypeOfUseId = 10, Description = "Sub Use 1" };

            await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
        }

        [Fact]
        public async Task Service_UpdateAsync_Valid_UpdatesSuccessfully()
        {
            var existingEntity = new AssetSubTypeOfUseEntity { Id = 1, TypeOfUseId = 10, Description = "Old", IsActive = true };
            var list = new List<AssetSubTypeOfUseEntity> { existingEntity };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var updateDto = new UpdateAssetSubTypeOfUseDto
            {
                TypeOfUseId = 10,
                Description = "Updated",
                SearchSequence = 2,
                IsActive = true
            };
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Updated", result.Description);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_UpdateAsync_Deactivate_TriggersValidation()
        {
            var existingEntity = new AssetSubTypeOfUseEntity { Id = 1, TypeOfUseId = 10, Description = "Old", IsActive = true };
            var list = new List<AssetSubTypeOfUseEntity> { existingEntity };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

            var updateDto = new UpdateAssetSubTypeOfUseDto
            {
                TypeOfUseId = 10,
                Description = "Old",
                SearchSequence = 1,
                IsActive = false
            };
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            Assert.NotNull(result);
            _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<AssetSubTypeOfUseEntity>(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Service_DeleteAsync_Existing_DeletesSuccessfully()
        {
            var entity = new AssetSubTypeOfUseEntity { Id = 1, TypeOfUseId = 10, Description = "Sub Use 1" };
            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

            var result = await _service.DeleteAsync(1, CancellationToken.None);

            Assert.True(result);
            _mockReferenceValidator.Verify(v => v.ValidateReferencesAsync<AssetSubTypeOfUseEntity>(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

    }
}
