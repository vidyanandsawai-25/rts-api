using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;
using System.Collections.Generic;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertySurveyControllerTests
{
    [Fact]
    public async Task SearchNewlyCreatedProperties_NoRecords_ReturnsOk()
    {
        // Arrange
        var mockService = new Mock<IPropertySurveyService>();
        var controller = new PropertySurveyController(mockService.Object);

        var request = new CreatedByUserPropertySearchRequestDto
        {
            UserId = 1,
            ModuleId = 1,
            WardId = 47,
            PageNumber = 1,
            PageSize = 10
        };

        var dto = new UserPropertyPageDto
        {
            Items = new List<CreatedByUserPropertyResponseDto>(),
            PageItemCount = 0,
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 0,
            HasNext = false
        };

        mockService
            .Setup(service =>
                service.SearchNewlyCreatedPropertiesAsync(
                    It.IsAny<CreatedByUserPropertySearchRequestDto>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await controller.SearchNewlyCreatedProperties(
            request,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var response =
            Assert.IsType<ApiResponse<UserPropertyPageDto>>(
                okResult.Value);

        Assert.True(response.Success);
        Assert.Equal("No properties found.", response.Message);
        Assert.NotNull(response.Items);
        Assert.Empty(response.Items!.Items);

        mockService.Verify(
            service => service.SearchNewlyCreatedPropertiesAsync(
                It.IsAny<CreatedByUserPropertySearchRequestDto>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchNewlyCreatedProperties_ValidRequest_ReturnsOk()
    {
        // Arrange
        var mockService = new Mock<IPropertySurveyService>();
        var controller = new PropertySurveyController(mockService.Object);

        var request = new CreatedByUserPropertySearchRequestDto
        {
            UserId = 1,
            ModuleId = 1,
            WardId = 47,
            PageNumber = 1,
            PageSize = 10
        };

        var dto = new UserPropertyPageDto
        {
            Items = new List<CreatedByUserPropertyResponseDto>
            {
                new CreatedByUserPropertyResponseDto { Id = 101, PropertyNo = "001" }
            },
            PageItemCount = 1,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1,
            HasNext = false
        };

        mockService.Setup(s => s.SearchNewlyCreatedPropertiesAsync(It.Is<CreatedByUserPropertySearchRequestDto>(r => r.UserId == 1 && r.ModuleId == 1 && r.WardId == 47), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await controller.SearchNewlyCreatedProperties(request, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<UserPropertyPageDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Properties fetched successfully.", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items!.TotalCount);
        Assert.Equal(dto.TotalCount, response.Items.TotalCount);
        Assert.Same(dto, response.Items);

        mockService.Verify(s => s.SearchNewlyCreatedPropertiesAsync(It.IsAny<CreatedByUserPropertySearchRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchNewlyCreatedProperties_PropagatesCancellationToken()
    {
        // Arrange
        var mockService = new Mock<IPropertySurveyService>();
        var controller = new PropertySurveyController(mockService.Object);

        var request = new CreatedByUserPropertySearchRequestDto
        {
            UserId = 1,
            ModuleId = 1,
            WardId = 47,
            PageNumber = 1,
            PageSize = 10
        };

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        mockService.Setup(s => s.SearchNewlyCreatedPropertiesAsync(It.IsAny<CreatedByUserPropertySearchRequestDto>(), token))
            .ReturnsAsync(new UserPropertyPageDto());

        // Act
        await controller.SearchNewlyCreatedProperties(request, token);

        // Assert
        mockService.Verify(s => s.SearchNewlyCreatedPropertiesAsync(It.IsAny<CreatedByUserPropertySearchRequestDto>(), token), Times.Once);
    }
}
