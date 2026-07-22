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

public class AssetFieldDefinitionServiceTests
{
    private readonly Mock<IRepository<AssetFieldDefinitionEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<ILogger<AssetFieldDefinitionController>> _mockLogger;
    private readonly AssetFieldDefinitionService _service;

    public AssetFieldDefinitionServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetFieldDefinitionEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockLogger = new Mock<ILogger<AssetFieldDefinitionController>>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new AssetFieldDefinitionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    private static AssetFieldDefinitionEntity CreateEntity(
        int id = 1,
        int assetCategoryId = 1,
        int assetTypeId = 2,
        string fieldCode = "FLD001",
        string fieldName = "SerialNumber",
        bool isActive = true)
    {
        return new AssetFieldDefinitionEntity
        {
            Id = id,
            AssetCategoryId = assetCategoryId,
            AssetTypeId = assetTypeId,
            FieldCode = fieldCode,
            FieldName = fieldName,
            FieldLabel = "Serial Number",
            FieldType = "Text",
            FieldGroup = "Group1",
            IsRequired = true,
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
        var entity = new AssetFieldDefinitionEntity
        {
            Id = 1,
            AssetCategoryId = 2,
            AssetTypeId = 3,
            FieldCode = "A",
            FieldName = "B",
            FieldLabel = "C",
            FieldType = "D",
            FieldGroup = "E",
            IsRequired = true,
            DisplayOrder = 1,
            IsActive = true,
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.UtcNow
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.AssetCategoryId);
        Assert.Equal(3, entity.AssetTypeId);
        Assert.Equal("A", entity.FieldCode);
        Assert.Equal("B", entity.FieldName);
        Assert.Equal("C", entity.FieldLabel);
        Assert.Equal("D", entity.FieldType);
        Assert.Equal("E", entity.FieldGroup);
        Assert.True(entity.IsRequired);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);

        // DTOs
        var dto = new AssetFieldDefinitionDto
        {
            Id = 1,
            AssetCategoryId = 2,
            AssetTypeId = 3,
            FieldCode = "A",
            FieldName = "B",
            FieldLabel = "C",
            FieldType = "D",
            FieldGroup = "E",
            IsRequired = true,
            DisplayOrder = 1,
            IsActive = true
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.AssetCategoryId);
        Assert.Equal(3, dto.AssetTypeId);
        Assert.Equal("A", dto.FieldCode);
        Assert.Equal("B", dto.FieldName);
        Assert.Equal("C", dto.FieldLabel);
        Assert.Equal("D", dto.FieldType);
        Assert.Equal("E", dto.FieldGroup);
        Assert.True(dto.IsRequired);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsActive);

        var createDto = new CreateAssetFieldDefinitionDto
        {
            AssetCategoryId = 2,
            AssetTypeId = 3,
            FieldCode = "A",
            FieldName = "B",
            FieldLabel = "C",
            FieldType = "D",
            FieldGroup = "E",
            IsRequired = true,
            DisplayOrder = 1,
            CreatedBy = 2
        };
        Assert.Equal(2, createDto.AssetCategoryId);
        Assert.Equal(3, createDto.AssetTypeId);
        Assert.Equal("A", createDto.FieldCode);
        Assert.Equal("B", createDto.FieldName);
        Assert.Equal("C", createDto.FieldLabel);
        Assert.Equal("D", createDto.FieldType);
        Assert.Equal("E", createDto.FieldGroup);
        Assert.True(createDto.IsRequired);
        Assert.Equal(1, createDto.DisplayOrder);
        Assert.Equal(2, createDto.CreatedBy);

        var updateDto = new UpdateAssetFieldDefinitionDto
        {
            AssetCategoryId = 2,
            AssetTypeId = 3,
            FieldCode = "A",
            FieldName = "B",
            FieldLabel = "C",
            FieldType = "D",
            FieldGroup = "E",
            IsRequired = true,
            DisplayOrder = 1,
            UpdatedBy = 3,
            IsActive = false
        };
        Assert.Equal(2, updateDto.AssetCategoryId);
        Assert.Equal(3, updateDto.AssetTypeId);
        Assert.Equal("A", updateDto.FieldCode);
        Assert.Equal("B", updateDto.FieldName);
        Assert.Equal("C", updateDto.FieldLabel);
        Assert.Equal("D", updateDto.FieldType);
        Assert.Equal("E", updateDto.FieldGroup);
        Assert.True(updateDto.IsRequired);
        Assert.Equal(1, updateDto.DisplayOrder);
        Assert.Equal(3, updateDto.UpdatedBy);
        Assert.False(updateDto.IsActive);

