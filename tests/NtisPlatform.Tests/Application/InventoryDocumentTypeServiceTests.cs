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

public class InventoryDocumentTypeServiceTests
{
    private readonly Mock<IRepository<InventoryDocumentTypeEntity, int>> _repositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IReferenceValidationService> _referenceValidatorMock = new();
    private readonly IMapper _mapper;

    public InventoryDocumentTypeServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<InventoryDocumentTypeMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _referenceValidatorMock
            .Setup(x => x.ValidateReferencesAsync<InventoryDocumentTypeEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());
    }

    private InventoryDocumentTypeService CreateService() =>
        new(_repositoryMock.Object, _unitOfWorkMock.Object, _mapper, _referenceValidatorMock.Object);

    #region Entity & DTO Property Coverage

    [Fact]
    public void EntityAndDtos_Properties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new InventoryDocumentTypeEntity
        {
            Id = 1,
            DocumentTypeCode = "INVOICE",
            DocumentTypeName = "Purchase Invoice",
            Description = "Purchase invoice doc",
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
        Assert.Equal("INVOICE", entity.DocumentTypeCode);
        Assert.Equal("Purchase Invoice", entity.DocumentTypeName);
        Assert.Equal("Purchase invoice doc", entity.Description);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsRequired);
        Assert.True(entity.IsActive);

        var dto = new InventoryDocumentTypeDto
        {
            Id = 1,
            DocumentTypeCode = "INVOICE",
            DocumentTypeName = "Purchase Invoice",
            Description = "Purchase invoice doc",
            DisplayOrder = 1,
            IsRequired = true,
            IsActive = true,
            CreatedDate = now,
            MarkedForDeletion = false
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("INVOICE", dto.DocumentTypeCode);

        var updateDto = new UpdateInventoryDocumentTypeDto
        {
            DocumentTypeCode = "INVOICE",
            DocumentTypeName = "Updated Invoice",
            Description = "Updated invoice description",
            DisplayOrder = 2,
            IsRequired = false,
            IsActive = true
        };

        Assert.Equal("Updated Invoice", updateDto.DocumentTypeName);

        var qp = new InventoryDocumentTypeQueryParameters
        {
            DocumentTypeCode = "INVOICE",
            DocumentTypeName = "Invoice",
            SearchTerm = "Purchase",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };

        Assert.Equal("INVOICE", qp.DocumentTypeCode);
        Assert.Equal("Invoice", qp.DocumentTypeName);
        Assert.Equal("Purchase", qp.SearchTerm);
    }

    [Fact]
    public void CreateDto_Validation_Passes()
    {
        var dto = new CreateInventoryDocumentTypeDto
        {
            DocumentTypeCode = "INVOICE",
            DocumentTypeName = "Purchase Invoice",
            Description = "Invoice doc",
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
        var list = new List<InventoryDocumentTypeEntity>
        {
            new()
            {
                Id = 1,
                DocumentTypeCode = "INVOICE",
                DocumentTypeName = "Purchase Invoice",
                IsActive = true
            }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var qp = new InventoryDocumentTypeQueryParameters { SearchTerm = "INVOICE", DocumentTypeCode = "INVOICE", DocumentTypeName = "Invoice" };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDto()
    {
        var entity = new InventoryDocumentTypeEntity { Id = 1, DocumentTypeCode = "INVOICE", DocumentTypeName = "Purchase Invoice", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("INVOICE", result.DocumentTypeCode);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsDto()
    {
        var existingList = new List<InventoryDocumentTypeEntity>();
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<InventoryDocumentTypeEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryDocumentTypeEntity entity, CancellationToken _) => { entity.Id = 1; return entity; });

        var service = CreateService();
        var dto = new CreateInventoryDocumentTypeDto
        {
            DocumentTypeCode = "INVOICE",
            DocumentTypeName = "Purchase Invoice",
            Description = "Invoice doc"
        };

        var result = await service.CreateAsync(dto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("INVOICE", result.DocumentTypeCode);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ThrowsValidationException()
    {
        var existingList = new List<InventoryDocumentTypeEntity>
        {
            new() { Id = 1, DocumentTypeCode = "INVOICE", MarkedForDeletion = false }
        };
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(existingList.BuildMockDbSet().Object);

        var service = CreateService();
        var dto = new CreateInventoryDocumentTypeDto
        {
            DocumentTypeCode = "INVOICE",
            DocumentTypeName = "Purchase Invoice"
        };

        await Assert.ThrowsAsync<AppValidationException>(() => service.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesSuccessfully()
    {
        var existingEntity = new InventoryDocumentTypeEntity { Id = 1, DocumentTypeCode = "INVOICE", DocumentTypeName = "Old", IsActive = true };
        var list = new List<InventoryDocumentTypeEntity> { existingEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateInventoryDocumentTypeDto
        {
            DocumentTypeCode = "INVOICE",
            DocumentTypeName = "Updated Invoice",
            IsActive = true
        };

        var result = await service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Invoice", result.DocumentTypeName);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateCode_ThrowsValidationException()
    {
        var existingEntity = new InventoryDocumentTypeEntity { Id = 1, DocumentTypeCode = "INVOICE", IsActive = true };
        var otherEntity = new InventoryDocumentTypeEntity { Id = 2, DocumentTypeCode = "RECEIPT", IsActive = true };
        var list = new List<InventoryDocumentTypeEntity> { existingEntity, otherEntity };

        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _repositoryMock.Setup(r => r.GetQueryable()).Returns(list.BuildMockDbSet().Object);

        var service = CreateService();
        var updateDto = new UpdateInventoryDocumentTypeDto { DocumentTypeCode = "RECEIPT", DocumentTypeName = "Receipt doc", IsActive = true };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_Deactivation_WithReferences_ThrowsValidationException()
    {
        var existingEntity = new InventoryDocumentTypeEntity { Id = 1, DocumentTypeCode = "INVOICE", IsActive = true };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _referenceValidatorMock
            .Setup(x => x.ValidateReferencesAsync<InventoryDocumentTypeEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Id", "In use"));

        var service = CreateService();
        var updateDto = new UpdateInventoryDocumentTypeDto { DocumentTypeCode = "INVOICE", DocumentTypeName = "Purchase Invoice", IsActive = false };

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_Existing_DeletesSuccessfully()
    {
        var entity = new InventoryDocumentTypeEntity { Id = 1, DocumentTypeCode = "INVOICE" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();
        var result = await service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
