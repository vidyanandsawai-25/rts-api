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
using NtisPlatform.Core.Interfaces;
using Xunit;
using AppValidationException = NtisPlatform.Application.Exceptions.ValidationException;
using SystemValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class AssetRentDocumentTypeServiceTests
{
    private readonly Mock<IRepository<AssetRentDocumentTypeEntity, int>> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IReferenceValidationService> _referenceValidatorMock = new();
    private readonly IMapper _mapper;

    public AssetRentDocumentTypeServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetRentDocumentTypeMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _referenceValidatorMock
            .Setup(x => x.ValidateReferencesAsync<AssetRentDocumentTypeEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());
    }

    private AssetRentDocumentTypeService CreateService() =>
        new(_repositoryMock.Object, _unitOfWorkMock.Object, _mapper, _referenceValidatorMock.Object);

    #region Entity & DTO Property Coverage

    [Fact]
    public void EntityAndDtos_Properties_GetSet_WorksCorrectly()
    {
        var now = DateTime.UtcNow;
        var entity = new AssetRentDocumentTypeEntity
        {
            Id = 1,
            DocumentTypeCode = "LEASE",
            DocumentTypeName = "Lease Agreement",
            Description = "Lease agreement doc",
            DisplayOrder = 1,
            IsRequired = true,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("LEASE", entity.DocumentTypeCode);
        Assert.Equal("Lease Agreement", entity.DocumentTypeName);
        Assert.Equal("Lease agreement doc", entity.Description);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsRequired);
        Assert.True(entity.IsActive);

        var dto = new AssetRentDocumentTypeDto
        {
            Id = 1,
            DocumentTypeCode = "LEASE",
            DocumentTypeName = "Lease Agreement",
            Description = "Lease agreement doc",
            DisplayOrder = 1,
            IsRequired = true,
            IsActive = true,
            CreatedDate = now,
            MarkedForDeletion = false
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("LEASE", dto.DocumentTypeCode);

        var updateDto = new UpdateAssetRentDocumentTypeDto
        {
            DocumentTypeCode = "LEASE",
            DocumentTypeName = "Updated Lease",
            Description = "Updated lease description",
            DisplayOrder = 2,
            IsRequired = false,
            IsActive = true
        };

        Assert.Equal("Updated Lease", updateDto.DocumentTypeName);

        var qp = new AssetRentDocumentTypeQueryParameters
        {
            DocumentTypeCode = "LEASE",
            DocumentTypeName = "Lease",
            SearchTerm = "Agreement",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };

        Assert.Equal("LEASE", qp.DocumentTypeCode);
        Assert.Equal("Lease", qp.DocumentTypeName);
        Assert.Equal("Agreement", qp.SearchTerm);
    }

    [Fact]
    public void CreateDto_Validation_Passes()
    {
        var dto = new CreateAssetRentDocumentTypeDto
        {
            DocumentTypeCode = "LEASE",
            DocumentTypeName = "Lease Agreement",
            Description = "Lease doc",
            DisplayOrder = 1,
            IsRequired = true,
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
        var list = new List<AssetRentDocumentTypeEntity>
        {
            new()
            {
                Id = 1,
                DocumentTypeCode = "LEASE",
                DocumentTypeName = "Lease Agreement",
                IsActive = true
            }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var qp = new AssetRentDocumentTypeQueryParameters { SearchTerm = "LEASE", DocumentTypeCode = "LEASE", DocumentTypeName = "Lease" };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDto()
    {
        var entity = new AssetRentDocumentTypeEntity { Id = 1, DocumentTypeCode = "LEASE", DocumentTypeName = "Lease Agreement", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("LEASE", result.DocumentTypeCode);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsDto()
    {
        var existingList = new List<AssetRentDocumentTypeEntity>();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<AssetRentDocumentTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetRentDocumentTypeEntity entity, CancellationToken _) => { entity.Id = 1; return entity; });

        var service = CreateService();
        var dto = new CreateAssetRentDocumentTypeDto
        {
            DocumentTypeCode = "LEASE",
            DocumentTypeName = "Lease Agreement",
            Description = "Lease doc"
        };

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("LEASE", result.DocumentTypeCode);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsValidationException()
    {
        var existingList = new List<AssetRentDocumentTypeEntity>
        {
            new() { Id = 1, DocumentTypeCode = "LEASE", MarkedForDeletion = false }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);

        var service = CreateService();
        var dto = new CreateAssetRentDocumentTypeDto
        {
            DocumentTypeCode = "LEASE",
            DocumentTypeName = "Lease Agreement"
        };

        await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesSuccessfully()
    {
        var existingEntity = new AssetRentDocumentTypeEntity { Id = 1, DocumentTypeCode = "LEASE", DocumentTypeName = "Old", IsActive = true };
        var list = new List<AssetRentDocumentTypeEntity> { existingEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetRentDocumentTypeDto
        {
            DocumentTypeCode = "LEASE",
            DocumentTypeName = "Updated Lease",
            IsActive = true
        };

        var result = await service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Lease", result.DocumentTypeName);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateCode_ThrowsValidationException()
    {
        var existingEntity = new AssetRentDocumentTypeEntity { Id = 1, DocumentTypeCode = "LEASE", IsActive = true };
        var otherEntity = new AssetRentDocumentTypeEntity { Id = 2, DocumentTypeCode = "RENT", IsActive = true };
        var list = new List<AssetRentDocumentTypeEntity> { existingEntity, otherEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateAssetRentDocumentTypeDto { DocumentTypeCode = "RENT", DocumentTypeName = "Rent Agreement", IsActive = true };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Deactivation_WithReferences_ThrowsValidationException()
    {
        var existingEntity = new AssetRentDocumentTypeEntity { Id = 1, DocumentTypeCode = "LEASE", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _referenceValidatorMock
            .Setup(x => x.ValidateReferencesAsync<AssetRentDocumentTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Id", "In use"));

        var service = CreateService();
        var updateDto = new UpdateAssetRentDocumentTypeDto { DocumentTypeCode = "LEASE", DocumentTypeName = "Lease Agreement", IsActive = false };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesSuccessfully()
    {
        var entity = new AssetRentDocumentTypeEntity { Id = 1, DocumentTypeCode = "LEASE" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