        // Query parameters
        var qp = new AssetFieldDefinitionQueryParameters
        {
            AssetCategoryId = 2,
            AssetTypeId = 3,
            FieldCode = "A",
            FieldName = "B",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };
        Assert.Equal(2, qp.AssetCategoryId);
        Assert.Equal(3, qp.AssetTypeId);
        Assert.Equal("A", qp.FieldCode);
        Assert.Equal("B", qp.FieldName);
        Assert.True(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    [Fact]
    public void TestMappingProfile()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetFieldDefinitionMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AssetFieldDefinitionDto>(entity)).Returns(new AssetFieldDefinitionDto { Id = 1, FieldCode = "FLD001" });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var entities = new List<AssetFieldDefinitionEntity> { CreateEntity(1), CreateEntity(2) };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<AssetFieldDefinitionMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var service = new AssetFieldDefinitionService(_mockRepository.Object, _mockUnitOfWork.Object, mapperConfig.CreateMapper(), _mockReferenceValidator.Object);

        var qp = new AssetFieldDefinitionQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await service.GetAllAsync(qp);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_Succeeds()
    {
        var dto = new CreateAssetFieldDefinitionDto { AssetCategoryId = 1, AssetTypeId = 2, FieldCode = "FLD001", FieldName = "Test" };
        var entity = CreateEntity(0, 1, 2, "FLD001", "Test");

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetFieldDefinitionEntity>().BuildMock());
        _mockMapper.Setup(m => m.Map<AssetFieldDefinitionEntity>(dto)).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AssetFieldDefinitionDto>(entity)).Returns(new AssetFieldDefinitionDto { Id = 1 });

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Duplicate_ThrowsValidationException()
    {
        var dto = new CreateAssetFieldDefinitionDto { AssetCategoryId = 1, AssetTypeId = 2, FieldCode = "FLD001" };
        var existing = CreateEntity(1, 1, 2, "FLD001");
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetFieldDefinitionEntity> { existing }.BuildMock());
        _mockMapper.Setup(m => m.Map<AssetFieldDefinitionEntity>(dto)).Returns(CreateEntity(0, 1, 2, "FLD001"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var dto = new UpdateAssetFieldDefinitionDto { AssetCategoryId = 1, AssetTypeId = 2, FieldCode = "FLD001", IsActive = false };
        var existing = CreateEntity(1, isActive: true);

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetFieldDefinitionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));
        _mockMapper.Setup(m => m.Map(dto, existing)).Returns(existing).Callback<UpdateAssetFieldDefinitionDto, AssetFieldDefinitionEntity>((d, e) => e.IsActive = d.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var existing = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetFieldDefinitionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_NoReferences_Succeeds()
    {
        var existing = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetFieldDefinitionEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockRepository.Setup(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IAssetFieldDefinitionService>();
        mockService.Setup(s => s.GetAllAsync(It.IsAny<AssetFieldDefinitionQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetFieldDefinitionDto>(new List<AssetFieldDefinitionDto>(), 0, 1, 10));
        var controller = new AssetFieldDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);
        var qp = new AssetFieldDefinitionQueryParameters();

        var result = await controller.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ReturnsOk()
    {
        var mockService = new Mock<IAssetFieldDefinitionService>();
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetFieldDefinitionDto { Id = 1 });
        var controller = new AssetFieldDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Create_ReturnsOk()
    {
        var mockService = new Mock<IAssetFieldDefinitionService>();
        var dto = new CreateAssetFieldDefinitionDto { FieldCode = "FLD001" };
        mockService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetFieldDefinitionDto { Id = 1 });
        var controller = new AssetFieldDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_ReturnsOk()
    {
        var mockService = new Mock<IAssetFieldDefinitionService>();
        var dto = new UpdateAssetFieldDefinitionDto { FieldCode = "FLD001" };
        mockService.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetFieldDefinitionDto { Id = 1 });
        var controller = new AssetFieldDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Update(1, dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Delete_ReturnsOk()
    {
        var mockService = new Mock<IAssetFieldDefinitionService>();
        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new AssetFieldDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Purge_ReturnsOk()
    {
        var mockService = new Mock<IAssetFieldDefinitionService>();
        var controller = new AssetFieldDefinitionController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);
        _mockCleanupService.Setup(c => c.ForceHardDeleteAsync<AssetFieldDefinitionEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
