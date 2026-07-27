using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;
using AppValidationException = NtisPlatform.Application.Exceptions.ValidationException;
using SystemValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class AssetPhotoTypeServiceTests
{
    private readonly Mock<IRepository<AssetPhotoTypeEntity, int>> _repositoryMock = new();
    private readonly Mock<IRepository<AssetCategoryEntity, int>> _categoryRepoMock = new();
    private readonly Mock<IRepository<AssetTypeEntity, int>> _typeRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IReferenceValidationService> _referenceValidatorMock = new();
    private readonly IMapper _mapper;

    public AssetPhotoTypeServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetPhotoTypeMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _referenceValidatorMock
            .Setup(x => x.ValidateReferencesAsync<AssetPhotoTypeEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());
    }

    private AssetPhotoTypeService CreateService() =>
        new(_repositoryMock.Object, _categoryRepoMock.Object, _typeRepoMock.Object, _unitOfWorkMock.Object, _mapper, _referenceValidatorMock.Object);

    #region Entity & DTO Property Coverage

    [Fact]
    public void EntityAndDtos_Properties_GetSet_WorksCorrectly()
    {
        var now = DateTime.UtcNow;
        var entity = new AssetPhotoTypeEntity
        {
            Id = 1,
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Front Elevation",
            Description = "Front view photo",
            AssetCategoryId = 2,
            AssetCategory = new AssetCategoryEntity { Id = 2, CategoryName = "Real Estate" },
            AssetTypeId = 3,
            AssetType = new AssetTypeEntity { Id = 3, TypeName = "Building" },
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("FRONT_ELEVATION", entity.PhotoTypeCode);
        Assert.Equal("Front Elevation", entity.PhotoTypeName);
        Assert.Equal("Front view photo", entity.Description);
        Assert.Equal(2, entity.AssetCategoryId);
        Assert.Equal(3, entity.AssetTypeId);

        var dto = new AssetPhotoTypeDto
        {
            Id = 1,
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Front Elevation",
            Description = "Front view photo",
            AssetCategoryId = 2,
            AssetCategoryName = "Real Estate",
            AssetTypeId = 3,
            AssetTypeName = "Building",
            IsActive = true,
            CreatedDate = now,
            MarkedForDeletion = false
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("Real Estate", dto.AssetCategoryName);
        Assert.Equal("Building", dto.AssetTypeName);

        var updateDto = new UpdateAssetPhotoTypeDto
        {
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Updated Front Elevation",
            Description = "Updated description",
            AssetCategoryId = 2,
            AssetTypeId = 3,
            IsActive = true
        };

        Assert.Equal("Updated Front Elevation", updateDto.PhotoTypeName);

        var qp = new AssetPhotoTypeQueryParameters
        {
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Front",
            AssetCategoryId = 2,
            AssetTypeId = 3,
            SearchTerm = "Elevation",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };

        Assert.Equal("FRONT_ELEVATION", qp.PhotoTypeCode);
        Assert.Equal("Front", qp.PhotoTypeName);
        Assert.Equal(2, qp.AssetCategoryId);
        Assert.Equal(3, qp.AssetTypeId);
        Assert.Equal("Elevation", qp.SearchTerm);
    }

    [Fact]
    public void CreateDto_Validation_Passes()
    {
        var dto = new CreateAssetPhotoTypeDto
        {
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Front Elevation",
            Description = "Front view",
            AssetCategoryId = 2,
            AssetTypeId = 3,
            IsActive = true
        };

        var results = new List<SystemValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
    }

    #endregion

    #region Service Operations

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var list = new List<AssetPhotoTypeEntity>
        {
            new()
            {
                Id = 1,
                PhotoTypeCode = "FRONT_ELEVATION",
                PhotoTypeName = "Front Elevation",
                AssetCategoryId = 1,
                AssetTypeId = 1,
                IsActive = true
            }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var qp = new AssetPhotoTypeQueryParameters { SearchTerm = "FRONT", PhotoTypeCode = "FRONT_ELEVATION", PhotoTypeName = "Front", AssetCategoryId = 1, AssetTypeId = 1 };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDto()
    {
        var entity = new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT_ELEVATION", PhotoTypeName = "Front Elevation", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("FRONT_ELEVATION", result.PhotoTypeCode);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsDto()
    {
        var categories = new List<AssetCategoryEntity> { new() { Id = 1, MarkedForDeletion = false } };
        _categoryRepoMock.Setup(r => r.GetQueryable()).Returns(categories.BuildMockDbSet().Object);

        var types = new List<AssetTypeEntity> { new() { Id = 1, MarkedForDeletion = false } };
        _typeRepoMock.Setup(r => r.GetQueryable()).Returns(types.BuildMockDbSet().Object);

        var existingList = new List<AssetPhotoTypeEntity>();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<AssetPhotoTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetPhotoTypeEntity entity, CancellationToken _) => { entity.Id = 1; return entity; });

        var service = CreateService();
        var dto = new CreateAssetPhotoTypeDto
        {
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Front Elevation Photo",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("FRONT_ELEVATION", result.PhotoTypeCode);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsValidationException()
    {
        var existingList = new List<AssetPhotoTypeEntity>
        {
            new() { Id = 1, PhotoTypeCode = "FRONT_ELEVATION", MarkedForDeletion = false }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);

        var service = CreateService();
        var dto = new CreateAssetPhotoTypeDto
        {
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Front Elevation Photo"
        };

        await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_InvalidCategory_ThrowsValidationException()
    {
        var existingList = new List<AssetPhotoTypeEntity>();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);
        _categoryRepoMock.Setup(r => r.GetQueryable()).Returns(new List<AssetCategoryEntity>().BuildMockDbSet().Object);

        var service = CreateService();
        var dto = new CreateAssetPhotoTypeDto
        {
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Front Elevation Photo",
            AssetCategoryId = 99
        };

        await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_InvalidType_ThrowsValidationException()
    {
        var existingList = new List<AssetPhotoTypeEntity>();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);
        _categoryRepoMock.Setup(r => r.GetQueryable()).Returns(new List<AssetCategoryEntity> { new() { Id = 1 } }.BuildMockDbSet().Object);
        _typeRepoMock.Setup(r => r.GetQueryable()).Returns(new List<AssetTypeEntity>().BuildMockDbSet().Object);

        var service = CreateService();
        var dto = new CreateAssetPhotoTypeDto
        {
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Front Elevation Photo",
            AssetCategoryId = 1,
            AssetTypeId = 99
        };

        await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesSuccessfully()
    {
        var existingEntity = new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT_ELEVATION", PhotoTypeName = "Old", IsActive = true };
        var list = new List<AssetPhotoTypeEntity> { existingEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var categories = new List<AssetCategoryEntity> { new() { Id = 1, MarkedForDeletion = false } };
        _categoryRepoMock.Setup(r => r.GetQueryable()).Returns(categories.BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetPhotoTypeDto
        {
            PhotoTypeCode = "FRONT_ELEVATION",
            PhotoTypeName = "Updated Front Elevation",
            AssetCategoryId = 1,
            IsActive = true
        };

        var result = await service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Front Elevation", result.PhotoTypeName);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateCode_ThrowsValidationException()
    {
        var existingEntity = new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT", IsActive = true };
        var otherEntity = new AssetPhotoTypeEntity { Id = 2, PhotoTypeCode = "SIDE", IsActive = true };
        var list = new List<AssetPhotoTypeEntity> { existingEntity, otherEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetPhotoTypeDto { PhotoTypeCode = "SIDE", PhotoTypeName = "Side Elevation", IsActive = true };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_InvalidCategory_ThrowsValidationException()
    {
        var existingEntity = new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT", IsActive = true };
        var list = new List<AssetPhotoTypeEntity> { existingEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);
        _categoryRepoMock.Setup(r => r.GetQueryable()).Returns(new List<AssetCategoryEntity>().BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetPhotoTypeDto { PhotoTypeCode = "FRONT", PhotoTypeName = "Front Elevation", AssetCategoryId = 99, IsActive = true };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_InvalidType_ThrowsValidationException()
    {
        var existingEntity = new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT", IsActive = true };
        var list = new List<AssetPhotoTypeEntity> { existingEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);
        _categoryRepoMock.Setup(r => r.GetQueryable()).Returns(new List<AssetCategoryEntity> { new() { Id = 1 } }.BuildMockDbSet().Object);
        _typeRepoMock.Setup(r => r.GetQueryable()).Returns(new List<AssetTypeEntity>().BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetPhotoTypeDto { PhotoTypeCode = "FRONT", PhotoTypeName = "Front Elevation", AssetCategoryId = 1, AssetTypeId = 99, IsActive = true };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Deactivation_WithReferences_ThrowsValidationException()
    {
        var existingEntity = new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _referenceValidatorMock
            .Setup(x => x.ValidateReferencesAsync<AssetPhotoTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Id", "In use"));

        var service = CreateService();
        var updateDto = new UpdateAssetPhotoTypeDto { PhotoTypeCode = "FRONT", PhotoTypeName = "Front Elevation", IsActive = false };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesSuccessfully()
    {
        var entity = new AssetPhotoTypeEntity { Id = 1, PhotoTypeCode = "FRONT" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
