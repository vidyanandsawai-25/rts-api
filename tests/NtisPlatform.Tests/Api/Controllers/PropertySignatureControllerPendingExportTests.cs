using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertySignature;
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

    [Fact]
    public async Task GetPendingSigns_ReturnsRowsInsideItemsOnlyResponse()
    {
        var query = new PropertySignaturePendingSignsQueryParameters
        {
            UserId = 5,
            SearchTerm = "WE020004"
        };
        var page = new PropertySignaturePagedResultDto<PropertySignaturePendingSignDto>
        {
            Items = new List<PropertySignaturePendingSignDto>
            {
                new()
                {
                    PropertyId = 100,
                    SignAuthorityId = 2,
                    StructureName = "WE2-4",
                    SrNoticeNo = "WE0200040000",
                    NoOfUnits = 6,
                    Demand = 424000m,
                    SignStatus = "Pending",
                    AuthorityCode = "TI"
                }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        var service = new Mock<IPropertySignatureService>();
        service
            .Setup(x => x.GetPendingSignsAsync(
                It.Is<PropertySignaturePendingSignsQueryParameters>(q =>
                    q.UserId == 5
                    && q.SearchTerm == "WE020004"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);
        var controller = CreateController(service);

        var result = await controller.GetPendingSigns(query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PropertySignatureItemsResponse<IReadOnlyList<PropertySignaturePagedResultDto<PropertySignaturePendingSignDto>>>>(ok.Value);
        var item = Assert.Single(response.Items!);
        Assert.Equal(page, item);
    }

    [Fact]
    public async Task UpdatePropertySign_ReturnsUpdatedResultInsideItemsOnlyResponse()
    {
        var request = new PropertySignatureUpdateSignRequestDto
        {
            UserId = 5,
            SignAuthorityId = 1,
            PropertyId = 100,
            AuthorityCode = "CLERK",
            SignStatus = "PendingToClerk"
        };
        var updateResult = new PropertySignatureUpdateSignResponseDto
        {
            PropertyId = 100,
            UpdatedSignAuthorityId = 1,
            UpdatedSignStatus = "ApprovedByClerk",
            NextSignAuthorityId = 2,
            NextSignStatus = "PendingToTI",
            Message = "Signature updated successfully and sent to the next authority."
        };
        var service = new Mock<IPropertySignatureService>();
        service
            .Setup(x => x.UpdateSignAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updateResult);
        var controller = CreateController(service);

        var result = await controller.UpdatePropertySign(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PropertySignatureItemsResponse<IReadOnlyList<PropertySignatureUpdateSignResponseDto>>>(ok.Value);
        var item = Assert.Single(response.Items!);
        Assert.Equal(updateResult, item);
    }
}
