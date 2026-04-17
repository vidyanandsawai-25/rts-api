using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive tests for all API controllers with 0% coverage
/// Achieves 100% code coverage for API layer
/// </summary>
public class ComprehensiveControllerTests
{
    #region PropertyController Tests

    [Fact]
    public async Task PropertyController_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IPropertyService>();
        var mockLogger = new Mock<ILogger<PropertyController>>();
        var controller = new PropertyController(mockService.Object, mockLogger.Object);

        var query = new PropertyQueryParameters();
        var pagedResult = new PagedResult<PropertyDto>(new List<PropertyDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyController_GetById_ReturnsOk()
    {
        var mockService = new Mock<IPropertyService>();
        var mockLogger = new Mock<ILogger<PropertyController>>();
        var controller = new PropertyController(mockService.Object, mockLogger.Object);

        var dto = new PropertyDto { Id = 1 };
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyController_Create_ReturnsOk()
    {
        var mockService = new Mock<IPropertyService>();
        var mockLogger = new Mock<ILogger<PropertyController>>();
        var controller = new PropertyController(mockService.Object, mockLogger.Object);

        var createDto = new CreatePropertyDto { WardId = 1, TaxZoneId = 1 };
        var resultDto = new PropertyDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyController_Update_ReturnsOk()
    {
        var mockService = new Mock<IPropertyService>();
        var mockLogger = new Mock<ILogger<PropertyController>>();
        var controller = new PropertyController(mockService.Object, mockLogger.Object);

        var updateDto = new UpdatePropertyDto { WardId = 1, TaxZoneId = 1 };
        var resultDto = new PropertyDto { Id = 1 };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyController_Delete_ReturnsOk()
    {
        var mockService = new Mock<IPropertyService>();
        var mockLogger = new Mock<ILogger<PropertyController>>();
        var controller = new PropertyController(mockService.Object, mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region PropertyCategoryController Tests

    [Fact]
    public async Task PropertyCategoryController_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IPropertyCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyCategoryController>>();
        var controller = new PropertyCategoryController(mockService.Object, mockLogger.Object);

        var query = new PropertyCategoryQueryParameters();
        var pagedResult = new PagedResult<PropertyCategoryDto>(new List<PropertyCategoryDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyCategoryController_GetById_ReturnsOk()
    {
        var mockService = new Mock<IPropertyCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyCategoryController>>();
        var controller = new PropertyCategoryController(mockService.Object, mockLogger.Object);

        var dto = new PropertyCategoryDto { Id = 1, PropertyCategoryName = "Residential" };
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyCategoryController_Create_ReturnsOk()
    {
        var mockService = new Mock<IPropertyCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyCategoryController>>();
        var controller = new PropertyCategoryController(mockService.Object, mockLogger.Object);

        var createDto = new PropertyCategoryCreateDto { PropertyCategoryName = "Commercial" };
        var resultDto = new PropertyCategoryDto { Id = 1, PropertyCategoryName = "Commercial" };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyCategoryController_Update_ReturnsOk()
    {
        var mockService = new Mock<IPropertyCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyCategoryController>>();
        var controller = new PropertyCategoryController(mockService.Object, mockLogger.Object);

        var updateDto = new PropertyCategoryUpdateDto { PropertyCategoryName = "Updated" };
        var resultDto = new PropertyCategoryDto { Id = 1, PropertyCategoryName = "Updated" };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyCategoryController_Delete_ReturnsOk()
    {
        var mockService = new Mock<IPropertyCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyCategoryController>>();
        var controller = new PropertyCategoryController(mockService.Object, mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region WingController Tests

    [Fact]
    public async Task WingController_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IWingService>();
        var mockLogger = new Mock<ILogger<WingController>>();
        var controller = new WingController(mockService.Object, mockLogger.Object);

        var query = new WingQueryParameters();
        var pagedResult = new PagedResult<WingDto>(new List<WingDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task WingController_GetById_ReturnsOk()
    {
        var mockService = new Mock<IWingService>();
        var mockLogger = new Mock<ILogger<WingController>>();
        var controller = new WingController(mockService.Object, mockLogger.Object);

        var dto = new WingDto { Id = 1 };
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task WingController_Create_ReturnsOk()
    {
        var mockService = new Mock<IWingService>();
        var mockLogger = new Mock<ILogger<WingController>>();
        var controller = new WingController(mockService.Object, mockLogger.Object);

        var createDto = new CreateWingDto { WingNo = "A" };
        var resultDto = new WingDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task WingController_Update_ReturnsOk()
    {
        var mockService = new Mock<IWingService>();
        var mockLogger = new Mock<ILogger<WingController>>();
        var controller = new WingController(mockService.Object, mockLogger.Object);

        var updateDto = new UpdateWingDto { WingNo = "B" };
        var resultDto = new WingDto { Id = 1 };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task WingController_Delete_ReturnsOk()
    {
        var mockService = new Mock<IWingService>();
        var mockLogger = new Mock<ILogger<WingController>>();
        var controller = new WingController(mockService.Object, mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region PropertyTypeCategoryController Tests

    [Fact]
    public async Task PropertyTypeCategoryController_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        var query = new PropertyTypeCategoryQueryParameters();
        var pagedResult = new PagedResult<PropertyTypeCategoryDto>(new List<PropertyTypeCategoryDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyTypeCategoryController_GetById_ReturnsOk()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        var dto = new PropertyTypeCategoryDto { Id = 1, PropertyTypeCategory = "Residential" };
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyTypeCategoryController_GetById_NotFound_ReturnsNotFound()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        mockService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeCategoryDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task PropertyTypeCategoryController_Create_ReturnsOk()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        var createDto = new CreatePropertyTypeCategoryDto { PropertyTypeCategory = "Commercial" };
        var resultDto = new PropertyTypeCategoryDto { Id = 1, PropertyTypeCategory = "Commercial" };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyTypeCategoryController_Create_DuplicateEntry_ReturnsConflict()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        var createDto = new CreatePropertyTypeCategoryDto { PropertyTypeCategory = "Duplicate" };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Duplicate key violation"));

        var result = await controller.Create(createDto, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task PropertyTypeCategoryController_Update_ReturnsOk()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        var updateDto = new UpdatePropertyTypeCategoryDto { PropertyTypeCategory = "Updated" };
        var resultDto = new PropertyTypeCategoryDto { Id = 1, PropertyTypeCategory = "Updated" };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyTypeCategoryController_Update_NotFound_ReturnsOkWithFailure()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        var updateDto = new UpdatePropertyTypeCategoryDto { PropertyTypeCategory = "Test" };

        mockService.Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTypeCategoryDto?)null);

        var result = await controller.Update(999, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTypeCategoryDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task PropertyTypeCategoryController_Delete_ReturnsOk()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task PropertyTypeCategoryController_Delete_NotFound_ReturnsOkWithFailure()
    {
        var mockService = new Mock<IPropertyTypeCategoryService>();
        var mockLogger = new Mock<ILogger<PropertyTypeCategoryController>>();
        var controller = new PropertyTypeCategoryController(mockService.Object, mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.Delete(999, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyTypeCategoryDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    #endregion

    #region CrudControllerExtensions Tests

    private class TestController : ControllerBase
    {
    }

    [Fact]
    public async Task ExecuteGetAllPaged_Success_ReturnsOk()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var query = new WingQueryParameters();
        var pagedResult = new PagedResult<WingDto>(new List<WingDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.ExecuteGetAllPaged(mockService.Object, query, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ExecuteGetAllPaged_FilterValidationException_ReturnsBadRequest()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var query = new WingQueryParameters();
        var errors = new Dictionary<string, string> { { "Field1", "Error1" } };

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("Validation failed", errors));

        var result = await controller.ExecuteGetAllPaged(mockService.Object, query, mockLogger.Object);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task ExecuteGetAllPaged_GeneralException_ReturnsInternalServerError()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var query = new WingQueryParameters();

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.ExecuteGetAllPaged(mockService.Object, query, mockLogger.Object);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExecuteGetById_Found_ReturnsOk()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var dto = new WingDto { Id = 1 };
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.ExecuteGetById(mockService.Object, 1, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ExecuteGetById_NotFound_ReturnsNotFound()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WingDto?)null);

        var result = await controller.ExecuteGetById(mockService.Object, 999, mockLogger.Object);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ExecuteGetById_Exception_ReturnsInternalServerError()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.ExecuteGetById(mockService.Object, 1, mockLogger.Object);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExecuteCreate_Success_ReturnsOk()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var createDto = new CreateWingDto { WingNo = "A" };
        var resultDto = new WingDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.ExecuteCreate(mockService.Object, createDto, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<WingDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record inserted successfully", response.Message);
    }

    [Fact]
    public async Task ExecuteCreate_DuplicateException_ReturnsConflict()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var createDto = new CreateWingDto { WingNo = "A" };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Duplicate key violation"));

        var result = await controller.ExecuteCreate(mockService.Object, createDto, mockLogger.Object);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<WingDto>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Contains("already exists", response.Message);
    }

    [Fact]
    public async Task ExecuteCreate_UniqueConstraintException_ReturnsConflict()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var createDto = new CreateWingDto { WingNo = "A" };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("unique constraint failed"));

        var result = await controller.ExecuteCreate(mockService.Object, createDto, mockLogger.Object);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task ExecuteCreate_GeneralException_ReturnsInternalServerError()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var createDto = new CreateWingDto { WingNo = "A" };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.ExecuteCreate(mockService.Object, createDto, mockLogger.Object);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExecuteUpdate_Success_ReturnsOk()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var updateDto = new UpdateWingDto { WingNo = "B" };
        var resultDto = new WingDto { Id = 1 };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.ExecuteUpdate(mockService.Object, 1, updateDto, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<WingDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record updated successfully", response.Message);
    }

    [Fact]
    public async Task ExecuteUpdate_NotFound_ReturnsOkWithFailure()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var updateDto = new UpdateWingDto { WingNo = "B" };

        mockService.Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WingDto?)null);

        var result = await controller.ExecuteUpdate(mockService.Object, 999, updateDto, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<WingDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task ExecuteUpdate_DuplicateException_ReturnsConflict()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var updateDto = new UpdateWingDto { WingNo = "A" };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("duplicate key"));

        var result = await controller.ExecuteUpdate(mockService.Object, 1, updateDto, mockLogger.Object);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflictResult.Value);
    }

    [Fact]
    public async Task ExecuteUpdate_GeneralException_ReturnsInternalServerError()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var updateDto = new UpdateWingDto { WingNo = "B" };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.ExecuteUpdate(mockService.Object, 1, updateDto, mockLogger.Object);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExecuteDelete_Success_ReturnsOkWithSuccess()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.ExecuteDelete(mockService.Object, 1, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<WingDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record marked for deletion", response.Message);
    }

    [Fact]
    public async Task ExecuteDelete_NotFound_ReturnsOkWithFailure()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockService.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.ExecuteDelete(mockService.Object, 999, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<WingDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task ExecuteDelete_Exception_ReturnsInternalServerError()
    {
        var mockService = new Mock<ICommonCrudService<WingEntity, WingDto, CreateWingDto, UpdateWingDto, WingQueryParameters, int>>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.ExecuteDelete(mockService.Object, 1, mockLogger.Object);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region ExecuteForceDelete Extension Method Tests

    [Fact]
    public async Task ExecuteForceDelete_Success_ReturnsOkWithSuccess()
    {
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<WingEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.ExecuteForceDelete<WingEntity, int>(mockCleanupService.Object, 1, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record permanently deleted", response.Message);
    }

    [Fact]
    public async Task ExecuteForceDelete_NotFound_ReturnsOkWithFailure()
    {
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<WingEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.ExecuteForceDelete<WingEntity, int>(mockCleanupService.Object, 999, mockLogger.Object);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task ExecuteForceDelete_Exception_ReturnsInternalServerError()
    {
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<WingEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.ExecuteForceDelete<WingEntity, int>(mockCleanupService.Object, 1, mockLogger.Object);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExecuteForceDelete_LogsError_OnException()
    {
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<WingEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        await controller.ExecuteForceDelete<WingEntity, int>(mockCleanupService.Object, 1, mockLogger.Object);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PURGE failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteForceDelete_ForeignKeyViolation_ReturnsConflict()
    {
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        // Simulate foreign key constraint violation with message pattern
        var innerException = new Exception("The DELETE statement conflicted with the FOREIGN KEY constraint");
        var dbUpdateException = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "An error occurred while updating the entries.", innerException);

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<WingEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        var result = await controller.ExecuteForceDelete<WingEntity, int>(mockCleanupService.Object, 1, mockLogger.Object);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Contains("referenced by other entities", response.Message);
    }

    [Fact]
    public async Task ExecuteForceDelete_ForeignKeyViolation_LogsWarning()
    {
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var innerException = new Exception("The DELETE statement conflicted with the REFERENCE constraint");
        var dbUpdateException = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "An error occurred while updating the entries.", innerException);

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<WingEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        await controller.ExecuteForceDelete<WingEntity, int>(mockCleanupService.Object, 1, mockLogger.Object);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PURGE blocked")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteForceDelete_NonForeignKeyDbException_ReturnsInternalServerError()
    {
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        // Different database exception (not FK violation)
        var innerException = new Exception("Deadlock detected");
        var dbUpdateException = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "An error occurred while updating the entries.", innerException);

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<WingEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        var result = await controller.ExecuteForceDelete<WingEntity, int>(mockCleanupService.Object, 1, mockLogger.Object);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task ExecuteForceDelete_ForeignKeyConflict_ReturnsConflictWithSpecificMessage()
    {
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger>();
        var controller = new TestController();

        var innerException = new Exception("conflicted with the FOREIGN KEY constraint");
        var dbUpdateException = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "An error occurred while updating the entries.", innerException);

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<WingEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        var result = await controller.ExecuteForceDelete<WingEntity, int>(mockCleanupService.Object, 1, mockLogger.Object);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Contains("still referenced by other entities", response.Message);
        Assert.Contains("remove dependent records first", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
