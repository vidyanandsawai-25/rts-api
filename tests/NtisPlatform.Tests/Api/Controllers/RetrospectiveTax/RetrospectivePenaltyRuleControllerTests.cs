using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectivePenaltyRule;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RetrospectivePenaltyRuleControllerTests
{
    private readonly Mock<IRetrospectivePenaltyRuleService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<RetrospectivePenaltyRuleController>> _mockLogger;
    private readonly RetrospectivePenaltyRuleController _controller;

    public RetrospectivePenaltyRuleControllerTests()
    {
        _mockService = new Mock<IRetrospectivePenaltyRuleService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<RetrospectivePenaltyRuleController>>();

        _controller = new RetrospectivePenaltyRuleController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var query = new RetrospectivePenaltyRuleQueryParameters();
        var pagedResult = new PagedResult<RetrospectivePenaltyRuleDto>(new List<RetrospectivePenaltyRuleDto>(), 0, 1, 10);

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
        var dto = new RetrospectivePenaltyRuleDto { Id = 1, RuleId = 10, PenaltyMode = "ACT_UNLAWFUL" };
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
        var createDto = new CreateRetrospectivePenaltyRuleDto { RuleId = 10, PenaltyMode = "ACT_UNLAWFUL" };
        var resultDto = new RetrospectivePenaltyRuleDto { Id = 1, RuleId = 10, PenaltyMode = "ACT_UNLAWFUL" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreate_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var items = new[]
        {
            new CreateRetrospectivePenaltyRuleDto { RuleId = 10, PenaltyMode = "ACT_UNLAWFUL" },
            new CreateRetrospectivePenaltyRuleDto { RuleId = 11, PenaltyMode = "NONE" }
        };

        var bulkResult = new BulkResult<RetrospectivePenaltyRuleDto>(2, 0, new List<RetrospectivePenaltyRuleDto>
        {
            new() { Id = 1, RuleId = 10, PenaltyMode = "ACT_UNLAWFUL" },
            new() { Id = 2, RuleId = 11, PenaltyMode = "NONE" }
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
        var updateDto = new UpdateRetrospectivePenaltyRuleDto { RuleId = 10, PenaltyMode = "NONE" };
        var resultDto = new RetrospectivePenaltyRuleDto { Id = 1, RuleId = 10, PenaltyMode = "NONE" };

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
            new BulkUpdateItem<int, UpdateRetrospectivePenaltyRuleDto>(
                1,
                new UpdateRetrospectivePenaltyRuleDto { RuleId = 10, PenaltyMode = "NONE" })
        };

        var bulkResult = new BulkResult<RetrospectivePenaltyRuleDto>(1, 0, new List<RetrospectivePenaltyRuleDto>
        {
            new() { Id = 1, RuleId = 10, PenaltyMode = "NONE" }
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
    public void GetPenaltyModes_ReturnsOkObjectResult()
    {
        // Act
        var result = _controller.GetPenaltyModes();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetPenaltyDateSourceTypes_ReturnsOkObjectResult()
    {
        // Act
        var result = _controller.GetPenaltyDateSourceTypes();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetPenaltyDateConditions_ReturnsOkObjectResult()
    {
        // Act
        var result = _controller.GetPenaltyDateConditions();

        // Assert
        Assert.IsType<OkObjectResult>(result);
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
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectivePenaltyRuleEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RetrospectivePenaltyRuleEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Purge(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<RetrospectivePenaltyRuleEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        // Arrange
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectivePenaltyRuleEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<RetrospectivePenaltyRuleEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<RetrospectivePenaltyRuleEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
