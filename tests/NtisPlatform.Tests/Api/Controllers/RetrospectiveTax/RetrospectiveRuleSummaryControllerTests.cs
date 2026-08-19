using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleSummary;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RetrospectiveRuleSummaryControllerTests
{
    private readonly Mock<IRetrospectiveRuleSummaryService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<RetrospectiveRuleSummaryController>> _mockLogger;
    private readonly RetrospectiveRuleSummaryController _controller;

    public RetrospectiveRuleSummaryControllerTests()
    {
        _mockService = new Mock<IRetrospectiveRuleSummaryService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<RetrospectiveRuleSummaryController>>();

        _controller = new RetrospectiveRuleSummaryController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        var query = new RetrospectiveRuleSummaryQueryParameters();
        var pagedResult = new PagedResult<RetrospectiveRuleSummaryDto>(new List<RetrospectiveRuleSummaryDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetForRule_Found_ReturnsOkObjectResult()
    {
        var viewDto = new RetrospectiveRuleSummaryViewDto
        {
            RuleId = 5,
            RuleCode = "THA-01",
            WhenSummary = "OC available",
            TaxSummary = "Tax x 1",
            PenaltySummary = "Do not apply penalty"
        };

        _mockService.Setup(s => s.GetForRuleAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(viewDto);

        var result = await _controller.GetForRule(5, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetForRuleAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetForRule_NotFound_ReturnsNotFoundObjectResult()
    {
        _mockService.Setup(s => s.GetForRuleAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleSummaryViewDto?)null);

        var result = await _controller.GetForRule(999, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        _mockService.Verify(s => s.GetForRuleAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_CallsService_AndReturnsOkObjectResult()
    {
        var dto = new RetrospectiveRuleSummaryDto { Id = 1, RuleId = 5, WhenSummary = "OC available" };
        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CallsService_AndReturnsOkObjectResult()
    {
        var createDto = new CreateRetrospectiveRuleSummaryDto { RuleId = 5, WhenSummary = "OC available", IsActive = true };
        var resultDto = new RetrospectiveRuleSummaryDto { Id = 1, RuleId = 5, WhenSummary = "OC available" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService_AndReturnsOkObjectResult()
    {
        var updateDto = new UpdateRetrospectiveRuleSummaryDto { RuleId = 5, WhenSummary = "Updated summary", IsActive = true };
        var resultDto = new RetrospectiveRuleSummaryDto { Id = 1, RuleId = 5, WhenSummary = "Updated summary" };

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
            new CreateRetrospectiveRuleSummaryDto { RuleId = 5, WhenSummary = "A" },
            new CreateRetrospectiveRuleSummaryDto { RuleId = 6, WhenSummary = "B" }
        };

        var bulkResult = new BulkResult<RetrospectiveRuleSummaryDto>(2, 0, new List<RetrospectiveRuleSummaryDto>
        {
            new() { Id = 1, RuleId = 5, WhenSummary = "A" },
            new() { Id = 2, RuleId = 6, WhenSummary = "B" }
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
            new BulkUpdateItem<int, UpdateRetrospectiveRuleSummaryDto>(1, new UpdateRetrospectiveRuleSummaryDto { RuleId = 5, WhenSummary = "A", IsActive = true })
        };

        var bulkResult = new BulkResult<RetrospectiveRuleSummaryDto>(1, 0, new List<RetrospectiveRuleSummaryDto> { new() { Id = 1, RuleId = 5, WhenSummary = "A" } });

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
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleSummaryEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RetrospectiveRuleSummaryEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<RetrospectiveRuleSummaryEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleSummaryEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleSummaryEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleSummaryEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
