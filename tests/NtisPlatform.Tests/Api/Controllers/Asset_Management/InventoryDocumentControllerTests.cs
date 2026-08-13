using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

public class InventoryDocumentControllerTests
{
    private static InventoryDocumentController Create(
        out Mock<IInventoryDocumentApplicationService> service,
        int? userId = 42)
    {
        service = new Mock<IInventoryDocumentApplicationService>();
        var logger = new Mock<ILogger<InventoryDocumentController>>();

        var controller = new InventoryDocumentController(
            service.Object,
            logger.Object);

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            }, "TestAuth"));
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    #region BulkSave

    [Fact]
    public async Task BulkSave_ReturnsBadRequest_WhenBatchIdInvalid()
    {
        var controller = Create(out _);
        var request = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 0,
            Documents = new List<InventoryDocumentItemDto> { new() { DocumentTypeId = 1 } }
        };

        var result = await controller.BulkSave(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("Invalid InventoryBatchId", response.Message);
    }

    [Fact]
    public async Task BulkSave_ReturnsBadRequest_WhenDocumentsListNullOrEmpty()
    {
        var controller = Create(out _);
        var request = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto>()
        };

        var result = await controller.BulkSave(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("Documents list cannot be empty", response.Message);
    }

    [Fact]
    public async Task BulkSave_ReturnsUnauthorized_WhenUserClaimMissing()
    {
        var controller = Create(out _, userId: null);
        var request = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto> { new() { DocumentTypeId = 1 } }
        };

        var result = await controller.BulkSave(request, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task BulkSave_ReturnsOk_WhenSuccessfulWithoutErrors()
    {
        var controller = Create(out var service, userId: 42);
        var request = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto> { new() { DocumentTypeId = 1, IsEnabled = true } }
        };

        service.Setup(s => s.BulkSaveAsync(request, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryDocumentBulkSaveResponseDto
            {
                InventoryBatchId = 10,
                EnabledCount = 1,
                DisabledCount = 0,
                TotalProcessed = 1,
                Errors = new List<string>()
            });

        var result = await controller.BulkSave(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryDocumentBulkSaveResponseDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("All inventory document slots saved", response.Message);
    }

    [Fact]
    public async Task BulkSave_ReturnsOkWithHasErrorsMessage_WhenPartialErrorsExist()
    {
        var controller = Create(out var service, userId: 42);
        var request = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto> { new() { DocumentTypeId = 1 } }
        };

        service.Setup(s => s.BulkSaveAsync(request, 42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryDocumentBulkSaveResponseDto
            {
                InventoryBatchId = 10,
                EnabledCount = 1,
                DisabledCount = 0,
                TotalProcessed = 1,
                Errors = new List<string> { "Error in doc type 1" }
            });

        var result = await controller.BulkSave(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<InventoryDocumentBulkSaveResponseDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Saved with 1 error(s)", response.Message);
        Assert.Single(response.Errors!);
    }

    [Fact]
    public async Task BulkSave_Returns500_OnUnhandledException()
    {
        var controller = Create(out var service);
        var request = new InventoryDocumentBulkSaveDto
        {
            InventoryBatchId = 10,
            Documents = new List<InventoryDocumentItemDto> { new() { DocumentTypeId = 1 } }
        };

        service.Setup(s => s.BulkSaveAsync(It.IsAny<InventoryDocumentBulkSaveDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fatal database error"));

        var result = await controller.BulkSave(request, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region GetDocumentsByInventoryItem

    [Fact]
    public async Task GetDocumentsByInventoryItem_ReturnsBadRequest_WhenBatchIdInvalid()
    {
        var controller = Create(out _);

        var result = await controller.GetDocumentsByInventoryItem(0, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("Invalid inventoryBatchId", response.Message);
    }

    [Fact]
    public async Task GetDocumentsByInventoryItem_ReturnsOk_WhenBatchIdValid()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetDocumentsByInventoryBatchAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryDocumentDto>
            {
                new() { InventoryDocumentId = 1, InventoryBatchId = 10, DocumentTypeId = 2 }
            });

        var result = await controller.GetDocumentsByInventoryItem(10, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<InventoryDocumentDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Single(response.Items!);
    }

    [Fact]
    public async Task GetDocumentsByInventoryItem_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetDocumentsByInventoryBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fatal exception"));

        var result = await controller.GetDocumentsByInventoryItem(10, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.False(response.Success);
    }

    #endregion
}
