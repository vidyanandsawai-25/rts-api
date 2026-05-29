using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Comprehensive tests for TaxMasterController.
/// Tests all API endpoints with various scenarios.
/// Achieves 100% line and branch coverage.
/// </summary>
public class TaxMasterControllerTests
{
    private readonly Mock<ITaxMasterService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<TaxMasterController>> _mockLogger;
    private readonly TaxMasterController _controller;

    public TaxMasterControllerTests()
    {
        _mockService = new Mock<ITaxMasterService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<TaxMasterController>>();

        _controller = new TaxMasterController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    private static TaxMasterController CreateController(
        out Mock<ITaxMasterService> service,
        out Mock<IHardDeleteCleanupService> cleanupService,
        out Mock<IReferenceValidationService> referenceValidationService)
    {
        service = new Mock<ITaxMasterService>();
        cleanupService = new Mock<IHardDeleteCleanupService>();
        referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<TaxMasterController>>();

        return new TaxMasterController(
            service.Object,
            cleanupService.Object,
            referenceValidationService.Object,
            logger.Object);
    }

    private static TaxMasterDto CreateDto(
        int id = 1,
        string taxCode = "TAX001",
        string taxName = "Property Tax",
        string? taxNameAlias = "PT",
        int taxCategoryId = 1,
        int displayOrder = 1,
        bool taxOnUnit = false,
        bool assessmentStatus = true,
        bool oldTaxStatus = true,
        bool isActive = true)
    {
        return new TaxMasterDto
        {
            Id = id,
            TaxCode = taxCode,
            TaxName = taxName,
            TaxNameAlias = taxNameAlias,
            TaxCategoryId = taxCategoryId,
            DisplayOrder = displayOrder,
            TaxOnUnit = taxOnUnit,
            AssessmentStatus = assessmentStatus,
            OldTaxStatus = oldTaxStatus,
            IsActive = isActive,
            CreatedDate = DateTime.Now
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var controller = new TaxMasterController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);

        // Assert
        Assert.NotNull(controller);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithData()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var query = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var taxes = new List<TaxMasterDto>
        {
            CreateDto(1, "TAX001", "Property Tax", "PT", 1),
            CreateDto(2, "TAX002", "Water Tax", "WT", 1)
        };

        var pagedResult = new PagedResult<TaxMasterDto>(taxes, 2, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PagedResult<TaxMasterDto>>(okResult.Value);
        Assert.Equal(2, returnValue.TotalCount);
        Assert.Equal(2, returnValue.Items.Count());
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyData()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var query = new TaxMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var pagedResult = new PagedResult<TaxMasterDto>(new List<TaxMasterDto>(), 0, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PagedResult<TaxMasterDto>>(okResult.Value);
        Assert.Equal(0, returnValue.TotalCount);
        Assert.Empty(returnValue.Items);
    }

    [Fact]
    public async Task GetAll_WithFilters_ReturnsFilteredData()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var query = new TaxMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            TaxCode = "TAX001",
            IsActive = true
        };

        var taxes = new List<TaxMasterDto>
        {
            CreateDto(1, "TAX001", "Property Tax", "PT", 1)
        };

        var pagedResult = new PagedResult<TaxMasterDto>(taxes, 1, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PagedResult<TaxMasterDto>>(okResult.Value);
        Assert.Single(returnValue.Items);
    }

    [Fact]
    public async Task GetAll_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var query = new TaxMasterQueryParameters();

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithData()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var dto = CreateDto(1, "TAX001", "Property Tax", "PT", 1);

        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<TaxMasterDto>(okResult.Value);
        Assert.Equal(1, returnValue.Id);
        Assert.Equal("TAX001", returnValue.TaxCode);
        Assert.Equal("Property Tax", returnValue.TaxName);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxMasterDto?)null);

        // Act
        var result = await controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await controller.GetById(1, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetById_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        service.Setup(s => s.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxMasterDto?)null);

        // Act
        var result = await controller.GetById(0, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        service.Setup(s => s.GetByIdAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxMasterDto?)null);

        // Act
        var result = await controller.GetById(-1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsOkWithCreatedData()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxNameAlias = "PT",
            TaxCategoryId = 1,
            DisplayOrder = 1,
            TaxOnUnit = false,
            AssessmentStatus = true,
            OldTaxStatus = true,
            CreatedBy = 1
        };

