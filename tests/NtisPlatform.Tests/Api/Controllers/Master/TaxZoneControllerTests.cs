using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class TaxZoneControllerTests
{
    private readonly Mock<ITaxZoneService> _serviceMock;
    private readonly Mock<IHardDeleteCleanupService> _cleanupServiceMock;
    private readonly Mock<ILogger<TaxZoneController>> _loggerMock;
    private readonly TaxZoneController _controller;

    public TaxZoneControllerTests()
    {
        _serviceMock = new Mock<ITaxZoneService>();
        _cleanupServiceMock = new Mock<IHardDeleteCleanupService>();
        _loggerMock = new Mock<ILogger<TaxZoneController>>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _controller = new TaxZoneController(_serviceMock.Object, _cleanupServiceMock.Object, mockReferenceValidationService.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        var queryParams = new TaxZoneQueryParameters();
        var pagedResult = new PagedResult<TaxZoneDto>
        {
            Items = new List<TaxZoneDto> { new TaxZoneDto { Id = 1 } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<TaxZoneQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        var dto = new TaxZoneDto { Id = 1 };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoneDto?)null);

        var result = await _controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkResult()
    {
        var createDto = new CreateTaxZoneDto();
        var createdDto = new TaxZoneDto { Id = 1 };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateTaxZoneDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsOkResult()
    {
        var updateDto = new UpdateTaxZoneDto();
        var updatedDto = new TaxZoneDto { Id = 1 };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateTaxZoneDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsOk()
    {
        var updateDto = new UpdateTaxZoneDto();
        _serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateTaxZoneDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxZoneDto?)null);

        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsOk()
    {
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsOk()
    {
        _serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Delete(999, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithValidId_ReturnsOk()
    {
        _cleanupServiceMock.Setup(s => s.ForceHardDeleteAsync<TaxZoneEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithInvalidId_ReturnsOk()
    {
        _cleanupServiceMock.Setup(s => s.ForceHardDeleteAsync<TaxZoneEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Purge(999, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
