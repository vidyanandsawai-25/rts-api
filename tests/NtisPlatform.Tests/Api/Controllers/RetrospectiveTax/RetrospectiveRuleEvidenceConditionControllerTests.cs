using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RetrospectiveRuleEvidenceConditionControllerTests
{
    private readonly Mock<IRetrospectiveRuleEvidenceConditionService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<RetrospectiveRuleEvidenceConditionController>> _mockLogger;
    private readonly RetrospectiveRuleEvidenceConditionController _controller;

    public RetrospectiveRuleEvidenceConditionControllerTests()
    {
        _mockService = new Mock<IRetrospectiveRuleEvidenceConditionService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<RetrospectiveRuleEvidenceConditionController>>();

        _controller = new RetrospectiveRuleEvidenceConditionController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        var query = new RetrospectiveRuleEvidenceConditionQueryParameters();
        var pagedResult = new PagedResult<RetrospectiveRuleEvidenceConditionDto>(new List<RetrospectiveRuleEvidenceConditionDto>(), 0, 1, 10);

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
        var dto = new RetrospectiveRuleEvidenceConditionDto { Id = 1, RuleId = 10, EvidenceTypeId = 1, EvidenceState = "AVAILABLE" };
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
        var createDto = new CreateRetrospectiveRuleEvidenceConditionDto { RuleId = 10, EvidenceTypeId = 1, EvidenceState = "AVAILABLE" };
        var resultDto = new RetrospectiveRuleEvidenceConditionDto { Id = 1, RuleId = 10, EvidenceTypeId = 1, EvidenceState = "AVAILABLE" };

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
            new CreateRetrospectiveRuleEvidenceConditionDto { RuleId = 10, EvidenceTypeId = 1, EvidenceState = "AVAILABLE" },
            new CreateRetrospectiveRuleEvidenceConditionDto { RuleId = 10, EvidenceTypeId = 2, EvidenceState = "UNAVAILABLE" }
        };

        var bulkResult = new BulkResult<RetrospectiveRuleEvidenceConditionDto>(2, 0, new List<RetrospectiveRuleEvidenceConditionDto>
        {
            new() { Id = 1, RuleId = 10, EvidenceTypeId = 1, EvidenceState = "AVAILABLE" },
            new() { Id = 2, RuleId = 10, EvidenceTypeId = 2, EvidenceState = "UNAVAILABLE" }
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
        var updateDto = new UpdateRetrospectiveRuleEvidenceConditionDto { RuleId = 10, EvidenceTypeId = 1, EvidenceState = "UNAVAILABLE" };
        var resultDto = new RetrospectiveRuleEvidenceConditionDto { Id = 1, RuleId = 10, EvidenceTypeId = 1, EvidenceState = "UNAVAILABLE" };

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
            new BulkUpdateItem<int, UpdateRetrospectiveRuleEvidenceConditionDto>(1, new UpdateRetrospectiveRuleEvidenceConditionDto { RuleId = 10, EvidenceTypeId = 1, EvidenceState = "UNAVAILABLE" })
        };

        var bulkResult = new BulkResult<RetrospectiveRuleEvidenceConditionDto>(1, 0, new List<RetrospectiveRuleEvidenceConditionDto>
        {
            new() { Id = 1, RuleId = 10, EvidenceTypeId = 1, EvidenceState = "UNAVAILABLE" }
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
    public async Task GetEvidenceState_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        const int ruleId = 10;
        var state = new List<RetrospectiveRuleEvidenceConditionStateDto>
        {
            new() { EvidenceTypeId = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate", DisplayOrder = 1, SelectedState = "AVAILABLE" },
            new() { EvidenceTypeId = 2, EvidenceCode = "CC", EvidenceName = "Completion Certificate", DisplayOrder = 2, SelectedState = null }
        };

        _mockService.Setup(s => s.GetEvidenceStateForRuleAsync(ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);

        // Act
        var result = await _controller.GetEvidenceState(ruleId, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetEvidenceStateForRuleAsync(ruleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetEvidenceState_CallsService_AndReturnsOkObjectResult()
    {
        // Arrange
        const int ruleId = 10;
        var request = new SetRetrospectiveRuleEvidenceConditionStateDto
        {
            AvailableEvidenceTypeIds = new List<int> { 1 },
            UnavailableEvidenceTypeIds = new List<int> { 2 },
            UpdatedBy = 99
        };

        var state = new List<RetrospectiveRuleEvidenceConditionStateDto>
        {
            new() { EvidenceTypeId = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate", DisplayOrder = 1, SelectedState = "AVAILABLE" },
            new() { EvidenceTypeId = 2, EvidenceCode = "CC", EvidenceName = "Completion Certificate", DisplayOrder = 2, SelectedState = "UNAVAILABLE" }
        };

        _mockService.Setup(s => s.SetEvidenceStateForRuleAsync(ruleId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);

        // Act
        var result = await _controller.SetEvidenceState(ruleId, request, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.SetEvidenceStateForRuleAsync(ruleId, request, It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleEvidenceConditionEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RetrospectiveRuleEvidenceConditionEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Purge(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<RetrospectiveRuleEvidenceConditionEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        // Arrange
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleEvidenceConditionEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleEvidenceConditionEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleEvidenceConditionEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
