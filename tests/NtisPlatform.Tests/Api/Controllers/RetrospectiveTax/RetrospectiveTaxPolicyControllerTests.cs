using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxPolicy;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RetrospectiveTaxPolicyControllerTests
{
    private readonly Mock<IRetrospectiveTaxPolicyService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<RetrospectiveTaxPolicyController>> _mockLogger;
    private readonly RetrospectiveTaxPolicyController _controller;

    public RetrospectiveTaxPolicyControllerTests()
    {
        _mockService = new Mock<IRetrospectiveTaxPolicyService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<RetrospectiveTaxPolicyController>>();

        _controller = new RetrospectiveTaxPolicyController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        var query = new RetrospectiveTaxPolicyQueryParameters();
        var pagedResult = new PagedResult<RetrospectiveTaxPolicyDto>(new List<RetrospectiveTaxPolicyDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetRateModes_ReturnsOkObjectResult()
    {
        var result = _controller.GetRateModes();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetPercentageModes_ReturnsOkObjectResult()
    {
        var result = _controller.GetPercentageModes();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_CallsService_AndReturnsOkObjectResult()
    {
        var dto = new RetrospectiveTaxPolicyDto { Id = 1, TaxPolicyCode = "DEFAULT", TaxPolicyName = "Default Policy" };
        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CallsService_AndReturnsOkObjectResult()
    {
        var createDto = new CreateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "DEFAULT",
            TaxPolicyName = "Default Policy",
            RateMode = "CURRENT_YEAR_FOR_ALL_YEARS",
            PercentageMode = "CURRENT_YEAR_FOR_ALL_YEARS"
        };
        var resultDto = new RetrospectiveTaxPolicyDto { Id = 1, TaxPolicyCode = "DEFAULT" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_CallsService_AndReturnsOkObjectResult()
    {
        var request = new SaveRetrospectiveTaxPolicyDto
        {
            RateMode = "CURRENT_YEAR_FOR_ALL_YEARS",
            PercentageMode = "HISTORIC_YEAR_WISE"
        };
        var resultDto = new RetrospectiveTaxPolicyDto { Id = 1, TaxPolicyCode = "DEFAULT", RateMode = "CURRENT_YEAR_FOR_ALL_YEARS" };

        _mockService.Setup(s => s.SaveAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _controller.Save(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.SaveAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromRange_CallsService_AndReturnsOkObjectResult()
    {
        var request = new RangeCreateRequest<CreateRetrospectiveTaxPolicyDto>
        {
            RangeFrom = "1",
            RangeTo = "3",
            Template = new CreateRetrospectiveTaxPolicyDto
            {
                TaxPolicyCode = "POL",
                TaxPolicyName = "Policy",
                RateMode = "CURRENT_YEAR_FOR_ALL_YEARS",
                PercentageMode = "CURRENT_YEAR_FOR_ALL_YEARS"
            }
        };

        var rangeResult = new RangeResult<RetrospectiveTaxPolicyDto>(
            3,
            0,
            new List<RetrospectiveTaxPolicyDto>
            {
                new() { Id = 1, TaxPolicyCode = "POL1" },
                new() { Id = 2, TaxPolicyCode = "POL2" },
                new() { Id = 3, TaxPolicyCode = "POL3" }
            });

        // The controller's Range endpoint dynamically dispatches to the service's narrow 2-arg
        // CreateFromRangeAsync(request, cancellationToken) overload (see
        // IRetrospectiveTaxPolicyService), not the base 3-arg overload.
        _mockService.Setup(s => s.CreateFromRangeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        var result = await _controller.CreateFromRange(request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateFromRangeAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService_AndReturnsOkObjectResult()
    {
        var updateDto = new UpdateRetrospectiveTaxPolicyDto
        {
            TaxPolicyCode = "DEFAULT",
            TaxPolicyName = "Updated Policy",
            RateMode = "HISTORIC_YEAR_WISE",
            PercentageMode = "HISTORIC_YEAR_WISE"
        };
        var resultDto = new RetrospectiveTaxPolicyDto { Id = 1, TaxPolicyName = "Updated Policy" };

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
            new CreateRetrospectiveTaxPolicyDto { TaxPolicyCode = "P1", TaxPolicyName = "P1", RateMode = "CURRENT_YEAR_FOR_ALL_YEARS", PercentageMode = "CURRENT_YEAR_FOR_ALL_YEARS" },
            new CreateRetrospectiveTaxPolicyDto { TaxPolicyCode = "P2", TaxPolicyName = "P2", RateMode = "CURRENT_YEAR_FOR_ALL_YEARS", PercentageMode = "CURRENT_YEAR_FOR_ALL_YEARS" }
        };

        var bulkResult = new BulkResult<RetrospectiveTaxPolicyDto>(2, 0, new List<RetrospectiveTaxPolicyDto>
        {
            new() { Id = 1, TaxPolicyCode = "P1" },
            new() { Id = 2, TaxPolicyCode = "P2" }
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
            new BulkUpdateItem<int, UpdateRetrospectiveTaxPolicyDto>(1, new UpdateRetrospectiveTaxPolicyDto
            {
                TaxPolicyCode = "P1", TaxPolicyName = "P1", RateMode = "CURRENT_YEAR_FOR_ALL_YEARS", PercentageMode = "CURRENT_YEAR_FOR_ALL_YEARS"
            })
        };

        var bulkResult = new BulkResult<RetrospectiveTaxPolicyDto>(1, 0, new List<RetrospectiveTaxPolicyDto> { new() { Id = 1, TaxPolicyCode = "P1" } });

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
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveTaxPolicyEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RetrospectiveTaxPolicyEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<RetrospectiveTaxPolicyEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveTaxPolicyEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<RetrospectiveTaxPolicyEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<RetrospectiveTaxPolicyEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
