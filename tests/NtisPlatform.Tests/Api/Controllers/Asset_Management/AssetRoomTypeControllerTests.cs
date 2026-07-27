using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Master.AssetRoomType;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

/// <summary>
/// Comprehensive tests for AssetRoomTypeController covering GetAll, GetById, Create, Update,
/// Delete (soft) and Purge (hard delete) endpoints, including asset-type/category validation,
/// duplicate room-type-name/code conflicts, and reference validation on deactivate/delete.
/// </summary>
public class AssetRoomTypeControllerTests
{
    private static AssetRoomTypeController Create(
        out Mock<IAssetRoomTypeMasterService> service,
        out Mock<IHardDeleteCleanupService> cleanup,
        out Mock<IReferenceValidationService> referenceValidation)
    {
        service = new Mock<IAssetRoomTypeMasterService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        referenceValidation = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<AssetRoomTypeController>>();
        return new AssetRoomTypeController(service.Object, cleanup.Object, referenceValidation.Object, logger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var controller = Create(out _, out _, out _);

        Assert.NotNull(controller);
    }

    #endregion

    #region Route / Attribute Contract Tests

    [Fact]
    public void Controller_HasExpectedRoutePrefix()
    {
        var attribute = typeof(AssetRoomTypeController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        Assert.NotNull(attribute);
        Assert.Equal("api/asset-management/asset-room-type", attribute!.Template);
    }

    [Fact]
    public void Purge_HasAuthorizeAttribute()
    {
        var method = typeof(AssetRoomTypeController).GetMethod(nameof(AssetRoomTypeController.Purge));

        var attributes = method?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

        Assert.NotNull(attributes);
        Assert.NotEmpty(attributes);
    }

    [Fact]
    public void Purge_HasCorrectRouteTemplate()
    {
        var method = typeof(AssetRoomTypeController).GetMethod(nameof(AssetRoomTypeController.Purge));

        var attribute = method?.GetCustomAttributes(typeof(HttpDeleteAttribute), false)
            .FirstOrDefault() as HttpDeleteAttribute;

        Assert.NotNull(attribute);
        Assert.Equal("{id}/purge", attribute!.Template);
    }

    [Fact]
    public void Purge_DeclaresApiResponseOfObject_MatchingExecuteForceDeleteContract()
    {
        // ExecuteForceDelete<TEntity, TKey> always wraps its payload as ApiResponse<object> (success,
        // not-found and FK-conflict paths alike) — the ProducesResponseType contract must match that,
        // not the entity's own DTO type.
        var method = typeof(AssetRoomTypeController).GetMethod(nameof(AssetRoomTypeController.Purge));

        var attributes = method?.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), false)
            .Cast<ProducesResponseTypeAttribute>()
            .ToList();

        Assert.NotNull(attributes);
        Assert.All(attributes!, a => Assert.Equal(typeof(ApiResponse<object>), a.Type));
        Assert.Contains(attributes!, a => a.StatusCode == StatusCodes.Status200OK);
        Assert.Contains(attributes!, a => a.StatusCode == StatusCodes.Status409Conflict);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidQuery_ReturnsOkWithPagedResult()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetRoomTypeQueryParameters { PageNumber = 1, PageSize = 10 };
        var data = new List<AssetRoomTypeMasterDto>
        {
            new() { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", IsActive = true },
            new() { Id = 2, AssetTypeId = 1, RoomTypeName = "Kitchen", IsActive = true }
        };
        var pagedResult = new PagedResult<AssetRoomTypeMasterDto>(data, 2, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.Equal(2, returnedData.TotalCount);
        Assert.Equal(2, returnedData.Items.Count());
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetRoomTypeQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetRoomTypeMasterDto>(new List<AssetRoomTypeMasterDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.Empty(returnedData.Items);
    }

    [Fact]
    public async Task GetAll_WithAssetTypeAndCategoryFilters_ReturnsFilteredResults()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetRoomTypeQueryParameters { AssetTypeId = 1, AssetCategoryId = 2, IsActive = true };
        var pagedResult = new PagedResult<AssetRoomTypeMasterDto>(
            new List<AssetRoomTypeMasterDto>
            {
                new() { Id = 1, AssetTypeId = 1, AssetCategoryId = 2, RoomTypeName = "Bedroom", IsActive = true }
            },
            1, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.Single(returnedData.Items);
    }

    [Fact]
    public async Task GetAll_ReturnedDto_IncludesEnrichedNames()
    {
        // AssetRoomTypeService enriches AssetCategoryName/AssetTypeName server-side;
        // the controller must pass that enrichment straight through untouched.
        var controller = Create(out var service, out _, out _);
        var query = new AssetRoomTypeQueryParameters();
        var data = new List<AssetRoomTypeMasterDto>
        {
            new() { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", AssetTypeName = "Residential", AssetCategoryName = "Housing" }
        };
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetRoomTypeMasterDto>(data, 1, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.Equal("Residential", returnedData.Items.First().AssetTypeName);
        Assert.Equal("Housing", returnedData.Items.First().AssetCategoryName);
    }

    [Fact]
    public async Task GetAll_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetRoomTypeQueryParameters();
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.GetAllAsync(query, cancellationToken))
            .ReturnsAsync(new PagedResult<AssetRoomTypeMasterDto>(new List<AssetRoomTypeMasterDto>(), 0, 1, 10));

        await controller.GetAll(query, cancellationToken);

        service.Verify(s => s.GetAllAsync(query, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenFilterValidationFails_ReturnsBadRequest()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetRoomTypeQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("SortBy", "Unknown sort field"));

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrowsUnexpectedException_Returns500()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetRoomTypeQueryParameters();
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
        var controller = Create(out var service, out _, out _);
        var dto = new AssetRoomTypeMasterDto { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", IsActive = true };
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<AssetRoomTypeMasterDto>(okResult.Value);
        Assert.Equal(1, returnedDto.Id);
        Assert.Equal("Bedroom", returnedDto.RoomTypeName);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((AssetRoomTypeMasterDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.GetByIdAsync(1, cancellationToken)).ReturnsAsync((AssetRoomTypeMasterDto?)null);

        await controller.GetById(1, cancellationToken);

        service.Verify(s => s.GetByIdAsync(1, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceThrowsUnexpectedException_Returns500()
    {
        var controller = Create(out var service, out _, out _);
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
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", IsActive = true, CreatedBy = 1 };
        var createdDto = new AssetRoomTypeMasterDto { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom", IsActive = true };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(createdDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.Id);
    }

    [Fact]
    public async Task Create_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom" };
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.CreateAsync(createDto, cancellationToken))
            .ReturnsAsync(new AssetRoomTypeMasterDto { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom" });

        await controller.Create(createDto, cancellationToken);

        service.Verify(s => s.CreateAsync(createDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Create_WithNonExistentAssetTypeId_ThrowsValidationException()
    {
        // Business rule enforced by AssetRoomTypeService.EnsureAssetTypeExistsAsync
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 999, RoomTypeName = "Bedroom" };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(createDto.AssetTypeId), "Asset type with ID 999 not found.", OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithNonExistentAssetCategoryId_ThrowsValidationException()
    {
        // Business rule enforced by AssetRoomTypeService.EnsureAssetCategoryExistsAsync
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, AssetCategoryId = 999, RoomTypeName = "Bedroom" };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(createDto.AssetCategoryId), "Asset category with ID 999 not found.", OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithDuplicateRoomTypeNameForSameAssetType_ThrowsValidationException()
    {
        // Business rule enforced by AssetRoomTypeService.ValidateForCreateAsync (UQ per AssetTypeId + RoomTypeName)
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom" };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(createDto.RoomTypeName), "AssetRoomType_RoomTypeName_Duplicate", OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithDuplicateRoomTypeCodeForSameAssetType_ThrowsValidationException()
    {
        // Business rule enforced by AssetRoomTypeService.ValidateForCreateAsync (UQ per AssetTypeId + RoomTypeCode)
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom 2", RoomTypeCode = "BR" };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(createDto.RoomTypeCode), "AssetRoomType_RoomTypeCode_Duplicate", OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WhenNonValidationDuplicateConstraintErrorOccurs_ReturnsConflict()
    {
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom" };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Violation of UNIQUE KEY constraint"));

        var result = await controller.Create(createDto, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetRoomTypeMasterDto>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Create_WhenUnexpectedExceptionOccurs_Returns500()
    {
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom" };

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
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom Updated", IsActive = true, UpdatedBy = 1 };
        var updatedDto = new AssetRoomTypeMasterDto { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom Updated", IsActive = true };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(updatedDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Bedroom Updated", response.Items!.RoomTypeName);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsOkWithFailureMessage()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom" };
        service.Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync((AssetRoomTypeMasterDto?)null);

        var result = await controller.Update(999, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom" };
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.UpdateAsync(1, updateDto, cancellationToken))
            .ReturnsAsync(new AssetRoomTypeMasterDto { Id = 1, AssetTypeId = 1, RoomTypeName = "Bedroom" });

        await controller.Update(1, updateDto, cancellationToken);

        service.Verify(s => s.UpdateAsync(1, updateDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistentAssetTypeId_ThrowsValidationException()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 999, RoomTypeName = "Bedroom" };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(updateDto.AssetTypeId), "Asset type with ID 999 not found.", OperationType.Update));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Update(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task Update_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        // Business rule enforced by AssetRoomTypeService.ValidateForDeactivationAsync
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom", IsActive = false };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("IsActive", "Cannot deactivate - record is referenced by other entities", OperationType.Update));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Update(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task Update_WhenNonValidationDuplicateConstraintErrorOccurs_ReturnsConflict()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetRoomTypeDto { AssetTypeId = 1, RoomTypeName = "Bedroom" };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate key value violates unique constraint"));

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetRoomTypeMasterDto>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Delete (Soft Delete) Tests

    [Fact]
    public async Task Delete_ExistingId_ReturnsOkWithSuccessTrue()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("marked for deletion", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsOkWithSuccessFalse()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(999, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetRoomTypeMasterDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_ReferencedRecord_ThrowsValidationException()
    {
        // Business rule enforced by AssetRoomTypeService.ValidateForDeleteAsync
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Id", "Cannot delete - record is referenced by other entities", OperationType.Delete));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Delete(1, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.DeleteAsync(1, cancellationToken)).ReturnsAsync(true);

        await controller.Delete(1, cancellationToken);

        service.Verify(s => s.DeleteAsync(1, cancellationToken), Times.Once);
    }

    #endregion

    #region Purge (Hard Delete) Tests

    [Fact]
    public async Task Purge_ExistingId_ReturnsOkWithSuccessTrue()
    {
        var controller = Create(out _, out var cleanup, out _);
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetRoomTypeMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("permanently deleted", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Purge_EntityNotFound_ReturnsOkWithSuccessFalse()
    {
        var controller = Create(out _, out var cleanup, out _);
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetRoomTypeMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.Purge(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Purge_WithForeignKeyConflict_ReturnsConflict()
    {
        var controller = Create(out _, out var cleanup, out _);

        var innerException = new Exception("The DELETE statement conflicted with the FOREIGN KEY constraint");
        var dbException = new Microsoft.EntityFrameworkCore.DbUpdateException("An error occurred while updating the entries.", innerException);

        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetRoomTypeMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbException);

        var result = await controller.Purge(1, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Cannot delete this record", response.Message);
    }

    [Fact]
    public async Task Purge_WithCancellationToken_PassesTokenCorrectly()
    {
        var controller = Create(out _, out var cleanup, out _);
        var cancellationToken = new CancellationToken();
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetRoomTypeMasterEntity, int>(1, cancellationToken))
            .ReturnsAsync(true);

        await controller.Purge(1, cancellationToken);

        cleanup.Verify(c => c.ForceHardDeleteAsync<AssetRoomTypeMasterEntity, int>(1, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Purge_WhenUnexpectedExceptionOccurs_Returns500()
    {
        var controller = Create(out _, out var cleanup, out _);
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetRoomTypeMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("unexpected failure"));

        var result = await controller.Purge(1, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region Delete vs Purge Semantic Difference

    [Fact]
    public async Task Delete_And_Purge_ReturnDistinctSuccessMessages()
    {
        var controller = Create(out var service, out var cleanup, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetRoomTypeMasterEntity, int>(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var softDeleteResult = await controller.Delete(1, CancellationToken.None);
        var purgeResult = await controller.Purge(2, CancellationToken.None);

        var softOk = Assert.IsType<OkObjectResult>(softDeleteResult);
        var purgeOk = Assert.IsType<OkObjectResult>(purgeResult);

        var softResponse = Assert.IsType<ApiResponse<AssetRoomTypeMasterDto>>(softOk.Value);
        var purgeResponse = Assert.IsType<ApiResponse<object>>(purgeOk.Value);

        Assert.NotEqual(softResponse.Message, purgeResponse.Message);
        Assert.Contains("marked", softResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permanently", purgeResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
