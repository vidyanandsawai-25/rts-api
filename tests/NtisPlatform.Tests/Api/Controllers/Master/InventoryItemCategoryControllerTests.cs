using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class InventoryItemCategoryControllerTests
{
    private static InventoryItemCategoryController Create(
        out Mock<IInventoryItemCategoryService> service,
        Mock<IHardDeleteCleanupService>? cleanupService = null,
        Mock<IReferenceValidationService>? referenceValidationService = null)
    {
        service = new Mock<IInventoryItemCategoryService>();
        var cleanup = cleanupService ?? new Mock<IHardDeleteCleanupService>();
        var refVal = referenceValidationService ?? new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<InventoryItemCategoryController>>();
        return new InventoryItemCategoryController(logger.Object, service.Object, cleanup.Object, refVal.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var qp = new InventoryItemCategoryQueryParameters();
        service.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<InventoryItemCategoryDto>(new List<InventoryItemCategoryDto>(), 0, 1, 10));

        var result = await controller.GetAll(qp, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemCategoryDto { Id = 1, TypeName = "Electronics" });

        var result = await controller.GetById(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<InventoryItemCategoryDto>(ok.Value);
        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemCategoryDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateAsync(It.IsAny<CreateInventoryItemCategoryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemCategoryDto { Id = 1, AssetCategoryId = 3, TypeCode = "CAT001", TypeName = "Electronics" });

        // AssetCategoryId is a required FK (AMS.InventoryItemCategoryMaster.AssetCategoryId is int
        // NOT NULL, FK_InventoryItemCategoryMaster_AssetCategory -> AMS.AssetCategoryMaster.Id) --
        // omitting it here would no longer represent a realistic/valid request payload.
        var result = await controller.Create(
            new CreateInventoryItemCategoryDto { AssetCategoryId = 3, TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemCategoryDto>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Create_DuplicateName_PropagatesValidationException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.CreateAsync(It.IsAny<CreateInventoryItemCategoryDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NtisPlatform.Application.Exceptions.ValidationException(
                "TypeName", "InventoryItemCategory_TypeName_Duplicate", NtisPlatform.Application.Enums.OperationType.Create));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => controller.Create(
                new CreateInventoryItemCategoryDto { AssetCategoryId = 3, TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1 },
                CancellationToken.None));
    }

    [Fact]
    public async Task Create_MissingAssetCategoryId_FailsDtoValidation()
    {
        // AssetCategoryId defaults to 0, which fails [Range(1, int.MaxValue)] -- guards the exact
        // failure class originally reported: a request that omits a required FK passing straight
        // through to the service/DB instead of being rejected by model binding.
        var dto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1 };

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            dto, new System.ComponentModel.DataAnnotations.ValidationContext(dto), results, true);

        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_AssetCategoryId_InvalidRange");
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateInventoryItemCategoryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryItemCategoryDto { Id = 1, AssetCategoryId = 3, TypeName = "Electronics Updated" });

        var result = await controller.Update(
            1,
            new UpdateInventoryItemCategoryDto { AssetCategoryId = 3, TypeCode = "CAT001", TypeName = "Electronics Updated", DisplayOrder = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemCategoryDto>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Update_NonExisting_ReturnsOkWithFailure()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateInventoryItemCategoryDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItemCategoryDto?)null);

        var result = await controller.Update(
            999,
            new UpdateInventoryItemCategoryDto { AssetCategoryId = 3, TypeCode = "CAT001", TypeName = "Test", DisplayOrder = 1 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemCategoryDto>>(ok.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_MissingAssetCategoryId_FailsDtoValidation()
    {
        var dto = new UpdateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1 };

        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            dto, new System.ComponentModel.DataAnnotations.ValidationContext(dto), results, true);

        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_AssetCategoryId_InvalidRange");
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemCategoryDto>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsOkWithFailure()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(999, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryItemCategoryDto>>(ok.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Purge_WithValidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<InventoryItemCategoryEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithInvalidId_ReturnsOk()
    {
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        cleanupMock.Setup(s => s.ForceHardDeleteAsync<InventoryItemCategoryEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = Create(out _, cleanupMock);

        var result = await controller.Purge(999, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
