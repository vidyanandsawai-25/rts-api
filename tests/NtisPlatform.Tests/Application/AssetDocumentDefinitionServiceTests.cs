using AutoMapper;
using MockQueryable;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Tests.Application;

public class AssetDocumentDefinitionServiceTests
{
    private readonly Mock<IRepository<AssetDocumentDefinitionEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<ILogger<AssetDocumentDefinitionController>> _mockLogger;
    private readonly AssetDocumentDefinitionService _service;

    public AssetDocumentDefinitionServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetDocumentDefinitionEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockLogger = new Mock<ILogger<AssetDocumentDefinitionController>>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new AssetDocumentDefinitionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    private static AssetDocumentDefinitionEntity CreateEntity(
        int id = 1,
        int assetCategoryId = 1,
        int? assetTypeId = 2,
        string documentCode = "DOC001",
        string documentName = "Inward Doc",
        bool isActive = true)
    {
        return new AssetDocumentDefinitionEntity
        {
            Id = id,
            AssetCategoryId = assetCategoryId,
            AssetTypeId = assetTypeId,
            DocumentCode = documentCode,
            DocumentName = documentName,
            Description = "Desc",
            IsRequired = true,
            MaxFileSizeMB = 10,
            AllowedExtensions = ".pdf",
            DisplayOrder = 1,
            IsActive = isActive,
            CreatedBy = 1,
            CreatedDate = DateTime.UtcNow,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };
    }

    [Fact]
    public void TestAllProperties_ToEnsureCodeCoverage()
    {
        // Entity
        var entity = new AssetDocumentDefinitionEntity
        {
            Id = 1,
            AssetCategoryId = 2,
            AssetTypeId = 3,
            DocumentCode = "A",
            DocumentName = "B",
            Description = "C",
            IsRequired = true,
            MaxFileSizeMB = 10,
            AllowedExtensions = ".pdf",
            DisplayOrder = 1,
            IsActive = true,
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.UtcNow
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.AssetCategoryId);
        Assert.Equal(3, entity.AssetTypeId);
        Assert.Equal("A", entity.DocumentCode);
        Assert.Equal("B", entity.DocumentName);
        Assert.Equal("C", entity.Description);
        Assert.True(entity.IsRequired);
        Assert.Equal(10, entity.MaxFileSizeMB);
        Assert.Equal(".pdf", entity.AllowedExtensions);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);

