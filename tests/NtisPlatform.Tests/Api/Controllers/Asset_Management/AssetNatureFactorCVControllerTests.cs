using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetNatureFactorCVMaster;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

/// <summary>
/// Comprehensive tests for AssetNatureFactorCVController covering GetAll, GetById, Create, Update,
/// Delete and Bulk (Create/Update/Delete) endpoints, including construction-type/year-range
/// existence validation and duplicate-combination conflicts.
/// </summary>
public class AssetNatureFactorCVControllerTests
{
    private static AssetNatureFactorCVController Create(out Mock<IAssetNatureFactorCVService> service)
    {
        service = new Mock<IAssetNatureFactorCVService>();
        var logger = new Mock<ILogger<AssetNatureFactorCVController>>();
        return new AssetNatureFactorCVController(service.Object, logger.Object);
    }

    #region Constructor / Route Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var controller = Create(out _);

        Assert.NotNull(controller);
    }

    [Fact]
    public void Controller_HasExpectedRoutePrefix()
    {
        var attribute = typeof(AssetNatureFactorCVController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        Assert.NotNull(attribute);
        Assert.Equal("api/asset-management/nature-factor-cv", attribute!.Template);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidQuery_ReturnsOkWithPagedResult()
    {
        var controller = Create(out var service);
        var query = new AssetNatureFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var data = new List<AssetNatureFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m, IsActive = true },
            new() { Id = 2, ConstructionTypeId = 2, YearRangeCVId = 1, Factor = 0.9m, IsActive = true }
        };
        var pagedResult = new PagedResult<AssetNatureFactorCVMasterDto>(data, 2, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetNatureFactorCVMasterDto>>(okResult.Value);
        Assert.Equal(2, returnedData.TotalCount);
        Assert.Equal(2, returnedData.Items.Count());
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        var controller = Create(out var service);
        var query = new AssetNatureFactorCVMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetNatureFactorCVMasterDto>(new List<AssetNatureFactorCVMasterDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetNatureFactorCVMasterDto>>(okResult.Value);
        Assert.Empty(returnedData.Items);
    }

    [Fact]
    public async Task GetAll_WithFilters_ReturnsFilteredResults()
    {
        var controller = Create(out var service);
        var query = new AssetNatureFactorCVMasterQueryParameters { ConstructionTypeId = 1, YearRangeCVId = 1, IsActive = true };
        var pagedResult = new PagedResult<AssetNatureFactorCVMasterDto>(
            new List<AssetNatureFactorCVMasterDto> { new() { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m, IsActive = true } },
            1, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetNatureFactorCVMasterDto>>(okResult.Value);
        Assert.Single(returnedData.Items);
    }

    [Fact]
    public async Task GetAll_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service);
        var query = new AssetNatureFactorCVMasterQueryParameters();
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.GetAllAsync(query, cancellationToken))
            .ReturnsAsync(new PagedResult<AssetNatureFactorCVMasterDto>(new List<AssetNatureFactorCVMasterDto>(), 0, 1, 10));

        await controller.GetAll(query, cancellationToken);

        service.Verify(s => s.GetAllAsync(query, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenFilterValidationFails_ReturnsBadRequest()
    {
        var controller = Create(out var service);
        var query = new AssetNatureFactorCVMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("SortBy", "Unknown sort field"));

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrowsUnexpectedException_Returns500()
    {
        var controller = Create(out var service);
        var query = new AssetNatureFactorCVMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await controller.GetAll(query, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithDto()
    {
        var controller = Create(out var service);
        var dto = new AssetNatureFactorCVMasterDto { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m, IsActive = true };
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<AssetNatureFactorCVMasterDto>(okResult.Value);
        Assert.Equal(1, returnedDto.Id);
        Assert.Equal(1.0m, returnedDto.Factor);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((AssetNatureFactorCVMasterDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service);
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.GetByIdAsync(1, cancellationToken)).ReturnsAsync((AssetNatureFactorCVMasterDto?)null);

        await controller.GetById(1, cancellationToken);

        service.Verify(s => s.GetByIdAsync(1, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceThrowsUnexpectedException_Returns500()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await controller.GetById(1, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkWithCreatedData()
    {
        var controller = Create(out var service);
        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m, IsActive = true, CreatedBy = 1 };
        var createdDto = new AssetNatureFactorCVMasterDto { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m, IsActive = true };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(createdDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetNatureFactorCVMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.Id);
    }

    [Fact]
    public async Task Create_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service);
        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m };
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.CreateAsync(createDto, cancellationToken))
            .ReturnsAsync(new AssetNatureFactorCVMasterDto { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m });

        await controller.Create(createDto, cancellationToken);

        service.Verify(s => s.CreateAsync(createDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Create_WithNonExistentConstructionTypeId_ThrowsValidationException()
    {
        // Business rule enforced by AssetNatureFactorCVService.EnsureConstructionTypeExistsAsync
        var controller = Create(out var service);
        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 999, YearRangeCVId = 1, Factor = 1.0m };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(createDto.ConstructionTypeId), "Construction type with ID 999 not found.", OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithNonExistentYearRangeCVId_ThrowsValidationException()
    {
        // Business rule enforced by AssetNatureFactorCVService.EnsureYearRangeExistsAsync
        var controller = Create(out var service);
        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 999, Factor = 1.0m };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(createDto.YearRangeCVId), "Assessment year range with ID 999 not found.", OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithDuplicateConstructionTypeAndYearRangeCombination_ThrowsValidationException()
    {
        // Business rule enforced by AssetNatureFactorCVService.ValidateForCreateAsync
        var controller = Create(out var service);
        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(createDto.YearRangeCVId), "NatureFactorCV_Combination_Duplicate", OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WhenNonValidationDuplicateConstraintErrorOccurs_ReturnsConflict()
    {
        var controller = Create(out var service);
        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Violation of UNIQUE KEY constraint"));

        var result = await controller.Create(createDto, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetNatureFactorCVMasterDto>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Create_WhenUnexpectedExceptionOccurs_Returns500()
    {
        var controller = Create(out var service);
        var createDto = new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("something else went wrong"));

        var result = await controller.Create(createDto, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingId_ReturnsOkWithUpdatedData()
    {
        var controller = Create(out var service);
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 0.95m, IsActive = true, UpdatedBy = 1 };
        var updatedDto = new AssetNatureFactorCVMasterDto { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 0.95m, IsActive = true };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(updatedDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetNatureFactorCVMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(0.95m, response.Items!.Factor);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsOkWithFailureMessage()
    {
        var controller = Create(out var service);
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m };
        service.Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync((AssetNatureFactorCVMasterDto?)null);

        var result = await controller.Update(999, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetNatureFactorCVMasterDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service);
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m };
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.UpdateAsync(1, updateDto, cancellationToken))
            .ReturnsAsync(new AssetNatureFactorCVMasterDto { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m });

        await controller.Update(1, updateDto, cancellationToken);

        service.Verify(s => s.UpdateAsync(1, updateDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistentConstructionTypeId_ThrowsValidationException()
    {
        var controller = Create(out var service);
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 999, YearRangeCVId = 1, Factor = 1.0m };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(updateDto.ConstructionTypeId), "Construction type with ID 999 not found.", OperationType.Update));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Update(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task Update_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        // Business rule enforced by AssetNatureFactorCVService.ValidateForDeactivationAsync
        var controller = Create(out var service);
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m, IsActive = false };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("IsActive", "Cannot deactivate - record is referenced by other entities", OperationType.Update));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Update(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task Update_WhenNonValidationDuplicateConstraintErrorOccurs_ReturnsConflict()
    {
        var controller = Create(out var service);
        var updateDto = new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate key value violates unique constraint"));

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetNatureFactorCVMasterDto>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingId_ReturnsOkWithSuccessTrue()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetNatureFactorCVMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("marked for deletion", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsOkWithSuccessFalse()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(999, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetNatureFactorCVMasterDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_ReferencedRecord_ThrowsValidationException()
    {
        // Business rule enforced by AssetNatureFactorCVService.ValidateForDeleteAsync
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Id", "Cannot delete - record is referenced by other entities", OperationType.Delete));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Delete(1, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service);
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.DeleteAsync(1, cancellationToken)).ReturnsAsync(true);

        await controller.Delete(1, cancellationToken);

        service.Verify(s => s.DeleteAsync(1, cancellationToken), Times.Once);
    }

    #endregion

    #region BulkCreate Tests

    [Fact]
    public async Task BulkCreate_WithValidItems_ReturnsOkWithSuccessCount()
    {
        var controller = Create(out var service);
        var items = new[]
        {
            new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m },
            new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 2, YearRangeCVId = 1, Factor = 0.9m }
        };
        var createdItems = new List<AssetNatureFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m },
            new() { Id = 2, ConstructionTypeId = 2, YearRangeCVId = 1, Factor = 0.9m }
        };
        var bulkResult = new BulkResult<AssetNatureFactorCVMasterDto>(2, 0, createdItems);

        service.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>())).ReturnsAsync(bulkResult);

        var result = await controller.BulkCreate(items, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<AssetNatureFactorCVMasterDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Items!.SuccessCount);
        Assert.Equal(0, response.Items!.FailedCount);
    }

    [Fact]
    public async Task BulkCreate_WithNullArray_ReturnsBadRequest()
    {
        var controller = Create(out _);

        var result = await controller.BulkCreate(null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<AssetNatureFactorCVMasterDto>>>(badRequest.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task BulkCreate_WithEmptyArray_ReturnsBadRequest()
    {
        var controller = Create(out _);
        var items = Array.Empty<CreateAssetNatureFactorCVMasterDto>();

        var result = await controller.BulkCreate(items, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<AssetNatureFactorCVMasterDto>>>(badRequest.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task BulkCreate_WithPartialFailures_ReturnsOkWithMixedResults()
    {
        var controller = Create(out var service);
        var items = new[]
        {
            new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m },
            new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 999, YearRangeCVId = 1, Factor = 0.9m }
        };
        var createdItems = new List<AssetNatureFactorCVMasterDto> { new() { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m } };
        var bulkResult = new BulkResult<AssetNatureFactorCVMasterDto>(1, 1, createdItems, new List<string> { "Item 1: ConstructionTypeId not found" });

        service.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>())).ReturnsAsync(bulkResult);

        var result = await controller.BulkCreate(items, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<AssetNatureFactorCVMasterDto>>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Equal(1, response.Items!.SuccessCount);
        Assert.Equal(1, response.Items!.FailedCount);
    }

    [Fact]
    public async Task BulkCreate_WhenNonValidationDuplicateConstraintErrorOccurs_ReturnsConflict()
    {
        var controller = Create(out var service);
        var items = new[] { new CreateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 1.0m } };

        service.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate key value violates unique constraint"));

        var result = await controller.BulkCreate(items, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<AssetNatureFactorCVMasterDto>>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region BulkUpdate Tests

    [Fact]
    public async Task BulkUpdate_WithValidItems_ReturnsOkWithSuccessCount()
    {
        var controller = Create(out var service);
        var items = new[]
        {
            new BulkUpdateItem<int, UpdateAssetNatureFactorCVMasterDto>(1, new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 0.95m }),
            new BulkUpdateItem<int, UpdateAssetNatureFactorCVMasterDto>(2, new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 2, YearRangeCVId = 1, Factor = 0.85m })
        };
        var updatedItems = new List<AssetNatureFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 0.95m },
            new() { Id = 2, ConstructionTypeId = 2, YearRangeCVId = 1, Factor = 0.85m }
        };
        var bulkResult = new BulkResult<AssetNatureFactorCVMasterDto>(2, 0, updatedItems);

        service.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>())).ReturnsAsync(bulkResult);

        var result = await controller.BulkUpdate(items, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<AssetNatureFactorCVMasterDto>>>(okResult.Value);
        Assert.Equal(2, response.Items!.SuccessCount);
        Assert.Equal(0, response.Items!.FailedCount);
    }

    [Fact]
    public async Task BulkUpdate_WithNullArray_ReturnsBadRequest()
    {
        var controller = Create(out _);

        var result = await controller.BulkUpdate(null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<AssetNatureFactorCVMasterDto>>>(badRequest.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task BulkUpdate_WithNonExistingIds_ReturnsPartialFailure()
    {
        var controller = Create(out var service);
        var items = new[]
        {
            new BulkUpdateItem<int, UpdateAssetNatureFactorCVMasterDto>(1, new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 0.95m }),
            new BulkUpdateItem<int, UpdateAssetNatureFactorCVMasterDto>(999, new UpdateAssetNatureFactorCVMasterDto { ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 0.85m })
        };
        var updatedItems = new List<AssetNatureFactorCVMasterDto> { new() { Id = 1, ConstructionTypeId = 1, YearRangeCVId = 1, Factor = 0.95m } };
        var bulkResult = new BulkResult<AssetNatureFactorCVMasterDto>(1, 1, updatedItems, new List<string> { "Record with Id '999' not found." });

        service.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>())).ReturnsAsync(bulkResult);

        var result = await controller.BulkUpdate(items, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<AssetNatureFactorCVMasterDto>>>(okResult.Value);
        Assert.Equal(1, response.Items!.SuccessCount);
        Assert.Equal(1, response.Items!.FailedCount);
    }

    #endregion

    #region BulkDelete Tests

    [Fact]
    public async Task BulkDelete_WithValidIds_ReturnsOkWithSuccessCount()
    {
        var controller = Create(out var service);
        var ids = new[] { 1, 2, 3 };
        var bulkResult = new BulkResult<int>(3, 0, new List<int> { 1, 2, 3 });

        service.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(bulkResult);

        var result = await controller.BulkDelete(ids, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.Equal(3, response.Items!.SuccessCount);
        Assert.Equal(0, response.Items!.FailedCount);
    }

    [Fact]
    public async Task BulkDelete_WithNullArray_ReturnsBadRequest()
    {
        var controller = Create(out _);

        var result = await controller.BulkDelete(null!, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequest.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task BulkDelete_WithReferencedRecords_ReturnsPartialFailure()
    {
        var controller = Create(out var service);
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(1, 1, new List<int> { 1 }, new List<string> { "Record with Id '2': Cannot delete - record is referenced by other entities" });

        service.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(bulkResult);

        var result = await controller.BulkDelete(ids, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.Equal(1, response.Items!.SuccessCount);
        Assert.Equal(1, response.Items!.FailedCount);
    }

    [Fact]
    public async Task BulkDelete_WhenServiceThrowsUnexpectedException_Returns500()
    {
        var controller = Create(out var service);
        var ids = new[] { 1 };

        service.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var result = await controller.BulkDelete(ids, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion
}
