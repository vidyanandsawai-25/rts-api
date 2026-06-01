using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class PolicyConfigurationControllerTests
{
    private static PolicyConfigurationController Create(out Mock<IPolicyConfigurationService> service)
    {
        service = new Mock<IPolicyConfigurationService>();
        var logger = new Mock<ILogger<PolicyConfigurationController>>();
        return new PolicyConfigurationController(service.Object, logger.Object);
    }

    private static PagedResult<PolicyConfigurationDto> EmptyPaged() =>
        new([], 0, 1, 10);

    private static PolicyConfigurationDto SampleDto(int id = 1) => new()
    {
        Id          = id,
        PolicyCode  = $"POL-00{id}",
        Category    = "Tax",
        DisplayName = "Test Policy",
        DataType    = "BIT",
        PolicyValue = "1",
        DefaultValue = "0"
    };

    // ──────────────────────── GET ALL ──────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new PolicyConfigurationQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new PagedResult<PolicyConfigurationDto>([SampleDto()], 1, 1, 10));

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_EmptyResult_ReturnsOk()
    {
        var controller = Create(out var service);
        var query = new PolicyConfigurationQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPaged());

        var result = await controller.GetAll(query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_FiltersByPolicyCode()
    {
        var controller = Create(out var service);
        var query = new PolicyConfigurationQueryParameters { PolicyCode = "POL-001" };
        service.Setup(s => s.GetAllAsync(It.IsAny<PolicyConfigurationQueryParameters>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new PagedResult<PolicyConfigurationDto>([SampleDto()], 1, 1, 10));

        await controller.GetAll(query, CancellationToken.None);

        service.Verify(s => s.GetAllAsync(
            It.Is<PolicyConfigurationQueryParameters>(q => q.PolicyCode == "POL-001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_FiltersByCategory()
    {
        var controller = Create(out var service);
        var query = new PolicyConfigurationQueryParameters { Category = "Tax" };
        service.Setup(s => s.GetAllAsync(It.IsAny<PolicyConfigurationQueryParameters>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPaged());

        await controller.GetAll(query, CancellationToken.None);

        service.Verify(s => s.GetAllAsync(
            It.Is<PolicyConfigurationQueryParameters>(q => q.Category == "Tax"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────── GET BY ID ────────────────────────────────────

    [Fact]
    public async Task GetById_Found_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
               .ReturnsAsync(SampleDto(1));

        var result = await controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNotFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
               .ReturnsAsync((PolicyConfigurationDto?)null);

        var result = await controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ──────────────────────── CREATE ───────────────────────────────────────

    [Fact]
    public async Task Create_ValidDto_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new CreatePolicyConfigurationDto
        {
            PolicyCode  = "POL-NEW",
            Category    = "Tax",
            DisplayName = "New Policy",
            DataType    = "BIT"
        };
        service.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
               .ReturnsAsync(SampleDto(5));

        var result = await controller.Create(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PolicyConfigurationDto>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Create_WithAllNewFields_CallsServiceCorrectly()
    {
        var controller = Create(out var service);
        var dto = new CreatePolicyConfigurationDto
        {
            PolicyCode   = "POL-FULL",
            Category     = "Finance",
            DisplayName  = "Full Policy",
            DataType     = "DECIMAL",
            PolicyValue  = "100.50",
            DefaultValue = "0.00",
            Unit         = "INR",
            EffectiveFrom = new DateTime(2025, 1, 1),
            EffectiveTo   = new DateTime(2025, 12, 31)
        };
        service.Setup(s => s.CreateAsync(It.IsAny<CreatePolicyConfigurationDto>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(SampleDto());

        await controller.Create(dto, CancellationToken.None);

        service.Verify(s => s.CreateAsync(
            It.Is<CreatePolicyConfigurationDto>(d =>
                d.PolicyCode  == "POL-FULL" &&
                d.PolicyValue == "100.50"   &&
                d.Unit        == "INR"      &&
                d.EffectiveFrom.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────── UPDATE ───────────────────────────────────────

    [Fact]
    public async Task Update_Found_ReturnsOk()
    {
        var controller = Create(out var service);
        var dto = new UpdatePolicyConfigurationDto
        {
            PolicyCode  = "POL-001",
            Category    = "Tax",
            DisplayName = "Updated Policy",
            DataType    = "INT",
            PolicyValue = "42"
        };
        service.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
               .ReturnsAsync(SampleDto(1));

        var result = await controller.Update(1, dto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsOkWithFailureMessage()
    {
        var controller = Create(out var service);
        var dto = new UpdatePolicyConfigurationDto
        {
            PolicyCode = "POL-999", Category = "X", DisplayName = "X", DataType = "BIT"
        };
        service.Setup(s => s.UpdateAsync(999, dto, It.IsAny<CancellationToken>()))
               .ReturnsAsync((PolicyConfigurationDto?)null);

        var result = await controller.Update(999, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PolicyConfigurationDto>>(ok.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_WithEffectiveDates_PassesThroughCorrectly()
    {
        var controller = Create(out var service);
        var from = new DateTime(2025, 4, 1);
        var to   = new DateTime(2025, 9, 30);
        var dto = new UpdatePolicyConfigurationDto
        {
            PolicyCode    = "POL-DATE",
            Category      = "Tax",
            DisplayName   = "Date Policy",
            DataType      = "DATE",
            EffectiveFrom = from,
            EffectiveTo   = to
        };
        service.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePolicyConfigurationDto>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(SampleDto(1));

        await controller.Update(1, dto, CancellationToken.None);

        service.Verify(s => s.UpdateAsync(1,
            It.Is<UpdatePolicyConfigurationDto>(d =>
                d.EffectiveFrom == from && d.EffectiveTo == to),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────── DELETE ───────────────────────────────────────

    [Fact]
    public async Task Delete_Found_ReturnsOk()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await controller.Delete(1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PolicyConfigurationDto>>(ok.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task Delete_NotFound_ReturnsOkWithFailureMessage()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await controller.Delete(999, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PolicyConfigurationDto>>(ok.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Delete_CallsServiceWithCorrectId()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await controller.Delete(7, CancellationToken.None);

        service.Verify(s => s.DeleteAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }
}
