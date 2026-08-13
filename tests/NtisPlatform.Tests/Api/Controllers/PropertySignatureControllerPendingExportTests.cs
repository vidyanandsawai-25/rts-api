using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertySignatureControllerPendingExportTests
{
    private static PropertySignatureController CreateController(Mock<IPropertySignatureService> service)
    {
        var logger = Mock.Of<ILogger<PropertySignatureController>>();
        return new PropertySignatureController(service.Object, logger);
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
        var response = Assert.IsType<PropertySignatureItemsResponse<IReadOnlyList<PropertySignaturePendingExportDto>>>(ok.Value);
        Assert.Equal(rows, response.Items);
    }

    [Fact]
    public async Task GetAuthorities_ReturnsRowsInsideItemsOnlyResponse()
    {
        var authorities = new List<SignAuthorityDto>
        {
            new() { Id = 1, AuthorityName = "Clerk", AuthorityCode = "CLERK", SequenceOrder = 1 }
        };
        var service = new Mock<IPropertySignatureService>();
        service
            .Setup(x => x.GetAuthoritiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorities);
        var controller = CreateController(service);

        var result = await controller.GetAuthorities(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PropertySignatureItemsResponse<IReadOnlyList<SignAuthorityDto>>>(ok.Value);
        Assert.Equal(authorities, response.Items);
    }

    [Fact]
    public async Task GetSignAuthorityGrid_ReturnsPayloadInsideItemsArray()
    {
        var grid = new SignAuthorityGridResponseDto
        {
            TotalRow = new SignAuthorityZoneDataDto { ZoneName = "TOTAL" }
        };
        var service = new Mock<IPropertySignatureService>();
        service
            .Setup(x => x.GetSignAuthorityGridDataAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grid);
        var controller = CreateController(service);

        var result = await controller.GetSignAuthorityGrid(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PropertySignatureItemsResponse<IReadOnlyList<SignAuthorityGridResponseDto>>>(ok.Value);
        var item = Assert.Single(response.Items!);
        Assert.Equal("TOTAL", item.TotalRow.ZoneName);
    }
}