        // DTOs
        var dto = new AssetDocumentDefinitionDto
        {
            Id = 1,
            AssetCategoryId = 2,
            AssetTypeId = 3,
            DocumentCode = "A",
            DocumentName = "B",
            Description = "C",
            IsRequired = true,
            MaxFileSizeMB = 10,
            AllowedExtensions = ".pdf",
            DisplayOrder = 1,
            IsActive = true
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.AssetCategoryId);
        Assert.Equal(3, dto.AssetTypeId);
        Assert.Equal("A", dto.DocumentCode);
        Assert.Equal("B", dto.DocumentName);
        Assert.Equal("C", dto.Description);
        Assert.True(dto.IsRequired);
        Assert.Equal(10, dto.MaxFileSizeMB);
        Assert.Equal(".pdf", dto.AllowedExtensions);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsActive);

        var createDto = new CreateAssetDocumentDefinitionDto
        {
            AssetCategoryId = 2,
            AssetTypeId = 3,
            DocumentCode = "A",
            DocumentName = "B",
            Description = "C",
            IsRequired = true,
            MaxFileSizeMB = 10,
            AllowedExtensions = ".pdf",
            DisplayOrder = 1,
            CreatedBy = 2
        };
        Assert.Equal(2, createDto.AssetCategoryId);
        Assert.Equal(3, createDto.AssetTypeId);
        Assert.Equal("A", createDto.DocumentCode);
        Assert.Equal("B", createDto.DocumentName);
        Assert.Equal("C", createDto.Description);
        Assert.True(createDto.IsRequired);
        Assert.Equal(10, createDto.MaxFileSizeMB);
        Assert.Equal(".pdf", createDto.AllowedExtensions);
        Assert.Equal(1, createDto.DisplayOrder);
        Assert.Equal(2, createDto.CreatedBy);

        var updateDto = new UpdateAssetDocumentDefinitionDto
        {
            AssetCategoryId = 2,
            AssetTypeId = 3,
            DocumentCode = "A",
            DocumentName = "B",
            Description = "C",
            IsRequired = true,
            MaxFileSizeMB = 10,
            AllowedExtensions = ".pdf",
            DisplayOrder = 1,
            UpdatedBy = 3,
            IsActive = false
        };
        Assert.Equal(2, updateDto.AssetCategoryId);
        Assert.Equal(3, updateDto.AssetTypeId);
        Assert.Equal("A", updateDto.DocumentCode);
        Assert.Equal("B", updateDto.DocumentName);
        Assert.Equal("C", updateDto.Description);
        Assert.True(updateDto.IsRequired);
        Assert.Equal(10, updateDto.MaxFileSizeMB);
        Assert.Equal(".pdf", updateDto.AllowedExtensions);
        Assert.Equal(1, updateDto.DisplayOrder);
        Assert.Equal(3, updateDto.UpdatedBy);
        Assert.False(updateDto.IsActive);

        // Query parameters
        var qp = new AssetDocumentDefinitionQueryParameters
        {
            AssetCategoryId = 2,
            AssetTypeId = 3,
            DocumentCode = "A",
            DocumentName = "B",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };
        Assert.Equal(2, qp.AssetCategoryId);
        Assert.Equal(3, qp.AssetTypeId);
        Assert.Equal("A", qp.DocumentCode);
        Assert.Equal("B", qp.DocumentName);
        Assert.True(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    [Fact]
    public void TestMappingProfile()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetDocumentDefinitionMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AssetDocumentDefinitionDto>(entity)).Returns(new AssetDocumentDefinitionDto { Id = 1, DocumentCode = "DOC001" });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var entities = new List<AssetDocumentDefinitionEntity> { CreateEntity(1), CreateEntity(2) };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<AssetDocumentDefinitionMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var service = new AssetDocumentDefinitionService(_mockRepository.Object, _mockUnitOfWork.Object, mapperConfig.CreateMapper(), _mockReferenceValidator.Object);

        var qp = new AssetDocumentDefinitionQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await service.GetAllAsync(qp);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_Succeeds()
    {
        var dto = new CreateAssetDocumentDefinitionDto { AssetCategoryId = 1, DocumentCode = "DOC001", DocumentName = "Test" };
        var entity = CreateEntity(0, 1, null, "DOC001", "Test");

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDocumentDefinitionEntity>().BuildMock());
        _mockMapper.Setup(m => m.Map<AssetDocumentDefinitionEntity>(dto)).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AssetDocumentDefinitionDto>(entity)).Returns(new AssetDocumentDefinitionDto { Id = 1 });

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Duplicate_ThrowsValidationException()
    {
        var dto = new CreateAssetDocumentDefinitionDto { AssetCategoryId = 1, AssetTypeId = 2, DocumentCode = "DOC001" };
        var existing = CreateEntity(1, 1, 2, "DOC001");
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetDocumentDefinitionEntity> { existing }.BuildMock());
        _mockMapper.Setup(m => m.Map<AssetDocumentDefinitionEntity>(dto)).Returns(CreateEntity(0, 1, 2, "DOC001"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var dto = new UpdateAssetDocumentDefinitionDto { AssetCategoryId = 1, DocumentCode = "DOC001", IsActive = false };
        var existing = CreateEntity(1, isActive: true);

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetDocumentDefinitionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));
        _mockMapper.Setup(m => m.Map(dto, existing)).Returns(existing).Callback<UpdateAssetDocumentDefinitionDto, AssetDocumentDefinitionEntity>((d, e) => e.IsActive = d.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var existing = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetDocumentDefinitionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_NoReferences_Succeeds()
    {
        var existing = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetDocumentDefinitionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockRepository.Setup(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IAssetDocumentDefinitionService>();
        mockService.Setup(s => s.GetAllAsync(It.IsAny<AssetDocumentDefinitionQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetDocumentDefinitionDto>(new List<AssetDocumentDefinitionDto>(), 0, 1, 10));
        var controller = new AssetDocumentDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);
        var qp = new AssetDocumentDefinitionQueryParameters();

        var result = await controller.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ReturnsOk()
    {
        var mockService = new Mock<IAssetDocumentDefinitionService>();
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDocumentDefinitionDto { Id = 1 });
        var controller = new AssetDocumentDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Create_ReturnsOk()
    {
        var mockService = new Mock<IAssetDocumentDefinitionService>();
        var dto = new CreateAssetDocumentDefinitionDto { DocumentCode = "DOC001" };
        mockService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDocumentDefinitionDto { Id = 1 });
        var controller = new AssetDocumentDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_ReturnsOk()
    {
        var mockService = new Mock<IAssetDocumentDefinitionService>();
        var dto = new UpdateAssetDocumentDefinitionDto { DocumentCode = "DOC001" };
        mockService.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetDocumentDefinitionDto { Id = 1 });
        var controller = new AssetDocumentDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Update(1, dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Delete_ReturnsOk()
    {
        var mockService = new Mock<IAssetDocumentDefinitionService>();
        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new AssetDocumentDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Purge_ReturnsOk()
    {
        var mockService = new Mock<IAssetDocumentDefinitionService>();
        var controller = new AssetDocumentDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);
        _mockCleanupService.Setup(c => c.ForceHardDeleteAsync<AssetDocumentDefinitionEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
