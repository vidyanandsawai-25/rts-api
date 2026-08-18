using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class OldWardMasterControllerTests
{
    private readonly Mock<IOldWardMasterService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<OldWardMasterController>> _mockLogger;
    private readonly OldWardMasterController _controller;

    public OldWardMasterControllerTests()
    {
        _mockService = new Mock<IOldWardMasterService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<OldWardMasterController>>();

        _controller = new OldWardMasterController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var query = new OldWardMasterQueryParameters();
        var pagedResult = new PagedResult<OldWardMasterDto>(new List<OldWardMasterDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(query, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var dto = new OldWardMasterDto { Id = 1, OldZoneName = "MM", OldWardNo = "MM11" };
        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var createDto = new CreateOldWardMasterDto { OldZoneName = "MM", OldWardNo = "MM11" };
        var resultDto = new OldWardMasterDto { Id = 1, OldZoneName = "MM", OldWardNo = "MM11" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var updateDto = new UpdateOldWardMasterDto { OldZoneName = "MM", OldWardNo = "MM11_Updated" };
        var resultDto = new OldWardMasterDto { Id = 1, OldZoneName = "MM", OldWardNo = "MM11_Updated" };

        _mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreate_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var items = new[]
        {
            new CreateOldWardMasterDto { OldZoneName = "MM", OldWardNo = "MM11" },
            new CreateOldWardMasterDto { OldZoneName = "MM", OldWardNo = "MM12" }
        };

        var bulkResult = new BulkResult<OldWardMasterDto>(2, 0, new List<OldWardMasterDto>
        {
            new() { Id = 1, OldZoneName = "MM", OldWardNo = "MM11" },
            new() { Id = 2, OldZoneName = "MM", OldWardNo = "MM12" }
        });

        _mockService.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Purge_CallsForceDelete_AndReturnsOkObjectResult()
    {
        // Arrange
        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<OldWardMasterEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<OldWardMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Purge(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<OldWardMasterEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        // Arrange
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<OldWardMasterEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<OldWardMasterEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<OldWardMasterEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
