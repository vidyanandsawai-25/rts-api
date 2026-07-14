using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class SocietyWingDetailsControllerTests
{
    private readonly Mock<ISocietyWingDetailsService> _mockService;
    private readonly Mock<ILogger<SocietyWingDetailsController>> _mockLogger;
    private readonly SocietyWingDetailsController _controller;

    public SocietyWingDetailsControllerTests()
    {
        _mockService = new Mock<ISocietyWingDetailsService>();
        _mockLogger = new Mock<ILogger<SocietyWingDetailsController>>();
        _controller = new SocietyWingDetailsController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithPagedData()
    {
        // Arrange
        var queryParams = new SocietyWingDetailsQueryParameters();
        var pagedResult = new PagedResult<SocietyWingDetailsDto>
        {
            Items = new List<SocietyWingDetailsDto> { new SocietyWingDetailsDto { Id = 1, NewWingName = "Wing A" } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeAssignableTo<PagedResult<SocietyWingDetailsDto>>().Subject;
        returnValue.Should().BeEquivalentTo(pagedResult);
    }

    [Fact]
    public async Task GetById_WhenExists_ReturnsOkResult_WithData()
    {
        // Arrange
        int id = 1;
        var dto = new SocietyWingDetailsDto { Id = id, NewWingName = "Wing A" };

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnValue = okResult.Value.Should().BeAssignableTo<SocietyWingDetailsDto>().Subject;
        returnValue.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        int id = 99;
        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocietyWingDetailsDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ReturnsOkResult_WithApiResponse()
    {
        // Arrange
        var createDto = new CreateSocietyWingDetailsDto { NewWingName = "Wing B" };
        var dto = new SocietyWingDetailsDto { Id = 2, NewWingName = "Wing B" };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<SocietyWingDetailsDto>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Items.Should().BeEquivalentTo(dto);
        apiResponse.Message.Should().Be("Record inserted successfully");
    }

    [Fact]
    public async Task Update_ReturnsOkResult_WithApiResponse()
    {
        // Arrange
        int id = 1;
        var updateDto = new UpdateSocietyWingDetailsDto { NewWingName = "Wing A Updated" };
        var dto = new SocietyWingDetailsDto { Id = id, NewWingName = "Wing A Updated" };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<SocietyWingDetailsDto>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Items.Should().BeEquivalentTo(dto);
        apiResponse.Message.Should().Be("Record updated successfully");
    }

    [Fact]
    public async Task Update_WhenNotExists_ReturnsOkResult_WithFailedSuccessFlag()
    {
        // Arrange
        int id = 99;
        var updateDto = new UpdateSocietyWingDetailsDto { NewWingName = "Non-existent" };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocietyWingDetailsDto?)null);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<SocietyWingDetailsDto>>().Subject;
        
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Record not found for Update ");
        apiResponse.Items.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ReturnsOkResult_WithApiResponse()
    {
        // Arrange
        int id = 1;
        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<SocietyWingDetailsDto>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("Record marked for deletion");
    }

    [Fact]
    public async Task Delete_WhenNotExists_ReturnsOkResult_WithFailedSuccessFlag()
    {
        // Arrange
        int id = 99;
        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeAssignableTo<ApiResponse<SocietyWingDetailsDto>>().Subject;
        
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Be("Record not found");
    }
}
