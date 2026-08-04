using NtisPlatform.Application.Interfaces;
using AutoMapper;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Mappings;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Application;
public class InventoryItemModelServiceTests
{
    private readonly Mock<IRepository<InventoryItemModelEntity, int>> _mockRepository;
    private readonly Mock<IRepository<InventoryItemNameEntity, int>> _mockInventoryItemNameRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly InventoryItemModelService _service;

    public InventoryItemModelServiceTests()
    {
        _mockRepository = new Mock<IRepository<InventoryItemModelEntity, int>>();
        _mockInventoryItemNameRepository = new Mock<IRepository<InventoryItemNameEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<InventoryItemModelMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _mockReferenceValidator
            .Setup(v => v.ValidateReferencesAsync<InventoryItemModelEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());

        var existingItemNames = new List<InventoryItemNameEntity>
        {
            new() { Id = 1, InventoryItemCategoryId = 1, SubTypeName = "Laptop", IsActive = true }
        };
        _mockInventoryItemNameRepository.Setup(r => r.GetQueryable()).Returns(existingItemNames.BuildMockDbSet().Object);

        _service = new InventoryItemModelService(
            _mockRepository.Object,
            _mockInventoryItemNameRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockReferenceValidator.Object);
    }

    #region Entity Tests

    [Fact]
    public void Entity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new InventoryItemModelEntity
        {
            Id = 1,
            InventoryItemNameId = 2,
            ModelName = "Model X",
            DisplayOrder = 3,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.InventoryItemNameId);
        Assert.Equal("Model X", entity.ModelName);
        Assert.Equal(3, entity.DisplayOrder);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Entity_DefaultValues_AreCorrect()
    {
        var entity = new InventoryItemModelEntity();
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.InventoryItemNameId);
        Assert.Equal("", entity.ModelName);
        Assert.Equal(0, entity.DisplayOrder);
        Assert.True(entity.IsActive);
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void Dto_Properties_GetSet_WorksCorrectly()
    {
        var dto = new InventoryItemModelDto
        {
            Id = 1,
            InventoryItemNameId = 2,
            ModelName = "Model X",
            DisplayOrder = 3,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.InventoryItemNameId);
        Assert.Equal("Model X", dto.ModelName);
        Assert.Equal(3, dto.DisplayOrder);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreateInventoryItemModelDto
        {
            InventoryItemNameId = 1,
            ModelName = "Model X",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null, "InventoryItemModel_ModelName_Required")]
    [InlineData("", "InventoryItemModel_ModelName_Required")]
    public void CreateDto_InvalidModelName_FailsValidation(string? modelName, string expectedError)
    {
        var dto = new CreateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = modelName!, DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void CreateDto_ModelNameTooLong_FailsValidation()
    {
        var dto = new CreateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = new string('A', 101), DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemModel_ModelName_MaxLen_100");
    }

    [Fact]
    public void CreateDto_MissingInventoryItemNameId_FailsValidation()
    {
        var dto = new CreateInventoryItemModelDto { InventoryItemNameId = 0, ModelName = "Model X", DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemModel_InventoryItemNameId_Required");
    }

    [Fact]
    public void UpdateDto_ValidData_PassesValidation()
    {
        var dto = new UpdateInventoryItemModelDto
        {
            InventoryItemNameId = 1,
            ModelName = "Model Y",
            DisplayOrder = 2,
            IsActive = true,
            UpdatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
    }

    #endregion

    #region QueryParameters Tests

    [Fact]
    public void QueryParameters_Properties_WorkCorrectly()
    {
        var qp = new InventoryItemModelQueryParameters
        {
            InventoryItemNameId = 5,
            IsActive = true,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "Test",
            SortBy = "ModelName"
        };
        Assert.Equal(5, qp.InventoryItemNameId);
        Assert.True(qp.IsActive);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
    }

    [Fact]
    public void QueryParameters_DefaultValues_AreCorrect()
    {
        var qp = new InventoryItemModelQueryParameters();
        Assert.Null(qp.InventoryItemNameId);
        Assert.Null(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    #endregion

    #region Service CRUD Tests

    [Fact]
    public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new InventoryItemModelEntity { Id = 1, InventoryItemNameId = 2, ModelName = "Model X", DisplayOrder = 1, IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var result = await _service.GetByIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Model X", result.ModelName);
    }

    [Fact]
    public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemModelEntity?)null);
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<InventoryItemModelEntity>
        {
   new() { Id = 1, InventoryItemNameId = 1, ModelName = "Model A", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, InventoryItemNameId = 1, ModelName = "Model B", DisplayOrder = 2, IsActive = true },
         new() { Id = 3, InventoryItemNameId = 2, ModelName = "Model C", DisplayOrder = 3, IsActive = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new InventoryItemModelQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
    }

    [Theory]
    [InlineData(1, null, 2)]
    [InlineData(null, true, 2)]
    [InlineData(1, true, 1)]
    public async Task Service_GetAllAsync_WithFilters_ReturnsFilteredEntities(int? nameId, bool? isActive, int expectedCount)
    {
        var entities = new List<InventoryItemModelEntity>
        {
       new() { Id = 1, InventoryItemNameId = 1, ModelName = "Model A", DisplayOrder = 1, IsActive = true },
   new() { Id = 2, InventoryItemNameId = 1, ModelName = "Model B", DisplayOrder = 2, IsActive = false },
            new() { Id = 3, InventoryItemNameId = 2, ModelName = "Model C", DisplayOrder = 3, IsActive = true }
   };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new InventoryItemModelQueryParameters { InventoryItemNameId = nameId, IsActive = isActive, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(expectedCount, result.Items.Count());
    }

    // InventoryItemNameId is a required FK (AMS.InventoryItemModelMaster.InventoryItemNameId ->
    // AMS.InventoryItemNameMaster.Id) -- GetAll must resolve and expose the referenced item name's
    // display name (InventoryItemName, sourced from InventoryItemNameEntity.SubTypeName) via a join,
    // not just the raw FK id.
    [Fact]
    public async Task Service_GetAllAsync_PopulatesInventoryItemName_FromReferencedInventoryItemName()
    {
        var entities = new List<InventoryItemModelEntity>
        {
            new() { Id = 1, InventoryItemNameId = 1, ModelName = "Model A", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, InventoryItemNameId = 999, ModelName = "Model B", DisplayOrder = 2, IsActive = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        // _mockInventoryItemNameRepository is already stubbed in the constructor with Id=1 -> "Laptop".

        var qp = new InventoryItemModelQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);

        var mapped = result.Items.ToDictionary(x => x.Id);
        Assert.Equal("Laptop", mapped[1].InventoryItemName);
        Assert.Null(mapped[2].InventoryItemName); // InventoryItemNameId 999 doesn't resolve to any InventoryItemNameEntity
    }

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Model X", DisplayOrder = 1, IsActive = true };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemModelEntity>().BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemModelEntity>(), It.IsAny<CancellationToken>()))
   .ReturnsAsync((InventoryItemModelEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_CreateAsync_InvalidInventoryItemNameId_ThrowsValidationException()
    {
        var createDto = new CreateInventoryItemModelDto { InventoryItemNameId = 999, ModelName = "Model X", DisplayOrder = 1 };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains(nameof(CreateInventoryItemModelDto.InventoryItemNameId), ex.Errors.Keys);
    }

    [Fact]
    public async Task Service_CreateAsync_DuplicateModelNameUnderSameItemName_ThrowsValidationException()
    {
        var existing = new List<InventoryItemModelEntity>
        {
            new() { Id = 1, InventoryItemNameId = 1, ModelName = "Model X" }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);

        var createDto = new CreateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Model X", DisplayOrder = 1 };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains("InventoryItemModel_ModelName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task Service_CreateAsync_SameModelNameUnderDifferentItemName_Succeeds()
    {
        // A model name only needs to be unique within its parent item name — "Model X" already
        // exists under item name 1, but creating it again under item name 1 (the only stubbed
        // parent) is what's blocked above; here a *different* existing row under a *different*
        // item name must not collide.
        var existing = new List<InventoryItemModelEntity>
        {
            new() { Id = 1, InventoryItemNameId = 2, ModelName = "Model X" }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemModelEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemModelEntity e, CancellationToken _) => { e.Id = 2; return e; });

        var createDto = new CreateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Model X", DisplayOrder = 1 };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var existingEntity = new InventoryItemModelEntity { Id = 1, InventoryItemNameId = 1, ModelName = "Old Model", DisplayOrder = 1, IsActive = true };
        var updateDto = new UpdateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "New Model", DisplayOrder = 2, IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemModelEntity> { existingEntity }.BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemModelEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("New Model", result.ModelName);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemModelEntity?)null);
        var updateDto = new UpdateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Test", DisplayOrder = 1 };
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_UpdateAsync_InvalidInventoryItemNameId_ThrowsValidationException()
    {
        var updateDto = new UpdateInventoryItemModelDto { InventoryItemNameId = 999, ModelName = "Test", DisplayOrder = 1 };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        Assert.Contains(nameof(UpdateInventoryItemModelDto.InventoryItemNameId), ex.Errors.Keys);
    }

    [Fact]
    public async Task Service_UpdateAsync_RenameToAnotherRowsName_ThrowsValidationException()
    {
        var existingEntity = new InventoryItemModelEntity { Id = 1, InventoryItemNameId = 1, ModelName = "Model A", IsActive = true };
        var other = new InventoryItemModelEntity { Id = 2, InventoryItemNameId = 1, ModelName = "Model B", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemModelEntity> { existingEntity, other }.BuildMockDbSet().Object);

        var updateDto = new UpdateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Model B", DisplayOrder = 1, IsActive = true };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        Assert.Contains("InventoryItemModel_ModelName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task Service_UpdateAsync_DeactivationReferenced_ThrowsValidationException()
    {
        var entity = new InventoryItemModelEntity { Id = 1, InventoryItemNameId = 1, ModelName = "Model A", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemModelEntity> { entity }.BuildMockDbSet().Object);
        _mockReferenceValidator
            .Setup(x => x.ValidateReferencesAsync<InventoryItemModelEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Referenced by Inventory Batch"));

        var updateDto = new UpdateInventoryItemModelDto { InventoryItemNameId = 1, ModelName = "Model A", DisplayOrder = 1, IsActive = false };

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new InventoryItemModelEntity { Id = 1 };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<InventoryItemModelEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<InventoryItemModelEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemModelEntity?)null);
        var result = await _service.DeleteAsync(999, CancellationToken.None);
        Assert.False(result);
    }

    #endregion
}
