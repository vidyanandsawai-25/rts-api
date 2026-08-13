using AutoMapper;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Tests.Application;
public class InventoryItemNameServiceTests
{
    private readonly Mock<IRepository<InventoryItemNameEntity, int>> _mockRepository;
    private readonly Mock<IRepository<InventoryItemCategoryEntity, int>> _mockInventoryItemCategoryRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly InventoryItemNameService _service;

    public InventoryItemNameServiceTests()
    {
        _mockRepository = new Mock<IRepository<InventoryItemNameEntity, int>>();
        _mockInventoryItemCategoryRepository = new Mock<IRepository<InventoryItemCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockReferenceValidator
            .Setup(x => x.ValidateReferencesAsync<InventoryItemNameEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<InventoryItemNameMappingFields>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var existingCategories = new List<InventoryItemCategoryEntity>
        {
            new() { Id = 1, TypeName = "Electronics", IsActive = true },
            new() { Id = 2, TypeName = "Furniture", IsActive = true }
        };
        _mockInventoryItemCategoryRepository.Setup(r => r.GetQueryable()).Returns(existingCategories.BuildMockDbSet().Object);

        _service = new InventoryItemNameService(
            _mockRepository.Object,
            _mockInventoryItemCategoryRepository.Object,
            _mockUnitOfWork.Object,
            _mapper,
            _mockReferenceValidator.Object);
    }

    #region Entity Tests

    [Fact]
    public void Entity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new InventoryItemNameEntity
        {
            Id = 1,
            InventoryItemCategoryId = 2,
            SubTypeCode = "CODE",
            SubTypeName = "Name",
            DisplayOrder = 3,
            IsActive = true,
            CreatedBy = 1,
            UpdatedBy = 2,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.InventoryItemCategoryId);
        Assert.Equal("CODE", entity.SubTypeCode);
        Assert.Equal("Name", entity.SubTypeName);
        Assert.Equal(3, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void Entity_DefaultValues_AreCorrect()
    {
        var entity = new InventoryItemNameEntity();
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.InventoryItemCategoryId);
        Assert.Equal("", entity.SubTypeCode);
        Assert.Equal("", entity.SubTypeName);
        Assert.Equal(0, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedDate);
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void Dto_Properties_GetSet_WorksCorrectly()
    {
        var dto = new InventoryItemNameDto
        {
            Id = 1,
            InventoryItemCategoryId = 2,
            SubTypeCode = "CODE",
            SubTypeName = "Name",
            DisplayOrder = 3,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.InventoryItemCategoryId);
        Assert.Equal("CODE", dto.SubTypeCode);
        Assert.Equal("Name", dto.SubTypeName);
        Assert.Equal(3, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void Dto_DefaultValues_AreCorrect()
    {
        var dto = new InventoryItemNameDto();
        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.InventoryItemCategoryId);
        Assert.Equal(string.Empty, dto.SubTypeCode);
        Assert.Equal(string.Empty, dto.SubTypeName);
        Assert.Null(dto.DisplayOrder);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void Dto_NullableProperties_CanBeNull()
    {
        var dto = new InventoryItemNameDto
        {
            SubTypeCode = null,
            DisplayOrder = null
        };
        Assert.Null(dto.SubTypeCode);
        Assert.Null(dto.DisplayOrder);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreateInventoryItemNameDto
        {
            InventoryItemCategoryId = 1,
            SubTypeCode = "CODE",
            SubTypeName = "Name",
            DisplayOrder = 2,
            IsActive = true,
            CreatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateDto_DefaultValues_AreCorrect()
    {
        var dto = new CreateInventoryItemNameDto();
        Assert.Equal(0, dto.InventoryItemCategoryId);
        Assert.Equal(string.Empty, dto.SubTypeCode);
        Assert.Equal(string.Empty, dto.SubTypeName);
        Assert.Null(dto.DisplayOrder);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedBy);
    }

    [Fact]
    public void CreateDto_NullableProperties_CanBeNull()
    {
        var dto = new CreateInventoryItemNameDto
        {
            InventoryItemCategoryId = 1,
            SubTypeName = "Name",
            SubTypeCode = null,
            DisplayOrder = null
        };
        Assert.Null(dto.SubTypeCode);
        Assert.Null(dto.DisplayOrder);
    }

    [Fact]
    public void CreateDto_MissingRequiredFields_FailsValidation()
    {
        var dto = new CreateInventoryItemNameDto
        {
            // SubTypeCode is omitted (required)
            SubTypeName = null!,
            DisplayOrder = 2
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemName_InventoryItemCategoryId_Required" || v.ErrorMessage == "InventoryItemName_SubTypeName_Required" || v.ErrorMessage == "InventoryItemName_SubTypeCode_Required");
    }

    [Fact]
    public void CreateDto_MissingInventoryItemCategoryId_FailsValidation()
    {
        var dto = new CreateInventoryItemNameDto
        {
            InventoryItemCategoryId = 0,
            SubTypeName = "Name"
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemName_InventoryItemCategoryId_Required");
    }

    [Fact]
    public void CreateDto_NegativeInventoryItemCategoryId_FailsValidation()
    {
        var dto = new CreateInventoryItemNameDto
        {
            InventoryItemCategoryId = -1,
            SubTypeName = "Name"
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemName_InventoryItemCategoryId_Required");
    }

    [Fact]
    public void CreateDto_MissingSubTypeName_FailsValidation()
    {
        var dto = new CreateInventoryItemNameDto
        {
            SubTypeCode = "CODE",
            SubTypeName = null!,
            DisplayOrder = 2
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemName_SubTypeName_Required");
    }

    [Fact]
    public void CreateDto_SubTypeNameTooLong_FailsValidation()
    {
        var dto = new CreateInventoryItemNameDto
        {
            InventoryItemCategoryId = 1,
            SubTypeName = new string('A', 51)
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemName_SubTypeName_MaxLen_50");
    }

    #endregion

    #region QueryParameters Tests

    [Fact]
    public void QueryParameters_Properties_WorkCorrectly()
    {
        var qp = new InventoryItemNameQueryParameters
        {
            InventoryItemCategoryId = 5,
            SubTypeName = "TestSubType",
            SubTypeCode = "TestCode",
            IsActive = true,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "search",
            SortBy = "SubTypeName",
            SortOrder = "asc"
        };
        Assert.Equal(5, qp.InventoryItemCategoryId);
        Assert.Equal("TestSubType", qp.SubTypeName);
        Assert.Equal("TestCode", qp.SubTypeCode);
        Assert.True(qp.IsActive.Value);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
        Assert.Equal("search", qp.SearchTerm);
        Assert.Equal("SubTypeName", qp.SortBy);
        Assert.Equal("asc", qp.SortOrder);
    }

    [Fact]
    public void QueryParameters_DefaultValues_AreCorrect()
    {
        var qp = new InventoryItemNameQueryParameters();
        Assert.Null(qp.InventoryItemCategoryId);
        Assert.Null(qp.SubTypeName);
        Assert.Null(qp.SubTypeCode);
        Assert.Null(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    #endregion

    #region Service CRUD Tests

    [Fact]
    public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new InventoryItemNameEntity { Id = 1, InventoryItemCategoryId = 2, SubTypeName = "Name" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var result = await _service.GetByIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemNameEntity?)null);
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    // InventoryItemCategoryId is a required FK (AMS.InventoryItemNameMaster.InventoryItemCategoryId ->
    // AMS.InventoryItemCategoryMaster.Id) -- GetAll must resolve and expose the referenced category's
    // display name (InventoryItemCategoryName, sourced from InventoryItemCategoryEntity.TypeName) via
    // a join, not just the raw FK id.
    [Fact]
    public async Task Service_GetAllAsync_PopulatesInventoryItemCategoryName_FromReferencedCategory()
    {
        var entities = new List<InventoryItemNameEntity>
        {
            new() { Id = 1, InventoryItemCategoryId = 1, SubTypeName = "Laptop", SubTypeCode = "LAP", IsActive = true },
            new() { Id = 2, InventoryItemCategoryId = 999, SubTypeName = "Chair", SubTypeCode = "CHR", IsActive = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        // _mockInventoryItemCategoryRepository is already stubbed in the constructor with Id=1 -> "Electronics".

        var qp = new InventoryItemNameQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);

        var mapped = result.Items.ToDictionary(x => x.Id);
        Assert.Equal("Electronics", mapped[1].InventoryItemCategoryName);
        Assert.Null(mapped[2].InventoryItemCategoryName); // InventoryItemCategoryId 999 doesn't resolve to any InventoryItemCategoryEntity
    }

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeCode = "CODE", SubTypeName = "Name" };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemNameEntity>().BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemNameEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemNameEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Service_CreateAsync_InvalidInventoryItemCategoryId_ThrowsValidationException()
    {
        var createDto = new CreateInventoryItemNameDto { InventoryItemCategoryId = 999, SubTypeCode = "CODE", SubTypeName = "Name" };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains(nameof(CreateInventoryItemNameDto.InventoryItemCategoryId), ex.Errors.Keys);
    }

    [Fact]
    public async Task Service_CreateAsync_DuplicateSubTypeNameUnderSameCategory_ThrowsValidationException()
    {
        var existing = new List<InventoryItemNameEntity>
        {
            new() { Id = 1, InventoryItemCategoryId = 1, SubTypeCode = "OLD", SubTypeName = "Name" }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);

        var createDto = new CreateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeCode = "NEW", SubTypeName = "Name" };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains("InventoryItemName_SubTypeName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task Service_CreateAsync_DuplicateSubTypeCodeUnderSameCategory_ThrowsValidationException()
    {
        var existing = new List<InventoryItemNameEntity>
        {
            new() { Id = 1, InventoryItemCategoryId = 1, SubTypeCode = "CODE", SubTypeName = "Old Name" }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);

        var createDto = new CreateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeCode = "CODE", SubTypeName = "New Name" };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains("InventoryItemName_SubTypeCode_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task Service_CreateAsync_SameSubTypeNameUnderDifferentCategory_Succeeds()
    {
        var existing = new List<InventoryItemNameEntity>
        {
            new() { Id = 1, InventoryItemCategoryId = 2, SubTypeCode = "CODE", SubTypeName = "Name" }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemNameEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemNameEntity e, CancellationToken _) => { e.Id = 2; return e; });

        var createDto = new CreateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeCode = "CODE2", SubTypeName = "Name" };

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var entity = new InventoryItemNameEntity { Id = 1, InventoryItemCategoryId = 2, SubTypeCode = "CODE", SubTypeName = "Old" };
        var updateDto = new UpdateInventoryItemNameDto { InventoryItemCategoryId = 2, SubTypeCode = "CODE", SubTypeName = "New" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemNameEntity> { entity }.BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemNameEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("New", result.SubTypeName);
    }

    [Fact]
    public async Task Service_UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemNameEntity?)null);
        var updateDto = new UpdateInventoryItemNameDto { InventoryItemCategoryId = 2, SubTypeName = "New" };
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_UpdateAsync_InvalidInventoryItemCategoryId_ThrowsValidationException()
    {
        var updateDto = new UpdateInventoryItemNameDto { InventoryItemCategoryId = 999, SubTypeCode = "CODE", SubTypeName = "New" };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        Assert.Contains(nameof(UpdateInventoryItemNameDto.InventoryItemCategoryId), ex.Errors.Keys);
    }

    [Fact]
    public async Task Service_UpdateAsync_RenameToAnotherRowsName_ThrowsValidationException()
    {
        var entity = new InventoryItemNameEntity { Id = 1, InventoryItemCategoryId = 1, SubTypeCode = "A", SubTypeName = "Name A", IsActive = true };
        var other = new InventoryItemNameEntity { Id = 2, InventoryItemCategoryId = 1, SubTypeCode = "B", SubTypeName = "Name B", IsActive = true };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemNameEntity> { entity, other }.BuildMockDbSet().Object);

        var updateDto = new UpdateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeCode = "A", SubTypeName = "Name B", IsActive = true };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        Assert.Contains("InventoryItemName_SubTypeName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task Service_UpdateAsync_Deactivation_CallsReferenceValidation()
    {
        var entity = new InventoryItemNameEntity { Id = 1, IsActive = true, InventoryItemCategoryId = 1, SubTypeCode = "CODE", SubTypeName = "Test" };
        var updateDto = new UpdateInventoryItemNameDto { IsActive = false, InventoryItemCategoryId = 1, SubTypeCode = "CODE", SubTypeName = "Test" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemNameEntity> { entity }.BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemNameEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockReferenceValidator.Verify(
            x => x.ValidateReferencesAsync<InventoryItemNameEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Service_UpdateAsync_DeactivationReferenced_ThrowsValidationException()
    {
        var entity = new InventoryItemNameEntity { Id = 1, IsActive = true, InventoryItemCategoryId = 1, SubTypeCode = "CODE", SubTypeName = "Test" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemNameEntity> { entity }.BuildMockDbSet().Object);
        _mockReferenceValidator
            .Setup(x => x.ValidateReferencesAsync<InventoryItemNameEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Referenced by Inventory Batch"));

        var updateDto = new UpdateInventoryItemNameDto { IsActive = false, InventoryItemCategoryId = 1, SubTypeCode = "CODE", SubTypeName = "Test" };

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new InventoryItemNameEntity { Id = 1 };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemNameEntity?)null);
        var result = await _service.DeleteAsync(999, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task Service_DeleteAsync_CallsReferenceValidation()
    {
        var entity = new InventoryItemNameEntity { Id = 1, InventoryItemCategoryId = 1, SubTypeName = "Test" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<InventoryItemNameEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteAsync(1, CancellationToken.None);

        _mockReferenceValidator.Verify(
            x => x.ValidateReferencesAsync<InventoryItemNameEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_ReferenceValidationFails_ThrowsException()
    {
        var entity = new InventoryItemNameEntity { Id = 1, InventoryItemCategoryId = 1, SubTypeName = "Test" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
            .Setup(x => x.ValidateReferencesAsync<InventoryItemNameEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Test error"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
    }

    #endregion
}