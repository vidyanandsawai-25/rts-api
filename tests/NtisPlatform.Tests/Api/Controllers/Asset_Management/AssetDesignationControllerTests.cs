using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

/// <summary>
/// Comprehensive tests for AssetDesignationController covering GetAll, GetById, Create, Update,
/// Delete (soft) and Purge (hard delete) endpoints, including owning-department validation and
/// reference validation on deactivate/delete.
/// </summary>
public class AssetDesignationControllerTests
{
    private static AssetDesignationController Create(
        out Mock<IAssetDesignationService> service,
        out Mock<IHardDeleteCleanupService> cleanup,
        out Mock<IReferenceValidationService> referenceValidation)
    {
        service = new Mock<IAssetDesignationService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        referenceValidation = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<AssetDesignationController>>();
        return new AssetDesignationController(service.Object, cleanup.Object, referenceValidation.Object, logger.Object);
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
        // [Route("api/[controller]")] resolves to "api/AssetDesignation" for AssetDesignationController.
        var attribute = typeof(AssetDesignationController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        Assert.NotNull(attribute);
        Assert.Equal("api/[controller]", attribute!.Template);
    }

    [Fact]
    public void Purge_HasAuthorizeAttribute()
    {
        var method = typeof(AssetDesignationController).GetMethod(nameof(AssetDesignationController.Purge));

        var attributes = method?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

        Assert.NotNull(attributes);
        Assert.NotEmpty(attributes);
    }

    [Fact]
    public void Purge_HasCorrectRouteTemplate()
    {
        var method = typeof(AssetDesignationController).GetMethod(nameof(AssetDesignationController.Purge));

        var attribute = method?.GetCustomAttributes(typeof(HttpDeleteAttribute), false)
            .FirstOrDefault() as HttpDeleteAttribute;

        Assert.NotNull(attribute);
        Assert.Equal("{id}/purge", attribute!.Template);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidQuery_ReturnsOkWithPagedResult()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetDesignationQueryParameters { PageNumber = 1, PageSize = 10 };
        var data = new List<AssetDesignationDto>
        {
            new() { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", IsActive = true },
            new() { Id = 2, OwningDepartmentId = 1, DesignationCode = "MGR", DesignationName = "Manager", IsActive = true }
        };
        var pagedResult = new PagedResult<AssetDesignationDto>(data, 2, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetDesignationDto>>(okResult.Value);
        Assert.Equal(2, returnedData.TotalCount);
        Assert.Equal(2, returnedData.Items.Count());
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetDesignationQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetDesignationDto>(new List<AssetDesignationDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetDesignationDto>>(okResult.Value);
        Assert.Empty(returnedData.Items);
    }

    [Fact]
    public async Task GetAll_WithOwningDepartmentFilter_ReturnsFilteredResults()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetDesignationQueryParameters { OwningDepartmentId = 1, IsActive = true };
        var pagedResult = new PagedResult<AssetDesignationDto>(
            new List<AssetDesignationDto> { new() { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", IsActive = true } },
            1, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetDesignationDto>>(okResult.Value);
        Assert.Single(returnedData.Items);
    }

    [Fact]
    public async Task GetAll_ReturnedDto_IncludesEnrichedOwningDepartmentName()
    {
        // AssetDesignationService enriches OwningDepartmentName server-side;
        // the controller must pass that enrichment straight through untouched.
        var controller = Create(out var service, out _, out _);
        var query = new AssetDesignationQueryParameters();
        var data = new List<AssetDesignationDto>
        {
            new() { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", OwningDepartmentName = "Public Works" }
        };
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetDesignationDto>(data, 1, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetDesignationDto>>(okResult.Value);
        Assert.Equal("Public Works", returnedData.Items.First().OwningDepartmentName);
    }

    [Fact]
    public async Task GetAll_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetDesignationQueryParameters();
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.GetAllAsync(query, cancellationToken))
            .ReturnsAsync(new PagedResult<AssetDesignationDto>(new List<AssetDesignationDto>(), 0, 1, 10));

        await controller.GetAll(query, cancellationToken);

        service.Verify(s => s.GetAllAsync(query, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenFilterValidationFails_ReturnsBadRequest()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetDesignationQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("SortBy", "Unknown sort field"));

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrowsUnexpectedException_Returns500()
    {
        var controller = Create(out var service, out _, out _);
        var query = new AssetDesignationQueryParameters();
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
        var dto = new AssetDesignationDto { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", IsActive = true };
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<AssetDesignationDto>(okResult.Value);
        Assert.Equal(1, returnedDto.Id);
        Assert.Equal("Engineer", returnedDto.DesignationName);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((AssetDesignationDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.GetByIdAsync(1, cancellationToken)).ReturnsAsync((AssetDesignationDto?)null);

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
        var createDto = new CreateAssetDesignationDto
        {
            OwningDepartmentId = 1,
            DesignationCode = "ENG",
            DesignationName = "Engineer",
            IsActive = true,
            CreatedBy = 1
        };
        var createdDto = new AssetDesignationDto { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", IsActive = true };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(createdDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetDesignationDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.Id);
    }

    [Fact]
    public async Task Create_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.CreateAsync(createDto, cancellationToken))
            .ReturnsAsync(new AssetDesignationDto { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" });

        await controller.Create(createDto, cancellationToken);

        service.Verify(s => s.CreateAsync(createDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Create_WithNonExistentOwningDepartmentId_ThrowsValidationException()
    {
        // Business rule enforced by AssetDesignationService.EnsureOwningDepartmentExistsAsync
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 999, DesignationCode = "ENG", DesignationName = "Engineer" };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(createDto.OwningDepartmentId), "Owning department with ID 999 not found.", OperationType.Create));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WhenNonValidationDuplicateConstraintErrorOccurs_ReturnsConflict()
    {
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Violation of UNIQUE KEY constraint"));

        var result = await controller.Create(createDto, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetDesignationDto>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Create_WhenUnexpectedExceptionOccurs_Returns500()
    {
        var controller = Create(out var service, out _, out _);
        var createDto = new CreateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };

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
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Senior Engineer", IsActive = true, UpdatedBy = 1 };
        var updatedDto = new AssetDesignationDto { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Senior Engineer", IsActive = true };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(updatedDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetDesignationDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Senior Engineer", response.Items!.DesignationName);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsOkWithFailureMessage()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };
        service.Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync((AssetDesignationDto?)null);

        var result = await controller.Update(999, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetDesignationDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WithCancellationToken_PassesTokenToService()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.UpdateAsync(1, updateDto, cancellationToken))
            .ReturnsAsync(new AssetDesignationDto { Id = 1, OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" });

        await controller.Update(1, updateDto, cancellationToken);

        service.Verify(s => s.UpdateAsync(1, updateDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistentOwningDepartmentId_ThrowsValidationException()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 999, DesignationCode = "ENG", DesignationName = "Engineer" };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(nameof(updateDto.OwningDepartmentId), "Owning department with ID 999 not found.", OperationType.Update));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Update(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task Update_DeactivatingReferencedRecord_ThrowsValidationException()
    {
        // Business rule enforced by AssetDesignationService.ValidateForDeactivationAsync
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer", IsActive = false };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("IsActive", "Cannot deactivate - record is referenced by other entities", OperationType.Update));

        await Assert.ThrowsAsync<ValidationException>(() => controller.Update(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task Update_WhenNonValidationDuplicateConstraintErrorOccurs_ReturnsConflict()
    {
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdateAssetDesignationDto { OwningDepartmentId = 1, DesignationCode = "ENG", DesignationName = "Engineer" };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate key value violates unique constraint"));

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetDesignationDto>>(conflictResult.Value);
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
        var response = Assert.IsType<ApiResponse<AssetDesignationDto>>(okResult.Value);
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
        var response = Assert.IsType<ApiResponse<AssetDesignationDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_ReferencedRecord_ThrowsValidationException()
    {
        // Business rule enforced by AssetDesignationService.ValidateForDeleteAsync
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
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetDesignationEntity, int>(1, It.IsAny<CancellationToken>()))
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
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetDesignationEntity, int>(1, It.IsAny<CancellationToken>()))
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

        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetDesignationEntity, int>(1, It.IsAny<CancellationToken>()))
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
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetDesignationEntity, int>(1, cancellationToken))
            .ReturnsAsync(true);

        await controller.Purge(1, cancellationToken);

        cleanup.Verify(c => c.ForceHardDeleteAsync<AssetDesignationEntity, int>(1, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Purge_WhenUnexpectedExceptionOccurs_Returns500()
    {
        var controller = Create(out _, out var cleanup, out _);
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetDesignationEntity, int>(1, It.IsAny<CancellationToken>()))
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
        cleanup.Setup(c => c.ForceHardDeleteAsync<AssetDesignationEntity, int>(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var softDeleteResult = await controller.Delete(1, CancellationToken.None);
        var purgeResult = await controller.Purge(2, CancellationToken.None);

        var softOk = Assert.IsType<OkObjectResult>(softDeleteResult);
        var purgeOk = Assert.IsType<OkObjectResult>(purgeResult);

        var softResponse = Assert.IsType<ApiResponse<AssetDesignationDto>>(softOk.Value);
        var purgeResponse = Assert.IsType<ApiResponse<object>>(purgeOk.Value);

        Assert.NotEqual(softResponse.Message, purgeResponse.Message);
        Assert.Contains("marked", softResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permanently", purgeResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
