using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculationDetail;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RetrospectiveTaxCalculationDetailControllerTests
{
    private readonly Mock<IRetrospectiveTaxCalculationDetailService> _mockService;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly Mock<ILogger<RetrospectiveTaxCalculationDetailController>> _mockLogger;
    private readonly RetrospectiveTaxCalculationDetailController _controller;

    public RetrospectiveTaxCalculationDetailControllerTests()
    {
        _mockService = new Mock<IRetrospectiveTaxCalculationDetailService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
        _mockLogger = new Mock<ILogger<RetrospectiveTaxCalculationDetailController>>();

        _controller = new RetrospectiveTaxCalculationDetailController(
            _mockService.Object,
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_CallsService_AndReturnsOkObjectResult()
    {
        var query = new RetrospectiveTaxCalculationDetailQueryParameters();
        var pagedResult = new PagedResult<RetrospectiveTaxCalculationDetailDto>(new List<RetrospectiveTaxCalculationDetailDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_CallsService_AndReturnsOkObjectResult()
    {
        var dto = new RetrospectiveTaxCalculationDetailDto { Id = 1, CalculationId = 10, FinancialYear = "2024-25" };
        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_CallsService_AndReturnsOkObjectResult()
    {
        var createDto = new CreateRetrospectiveTaxCalculationDetailDto
        {
            CalculationId = 10,
            PropertyId = 100,
            FloorId = 1,
            FinancialYear = "2024-25",
            FromDate = DateTime.Now,
            ToDate = DateTime.Now
        };
        var resultDto = new RetrospectiveTaxCalculationDetailDto { Id = 1, CalculationId = 10, FinancialYear = "2024-25" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_CallsService_AndReturnsOkObjectResult()
    {
        var updateDto = new UpdateRetrospectiveTaxCalculationDetailDto
        {
            CalculationId = 10,
            PropertyId = 100,
            FloorId = 1,
            FinancialYear = "2025-26",
            FromDate = DateTime.Now,
            ToDate = DateTime.Now
        };
        var resultDto = new RetrospectiveTaxCalculationDetailDto { Id = 1, CalculationId = 10, FinancialYear = "2025-26" };

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
            new CreateRetrospectiveTaxCalculationDetailDto { CalculationId = 10, PropertyId = 100, FloorId = 1, FinancialYear = "2024-25", FromDate = DateTime.Now, ToDate = DateTime.Now },
            new CreateRetrospectiveTaxCalculationDetailDto { CalculationId = 10, PropertyId = 100, FloorId = 2, FinancialYear = "2024-25", FromDate = DateTime.Now, ToDate = DateTime.Now }
        };

        var bulkResult = new BulkResult<RetrospectiveTaxCalculationDetailDto>(2, 0, new List<RetrospectiveTaxCalculationDetailDto>
        {
            new() { Id = 1, CalculationId = 10, FinancialYear = "2024-25" },
            new() { Id = 2, CalculationId = 10, FinancialYear = "2024-25" }
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
            new BulkUpdateItem<int, UpdateRetrospectiveTaxCalculationDetailDto>(1, new UpdateRetrospectiveTaxCalculationDetailDto
            {
                CalculationId = 10, PropertyId = 100, FloorId = 1, FinancialYear = "2024-25", FromDate = DateTime.Now, ToDate = DateTime.Now
            })
        };

        var bulkResult = new BulkResult<RetrospectiveTaxCalculationDetailDto>(1, 0, new List<RetrospectiveTaxCalculationDetailDto> { new() { Id = 1, CalculationId = 10 } });

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
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveTaxCalculationDetailEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RetrospectiveTaxCalculationDetailEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.ForceHardDeleteAsync<RetrospectiveTaxCalculationDetailEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_CallsBulkForceDelete_AndReturnsOkObjectResult()
    {
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(2, 0, new List<int> { 1, 2 });

        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveTaxCalculationDetailEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(s => s.BulkForceHardDeleteAsync<RetrospectiveTaxCalculationDetailEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.BulkPurge(ids, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _mockCleanupService.Verify(s => s.BulkForceHardDeleteAsync<RetrospectiveTaxCalculationDetailEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }
}
