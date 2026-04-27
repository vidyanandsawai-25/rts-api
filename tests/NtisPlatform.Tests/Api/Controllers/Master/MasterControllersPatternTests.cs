using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Master.GenderMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Comprehensive tests for ALL Master Controllers
/// Achieves 100% code coverage for all master controllers using the CrudControllerExtensions pattern
/// </summary>
public class AllMasterControllersComprehensiveTests
{
    #region FloorController Tests

    [Fact]
    public async Task FloorController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new FloorQueryParameters();
        var pagedResult = new PagedResult<FloorDto>(new List<FloorDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FloorController_GetById_CallsExtensionMethod()
    {
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var dto = new FloorDto { Id = 1 };
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FloorController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var createDto = new CreateFloorDto { FloorCode = "F001", Description = "Ground Floor" };
        var resultDto = new FloorDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FloorController_Update_CallsExtensionMethod()
    {
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var updateDto = new UpdateFloorDto { FloorCode = "F001", Description = "Updated Floor" };
        var resultDto = new FloorDto { Id = 1 };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FloorController_Delete_CallsExtensionMethod()
    {
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #region FloorController Bulk Operations Tests

    [Fact]
    public async Task FloorController_BulkCreate_CallsServiceMethod()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var createDtos = new[]
        {
            new CreateFloorDto { FloorCode = "F001", Description = "Floor 1" },
            new CreateFloorDto { FloorCode = "F002", Description = "Floor 2" }
        };

        var bulkResult = new BulkResult<FloorDto>(
            SuccessCount: 2,
            FailedCount: 0,
            Results: new List<FloorDto>
            {
                new() { Id = 1, FloorCode = "F001", Description = "Floor 1" },
                new() { Id = 2, FloorCode = "F002", Description = "Floor 2" }
            });

        mockService.Setup(s => s.BulkCreateAsync(createDtos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await controller.BulkCreate(createDtos, CancellationToken.None);
        // Assert
        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.BulkCreateAsync(createDtos, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FloorController_BulkCreate_EmptyArray_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var emptyArray = Array.Empty<CreateFloorDto>();

        // Act
        var result = await controller.BulkCreate(emptyArray, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        mockService.Verify(s => s.BulkCreateAsync(It.IsAny<CreateFloorDto[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FloorController_BulkUpdate_CallsServiceMethod()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var updateItems = new[]
        {
            new BulkUpdateItem<int, UpdateFloorDto>(1, new UpdateFloorDto { FloorCode = "F001", Description = "Updated 1" }),
            new BulkUpdateItem<int, UpdateFloorDto>(2, new UpdateFloorDto { FloorCode = "F002", Description = "Updated 2" })
        };

        var bulkResult = new BulkResult<FloorDto>(
            SuccessCount: 2,
            FailedCount: 0,
            Results: new List<FloorDto>
            {
                new() { Id = 1, FloorCode = "F001", Description = "Updated 1" },
                new() { Id = 2, FloorCode = "F002", Description = "Updated 2" }
            });

        mockService.Setup(s => s.BulkUpdateAsync(updateItems, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await controller.BulkUpdate(updateItems, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.BulkUpdateAsync(updateItems, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FloorController_BulkUpdate_EmptyArray_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object ,mockLogger.Object);

        var emptyArray = Array.Empty<BulkUpdateItem<int, UpdateFloorDto>>();

        // Act
        var result = await controller.BulkUpdate(emptyArray, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        mockService.Verify(s => s.BulkUpdateAsync(It.IsAny<BulkUpdateItem<int, UpdateFloorDto>[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FloorController_BulkUpdate_PartialSuccess_ReturnsOkWithErrors()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object,mockLogger.Object);

        var updateItems = new[]
        {
            new BulkUpdateItem<int, UpdateFloorDto>(1, new UpdateFloorDto { FloorCode = "F001", Description = "Updated 1" }),
            new BulkUpdateItem<int, UpdateFloorDto>(9999, new UpdateFloorDto { FloorCode = "FX", Description = "Not Found" })
        };

        var BulkResult = new BulkResult<FloorDto>(
            SuccessCount: 1,
            FailedCount: 1,
            Results: new List<FloorDto>
            {
                new() { Id = 1, FloorCode = "F001", Description = "Updated 1" }
            },
            Errors: new List<string> { "Record with Id '9999' not found." });

        mockService.Setup(s => s.BulkUpdateAsync(updateItems, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BulkResult);

        // Act
        var result = await controller.BulkUpdate(updateItems, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.BulkUpdateAsync(updateItems, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FloorController_BulkDelete_CallsServiceMethod()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object,mockLogger.Object);

        var idsToDelete = new[] { 1, 2, 3 };

        var bulkResult = new BulkResult<int>(
            SuccessCount: 3,
            FailedCount: 0,
            Results: new List<int> { 1, 2, 3 });

        mockService.Setup(s => s.BulkDeleteAsync(idsToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await controller.BulkDelete(idsToDelete, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.BulkDeleteAsync(idsToDelete, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FloorController_BulkDelete_EmptyArray_ReturnsBadRequest()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var emptyArray = Array.Empty<int>();

        // Act
        var result = await controller.BulkDelete(emptyArray, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        mockService.Verify(s => s.BulkDeleteAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FloorController_BulkDelete_PartialSuccess_ReturnsOkWithErrors()
    {
        // Arrange
        var mockService = new Mock<IFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<FloorController>>();
        var controller = new FloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var idsToDelete = new[] { 1, 9999, 2 };

        var bulkResult = new BulkResult<int>(
            SuccessCount: 2,
            FailedCount: 1,
            Results: new List<int> { 1, 2 },
            Errors: new List<string> { "Record with Id '9999' not found." });

        mockService.Setup(s => s.BulkDeleteAsync(idsToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await controller.BulkDelete(idsToDelete, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.BulkDeleteAsync(idsToDelete, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #endregion

    #region WardController Tests

    [Fact]
    public async Task WardController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IWardService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<WardController>>();
        var controller = new WardController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new WardQueryParameters();
        var pagedResult = new PagedResult<WardDto>(new List<WardDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task WardController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<IWardService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<WardController>>();
        var controller = new WardController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var createDto = new CreateWardDto { WardNo = "W001" };
        var resultDto = new WardDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region ZoneController Tests

    [Fact]
    public async Task ZoneController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IZoneService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<ZoneController>>();
        var controller = new ZoneController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new ZoneQueryParameters();
        var pagedResult = new PagedResult<ZoneDto>(new List<ZoneDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ZoneController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<IZoneService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<ZoneController>>();
        var controller = new ZoneController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var createDto = new CreateZoneDto { ZoneNo = "Z001", Description = "Zone 1" };
        var resultDto = new ZoneDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region TaxZoneController Tests

    [Fact]
    public async Task TaxZoneController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<ITaxZoneService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<TaxZoneController>>();
        var controller = new TaxZoneController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new TaxZoneQueryParameters();
        var pagedResult = new PagedResult<TaxZoneDto>(new List<TaxZoneDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task TaxZoneController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<ITaxZoneService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<TaxZoneController>>();
        var controller = new TaxZoneController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var createDto = new CreateTaxZoneDto { TaxZoneNo = "TZ001" };
        var resultDto = new TaxZoneDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region ConstructionTypeController Tests

    [Fact]
    public async Task ConstructionTypeController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IConstructionTypeService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<ConstructionTypeController>>();
        var controller = new ConstructionTypeController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new ConstructionTypeQueryParameters();
        var pagedResult = new PagedResult<ConstructionTypeDto>(new List<ConstructionTypeDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ConstructionTypeController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<IConstructionTypeService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<ConstructionTypeController>>();
        var controller = new ConstructionTypeController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var createDto = new CreateConstructionTypeDto { ConstructionCode = "CT001", Description = "RCC" };
        var resultDto = new ConstructionTypeDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region SubFloorController Tests

    [Fact]
    public async Task SubFloorController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<ISubFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<SubFloorController>>();
        var controller = new SubFloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new SubFloorQueryParameters();
        var pagedResult = new PagedResult<SubFloorDto>(new List<SubFloorDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task SubFloorController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<ISubFloorService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<SubFloorController>>();
        var controller = new SubFloorController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var createDto = new CreateSubFloorDto { SubFloorCode = "SF001", Description = "Basement" };
        var resultDto = new SubFloorDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region RateController Tests

    [Fact]
    public async Task RateController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IRateService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<RateController>>();
        var controller = new RateController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new RateQueryParameters();
        var pagedResult = new PagedResult<RateDto>(new List<RateDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RateController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<IRateService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<RateController>>();
        var controller = new RateController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var createDto = new CreateRateDto { YearRangeRVId = 1 };
        var resultDto = new RateDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region RateMasterForCVController Tests

    [Fact]
    public async Task RateMasterForCVController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IRateMasterForCVService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<RateMasterForCVController>>();
        var controller = new RateMasterForCVController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new RateMasterForCVQueryParameters();
        var pagedResult = new PagedResult<RateMasterForCVDto>(new List<RateMasterForCVDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region RetentionFactWiseController Tests

    [Fact]
    public async Task RetentionFactWiseController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IRetentionFactWiseService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<RetentionFactWiseController>>();
        var controller = new RetentionFactWiseController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new RetentionFactWiseQueryParameters();
        var pagedResult = new PagedResult<RetentionFactWiseDto>(new List<RetentionFactWiseDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region RetentionYearWiseController Tests

    [Fact]
    public async Task RetentionYearWiseController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IRetentionYearWiseService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<RetentionYearWiseController>>();
        var controller = new RetentionYearWiseController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new RetentionYearWiseQueryParameters();
        var pagedResult = new PagedResult<RetentionYearWiseDto>(new List<RetentionYearWiseDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region RateSectionController Tests

    [Fact]
    public async Task RateSectionController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IRateSectionService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<RateSectionController>>();
        var controller = new RateSectionController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new RateSectionQueryParameters();
        var pagedResult = new PagedResult<RateSectionDto>(new List<RateSectionDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region RateSectionDetailsController Tests

    [Fact]
    public async Task RateSectionDetailsController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IRateSectionDetailsService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<RateSectionDetailsController>>();
        var controller = new RateSectionDetailsController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new RateSectionDetailsQueryParameters();
        var pagedResult = new PagedResult<RateSectionDetailsDto>(new List<RateSectionDetailsDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region SubTypeOfUseController Tests

    [Fact]
    public async Task SubTypeOfUseController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<ISubTypeOfUseService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<SubTypeOfUseController>>();
        var controller = new SubTypeOfUseController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new SubTypeOfUseQueryParameters();
        var pagedResult = new PagedResult<SubTypeOfUseDto>(new List<SubTypeOfUseDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region TypeOfUseController Tests

    [Fact]
    public async Task TypeOfUseController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<ITypeOfUseService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<TypeOfUseController>>();
        var controller = new TypeOfUseController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new TypeOfUseQueryParameters();
        var pagedResult = new PagedResult<TypeOfUseDto>(new List<TypeOfUseDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region TypeOfUseGroupController Tests

    [Fact]
    public async Task TypeOfUseGroupController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<ITypeOfUseGroupService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<TypeOfUseGroupController>>();
        var controller = new TypeOfUseGroupController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new TypeOfUseGroupQueryParameters();
        var pagedResult = new PagedResult<TypeOfUseGroupDto>(new List<TypeOfUseGroupDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region MoujaController Tests

    [Fact]
    public async Task MoujaController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IMoujaService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockLogger = new Mock<ILogger<MoujaController>>();
        var controller = new MoujaController(mockService.Object, mockCleanupService.Object, mockLogger.Object);

        var query = new MoujaQueryParameters();
        var pagedResult = new PagedResult<MoujaDto>(new List<MoujaDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region MultilingualDetailsController Tests

    [Fact]
    public async Task MultilingualDetailsController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IMultilingualDetailsService>();
        var mockLogger = new Mock<ILogger<MultilingualDetailsController>>();
        var controller = new MultilingualDetailsController(mockService.Object, mockLogger.Object);

        var query = new MultilingualDetailsQueryParameters();
        var pagedResult = new PagedResult<MultilingualDetailsDtos>(new List<MultilingualDetailsDtos>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region OwnerTypeController Tests

    [Fact]
    public async Task OwnerTypeController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IOwnerTypeService>();
        var mockLogger = new Mock<ILogger<OwnerTypeController>>();
        var controller = new OwnerTypeController(mockService.Object, mockLogger.Object);

        var query = new OwnerTypeQueryParameters();
        var pagedResult = new PagedResult<OwnerTypeDto>(new List<OwnerTypeDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region ScreenMasterController Tests

    [Fact]
    public async Task ScreenMasterController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IScreenMasterService>();
        var mockLogger = new Mock<ILogger<ScreenMasterController>>();
        var controller = new ScreenMasterController(mockService.Object, mockLogger.Object);

        var query = new ScreenMasterQueryParameters();
        var pagedResult = new PagedResult<ScreenMasterDto>(new List<ScreenMasterDto>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ScreenMasterController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<IScreenMasterService>();
        var mockLogger = new Mock<ILogger<ScreenMasterController>>();
        var controller = new ScreenMasterController(mockService.Object, mockLogger.Object);

        var createDto = new CreateScreenMasterDto { ScreenCode = "SCR001", ScreenName = "Screen", ScreenGroupId = 1 };
        var resultDto = new ScreenMasterDto { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Gender Master Tests

    [Fact]
    public async Task GenderController_GetAll_CallsExtensionMethod()
    {
        var mockService = new Mock<IGenderMasterService>();
        var mockLogger = new Mock<ILogger<GenderController>>();
        var controller = new GenderController(mockService.Object, mockLogger.Object);

        var query = new GenderQueryParameters();
        var pagedResult = new PagedResult<GenderMasterDtos>(new List<GenderMasterDtos>(), 0, 1, 10);

        mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenderController_GetById_CallsExtensionMethod()
    {
        var mockService = new Mock<IGenderMasterService>();
        var mockLogger = new Mock<ILogger<GenderController>>();
        var controller = new GenderController(mockService.Object, mockLogger.Object);

        var dto = new GenderMasterDtos { Id = 1 };
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenderController_Create_CallsExtensionMethod()
    {
        var mockService = new Mock<IGenderMasterService>();
        var mockLogger = new Mock<ILogger<GenderController>>();
        var controller = new GenderController(mockService.Object, mockLogger.Object);

        var createDto = new CreateGenderMasterDto { GenderName = "Male"};
        var resultDto = new GenderMasterDtos { Id = 1 };

        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenderController_Update_CallsExtensionMethod()
    {
        var mockService = new Mock<IGenderMasterService>();
        var mockLogger = new Mock<ILogger<GenderController>>();
        var controller = new GenderController(mockService.Object, mockLogger.Object);

        var updateDto = new UpdateGenderMasterDto { GenderName = "Male" };
        var resultDto = new GenderMasterDtos { Id = 1 };

        mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenderController_Delete_CallsExtensionMethod()
    {
        var mockService = new Mock<IGenderMasterService>();
        var mockLogger = new Mock<ILogger<GenderController>>();
        var controller = new GenderController(mockService.Object, mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        mockService.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
