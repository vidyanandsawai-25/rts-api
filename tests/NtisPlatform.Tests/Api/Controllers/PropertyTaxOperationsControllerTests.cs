using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyTaxOperations;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyTaxOperationsControllerTests
{
    private readonly Mock<IPropertyTaxOperationsService> _mockService;
    private readonly Mock<ILogger<PropertyTaxOperationsController>> _mockLogger;
    private readonly PropertyTaxOperationsController _controller;

    public PropertyTaxOperationsControllerTests()
    {
        _mockService = new Mock<IPropertyTaxOperationsService>();
        _mockLogger = new Mock<ILogger<PropertyTaxOperationsController>>();

        _controller = new PropertyTaxOperationsController(
            _mockService.Object,
            _mockLogger.Object);

        SetupAuthenticatedUser(1);
    }

    private void SetupAuthenticatedUser(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("name", "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task Init_ReturnsOkResult_WithInitDto()
    {
        var dto = new OperationsInitDto();
        _mockService.Setup(s => s.GetInitAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.Init(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task ImportTemplate_ReturnsOkResult()
    {
        var dto = new ImportTemplateDto();
        _mockService.Setup(s => s.GetImportTemplateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.ImportTemplate(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task EligibleCount_ReturnsOkResult()
    {
        var request = new EligibleCountRequestDto();
        var response = new EligibleCountResponseDto();
        _mockService.Setup(s => s.GetEligibleCountAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.EligibleCount(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task Preview_ReturnsOkResult()
    {
        var request = new OperationPreviewRequestDto();
        var response = new OperationPreviewResponseDto();
        _mockService.Setup(s => s.GetPreviewAsync(request, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.Preview(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, okResult.Value);
    }

    [Fact]
    public async Task Execute_WithInvalidOperation_ReturnsForbid()
    {
        var request = new ExecuteOperationRequestDto { Operation = "InvalidOp" };

        var result = await _controller.Execute(request, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Execute_WithValidOperation_ReturnsOkResult()
    {
        var request = new ExecuteOperationRequestDto { Operation = "AddTax" };
        var response = new ExecuteOperationResponseDto();
        _mockService.Setup(s => s.ExecuteAsync(request, It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await _controller.Execute(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<ExecuteOperationResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal(response, apiResponse.Items);
    }

    [Fact]
    public async Task JobStatus_ReturnsOkResult()
    {
        var dto = new JobStatusDto();
        _mockService.Setup(s => s.GetJobStatusAsync(123, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.JobStatus(123, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, okResult.Value);
    }

    [Fact]
    public async Task JobProperties_ReturnsOkResult()
    {
        var query = new JobPropertiesQueryParameters();
        var pagedResult = new PagedResult<JobPropertyResultDto>();
        _mockService.Setup(s => s.GetJobPropertiesAsync(123, query, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.JobProperties(123, query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(pagedResult, okResult.Value);
    }

    [Fact]
    public async Task Audit_ReturnsOkResult()
    {
        var query = new OperationAuditQueryParameters();
        var pagedResult = new PagedResult<JobAuditDto>();
        _mockService.Setup(s => s.GetAuditListAsync(query, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.Audit(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(pagedResult, okResult.Value);
    }

    [Fact]
    public async Task AuditDetail_ReturnsOkResult()
    {
        var query = new JobPropertiesQueryParameters();
        var detail = new JobAuditDetailDto();
        _mockService.Setup(s => s.GetAuditDetailAsync(123, query, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var result = await _controller.AuditDetail(123, query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(detail, okResult.Value);
    }

   
}