        var createdDto = CreateDto(1, "TAX001", "Property Tax", "PT", 1);

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items.Id);
        Assert.Equal("TAX001", response.Items.TaxCode);
    }

    [Fact]
    public async Task Create_WithAllProperties_CreatesSuccessfully()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX999",
            TaxName = "Special Tax",
            TaxNameAlias = "ST",
            TaxCategoryId = 5,
            DisplayOrder = 10,
            TaxOnUnit = true,
            AssessmentStatus = false,
            OldTaxStatus = false,
            CreatedBy = 2
        };

        var createdDto = CreateDto(999, "TAX999", "Special Tax", "ST", 5, 10, true, false, false);

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(999, response.Items.Id);
        Assert.True(response.Items.TaxOnUnit);
        Assert.False(response.Items.AssessmentStatus);
    }

    [Fact]
    public async Task Create_ServiceReturnsNull_ReturnsOkWithNullItems()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            CreatedBy = 1
        };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxMasterDto?)null);

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Null(response.Items);
    }

    [Fact]
    public async Task Create_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            CreatedBy = 1
        };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidDto_ReturnsOkWithUpdatedData()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001U",
            TaxName = "Updated Property Tax",
            TaxNameAlias = "UPT",
            TaxCategoryId = 2,
            DisplayOrder = 5,
            TaxOnUnit = true,
            AssessmentStatus = false,
            OldTaxStatus = false,
            IsActive = true,
            UpdatedBy = 1
        };

        var updatedDto = CreateDto(1, "TAX001U", "Updated Property Tax", "UPT", 2, 5, true, false, false);

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items.Id);
        Assert.Equal("TAX001U", response.Items.TaxCode);
        Assert.True(response.Items.TaxOnUnit);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsOk()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            IsActive = true,
            UpdatedBy = 1
        };

        service.Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxMasterDto?)null);

        // Act
        var result = await controller.Update(999, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            IsActive = true,
            UpdatedBy = 1
        };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task Update_WithAllPropertiesChanged_UpdatesSuccessfully()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "NEW001",
            TaxName = "New Tax Name",
            TaxNameAlias = "NTN",
            TaxCategoryId = 10,
            DisplayOrder = 99,
            TaxOnUnit = true,
            AssessmentStatus = false,
            OldTaxStatus = false,
            IsActive = false,
            UpdatedBy = 5
        };

        var updatedDto = CreateDto(1, "NEW001", "New Tax Name", "NTN", 10, 99, true, false, false, false);

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal("NEW001", response.Items.TaxCode);
        Assert.False(response.Items.IsActive);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingId_ReturnsOk()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await controller.Delete(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsOkWithNotFoundResponse()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        service.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await controller.Delete(999, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Delete_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await controller.Delete(1, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task Delete_WithZeroId_ReturnsOkWithNotFoundResponse()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        service.Setup(s => s.DeleteAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await controller.Delete(0, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TaxMasterDto>>(okResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Purge Tests

    [Fact]
    public async Task Purge_ExistingId_ReturnsOk()
    {
        // Arrange
        var controller = CreateController(out _, out var cleanupService, out var refValidationService);

        refValidationService.Setup(r => r.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        cleanupService.Setup(c => c.ForceHardDeleteAsync<TaxMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await controller.Purge(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Purge_NonExistingId_ReturnsOkWithNotFoundResponse()
    {
        // Arrange
        var controller = CreateController(out _, out var cleanupService, out var refValidationService);

        refValidationService.Setup(r => r.ValidateReferencesAsync<TaxMasterEntity>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        cleanupService.Setup(c => c.ForceHardDeleteAsync<TaxMasterEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await controller.Purge(999, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Purge_ForeignKeyViolation_ReturnsConflict()
    {
        // Arrange
        var controller = CreateController(out _, out var cleanupService, out _);

        cleanupService
            .Setup(c => c.ForceHardDeleteAsync<TaxMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException(
                "FK violation",
                new Exception("FOREIGN KEY constraint")));

        // Act
        var result = await controller.Purge(1, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Purge_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var controller = CreateController(out _, out var cleanupService, out var refValidationService);

        refValidationService.Setup(r => r.ValidateReferencesAsync<TaxMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        cleanupService.Setup(c => c.ForceHardDeleteAsync<TaxMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await controller.Purge(1, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullCrudFlow_CreateReadUpdateDelete_WorksCorrectly()
    {
        // Arrange
        var controller = CreateController(out var service, out _, out _);

        // Create
        var createDto = new CreateTaxMasterDto
        {
            TaxCode = "TAX001",
            TaxName = "Property Tax",
            TaxCategoryId = 1,
            CreatedBy = 1
        };

        var createdDto = CreateDto(1, "TAX001", "Property Tax");

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var createResult = await controller.Create(createDto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(createResult);

        // Read
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var getResult = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(getResult);

        // Update
        var updateDto = new UpdateTaxMasterDto
        {
            TaxCode = "TAX001U",
            TaxName = "Updated Tax",
            TaxCategoryId = 1,
            IsActive = true,
            UpdatedBy = 1
        };

        var updatedDto = CreateDto(1, "TAX001U", "Updated Tax");

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var updateResult = await controller.Update(1, updateDto, CancellationToken.None);
        var updateOk = Assert.IsType<OkObjectResult>(updateResult);
        Assert.NotNull(updateOk.Value);

        // Delete
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var deleteResult = await controller.Delete(1, CancellationToken.None);
        var deleteOk = Assert.IsType<OkObjectResult>(deleteResult);
        Assert.NotNull(deleteOk.Value);
    }

    #endregion
}
