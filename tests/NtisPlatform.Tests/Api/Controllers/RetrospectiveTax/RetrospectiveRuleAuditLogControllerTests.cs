using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAuditLog;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RetrospectiveRuleAuditLogControllerTests
{
    private readonly Mock<IRetrospectiveRuleAuditLogService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<RetrospectiveRuleAuditLogController>> _mockLogger;
    private readonly RetrospectiveRuleAuditLogController _controller;

    public RetrospectiveRuleAuditLogControllerTests()
    {
        _mockService = new Mock<IRetrospectiveRuleAuditLogService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<RetrospectiveRuleAuditLogController>>();

        _controller = new RetrospectiveRuleAuditLogController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        var query = new RetrospectiveRuleAuditLogQueryParameters();
        var pagedResult = new PagedResult<RetrospectiveRuleAuditLogDto>(new List<RetrospectiveRuleAuditLogDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_CallsService_AndReturnsOkObjectResult()
    {
        var dto = new RetrospectiveRuleAuditLogDto { Id = 1, RuleId = 5, ActionType = "CREATE" };
        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CallsService_AndReturnsOkObjectResult()
    {
        var createDto = new CreateRetrospectiveRuleAuditLogDto { RuleId = 5, ActionType = "CREATE" };
        var resultDto = new RetrospectiveRuleAuditLogDto { Id = 1, RuleId = 5, ActionType = "CREATE" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService_AndReturnsOkObjectResult()
    {
        var updateDto = new UpdateRetrospectiveRuleAuditLogDto { RuleId = 5, ActionType = "UPDATE" };
        var resultDto = new RetrospectiveRuleAuditLogDto { Id = 1, RuleId = 5, ActionType = "UPDATE" };

        _mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_CallsService_AndReturnsOkObjectResult()
    {
        _mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreate_CallsService_AndReturnsOkObjectResult()
    {
        var items = new[]
        {
            new CreateRetrospectiveRuleAuditLogDto { RuleId = 5, ActionType = "CREATE" },
            new CreateRetrospectiveRuleAuditLogDto { RuleId = 5, ActionType = "PUBLISH" }
        };

        var bulkResult = new BulkResult<RetrospectiveRuleAuditLogDto>(2, 0, new List<RetrospectiveRuleAuditLogDto>
        {
            new() { Id = 1, RuleId = 5, ActionType = "CREATE" },
            new() { Id = 2, RuleId = 5, ActionType = "PUBLISH" }
        });

        _mockService.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkCreate(items, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdate_CallsService_AndReturnsOkObjectResult()
    {
        var items = new[]
        {
            new BulkUpdateItem<int, UpdateRetrospectiveRuleAuditLogDto>(1, new UpdateRetrospectiveRuleAuditLogDto { RuleId = 5, ActionType = "UPDATE" })
        };

        var bulkResult = new BulkResult<RetrospectiveRuleAuditLogDto>(1, 0, new List<RetrospectiveRuleAuditLogDto> { new() { Id = 1, RuleId = 5, ActionType = "UPDATE" } });

        _mockService.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkUpdate(items, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDelete_CallsService_AndReturnsOkObjectResult()
    {
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockService.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkDelete(ids, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Purge_CallsForceDelete_AndReturnsOkObjectResult()
    {
        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleAuditLogEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RetrospectiveRuleAuditLogEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<RetrospectiveRuleAuditLogEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleAuditLogEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleAuditLogEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleAuditLogEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
