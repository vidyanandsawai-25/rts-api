using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Comprehensive tests for ActiveTaxesController to achieve 100% line coverage
/// </summary>
public class ActiveTaxesControllerTests
{
    private readonly Mock<IActiveTaxesService> _serviceMock;
    private readonly Mock<ILogger<ActiveTaxesController>> _loggerMock;
    private readonly ActiveTaxesController _controller;

    public ActiveTaxesControllerTests()
    {
        _serviceMock = new Mock<IActiveTaxesService>();
        _loggerMock = new Mock<ILogger<ActiveTaxesController>>();
        _controller = new ActiveTaxesController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        // Arrange
        var queryParams = new ActiveTaxesQueryParameters();
        var pagedResult = new PagedResult<ActiveTaxesDto>
        {
            Items = new List<ActiveTaxesDto> { new ActiveTaxesDto { Id = 1 } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<ActiveTaxesQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var dto = new ActiveTaxesDto { Id = 1 };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesDto?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreateActiveTaxesDto();
        var createdDto = new ActiveTaxesDto { Id = 1 };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateActiveTaxesDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdateActiveTaxesDto();
        var updatedDto = new ActiveTaxesDto { Id = 1 };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateActiveTaxesDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsOkResultWithNull()
    {
        // Arrange
        var updateDto = new UpdateActiveTaxesDto();
        _serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateActiveTaxesDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveTaxesDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsOkResult()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsOkResultWithFalse()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
