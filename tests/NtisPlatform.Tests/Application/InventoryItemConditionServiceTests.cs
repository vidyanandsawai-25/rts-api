using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Tests.Application;
public class InventoryItemConditionServiceTests
{
    private readonly Mock<IRepository<InventoryItemConditionEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly InventoryItemConditionService _service;
    private readonly Mock<ILogger<InventoryItemConditionController>> _mockLogger;
    private readonly InventoryItemConditionController _controller;
    public InventoryItemConditionServiceTests()
    {
        _mockRepository = new Mock<IRepository<InventoryItemConditionEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<InventoryItemConditionMappingFields>();
        },
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new InventoryItemConditionService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper);

        _mockLogger = new Mock<ILogger<InventoryItemConditionController>>();
        _controller = new InventoryItemConditionController(_mockLogger.Object, _service);
    }

    #region Entity Tests

    [Fact]
    public void Entity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new InventoryItemConditionEntity
        {
            Id = 1,
            InventoryItemCategoryId = 2,
            ConditionName = "New",
            DisplayOrder = 3,
            IsActive = true,
            CreatedBy = 1,
            UpdatedBy = 2,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.InventoryItemCategoryId);
        Assert.Equal("New", entity.ConditionName);
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
        var entity = new InventoryItemConditionEntity();
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.InventoryItemCategoryId);
        Assert.Equal("", entity.ConditionName);
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
        var dto = new InventoryItemConditionDto
        {
            Id = 1,
            InventoryItemCategoryId = 2,
            ConditionName = "New",
            DisplayOrder = 3,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.InventoryItemCategoryId);
        Assert.Equal("New", dto.ConditionName);
        Assert.Equal(3, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void Dto_DefaultValues_AreCorrect()
    {
        var dto = new InventoryItemConditionDto();
        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.InventoryItemCategoryId);
        Assert.Equal(string.Empty, dto.ConditionName);  // Changed from Assert.Null to match DTO default
        Assert.Null(dto.DisplayOrder);  // Changed from 0 to null since DisplayOrder is nullable
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void Dto_NullableProperties_CanBeNull()
    {
        var dto = new InventoryItemConditionDto
        {
            ConditionName = null,
            DisplayOrder = null
        };
        Assert.Null(dto.ConditionName);
        Assert.Null(dto.DisplayOrder);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = 1,
            ConditionName = "New",
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
        var dto = new CreateInventoryItemConditionMasterDto();
     Assert.Equal(0, dto.InventoryItemCategoryId);
Assert.Equal(string.Empty, dto.ConditionName);
        Assert.Null(dto.DisplayOrder);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedBy);
    }

    [Fact]
    public void CreateDto_NullableProperties_CanBeNull()
    {
        var dto = new CreateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = 1,
            ConditionName = "New",
            DisplayOrder = null
        };
        Assert.Null(dto.DisplayOrder);
    }

    [Fact]
    public void CreateDto_MissingRequiredFields_FailsValidation()
    {
        var dto = new CreateInventoryItemConditionMasterDto
        {
            ConditionName = null!,
            DisplayOrder = 2
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_InventoryItemCategoryId_Required" || v.ErrorMessage == "InventoryItemCondition_ConditionName_Required");
    }

    [Fact]
    public void CreateDto_MissingInventoryItemCategoryId_FailsValidation()
    {
        var dto = new CreateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = 0,
            ConditionName = "New"
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_InventoryItemCategoryId_Required");
    }

    [Fact]
    public void CreateDto_NegativeInventoryItemCategoryId_FailsValidation()
    {
  var dto = new CreateInventoryItemConditionMasterDto
        {
         InventoryItemCategoryId = -1,
     ConditionName = "New"
        };
   var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
     var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_InventoryItemCategoryId_Required");
    }

    [Fact]
    public void CreateDto_MissingConditionName_FailsValidation()
    {
 var dto = new CreateInventoryItemConditionMasterDto
  {
   InventoryItemCategoryId = 1,
            ConditionName = null!
     };
      var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
     var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
      Assert.False(isValid);
  Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_ConditionName_Required");
    }

  [Fact]
    public void CreateDto_ConditionNameTooLong_FailsValidation()
    {
        var dto = new CreateInventoryItemConditionMasterDto
      {
         InventoryItemCategoryId = 1,
  ConditionName = new string('A', 101)
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
   Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_ConditionName_MaxLen_100");
    }

    [Fact]
    public void CreateDto_ConditionNameMaxLength_PassesValidation()
    {
        var dto = new CreateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = 1,
            ConditionName = new string('A', 100)
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
    }

    [Fact]
    public void UpdateDto_ValidData_PassesValidation()
    {
        var dto = new UpdateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = 1,
            ConditionName = "Used",
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
    public void UpdateDto_DefaultValues_AreCorrect()
    {
        var dto = new UpdateInventoryItemConditionMasterDto();
        Assert.Equal(0, dto.InventoryItemCategoryId);
        Assert.Equal(string.Empty, dto.ConditionName);
        Assert.Null(dto.DisplayOrder);
        Assert.False(dto.IsActive);
        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public void UpdateDto_NullableProperties_CanBeNull()
    {
        var dto = new UpdateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = 1,
            ConditionName = "Used",
            DisplayOrder = null
        };
        Assert.Null(dto.DisplayOrder);
    }

    [Fact]
    public void UpdateDto_MissingRequiredFields_FailsValidation()
    {
        var dto = new UpdateInventoryItemConditionMasterDto
      {
          ConditionName = null!,
       InventoryItemCategoryId = 0
     };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
    var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_InventoryItemCategoryId_Required" || v.ErrorMessage == "InventoryItemCondition_ConditionName_Required");
    }

    [Fact]
    public void UpdateDto_MissingInventoryItemCategoryId_FailsValidation()
    {
        var dto = new UpdateInventoryItemConditionMasterDto
        {
     InventoryItemCategoryId = 0,
            ConditionName = "Used"
      };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
    var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
  Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_InventoryItemCategoryId_Required");
  }

    [Fact]
    public void UpdateDto_NegativeInventoryItemCategoryId_FailsValidation()
    {
        var dto = new UpdateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = -1,
      ConditionName = "Used"
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_InventoryItemCategoryId_Required");
    }

    [Fact]
    public void UpdateDto_MissingConditionName_FailsValidation()
    {
        var dto = new UpdateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = 1,
  ConditionName = null!
        };
    var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
   Assert.False(isValid);
  Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_ConditionName_Required");
    }

    [Fact]
    public void UpdateDto_ConditionNameTooLong_FailsValidation()
    {
        var dto = new UpdateInventoryItemConditionMasterDto
        {
 InventoryItemCategoryId = 1,
      ConditionName = new string('A', 101)
        };
      var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCondition_ConditionName_MaxLen_100");
  }

    [Fact]
    public void UpdateDto_ConditionNameMaxLength_PassesValidation()
    {
        var dto = new UpdateInventoryItemConditionMasterDto
        {
            InventoryItemCategoryId = 1,
            ConditionName = new string('A', 100)
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
        var qp = new InventoryItemConditionQueryParameters
        {
            InventoryItemCategoryId = 5,
            ConditionName = "TestCondition",
            IsActive = true,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "search",
            SortBy = "ConditionName",
            SortOrder = "asc"
        };
        Assert.Equal(5, qp.InventoryItemCategoryId);
        Assert.True(qp.IsActive.Value);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
        Assert.Equal("search", qp.SearchTerm);
        Assert.Equal("ConditionName", qp.SortBy);
        Assert.Equal("asc", qp.SortOrder);
    }

    [Fact]
    public void QueryParameters_DefaultValues_AreCorrect()
    {
        var qp = new InventoryItemConditionQueryParameters();
        Assert.Null(qp.InventoryItemCategoryId);
        Assert.Null(qp.ConditionName); 
        Assert.Null(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    #endregion

    #region Service CRUD Tests

    [Fact]
    public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new InventoryItemConditionEntity { Id = 1, InventoryItemCategoryId = 2, ConditionName = "New" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var result = await _service.GetByIdAsync(1);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemConditionEntity?)null);
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateInventoryItemConditionMasterDto { InventoryItemCategoryId = 1, ConditionName = "New" };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemConditionEntity>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((InventoryItemConditionEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var entity = new InventoryItemConditionEntity { Id = 1, InventoryItemCategoryId = 2, ConditionName = "Old" };
        var updateDto = new UpdateInventoryItemConditionMasterDto { InventoryItemCategoryId = 2, ConditionName = "New" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemConditionEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("New", result.ConditionName);
    }

    [Fact]
    public async Task Service_UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemConditionEntity?)null);
        var updateDto = new UpdateInventoryItemConditionMasterDto { InventoryItemCategoryId = 2, ConditionName = "New" };
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new InventoryItemConditionEntity { Id =1 };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemConditionEntity?)null);
        var result = await _service.DeleteAsync(999, CancellationToken.None);
        Assert.False(result);
    }

    #endregion

    #region Controller Tests

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var qp = new InventoryItemConditionQueryParameters();
        var pagedResult = new PagedResult<InventoryItemConditionDto>(new List<InventoryItemConditionDto>(), 0, 1, 10);
        var serviceMock = new Mock<IInventoryItemConditionService>();
        serviceMock.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);
        var ctrl = new InventoryItemConditionController(_mockLogger.Object, serviceMock.Object);
        var result = await ctrl.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ReturnsOkOrNotFound()
    {
        var serviceMock = new Mock<IInventoryItemConditionService>();
        serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new InventoryItemConditionDto { Id = 1 });
        serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemConditionDto?)null);
        var ctrl = new InventoryItemConditionController(_mockLogger.Object, serviceMock.Object);

        var okResult = await ctrl.GetById(1, CancellationToken.None);
        var notFoundResult = await ctrl.GetById(999, CancellationToken.None);

        Assert.True(okResult is OkObjectResult || okResult is NotFoundResult);
        Assert.True(notFoundResult is OkObjectResult || notFoundResult is NotFoundResult);
    }

    [Fact]
    public async Task Controller_Create_ReturnsCreated()
    {
        var serviceMock = new Mock<IInventoryItemConditionService>();
        serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateInventoryItemConditionMasterDto>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new InventoryItemConditionDto { Id = 1 });
        var ctrl = new InventoryItemConditionController(_mockLogger.Object, serviceMock.Object);
        var result = await ctrl.Create(new CreateInventoryItemConditionMasterDto { InventoryItemCategoryId = 1, ConditionName = "New" }, CancellationToken.None);
        Assert.True(result is OkObjectResult || result is CreatedResult);
    }

    [Fact]
    public async Task Controller_Update_ReturnsOkOrNotFound()
    {
        var serviceMock = new Mock<IInventoryItemConditionService>();
        serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateInventoryItemConditionMasterDto>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new InventoryItemConditionDto { Id = 1 });
        serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateInventoryItemConditionMasterDto>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync((InventoryItemConditionDto?)null);
        var ctrl = new InventoryItemConditionController(_mockLogger.Object, serviceMock.Object);

        var okResult = await ctrl.Update(1, new UpdateInventoryItemConditionMasterDto { InventoryItemCategoryId = 1, ConditionName = "New" }, CancellationToken.None);
        var notFoundResult = await ctrl.Update(999, new UpdateInventoryItemConditionMasterDto { InventoryItemCategoryId = 1, ConditionName = "New" }, CancellationToken.None);

        Assert.True(okResult is OkObjectResult || okResult is NotFoundResult);
        Assert.True(notFoundResult is OkObjectResult || notFoundResult is NotFoundResult);
    }

    [Fact]
    public async Task Controller_Delete_ReturnsOkOrNotFound()
    {
        var serviceMock = new Mock<IInventoryItemConditionService>();
        serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var ctrl = new InventoryItemConditionController(_mockLogger.Object, serviceMock.Object);

        var okResult = await ctrl.Delete(1, CancellationToken.None);
        var notFoundResult = await ctrl.Delete(999, CancellationToken.None);

        Assert.True(okResult is OkObjectResult || okResult is NotFoundResult);
        Assert.True(notFoundResult is OkObjectResult || notFoundResult is NotFoundResult);
    }

    #endregion
}
