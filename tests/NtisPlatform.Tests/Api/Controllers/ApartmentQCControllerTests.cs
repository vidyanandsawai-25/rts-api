using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class ApartmentQCControllerTests
{
    private readonly Mock<IApartmentQCService> _mockService;
    private readonly Mock<ICapitalValueService> _mockCapitalValueService;
    private readonly Mock<IRateableValueService> _mockRateableValueService;
    private readonly Mock<ILogger<ApartmentQCController>> _mockLogger;
    private readonly ApartmentQCController _controller;

    public ApartmentQCControllerTests()
    {
        _mockService = new Mock<IApartmentQCService>();
        _mockCapitalValueService = new Mock<ICapitalValueService>();
        _mockRateableValueService = new Mock<IRateableValueService>();
        _mockLogger  = new Mock<ILogger<ApartmentQCController>>();
        _controller  = new ApartmentQCController(_mockService.Object, _mockRateableValueService.Object, _mockCapitalValueService.Object, _mockLogger.Object);
    }

    private void SetAuthenticatedUser(int userId = 42)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static PagedResult<PropertyApartmentTaxDto> EmptyPaged() =>
        new([], 0, 1, 10);

    private static PagedResult<PropertyApartmentTaxDto> SinglePaged() =>
        new([new PropertyApartmentTaxDto { Id = 1, PropertyNo = "P001" }], 1, 1, 10);

    #region GetAll

    [Fact]
    public async Task GetAll_WithResults_ReturnsOkWithFoundMessage()
    {
        _mockService
            .Setup(s => s.GetPagedAsync(It.IsAny<ApartmentQCQueryParameters>(), default))
            .ReturnsAsync(SinglePaged());

        var result = await _controller.GetAll(new ApartmentQCQueryParameters(), default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertyApartmentTaxDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Record found successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items.TotalCount);
    }

    [Fact]
    public async Task GetAll_NoResults_ReturnsOkWithNoRecordsMessage()
    {
        _mockService
            .Setup(s => s.GetPagedAsync(It.IsAny<ApartmentQCQueryParameters>(), default))
            .ReturnsAsync(EmptyPaged());

        var result = await _controller.GetAll(new ApartmentQCQueryParameters(), default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertyApartmentTaxDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("No records found", response.Message);
    }

    [Fact]
    public async Task GetAll_PassesQueryParametersToService()
    {
        var query = new ApartmentQCQueryParameters { WardId = 5, PropertyNo = "P100" };
        _mockService
            .Setup(s => s.GetPagedAsync(It.IsAny<ApartmentQCQueryParameters>(), default))
            .ReturnsAsync(EmptyPaged());

        await _controller.GetAll(query, default);

        _mockService.Verify(
            s => s.GetPagedAsync(
                It.Is<ApartmentQCQueryParameters>(q => q.WardId == 5 && q.PropertyNo == "P100"),
                default),
            Times.Once);
    }

    #endregion

    #region ExportExcel

    [Fact]
    public async Task ExportExcel_NullSection_DefaultsToDualAndReturnsFile()
    {
        SetAuthenticatedUser();
        var bytes = new byte[] { 1, 2, 3 };
        _mockService
            .Setup(s => s.ExportToExcelAsync(It.IsAny<ApartmentQCQueryParameters>(), ApartmentQCResultType.Dual, default))
            .ReturnsAsync(bytes);

        var result = await _controller.ExportExcel(new ApartmentQCQueryParameters(), null, default);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        Assert.Equal(bytes, file.FileContents);
    }

    [Fact]
    public async Task ExportExcel_ValidSectionRateable_CallsServiceWithCorrectResultType()
    {
        SetAuthenticatedUser();
        var bytes = new byte[] { 4, 5, 6 };
        _mockService
            .Setup(s => s.ExportToExcelAsync(It.IsAny<ApartmentQCQueryParameters>(), ApartmentQCResultType.Rateable, default))
            .ReturnsAsync(bytes);

        var result = await _controller.ExportExcel(new ApartmentQCQueryParameters(), "Rateable", default);

        Assert.IsType<FileContentResult>(result);
        _mockService.Verify(
            s => s.ExportToExcelAsync(It.IsAny<ApartmentQCQueryParameters>(), ApartmentQCResultType.Rateable, default),
            Times.Once);
    }

    [Fact]
    public async Task ExportExcel_ValidSectionCapital_ReturnsFile()
    {
        SetAuthenticatedUser();
        _mockService
            .Setup(s => s.ExportToExcelAsync(It.IsAny<ApartmentQCQueryParameters>(), ApartmentQCResultType.Capital, default))
            .ReturnsAsync(new byte[] { 7 });

        var result = await _controller.ExportExcel(new ApartmentQCQueryParameters(), "Capital", default);

        Assert.IsType<FileContentResult>(result);
    }

    [Fact]
    public async Task ExportExcel_InvalidSection_ReturnsBadRequest()
    {
        SetAuthenticatedUser();

        var result = await _controller.ExportExcel(new ApartmentQCQueryParameters(), "InvalidSection", default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("InvalidSection", response.Message);
        _mockService.Verify(
            s => s.ExportToExcelAsync(It.IsAny<ApartmentQCQueryParameters>(), It.IsAny<ApartmentQCResultType>(), default),
            Times.Never);
    }

    #endregion

    #region GetByProperty

    [Fact]
    public async Task GetByProperty_NoType_DefaultsToDualAndReturnsOk()
    {
        _mockService
            .Setup(s => s.GetByPropertyDetailAsync(1, ApartmentQCResultType.Dual, default))
            .ReturnsAsync(SinglePaged());

        var result = await _controller.GetByProperty(1, null, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertyApartmentTaxDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Record found successfully", response.Message);
    }

    [Fact]
    public async Task GetByProperty_ValidType_CaseInsensitive_ReturnsOk()
    {
        _mockService
            .Setup(s => s.GetByPropertyDetailAsync(2, ApartmentQCResultType.Rateable, default))
            .ReturnsAsync(SinglePaged());

        var result = await _controller.GetByProperty(2, "rateable", default);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.IsType<ApiResponse<PagedResult<PropertyApartmentTaxDto>>>(ok.Value);
    }

    [Fact]
    public async Task GetByProperty_InvalidType_ReturnsBadRequest()
    {
        var result = await _controller.GetByProperty(1, "Unknown", default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("Unknown", response.Message);
        _mockService.Verify(
            s => s.GetByPropertyDetailAsync(It.IsAny<int>(), It.IsAny<ApartmentQCResultType>(), default),
            Times.Never);
    }

    [Fact]
    public async Task GetByProperty_NoDetailsFound_ReturnsOkWithNoRecordsMessage()
    {
        _mockService
            .Setup(s => s.GetByPropertyDetailAsync(99, ApartmentQCResultType.Dual, default))
            .ReturnsAsync(EmptyPaged());

        var result = await _controller.GetByProperty(99, null, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<PropertyApartmentTaxDto>>>(ok.Value);
        Assert.Equal("No records found", response.Message);
    }

    #endregion

    #region GetFilterOptions

    [Fact]
    public async Task GetFilterOptions_MissingWardId_ReturnsBadRequest()
    {
        var query = new ApartmentQCQueryParameters { PropertyNo = "P001" };

        var result = await _controller.GetFilterOptions(query, null, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("WardId", response.Message);
    }

    [Fact]
    public async Task GetFilterOptions_MissingPropertyNo_ReturnsBadRequest()
    {
        var query = new ApartmentQCQueryParameters { WardId = 1 };

        var result = await _controller.GetFilterOptions(query, null, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetFilterOptions_BothMissing_ReturnsBadRequest()
    {
        var result = await _controller.GetFilterOptions(new ApartmentQCQueryParameters(), null, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetFilterOptions_InvalidField_ReturnsBadRequest()
    {
        var query = new ApartmentQCQueryParameters { WardId = 1, PropertyNo = "P001" };

        var result = await _controller.GetFilterOptions(query, "InvalidField", default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("InvalidField", response.Message);
    }

    [Fact]
    public async Task GetFilterOptions_ValidParams_ReturnsOkWithOptions()
    {
        var query = new ApartmentQCQueryParameters { WardId = 1, PropertyNo = "P001" };
        var options = new ApartmentQCFilterOptionsDto
        {
            Wings = ["A", "B"],
            ApartmentTypes = ["2BHK"],
            FlatOrShopNos = ["101"],
            PropertyTypes = [1, 2]
        };

        _mockService
            .Setup(s => s.GetFilterOptionsAsync(It.IsAny<ApartmentQCQueryParameters>(), null, default))
            .ReturnsAsync(options);

        var result = await _controller.GetFilterOptions(query, null, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ApartmentQCFilterOptionsDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(2, response.Items.Wings.Count);
    }

    [Fact]
    public async Task GetFilterOptions_ValidField_PassesFieldToService()
    {
        var query = new ApartmentQCQueryParameters { WardId = 1, PropertyNo = "P001" };
        _mockService
            .Setup(s => s.GetFilterOptionsAsync(It.IsAny<ApartmentQCQueryParameters>(), "Wing", default))
            .ReturnsAsync(new ApartmentQCFilterOptionsDto());

        var result = await _controller.GetFilterOptions(query, "Wing", default);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(
            s => s.GetFilterOptionsAsync(It.IsAny<ApartmentQCQueryParameters>(), "Wing", default),
            Times.Once);
    }

    #endregion

    #region GetOldPropertyData

    [Fact]
    public async Task GetOldPropertyData_NullOldPropertyNo_ReturnsBadRequest()
    {
        var result = await _controller.GetOldPropertyData(null, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task GetOldPropertyData_EmptyOldPropertyNo_ReturnsBadRequest()
    {
        var result = await _controller.GetOldPropertyData("   ", default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetOldPropertyData_NotFound_Returns404()
    {
        _mockService
            .Setup(s => s.GetOldPropertyDataAsync("OLD-999", default))
            .ReturnsAsync((OldPropertyLookupDto?)null);

        var result = await _controller.GetOldPropertyData("OLD-999", default);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
        Assert.Contains("OLD-999", response.Message);
    }

    [Fact]
    public async Task GetOldPropertyData_Found_ReturnsOkWithDto()
    {
        var dto = new OldPropertyLookupDto
        {
            OldPropertyNo = "OLD-001",
            OldRV = 5000m,
            OldTotalTax = 300m
        };

        _mockService
            .Setup(s => s.GetOldPropertyDataAsync("OLD-001", default))
            .ReturnsAsync(dto);

        var result = await _controller.GetOldPropertyData("OLD-001", default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<OldPropertyLookupDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal("OLD-001", response.Items.OldPropertyNo);
        Assert.Equal(5000m, response.Items.OldRV);
    }

    #endregion

    #region UpdateDetail

    [Fact]
    public async Task UpdateDetail_NullBody_ReturnsBadRequest()
    {
        SetAuthenticatedUser();

        var result = await _controller.UpdateDetail(1, null!, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task UpdateDetail_EmptyList_ReturnsBadRequest()
    {
        SetAuthenticatedUser();

        var result = await _controller.UpdateDetail(1, [], default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
        _mockService.Verify(
            s => s.UpdateDetailAsync(It.IsAny<int>(), It.IsAny<List<UpdateApartmentQCDetailsDto>>(), It.IsAny<int>(), default),
            Times.Never);
    }

    [Fact]
    public async Task UpdateDetail_PropertyNotFound_Returns404()
    {
        SetAuthenticatedUser();
        _mockService
            .Setup(s => s.UpdateDetailAsync(99, It.IsAny<List<UpdateApartmentQCDetailsDto>>(), 42, default))
            .ReturnsAsync((ApartmentQCBulkUpdateResultDto?)null);

        var dtos = new List<UpdateApartmentQCDetailsDto> { new() { DetailId = 1, FloorId = 2 } };
        var result = await _controller.UpdateDetail(99, dtos, default);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
        Assert.Contains("99", response.Message);
    }

    [Fact]
    public async Task UpdateDetail_ValidationFailures_ReturnsBadRequestWithFailures()
    {
        SetAuthenticatedUser();
        var updateResult = new ApartmentQCBulkUpdateResultDto
        {
            TotalRequested = 1,
            Updated = 0,
            Failures = [new ApartmentQCBulkUpdateFailureDto { DetailId = 5, Reason = "Invalid FloorId" }]
        };

        _mockService
            .Setup(s => s.UpdateDetailAsync(1, It.IsAny<List<UpdateApartmentQCDetailsDto>>(), 42, default))
            .ReturnsAsync(updateResult);

        var dtos = new List<UpdateApartmentQCDetailsDto> { new() { DetailId = 5, FloorId = 999 } };
        var result = await _controller.UpdateDetail(1, dtos, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ApartmentQCBulkUpdateResultDto>>(bad.Value);
        Assert.False(response.Success);
        Assert.NotNull(response.Items);
        Assert.Single(response.Items.Failures);
        Assert.NotNull(response.Errors);
        Assert.Contains("DetailId 5", response.Errors[0]);
    }

    [Fact]
    public async Task UpdateDetail_Success_ReturnsOkWithUpdatedCount()
    {
        SetAuthenticatedUser();
        var updateResult = new ApartmentQCBulkUpdateResultDto
        {
            TotalRequested = 2,
            Updated = 2,
            UpdatedDetailIds = [10, 11],
            Failures = []
        };

        _mockService
            .Setup(s => s.UpdateDetailAsync(1, It.IsAny<List<UpdateApartmentQCDetailsDto>>(), 42, default))
            .ReturnsAsync(updateResult);

        var dtos = new List<UpdateApartmentQCDetailsDto>
        {
            new() { DetailId = 10, FloorId = 1 },
            new() { DetailId = 11, FloorId = 2 }
        };
        var result = await _controller.UpdateDetail(1, dtos, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<ApartmentQCBulkUpdateResultDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Contains("2", response.Message);
        Assert.Equal(2, response.Items!.Updated);
    }

    [Fact]
    public async Task UpdateDetail_CallsServiceWithExtractedUserId()
    {
        SetAuthenticatedUser(userId: 7);
        _mockService
            .Setup(s => s.UpdateDetailAsync(1, It.IsAny<List<UpdateApartmentQCDetailsDto>>(), 7, default))
            .ReturnsAsync(new ApartmentQCBulkUpdateResultDto { TotalRequested = 1, Updated = 1, Failures = [] });

        await _controller.UpdateDetail(1, [new() { DetailId = 1, FloorId = 1 }], default);

        _mockService.Verify(
            s => s.UpdateDetailAsync(1, It.IsAny<List<UpdateApartmentQCDetailsDto>>(), 7, default),
            Times.Once);
    }

    #endregion

    #region UpdateBasicDetails

    [Fact]
    public async Task UpdateBasicDetails_NullBody_ReturnsBadRequest()
    {
        SetAuthenticatedUser();

        var result = await _controller.UpdateBasicDetails(1, null!, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task UpdateBasicDetails_PropertyNotFound_Returns404()
    {
        SetAuthenticatedUser();
        _mockService
            .Setup(s => s.UpdateBasicDetailsAsync(99, It.IsAny<UpdateApartmentQCBasicDetailsDto>(), 42, default))
            .ReturnsAsync(BasicDetailsPatchOutcome.PropertyNotFound);

        var result = await _controller.UpdateBasicDetails(99, new UpdateApartmentQCBasicDetailsDto(), default);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
        Assert.Contains("99", response.Message);
    }

    [Fact]
    public async Task UpdateBasicDetails_OldPropertyNoNotFound_Returns400()
    {
        SetAuthenticatedUser();
        _mockService
            .Setup(s => s.UpdateBasicDetailsAsync(1, It.IsAny<UpdateApartmentQCBasicDetailsDto>(), 42, default))
            .ReturnsAsync(BasicDetailsPatchOutcome.OldPropertyNoNotFound);

        var dto = new UpdateApartmentQCBasicDetailsDto { OldPropertyNo = "INVALID" };
        var result = await _controller.UpdateBasicDetails(1, dto, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
        Assert.Contains("INVALID", response.Message);
    }

    [Fact]
    public async Task UpdateBasicDetails_Success_ReturnsOk()
    {
        SetAuthenticatedUser();
        _mockService
            .Setup(s => s.UpdateBasicDetailsAsync(1, It.IsAny<UpdateApartmentQCBasicDetailsDto>(), 42, default))
            .ReturnsAsync(BasicDetailsPatchOutcome.Success);

        var dto = new UpdateApartmentQCBasicDetailsDto { OwnerName = "John Doe", Wing = "A" };
        var result = await _controller.UpdateBasicDetails(1, dto, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.True(response.Success);
        Assert.Contains("updated successfully", response.Message);
    }

    [Fact]
    public async Task UpdateBasicDetails_CallsServiceWithExtractedUserId()
    {
        SetAuthenticatedUser(userId: 15);
        _mockService
            .Setup(s => s.UpdateBasicDetailsAsync(1, It.IsAny<UpdateApartmentQCBasicDetailsDto>(), 15, default))
            .ReturnsAsync(BasicDetailsPatchOutcome.Success);

        await _controller.UpdateBasicDetails(1, new UpdateApartmentQCBasicDetailsDto(), default);

        _mockService.Verify(
            s => s.UpdateBasicDetailsAsync(1, It.IsAny<UpdateApartmentQCBasicDetailsDto>(), 15, default),
            Times.Once);
    }

    #endregion

    #region SyncRoomAggregates

    [Fact]
    public async Task SyncRoomAggregates_Success_ReturnsOk()
    {
        SetAuthenticatedUser();
        _mockService
            .Setup(s => s.SyncRoomAggregatesAsync(42, 42, default))
            .ReturnsAsync(true);

        var result = await _controller.SyncRoomAggregates(5, 42, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task SyncRoomAggregates_PropertyDetailsNotFound_ReturnsNotFound()
    {
        SetAuthenticatedUser();
        _mockService
            .Setup(s => s.SyncRoomAggregatesAsync(99, 42, default))
            .ReturnsAsync(false);

        var result = await _controller.SyncRoomAggregates(1, 99, default);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
    }

    #endregion
}
