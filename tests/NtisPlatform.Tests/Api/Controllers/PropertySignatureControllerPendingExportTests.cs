using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertySignatureControllerPendingExportTests
{
    private static PropertySignatureController CreateController(Mock<IPropertySignatureService> service)
    {
        var logger = Mock.Of<ILogger<PropertySignatureController>>();
        var fileValidationHelper = new FileValidationHelper(new ConfigurationBuilder().Build());
        return new PropertySignatureController(logger, service.Object, fileValidationHelper);
    }

    [Fact]
    public async Task GetPendingExportData_WithInvalidSignAuthorityId_ReturnsBadRequest()
    {
        var service = new Mock<IPropertySignatureService>();
        var controller = CreateController(service);

        var result = await controller.GetPendingExportData(0, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("SignAuthorityId parameter is required.", response.Message);
        service.Verify(
            x => x.GetPendingExportDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPendingExportData_WithValidSignAuthorityId_ReturnsRows()
    {
        var rows = new List<PropertySignaturePendingExportDto>
        {
            new()
            {
                Zone = "MM",
                BuildingNo = "MM8-216",
                SrNoticeNo = "TMC2025MM080216RPNI01",
                PendingSignAt = "Tax Inspector (Level 2)",
                PendingOfficerName = "Motilal Kuwar"
            }
        };
        var service = new Mock<IPropertySignatureService>();
        service
            .Setup(x => x.GetPendingExportDataAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);
        var controller = CreateController(service);

        var result = await controller.GetPendingExportData(2, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<List<PropertySignaturePendingExportDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal(rows, response.Items);
        Assert.Equal("1 pending signature record(s) found.", response.Message);
    }
}
