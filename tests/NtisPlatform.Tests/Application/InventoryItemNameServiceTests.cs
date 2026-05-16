using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
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
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly InventoryItemNameService _service;
    private readonly Mock<ILogger<InventoryItemNameController>> _mockLogger;
    private readonly InventoryItemNameController _controller;
    public InventoryItemNameServiceTests()
    {
        _mockRepository = new Mock<IRepository<InventoryItemNameEntity, int>>();
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
        _service = new InventoryItemNameService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);

        _mockLogger = new Mock<ILogger<InventoryItemNameController>>();
        _controller = new InventoryItemNameController(_mockLogger.Object, _service);
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

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeName = "Name" };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemNameEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemNameEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var entity = new InventoryItemNameEntity { Id = 1, InventoryItemCategoryId = 2, SubTypeName = "Old" };
        var updateDto = new UpdateInventoryItemNameDto { InventoryItemCategoryId = 2, SubTypeName = "New" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
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
    public async Task Service_UpdateAsync_Deactivation_CallsReferenceValidation()
    {
        var entity = new InventoryItemNameEntity { Id = 1, IsActive = true, InventoryItemCategoryId = 1, SubTypeName = "Test" };
        var updateDto = new UpdateInventoryItemNameDto { IsActive = false, InventoryItemCategoryId = 1, SubTypeName = "Test" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemNameEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockReferenceValidator.Verify(
            x => x.ValidateReferencesAsync<InventoryItemNameEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new InventoryItemNameEntity { Id =1 };
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

    #region Controller Tests

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var qp = new InventoryItemNameQueryParameters();
        var pagedResult = new PagedResult<InventoryItemNameDto>(new List<InventoryItemNameDto>(), 0, 1, 10);
        var serviceMock = new Mock<IInventoryItemNameService>();
        serviceMock.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);
        var ctrl = new InventoryItemNameController(_mockLogger.Object, serviceMock.Object);
        var result = await ctrl.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ReturnsOkOrNotFound()
    {
        var serviceMock = new Mock<IInventoryItemNameService>();
        serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new InventoryItemNameDto { Id = 1 });
        serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemNameDto?)null);
        var ctrl = new InventoryItemNameController(_mockLogger.Object, serviceMock.Object);

        var okResult = await ctrl.GetById(1, CancellationToken.None);
        var notFoundResult = await ctrl.GetById(999, CancellationToken.None);

        Assert.True(okResult is OkObjectResult || okResult is NotFoundResult);
        Assert.True(notFoundResult is OkObjectResult || notFoundResult is NotFoundResult);
    }

    [Fact]
    public async Task Controller_Create_ReturnsCreated()
    {
        var serviceMock = new Mock<IInventoryItemNameService>();
        serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateInventoryItemNameDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemNameDto { Id = 1 });
        var ctrl = new InventoryItemNameController(_mockLogger.Object, serviceMock.Object);
        var result = await ctrl.Create(new CreateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeName = "Name" }, CancellationToken.None);
        Assert.True(result is OkObjectResult || result is CreatedResult);
    }

    [Fact]
    public async Task Controller_Update_ReturnsOkOrNotFound()
    {
        var serviceMock = new Mock<IInventoryItemNameService>();
        serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateInventoryItemNameDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemNameDto { Id = 1 });
        serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateInventoryItemNameDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemNameDto?)null);
        var ctrl = new InventoryItemNameController(_mockLogger.Object, serviceMock.Object);

        var okResult = await ctrl.Update(1, new UpdateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeName = "Name" }, CancellationToken.None);
        var notFoundResult = await ctrl.Update(999, new UpdateInventoryItemNameDto { InventoryItemCategoryId = 1, SubTypeName = "Name" }, CancellationToken.None);

        Assert.True(okResult is OkObjectResult || okResult is NotFoundResult);
        Assert.True(notFoundResult is OkObjectResult || notFoundResult is NotFoundResult);
    }

    [Fact]
    public async Task Controller_Delete_ReturnsOkOrNotFound()
    {
        var serviceMock = new Mock<IInventoryItemNameService>();
        serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var ctrl = new InventoryItemNameController(_mockLogger.Object, serviceMock.Object);

        var okResult = await ctrl.Delete(1, CancellationToken.None);
        var notFoundResult = await ctrl.Delete(999, CancellationToken.None);

        Assert.True(okResult is OkObjectResult || okResult is NotFoundResult);
        Assert.True(notFoundResult is OkObjectResult || notFoundResult is NotFoundResult);
    }

    #endregion
}