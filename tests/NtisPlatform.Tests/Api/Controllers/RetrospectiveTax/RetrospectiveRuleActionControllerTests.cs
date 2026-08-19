using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.RetrospectiveTax;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAction;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.RetrospectiveTax;

public class RetrospectiveRuleActionControllerTests
{
    private static RetrospectiveRuleActionController Create(
        out Mock<IRetrospectiveRuleActionService> service,
        out Mock<IHardDeleteCleanupService> cleanupService,
        out Mock<IReferenceValidationService> referenceValidationService)
    {
        service = new Mock<IRetrospectiveRuleActionService>();
        cleanupService = new Mock<IHardDeleteCleanupService>();
        referenceValidationService = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<RetrospectiveRuleActionController>>();
        return new RetrospectiveRuleActionController(service.Object, cleanupService.Object, referenceValidationService.Object, logger.Object);
    }

    private static RetrospectiveRuleActionController Create(out Mock<IRetrospectiveRuleActionService> service)
    {
        return Create(out service, out _, out _);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new RetrospectiveRuleActionQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<RetrospectiveRuleActionDto>(new List<RetrospectiveRuleActionDto>(), 0, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RetrospectiveRuleActionDto { Id = 1, RuleId = 10 });

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new CreateRetrospectiveRuleActionDto
        {
            RuleId = 10,
            TaxStartMode = "EVIDENCE_DATE",
            RetrospectiveLimitType = "MAXIMUM_YEARS",
            TaxCalculationMode = "SINGLE"
        };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RetrospectiveRuleActionDto { Id = 1, RuleId = 10 });

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkCreate_ReturnsOk()
    {
        var controller = Create(out var service);
        var items = new[]
        {
            new CreateRetrospectiveRuleActionDto
            {
                RuleId = 10,
                TaxStartMode = "EVIDENCE_DATE",
                RetrospectiveLimitType = "MAXIMUM_YEARS",
                TaxCalculationMode = "SINGLE"
            }
        };
        service.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<RetrospectiveRuleActionDto>(1, 0, new List<RetrospectiveRuleActionDto>()));

        var result = await controller.BulkCreate(items, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new UpdateRetrospectiveRuleActionDto
        {
            RuleId = 10,
            TaxStartMode = "FIXED_CUTOFF",
            RetrospectiveLimitType = "FIXED_CUTOFF_DATE",
            TaxCalculationMode = "SINGLE"
        };
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RetrospectiveRuleActionDto { Id = 1, RuleId = 10 });

        var result = await controller.Update(1, dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkUpdate_ReturnsOk()
    {
        var controller = Create(out var service);
        var items = new[]
        {
            new BulkUpdateItem<int, UpdateRetrospectiveRuleActionDto>(1, new UpdateRetrospectiveRuleActionDto
            {
                RuleId = 10,
                TaxStartMode = "FIXED_CUTOFF",
                RetrospectiveLimitType = "FIXED_CUTOFF_DATE",
                TaxCalculationMode = "SINGLE"
            })
        };
        service.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<RetrospectiveRuleActionDto>(1, 0, new List<RetrospectiveRuleActionDto>()));

        var result = await controller.BulkUpdate(items, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDelete_ReturnsOk()
    {
        var controller = Create(out var service);
        var ids = new[] { 1, 2 };
        service.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<int>(2, 0, new List<int>()));

        var result = await controller.BulkDelete(ids, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Purge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out var referenceValidationService);
        referenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleActionEntity, int>(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        cleanupService.Setup(s => s.ForceHardDeleteAsync<RetrospectiveRuleActionEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        cleanupService.Verify(s => s.ForceHardDeleteAsync<RetrospectiveRuleActionEntity, int>(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkPurge_ReturnsOk()
    {
        var controller = Create(out _, out var cleanupService, out var referenceValidationService);
        var ids = new[] { 1, 2 };
        referenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<RetrospectiveRuleActionEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        cleanupService.Setup(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleActionEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkResult<int>(2, 0, new List<int> { 1, 2 }));

        var result = await controller.BulkPurge(ids, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
        cleanupService.Verify(s => s.BulkForceHardDeleteAsync<RetrospectiveRuleActionEntity, int>(ids, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetTaxStartModes_ReturnsOk()
    {
        var controller = Create(out _);

        var result = controller.GetTaxStartModes();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetUseDateOptions_CallsService_AndReturnsOk()
    {
        var controller = Create(out var service);
        var options = new List<RetrospectiveRuleActionUseDateOptionDto>
        {
            new() { EvidenceTypeId = 1, Label = "OC date", IsCutoffDate = false },
            new() { EvidenceTypeId = null, Label = "Cutoff date", IsCutoffDate = true }
        };
        service.Setup(s => s.GetUseDateOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(options);

        var result = await controller.GetUseDateOptions(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetUseDateOptionsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void GetRetrospectiveLimitTypes_ReturnsOk()
    {
        var controller = Create(out _);

        var result = controller.GetRetrospectiveLimitTypes();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetTaxCalculationModes_ReturnsOk()
    {
        var controller = Create(out _);

        var result = controller.GetTaxCalculationModes();

        Assert.IsType<OkObjectResult>(result);
    }
}
