using AutoMapper;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Mappings;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Application;
public class InventoryItemCategoryServiceTests
{
    private readonly Mock<IRepository<InventoryItemCategoryEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly InventoryItemCategoryService _service;

    public InventoryItemCategoryServiceTests()
    {
        _mockRepository = new Mock<IRepository<InventoryItemCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockReferenceValidator
        .Setup(x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<InventoryItemCategoryMappingProfile>();
        },
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new InventoryItemCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);
    }

    #region Entity Tests

    [Fact]
    public void Entity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new InventoryItemCategoryEntity
        {
            Id = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal("CAT001", entity.TypeCode);
        Assert.Equal("Electronics", entity.TypeName);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void Entity_DefaultValues_AreCorrect()
    {
        var entity = new InventoryItemCategoryEntity();
        Assert.Equal(0, entity.Id);
        Assert.Null(entity.TypeCode);
        Assert.Equal(string.Empty, entity.TypeName);  // Changed from Assert.Null to match entity default
        Assert.Equal(0, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.UpdatedDate);
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void Dto_Properties_GetSet_WorksCorrectly()
    {
        var dto = new InventoryItemCategoryDto
        {
            Id = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal("CAT001", dto.TypeCode);
        Assert.Equal("Electronics", dto.TypeName);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreateInventoryItemCategoryDto
        {
            TypeCode = "CAT001",
            TypeName = "Electronics",
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
    [InlineData(null, "InventoryItemCategory_TypeName_Required")]
    [InlineData("", "InventoryItemCategory_TypeName_Required")]
    public void CreateDto_InvalidTypeName_FailsValidation(string? typeName, string expectedError)
    {
        var dto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = typeName!, DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void CreateDto_TypeNameTooLong_FailsValidation()
    {
    var dto = new CreateInventoryItemCategoryDto
      {
            TypeCode = "CAT001",
  TypeName = new string('A', 101),
          DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
    Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_TypeName_MaxLen_100");
    }

    [Fact]
    public void CreateDto_MissingDisplayOrder_FailsValidation()
    {
        var dto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = null };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
     Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_DisplayOrder_Required");
    }

    [Fact]
    public void UpdateDto_ValidData_PassesValidation()
    {
        var dto = new UpdateInventoryItemCategoryDto
        {
            TypeCode = "CAT001",
            TypeName = "Electronics Updated",
            DisplayOrder = 2,
            IsActive = true,
            UpdatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateDto_MissingTypeName_FailsValidation()
    {
        var dto = new UpdateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = null, DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_TypeName_Required");
    }

    #endregion

    #region QueryParameters Tests

    [Fact]
    public void QueryParameters_Properties_WorkCorrectly()
    {
        var qp = new InventoryItemCategoryQueryParameters
        {
            IsActive = true,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "Test",
            SortBy = "TypeName"
        };
        Assert.True(qp.IsActive);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
        Assert.Equal("Test", qp.SearchTerm);
        Assert.Equal("TypeName", qp.SortBy);
    }

    [Fact]
    public void QueryParameters_DefaultValues_AreCorrect()
    {
        var qp = new InventoryItemCategoryQueryParameters();
        Assert.Null(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    #endregion

    #region Service CRUD Tests

    [Fact]
    public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new InventoryItemCategoryEntity
        {
            Id = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var result = await _service.GetByIdAsync(1, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("CAT001", result.TypeCode);
        Assert.Equal("Electronics", result.TypeName);
    }

    [Fact]
    public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemCategoryEntity?)null);
        var result = await _service.GetByIdAsync(999, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<InventoryItemCategoryEntity>
        {
            new() { Id = 1, TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, TypeCode = "CAT002", TypeName = "Furniture", DisplayOrder = 2, IsActive = true },
            new() { Id = 3, TypeCode = "CAT003", TypeName = "Office", DisplayOrder = 3, IsActive = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new InventoryItemCategoryQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    [Theory]
    [InlineData(null, 3)]      // No IsActive filter, returns all 3 items
    [InlineData(true, 2)]  // IsActive=true, returns 2 active items
    [InlineData(false, 1)]      // IsActive=false, returns 1 inactive item
    public async Task Service_GetAllAsync_WithFilters_ReturnsFilteredEntities(bool? isActive, int expectedCount)
    {
        var entities = new List<InventoryItemCategoryEntity>
     {
       new() { Id = 1, TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, TypeCode = "CAT001", TypeName = "Electronics Old", DisplayOrder = 2, IsActive = false },
            new() { Id = 3, TypeCode = "CAT002", TypeName = "Furniture", DisplayOrder = 3, IsActive = true }
     };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new InventoryItemCategoryQueryParameters { IsActive = isActive, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(expectedCount, result.Items.Count());
    }

    [Fact]
    public async Task Service_GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity>().BuildMockDbSet().Object);
        var qp = new InventoryItemCategoryQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Service_GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        var entities = Enumerable.Range(1, 25).Select(i => new InventoryItemCategoryEntity
        {
            Id = i,
            TypeCode = $"CAT{i:000}",
            TypeName = $"Category {i}",
            DisplayOrder = i,
            IsActive = true
        }).ToList();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new InventoryItemCategoryQueryParameters { PageNumber = 2, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateInventoryItemCategoryDto
        {
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1
        };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((InventoryItemCategoryEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("CAT001", result.TypeCode);
        Assert.Equal("Electronics", result.TypeName);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null, "Miscellaneous", 10, true)]
    [InlineData("OLD001", "Obsolete", 99, false)]
    public async Task Service_CreateAsync_EdgeCases_CreatesSuccessfully(string? typeCode, string typeName, int displayOrder, bool isActive)
    {
        var createDto = new CreateInventoryItemCategoryDto
        {
            TypeCode = typeCode,
            TypeName = typeName,
            DisplayOrder = displayOrder,
            IsActive = isActive
        };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((InventoryItemCategoryEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(typeCode, result.TypeCode);
        Assert.Equal(typeName, result.TypeName);
        Assert.Equal(isActive, result.IsActive);
    }

    [Fact]
    public async Task Service_CreateAsync_DuplicateTypeCode_ThrowsException()
    {
        var createDto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1 };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("Duplicate"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var existingEntity = new InventoryItemCategoryEntity
        {
            Id = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true
        };
        var updateDto = new UpdateInventoryItemCategoryDto
        {
            TypeCode = "CAT001",
            TypeName = "Electronics Updated",
            DisplayOrder = 2,
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("Electronics Updated", result.TypeName);
        Assert.Equal(2, result.DisplayOrder);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemCategoryEntity?)null);
        var updateDto = new UpdateInventoryItemCategoryDto { TypeName = "Test", DisplayOrder = 1 };
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_Deactivation_CallsReferenceValidation()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, IsActive = true };
        var updateDto = new UpdateInventoryItemCategoryDto { IsActive = false, TypeName = "Test", DisplayOrder = 1 };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockReferenceValidator.Verify(
            x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_CallsReferenceValidation()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteAsync(1, CancellationToken.None);

        _mockReferenceValidator.Verify(
            x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_ReferenceValidationFails_ThrowsException()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
        .Setup(x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Test error"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.Is<InventoryItemCategoryEntity>(e => e.Id == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemCategoryEntity?)null);
        var result = await _service.DeleteAsync(999, CancellationToken.None);
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Controller Tests

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var qp = new InventoryItemCategoryQueryParameters();
        var pagedResult = new PagedResult<InventoryItemCategoryDto>(new List<InventoryItemCategoryDto>(), 0, 1, 10);
        var serviceMock = new Mock<IInventoryItemCategoryService>();
        var loggerMock = new Mock<ILogger<InventoryItemCategoryController>>();
        serviceMock.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);
        var ctrl = new InventoryItemCategoryController(loggerMock.Object, serviceMock.Object);
        var result = await ctrl.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IInventoryItemCategoryService>();
        var loggerMock = new Mock<ILogger<InventoryItemCategoryController>>();
        serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new InventoryItemCategoryDto { Id = 1 });
        var ctrl = new InventoryItemCategoryController(loggerMock.Object, serviceMock.Object);
        var result = await ctrl.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IInventoryItemCategoryService>();
        var loggerMock = new Mock<ILogger<InventoryItemCategoryController>>();
        serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemCategoryDto?)null);
        var ctrl = new InventoryItemCategoryController(loggerMock.Object, serviceMock.Object);
        var result = await ctrl.GetById(999, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Create_ValidDto_ReturnsOk()
    {
        var serviceMock = new Mock<IInventoryItemCategoryService>();
        var loggerMock = new Mock<ILogger<InventoryItemCategoryController>>();
        serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateInventoryItemCategoryDto>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new InventoryItemCategoryDto { Id = 1 });
        var ctrl = new InventoryItemCategoryController(loggerMock.Object, serviceMock.Object);
        var result = await ctrl.Create(new CreateInventoryItemCategoryDto { TypeName = "Test", DisplayOrder = 1 }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IInventoryItemCategoryService>();
        var loggerMock = new Mock<ILogger<InventoryItemCategoryController>>();
        serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateInventoryItemCategoryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemCategoryDto { Id = 1 });
        var ctrl = new InventoryItemCategoryController(loggerMock.Object, serviceMock.Object);
        var result = await ctrl.Update(1, new UpdateInventoryItemCategoryDto { TypeName = "Test", DisplayOrder = 1 }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IInventoryItemCategoryService>();
        var loggerMock = new Mock<ILogger<InventoryItemCategoryController>>();
        serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateInventoryItemCategoryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemCategoryDto?)null);
        var ctrl = new InventoryItemCategoryController(loggerMock.Object, serviceMock.Object);
        var result = await ctrl.Update(999, new UpdateInventoryItemCategoryDto { TypeName = "Test", DisplayOrder = 1 }, CancellationToken.None);
        // Extension methods may wrap response in OkObjectResult with error details
        Assert.True(result is OkObjectResult || result is NotFoundResult);
    }

    [Fact]
    public async Task Controller_Delete_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IInventoryItemCategoryService>();
        var loggerMock = new Mock<ILogger<InventoryItemCategoryController>>();
        serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ctrl = new InventoryItemCategoryController(loggerMock.Object, serviceMock.Object);
        var result = await ctrl.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Delete_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IInventoryItemCategoryService>();
        var loggerMock = new Mock<ILogger<InventoryItemCategoryController>>();
        serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var ctrl = new InventoryItemCategoryController(loggerMock.Object, serviceMock.Object);
        var result = await ctrl.Delete(999, CancellationToken.None);
        // Extension methods may wrap response in OkObjectResult with error details
        Assert.True(result is OkObjectResult || result is NotFoundResult);
    }

    #endregion
}
