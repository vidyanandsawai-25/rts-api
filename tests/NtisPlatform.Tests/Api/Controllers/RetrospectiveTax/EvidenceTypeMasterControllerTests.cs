using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class EvidenceTypeMasterControllerTests
{
    private readonly Mock<IEvidenceTypeMasterService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<EvidenceTypeMasterController>> _mockLogger;
    private readonly EvidenceTypeMasterController _controller;

    public EvidenceTypeMasterControllerTests()
    {
        _mockService = new Mock<IEvidenceTypeMasterService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<EvidenceTypeMasterController>>();

        _controller = new EvidenceTypeMasterController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var query = new EvidenceTypeMasterQueryParameters();
        var pagedResult = new PagedResult<EvidenceTypeMasterDto>(new List<EvidenceTypeMasterDto>(), 0, 1, 10);

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
        var dto = new EvidenceTypeMasterDto { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" };
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
        var createDto = new CreateEvidenceTypeMasterDto { EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" };
        var resultDto = new EvidenceTypeMasterDto { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromRange_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var request = new RangeCreateRequest<CreateEvidenceTypeMasterDto>
        {
            RangeFrom = "1",
            RangeTo = "3",
            Template = new CreateEvidenceTypeMasterDto { EvidenceCode = "EV", EvidenceName = "Evidence" }
        };

        var rangeResult = new RangeResult<EvidenceTypeMasterDto>(
            3,
            0,
            new List<EvidenceTypeMasterDto>
            {
                new() { Id = 1, EvidenceCode = "EV1", EvidenceName = "Evidence1" },
                new() { Id = 2, EvidenceCode = "EV2", EvidenceName = "Evidence2" },
                new() { Id = 3, EvidenceCode = "EV3", EvidenceName = "Evidence3" }
            });

        // The controller's Range endpoint dynamically dispatches to the service's narrow 2-arg
        // CreateFromRangeAsync(request, cancellationToken) overload (see
        // IEvidenceTypeMasterService), not the base 3-arg (request, transformer, cancellationToken)
        // overload — mock that one.
        _mockService.Setup(s => s.CreateFromRangeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        // Act
        var result = await _controller.CreateFromRange(request, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateFromRangeAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreate_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var items = new[]
        {
            new CreateEvidenceTypeMasterDto { EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" },
            new CreateEvidenceTypeMasterDto { EvidenceCode = "CC", EvidenceName = "Completion Certificate" }
        };

        var bulkResult = new BulkResult<EvidenceTypeMasterDto>(2, 0, new List<EvidenceTypeMasterDto>
        {
            new() { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" },
            new() { Id = 2, EvidenceCode = "CC", EvidenceName = "Completion Certificate" }
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
    public async Task Update_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var updateDto = new UpdateEvidenceTypeMasterDto { EvidenceCode = "OC", EvidenceName = "Occupancy Certificate Updated" };
        var resultDto = new EvidenceTypeMasterDto { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate Updated" };

        _mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdate_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var items = new[]
        {
            new BulkUpdateItem<int, UpdateEvidenceTypeMasterDto>(1, new UpdateEvidenceTypeMasterDto { EvidenceCode = "OC", EvidenceName = "Occupancy Certificate Updated" })
        };

        var bulkResult = new BulkResult<EvidenceTypeMasterDto>(1, 0, new List<EvidenceTypeMasterDto>
        {
            new() { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate Updated" }
        });

        _mockService.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkUpdate(items, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task BulkDelete_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockService.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkDelete(ids, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Purge_CallsForceDelete_AndReturnsOkObjectResult()
    {
        // Arrange
        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<EvidenceTypeMasterEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<EvidenceTypeMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Purge(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<EvidenceTypeMasterEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        // Arrange
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<EvidenceTypeMasterEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<EvidenceTypeMasterEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<EvidenceTypeMasterEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
