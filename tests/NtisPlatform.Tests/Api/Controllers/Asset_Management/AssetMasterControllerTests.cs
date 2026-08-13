using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

/// <summary>
/// Tests for AssetMasterController covering the standard CRUD endpoints (delegated through
/// CrudControllerExtensions) plus the 5 hand-written custom endpoints: ExportExcel,
/// GetFloorAndOtherDetails, GetSubAssetsGroupedByParent, ActivateAsset, BulkSaveFieldValues.
/// </summary>
public class AssetMasterControllerTests
{
    private static AssetMasterController Create(
        out Mock<IAssetMasterService> service,
        out Mock<ILogger<AssetMasterController>> logger)
    {
        service = new Mock<IAssetMasterService>();
        logger = new Mock<ILogger<AssetMasterController>>();
        return new AssetMasterController(logger.Object, service.Object);
    }

    #region Constructor / Route / Attribute Contract Tests

    [Fact]
    public void Controller_HasExpectedRoutePrefix()
    {
        var attribute = typeof(AssetMasterController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .FirstOrDefault() as RouteAttribute;

        Assert.NotNull(attribute);
        Assert.Equal("api/[controller]", attribute!.Template);
    }

    [Fact]
    public void Controller_RequiresAuthorizationForAllEndpoints()
    {
        var attributes = typeof(AssetMasterController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), false);

        Assert.NotEmpty(attributes);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_DelegatesToService_ReturnsPagedResult()
    {
        var controller = Create(out var service, out _);
        var query = new AssetMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var data = new List<AssetMasterDto>
        {
            new() { Id = 1, AssetName = "Asset A" },
            new() { Id = 2, AssetName = "Asset B" }
        };
        var pagedResult = new PagedResult<AssetMasterDto>(data, 2, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await controller.GetAll(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<PagedResult<AssetMasterDto>>(okResult.Value);
        Assert.Equal(2, returned.TotalCount);
        Assert.Equal(2, returned.Items.Count());
        service.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenFilterValidationExceptionThrown_Returns400()
    {
        var controller = Create(out var service, out _);
        var query = new AssetMasterQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("SortBy", "Unknown sort field"));

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenFound_Returns200WithDto()
    {
        var controller = Create(out var service, out _);
        var dto = new AssetMasterDto { Id = 1, AssetName = "Asset A" };
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<AssetMasterDto>(okResult.Value);
        Assert.Equal(1, returned.Id);
        Assert.Equal("Asset A", returned.AssetName);
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetMasterDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_Returns200WithApiResponse()
    {
        var controller = Create(out var service, out _);
        var createDto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Asset A",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };
        var createdDto = new AssetMasterDto { Id = 1, AssetName = "Asset A" };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(createdDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.Id);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsDuplicateMessage_Returns409()
    {
        var controller = Create(out var service, out _);
        var createDto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Asset A",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Violation of UNIQUE KEY constraint - duplicate AssetNo"));

        var result = await controller.Create(createDto, CancellationToken.None);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetMasterDto>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    /// <summary>
    /// Sensitive-data sanity check: this endpoint accepts multipart file uploads, so the response
    /// DTO (and any nested photo DTOs) must never surface server-side storage internals like
    /// physical/temp file paths, nor the raw uploaded IFormFile collection.
    /// </summary>
    [Fact]
    public async Task Create_ResponseDoesNotExposeInternalFields()
    {
        var controller = Create(out var service, out _);
        var createDto = new CreateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Asset A",
            AssetCategoryId = 1,
            AssetTypeId = 1,
            PhotoFiles = new List<IFormFile>(),
            PhotoMetadataJson = "[]"
        };
        var createdDto = new AssetMasterDto
        {
            Id = 1,
            AssetName = "Asset A",
            Photos = new List<AssetPhotoDto>
            {
                new() { PhotoId = 1, FileName = "front.jpg", MimeType = "image/jpeg" }
            }
        };

        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(createdDto);

        var result = await controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetMasterDto>>(okResult.Value);

        var forbiddenPropertyNames = new[]
        {
            "FilePath", "PhysicalPath", "TempFilePath", "StoragePath", "ServerPath", "FullPath", "PhotoFiles"
        };

        var photoDtoProps = typeof(AssetPhotoDto).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain(photoDtoProps, name => forbiddenPropertyNames.Contains(name, StringComparer.OrdinalIgnoreCase));

        var assetDtoProps = typeof(AssetMasterDto).GetProperties();
        Assert.DoesNotContain(assetDtoProps, p =>
            forbiddenPropertyNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase) ||
            p.PropertyType == typeof(List<IFormFile>));

        Assert.NotNull(response.Items);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDto_Returns200()
    {
        var controller = Create(out var service, out _);
        var updateDto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Updated Asset",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };
        var updatedDto = new AssetMasterDto { Id = 1, AssetName = "Updated Asset" };

        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(updatedDto);

        var result = await controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Updated Asset", response.Items!.AssetName);
    }

    /// <summary>
    /// Per CrudControllerExtensions.ExecuteUpdate, "not found" is 200 OK with Success = false,
    /// NOT a 404 - do not assume 404 here, that would be the wrong (but tempting) assumption.
    /// </summary>
    [Fact]
    public async Task Update_WhenNotFound_Returns200WithSuccessFalse()
    {
        var controller = Create(out var service, out _);
        var updateDto = new UpdateAssetMasterDto
        {
            OrganizationId = 1,
            AssetName = "Updated Asset",
            AssetCategoryId = 1,
            AssetTypeId = 1
        };
        service.Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetMasterDto?)null);

        var result = await controller.Update(999, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetMasterDto>>(okResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Delete Tests

    /// <summary>
    /// Soft delete: per CrudControllerExtensions.ExecuteDelete, a successful delete is 200 OK
    /// with a "marked for deletion" message - not a 204/no content.
    /// </summary>
    [Fact]
    public async Task Delete_MarksForDeletion_Returns200()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetMasterDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("marked for deletion", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ExportExcel Tests

    [Fact]
    public async Task ExportExcel_ReturnsFileContentResult_WithXlsxContentTypeAndFilename()
    {
        var controller = Create(out var service, out _);
        var query = new AssetMasterQueryParameters();
        var bytes = new byte[] { 1, 2, 3, 4 };

        service.Setup(s => s.ExportToExcelAsync(query, It.IsAny<CancellationToken>())).ReturnsAsync(bytes);

        var result = await controller.ExportExcel(query, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
        Assert.Matches(new Regex(@"^AssetMaster_\d{14}\.xlsx$"), fileResult.FileDownloadName);
    }

    #endregion

    #region GetFloorAndOtherDetails Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetFloorAndOtherDetails_WithParentAssetIdLessThanOrEqualToZero_Returns400(int parentAssetId)
    {
        var controller = Create(out var service, out _);

        var result = await controller.GetFloorAndOtherDetails(parentAssetId, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetFloorAndOtherDetailsResponseDto>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(
            s => s.GetAssetFloorAndOtherDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFloorAndOtherDetails_WhenAssetNull_Returns404()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.GetAssetFloorAndOtherDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetFloorAndOtherDetailsResponseDto?)null);

        var result = await controller.GetFloorAndOtherDetails(1, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetFloorAndOtherDetails_WhenFound_ReturnsApiResponseWrappedDto()
    {
        var controller = Create(out var service, out _);
        var dto = new AssetFloorAndOtherDetailsResponseDto();
        service.Setup(s => s.GetAssetFloorAndOtherDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await controller.GetFloorAndOtherDetails(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<AssetFloorAndOtherDetailsResponseDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Same(dto, response.Items);
    }

    #endregion

    #region GetSubAssetsGroupedByParent Tests

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetSubAssetsGroupedByParent_WithParentAssetIdLessThanOrEqualToZero_Returns400(int parentAssetId)
    {
        var controller = Create(out var service, out _);

        var result = await controller.GetSubAssetsGroupedByParent(parentAssetId, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<SubAssetGroupedResponseDto>>(badRequest.Value);
        Assert.False(response.Success);
        service.Verify(
            s => s.GetSubAssetsGroupedByParentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Documents a deliberate asymmetry (see CLAUDE.md / roadmap Section B item 4): unlike
    /// GetFloorAndOtherDetails, this endpoint's success path returns the raw
    /// SubAssetGroupedResponseDto via Ok(result), NOT wrapped in ApiResponse&lt;T&gt;. Only the
    /// error (400) path is wrapped. Do not "fix" this test to assume symmetry with its sibling.
    /// </summary>
    [Fact]
    public async Task GetSubAssetsGroupedByParent_ReturnsRawDtoOnSuccess_NotApiResponseWrapped()
    {
        var controller = Create(out var service, out _);
        var dto = new SubAssetGroupedResponseDto { TotalSubAssets = 2 };
        service.Setup(s => s.GetSubAssetsGroupedByParentAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        var result = await controller.GetSubAssetsGroupedByParent(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<SubAssetGroupedResponseDto>(okResult.Value);
        Assert.Same(dto, returned);
        Assert.IsNotType<ApiResponse<SubAssetGroupedResponseDto>>(okResult.Value);
    }

    #endregion

    #region ActivateAsset Tests

    [Fact]
    public async Task ActivateAsset_WhenServiceReturnsTrue_Returns200()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.ActivateAssetAndFieldValuesAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.ActivateAsset(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Items);
    }

    [Fact]
    public async Task ActivateAsset_WhenServiceReturnsFalse_Returns404()
    {
        var controller = Create(out var service, out _);
        service.Setup(s => s.ActivateAssetAndFieldValuesAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.ActivateAsset(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region BulkSaveFieldValues Tests

    [Fact]
    public async Task BulkSaveFieldValues_WhenServiceReturnsTrue_Returns200()
    {
        var controller = Create(out var service, out _);
        var fieldValues = new List<CreateAssetFieldValueDto>
        {
            new() { AssetId = 1, FieldName = "Color", FieldValue = "Red" }
        };
        service.Setup(s => s.BulkSaveFieldValuesAsync(1, fieldValues, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.BulkSaveFieldValues(1, fieldValues, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
        Assert.True(response.Items);
    }

    [Fact]
    public async Task BulkSaveFieldValues_WhenServiceReturnsFalse_Returns500()
    {
        var controller = Create(out var service, out _);
        var fieldValues = new List<CreateAssetFieldValueDto>
        {
            new() { AssetId = 1, FieldName = "Color", FieldValue = "Red" }
        };
        service.Setup(s => s.BulkSaveFieldValuesAsync(1, fieldValues, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.BulkSaveFieldValues(1, fieldValues, CancellationToken.None);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusResult.StatusCode);
        var response = Assert.IsType<ApiResponse<bool>>(statusResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task BulkSaveFieldValues_WithEmptyList_StillDelegatesToService()
    {
        var controller = Create(out var service, out _);
        var emptyList = new List<CreateAssetFieldValueDto>();
        service.Setup(s => s.BulkSaveFieldValuesAsync(1, emptyList, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.BulkSaveFieldValues(1, emptyList, CancellationToken.None);

        service.Verify(s => s.BulkSaveFieldValuesAsync(1, emptyList, It.IsAny<CancellationToken>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<bool>>(okResult.Value);
        Assert.True(response.Success);
    }

    #endregion
}
