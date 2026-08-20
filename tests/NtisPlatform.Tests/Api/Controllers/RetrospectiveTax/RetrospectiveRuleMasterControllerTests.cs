using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RetrospectiveRuleMasterControllerTests
{
    private readonly Mock<IRetrospectiveRuleMasterService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<RetrospectiveRuleMasterController>> _mockLogger;
    private readonly RetrospectiveRuleMasterController _controller;

    public RetrospectiveRuleMasterControllerTests()
    {
        _mockService = new Mock<IRetrospectiveRuleMasterService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<RetrospectiveRuleMasterController>>();

        _controller = new RetrospectiveRuleMasterController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        var query = new RetrospectiveRuleMasterQueryParameters();
        var pagedResult = new PagedResult<RetrospectiveRuleMasterDto>(new List<RetrospectiveRuleMasterDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_CallsService_AndReturnsOkObjectResult()
    {
        var dto = new RetrospectiveRuleMasterDto { Id = 1, RuleCode = "THA-01", RuleName = "Rule One" };
        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDetail_Found_ReturnsOkObjectResult()
    {
        var detail = new RetrospectiveRuleDetailDto { Rule = new RetrospectiveRuleMasterDto { Id = 1, RuleCode = "THA-01" } };
        _mockService.Setup(s => s.GetDetailAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await _controller.GetDetail(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetDetailAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDetail_NotFound_ReturnsNotFoundObjectResult()
    {
        _mockService.Setup(s => s.GetDetailAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleDetailDto?)null);

        var result = await _controller.GetDetail(999, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        _mockService.Verify(s => s.GetDetailAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_Found_ReturnsOkObjectResult()
    {
        var request = new PublishRetrospectiveRuleDto { PublishedBy = 7 };
        var resultDto = new RetrospectiveRuleMasterDto { Id = 1, RuleCode = "THA-01", RuleStatus = "Active" };

        _mockService.Setup(s => s.PublishAsync(1, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _controller.Publish(1, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.PublishAsync(1, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_NotFound_ReturnsNotFoundObjectResult()
    {
        var request = new PublishRetrospectiveRuleDto { PublishedBy = 7 };

        _mockService.Setup(s => s.PublishAsync(999, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleMasterDto?)null);

        var result = await _controller.Publish(999, request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        _mockService.Verify(s => s.PublishAsync(999, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_Found_ReturnsOkObjectResult()
    {
        var request = new SaveRetrospectiveRuleDto
        {
            RuleCode = "THA-05",
            RuleName = "New Rule",
            Action = new SaveRetrospectiveRuleActionDto { TaxStartMode = "EVIDENCE_DATE", RetrospectiveLimitType = "NONE" }
        };
        var detail = new RetrospectiveRuleDetailDto { Rule = new RetrospectiveRuleMasterDto { Id = 5, RuleCode = "THA-05" } };

        _mockService.Setup(s => s.SaveAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await _controller.Save(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.SaveAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_NotFound_ReturnsNotFoundObjectResult()
    {
        var request = new SaveRetrospectiveRuleDto
        {
            Id = 999,
            RuleCode = "THA-05",
            RuleName = "New Rule",
            Action = new SaveRetrospectiveRuleActionDto { TaxStartMode = "EVIDENCE_DATE", RetrospectiveLimitType = "NONE" }
        };

        _mockService.Setup(s => s.SaveAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleDetailDto?)null);

        var result = await _controller.Save(request, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        _mockService.Verify(s => s.SaveAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CallsService_AndReturnsOkObjectResult()
    {
        var createDto = new CreateRetrospectiveRuleMasterDto { RuleCode = "THA-03", RuleName = "Rule Three" };
        var resultDto = new RetrospectiveRuleMasterDto { Id = 1, RuleCode = "THA-03" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromRange_CallsService_AndReturnsOkObjectResult()
    {
        var request = new RangeCreateRequest<CreateRetrospectiveRuleMasterDto>
        {
            RangeFrom = "1",
            RangeTo = "2",
            Template = new CreateRetrospectiveRuleMasterDto { RuleName = "Generated" }
        };

        var rangeResult = new RangeResult<RetrospectiveRuleMasterDto>(
            2, 0, new List<RetrospectiveRuleMasterDto> { new() { Id = 1, RuleCode = "1" }, new() { Id = 2, RuleCode = "2" } });

        // The controller's Range endpoint dynamically dispatches to the service's narrow 2-arg
        // CreateFromRangeAsync(request, cancellationToken) overload (see
        // IRetrospectiveRuleMasterService), not the base 3-arg overload.
        _mockService.Setup(s => s.CreateFromRangeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        var result = await _controller.CreateFromRange(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateFromRangeAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService_AndReturnsOkObjectResult()
    {
        var updateDto = new UpdateRetrospectiveRuleMasterDto { RuleCode = "THA-01", RuleName = "Updated Name" };
        var resultDto = new RetrospectiveRuleMasterDto { Id = 1, RuleName = "Updated Name" };

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
            new CreateRetrospectiveRuleMasterDto { RuleCode = "R1", RuleName = "R1" },
            new CreateRetrospectiveRuleMasterDto { RuleCode = "R2", RuleName = "R2" }
        };

        var bulkResult = new BulkResult<RetrospectiveRuleMasterDto>(2, 0, new List<RetrospectiveRuleMasterDto>
        {
            new() { Id = 1, RuleCode = "R1" },
            new() { Id = 2, RuleCode = "R2" }
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
            new BulkUpdateItem<int, UpdateRetrospectiveRuleMasterDto>(1, new UpdateRetrospectiveRuleMasterDto { RuleCode = "R1", RuleName = "R1" })
        };

        var bulkResult = new BulkResult<RetrospectiveRuleMasterDto>(1, 0, new List<RetrospectiveRuleMasterDto> { new() { Id = 1, RuleCode = "R1" } });

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
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleMasterEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RetrospectiveRuleMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<RetrospectiveRuleMasterEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleMasterEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleMasterEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleMasterEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
