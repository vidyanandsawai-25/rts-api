using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class WaterConnectionDetailsControllerTests
{
    [Fact]
    public void WaterConnectionDetailsController_Constructor_InitializesCorrectly()
    {
        // Arrange
        var serviceMock = new Mock<IWaterConnectionDetailsService>();
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        var referenceValidationMock = new Mock<IReferenceValidationService>();
        var loggerMock = new Mock<ILogger<WaterConnectionDetailsController>>();

        // Act
        var controller = new WaterConnectionDetailsController(serviceMock.Object, cleanupMock.Object, referenceValidationMock.Object, loggerMock.Object);

        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public async Task GenerateBill_ReturnsOk_WhenBillGeneratedSuccessfully()
    {
        // Arrange
        var serviceMock = new Mock<IWaterConnectionDetailsService>();
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        var referenceValidationMock = new Mock<IReferenceValidationService>();
        var loggerMock = new Mock<ILogger<WaterConnectionDetailsController>>();

        var dto = new WaterConnectionDetailsDto { Id = 1 };
        serviceMock.Setup(x => x.GenerateBillAsync(1, 2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new WaterConnectionDetailsController(serviceMock.Object, cleanupMock.Object, referenceValidationMock.Object, loggerMock.Object);
        var request = new GenerateBillRequest { WaterConnectionId = 1, FinanceYearId = 2024 };

        // Act
        var result = await controller.GenerateBill(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<WaterConnectionDetailsDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Bill generated successfully", response.Message);
    }

    [Fact]
    public async Task GenerateBill_ReturnsNoContent_WhenResultIsNull()
    {
        // Arrange
        var serviceMock = new Mock<IWaterConnectionDetailsService>();
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        var referenceValidationMock = new Mock<IReferenceValidationService>();
        var loggerMock = new Mock<ILogger<WaterConnectionDetailsController>>();

        serviceMock.Setup(x => x.GenerateBillAsync(1, 2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WaterConnectionDetailsDto?)null);

        var controller = new WaterConnectionDetailsController(serviceMock.Object, cleanupMock.Object, referenceValidationMock.Object, loggerMock.Object);
        var request = new GenerateBillRequest { WaterConnectionId = 1, FinanceYearId = 2024 };

        // Act
        var result = await controller.GenerateBill(request, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GenerateBill_ReturnsBadRequest_OnInvalidOperationException()
    {
        // Arrange
        var serviceMock = new Mock<IWaterConnectionDetailsService>();
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        var loggerMock = new Mock<ILogger<WaterConnectionDetailsController>>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();

        serviceMock.Setup(x => x.GenerateBillAsync(1, 2024, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection is inactive"));

        var controller = new WaterConnectionDetailsController(serviceMock.Object, cleanupMock.Object, mockReferenceValidationService.Object, loggerMock.Object);
        var request = new GenerateBillRequest { WaterConnectionId = 1, FinanceYearId = 2024 };

        // Act
        var result = await controller.GenerateBill(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<WaterConnectionDetailsDto>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Connection is inactive", response.Message);
    }

    [Fact]
    public async Task GenerateBill_ReturnsInternalServerError_OnException()
    {
        // Arrange
        var serviceMock = new Mock<IWaterConnectionDetailsService>();
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        var loggerMock = new Mock<ILogger<WaterConnectionDetailsController>>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();

        serviceMock.Setup(x => x.GenerateBillAsync(1, 2024, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        var controller = new WaterConnectionDetailsController(serviceMock.Object, cleanupMock.Object, mockReferenceValidationService.Object, loggerMock.Object);
        var request = new GenerateBillRequest { WaterConnectionId = 1, FinanceYearId = 2024 };

        // Act
        var result = await controller.GenerateBill(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<WaterConnectionDetailsDto>>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred while generating the bill.", response.Message);
    }

    [Fact]
    public async Task GenerateBill_CallsServiceWithCorrectParameters()
    {
        // Arrange
        var serviceMock = new Mock<IWaterConnectionDetailsService>();
        var cleanupMock = new Mock<IHardDeleteCleanupService>();
        var loggerMock = new Mock<ILogger<WaterConnectionDetailsController>>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        var dto = new WaterConnectionDetailsDto { Id = 1 };
        serviceMock.Setup(x => x.GenerateBillAsync(123, 2024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new WaterConnectionDetailsController(serviceMock.Object, cleanupMock.Object, mockReferenceValidationService.Object, loggerMock.Object);
        var request = new GenerateBillRequest { WaterConnectionId = 123, FinanceYearId = 2024 };

        // Act
        await controller.GenerateBill(request, CancellationToken.None);

        // Assert
        serviceMock.Verify(x => x.GenerateBillAsync(123, 2024, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class GenerateBillRequestValidationTests
{
    [Fact]
    public void GenerateBillRequest_PropertiesWork()
    {
        // Arrange & Act
        var request = new GenerateBillRequest
        {
            WaterConnectionId = 123,
            FinanceYearId = 2024
        };

        // Assert
        Assert.Equal(123, request.WaterConnectionId);
        Assert.Equal(2024, request.FinanceYearId);
    }

    [Fact]
    public void GenerateBillRequest_DefaultValues()
    {
        // Arrange & Act
        var request = new GenerateBillRequest();

        // Assert
        Assert.Equal(0, request.WaterConnectionId);
        Assert.Equal(0, request.FinanceYearId);
    }

    [Fact]
    public void GenerateBillRequest_CanSetPositiveValues()
    {
        // Arrange & Act
        var request = new GenerateBillRequest
        {
            WaterConnectionId = int.MaxValue,
            FinanceYearId = int.MaxValue
        };

        // Assert
        Assert.Equal(int.MaxValue, request.WaterConnectionId);
        Assert.Equal(int.MaxValue, request.FinanceYearId);
    }
}
