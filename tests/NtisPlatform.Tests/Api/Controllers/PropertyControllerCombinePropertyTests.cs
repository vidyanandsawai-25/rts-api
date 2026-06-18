using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerCombinePropertyTests
{
    private static PropertyController CreateController()
    {
        var service = new Mock<IPropertyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(service, logger);
    }

    [Fact]
    public async Task GetAllCombineProperties_ReturnsOk()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var query = new CombinePropertyQueryParameters();
        combineService.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CombinePropertyDto>(new List<CombinePropertyDto>(), 0, 1, 10));

        var result = await controller.GetAllCombineProperties(combineService.Object, query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetails_ReturnsOk()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var query = new PropertyCombineDetailsQueryParameters();
        combineService.Setup(s => s.GetPropertyCombineDetailsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyCombineDetailsDto>());

        var result = await controller.GetPropertyCombineDetails(combineService.Object, query, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetPropertyCombineDetails_Returns500_OnException()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        combineService.Setup(s => s.GetPropertyCombineDetailsAsync(It.IsAny<PropertyCombineDetailsQueryParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetPropertyCombineDetails(combineService.Object, new PropertyCombineDetailsQueryParameters(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task CombineProperties_ReturnsOk_OnSuccess()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var request = new CombinePropertiesRequestDto { SourcePropertyId = 1 };
        combineService.Setup(s => s.CombinePropertiesAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CombinePropertiesResponseDto { Success = true, Message = "ok" });

        var result = await controller.CombineProperties(combineService.Object, request, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CombineProperties_ReturnsBadRequest_WhenServiceReportsFailure()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var request = new CombinePropertiesRequestDto { SourcePropertyId = 1 };
        combineService.Setup(s => s.CombinePropertiesAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CombinePropertiesResponseDto { Success = false, Message = "validation failed" });

        var result = await controller.CombineProperties(combineService.Object, request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CombineProperties_Returns500_OnException()
    {
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        combineService.Setup(s => s.CombinePropertiesAsync(It.IsAny<CombinePropertiesRequestDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.CombineProperties(combineService.Object, new CombinePropertiesRequestDto { SourcePropertyId = 1 }, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    #region GetCombinePropertyHistory Tests

    [Fact]
    public async Task GetCombinePropertyHistory_WithValidQueryParams_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 100 };

        var historyData = new List<CombinePropertyHistoryDto>
        {
            new() { PropertyId = 100, WardId = 60, WardNo = "WARD60", PropertyNo = "1", PartitionNo = "A1", OwnerName = "Source Owner" },
            new() { PropertyId = 101, WardId = 60, WardNo = "WARD60", PropertyNo = "1", PartitionNo = "A2", OwnerName = "Combined Owner" }
        };

        combineService.Setup(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 100), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CombinePropertyHistoryDto>(historyData, historyData.Count, 1, 10));

        // Act
        var result = await controller.GetCombinePropertyHistory(combineService.Object, queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<CombinePropertyHistoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Combine property history fetched successfully", response.Message);
        Assert.NotNull(response.Items);
        Assert.Equal(2, response.Items.TotalCount);
    }

    [Fact]
    public async Task GetCombinePropertyHistory_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 999 };

        combineService.Setup(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 999), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CombinePropertyHistoryDto>(new List<CombinePropertyHistoryDto>(), 0, 1, 10));

        // Act
        var result = await controller.GetCombinePropertyHistory(combineService.Object, queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<CombinePropertyHistoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Empty(response.Items.Items);
    }

    [Fact]
    public async Task GetCombinePropertyHistory_Returns500_OnException()
    {
        // Arrange
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 100 };

        combineService.Setup(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 100), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await controller.GetCombinePropertyHistory(combineService.Object, queryParams, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<PagedResult<CombinePropertyHistoryDto>>>(statusCodeResult.Value);
        Assert.False(response.Success);
        Assert.Equal("An error occurred while retrieving combine property history", response.Message);
    }

    [Fact]
    public async Task GetCombinePropertyHistory_WithSingleProperty_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 100 };

        var historyData = new List<CombinePropertyHistoryDto>
        {
            new() { PropertyId = 100, WardId = 60, WardNo = "WARD60", PropertyNo = "1", OwnerName = "Only Source" }
        };

        combineService.Setup(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 100), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CombinePropertyHistoryDto>(historyData, historyData.Count, 1, 10));

        // Act
        var result = await controller.GetCombinePropertyHistory(combineService.Object, queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<CombinePropertyHistoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Single(response.Items.Items);
    }

    [Fact]
    public async Task GetCombinePropertyHistory_VerifiesServiceCalledWithCorrectId()
    {
        // Arrange
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 12345 };

        combineService.Setup(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 12345), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CombinePropertyHistoryDto>(new List<CombinePropertyHistoryDto>(), 0, 1, 10));

        // Act
        await controller.GetCombinePropertyHistory(combineService.Object, queryParams, CancellationToken.None);

        // Assert
        combineService.Verify(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 12345), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCombinePropertyHistory_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 100 };
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        combineService.Setup(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 100), token))
            .ReturnsAsync(new PagedResult<CombinePropertyHistoryDto>(new List<CombinePropertyHistoryDto>(), 0, 1, 10));

        // Act
        await controller.GetCombinePropertyHistory(combineService.Object, queryParams, token);

        // Assert
        combineService.Verify(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 100), token), Times.Once);
    }

    [Fact]
    public async Task GetCombinePropertyHistory_WithLargeDataSet_ReturnsOk()
    {
        // Arrange
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 100 };

        var historyData = Enumerable.Range(100, 50)
            .Select(id => new CombinePropertyHistoryDto
            {
                PropertyId = id,
                WardId = 60,
                WardNo = "WARD60",
                PropertyNo = "1",
                PartitionNo = $"A{id - 99}",
                OwnerName = $"Owner {id}"
            })
            .ToList();

        combineService.Setup(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 100), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CombinePropertyHistoryDto>(historyData, historyData.Count, 1, 50));

        // Act
        var result = await controller.GetCombinePropertyHistory(combineService.Object, queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<CombinePropertyHistoryDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(50, response.Items.TotalCount);
    }

    [Fact]
    public async Task GetCombinePropertyHistory_ResponseContainsCorrectPropertyData()
    {
        // Arrange
        var controller = CreateController();
        var combineService = new Mock<ICombinePropertyService>();
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 100 };

        var historyData = new List<CombinePropertyHistoryDto>
        {
            new()
            {
                PropertyId = 100,
                WardId = 60,
                WardNo = "DIMAJOR1",
                PropertyNo = "1",
                PartitionNo = "A1",
                OldPropertyNo = "OLD-1",
                OwnerName = "NAGNATH APARTMENT",
                OccupierName = "THE HOLDER",
                CategoryId = 6,
                PropertyTypeId = 12,
                PropertyDescription = "??????",
                TaxAmount = 1231390,
                PendingAmount = 552399
            }
        };

        combineService.Setup(s => s.GetCombinePropertyHistoryAsync(It.Is<CombinePropertyHistoryQueryParameters>(q => q.SourcePropertyId == 100), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CombinePropertyHistoryDto>(historyData, historyData.Count, 1, 10));

        // Act
        var result = await controller.GetCombinePropertyHistory(combineService.Object, queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<CombinePropertyHistoryDto>>>(okResult.Value);
        
        Assert.NotNull(response.Items);
        var item = response.Items.Items.First();
        Assert.Equal(100, item.PropertyId);
        Assert.Equal(60, item.WardId);
        Assert.Equal("DIMAJOR1", item.WardNo);
        Assert.Equal("1", item.PropertyNo);
        Assert.Equal("A1", item.PartitionNo);
        Assert.Equal("OLD-1", item.OldPropertyNo);
        Assert.Equal("NAGNATH APARTMENT", item.OwnerName);
        Assert.Equal("THE HOLDER", item.OccupierName);
        Assert.Equal(6, item.CategoryId);
        Assert.Equal(12, item.PropertyTypeId);
        Assert.Equal("??????", item.PropertyDescription);
        Assert.Equal(1231390, item.TaxAmount);
        Assert.Equal(552399, item.PendingAmount);
    }

    #endregion

    #region CombinePropertyHistoryQueryParameters Validation Tests

    [Fact]
    public void CombinePropertyHistoryQueryParameters_WithValidSourcePropertyId_IsValid()
    {
        // Arrange
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 100 };

        // Act & Assert - Validation attributes should pass
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(queryParams);
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(queryParams, validationContext, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CombinePropertyHistoryQueryParameters_WithNullSourcePropertyId_IsValid()
    {
        // Arrange - SourcePropertyId is now optional, so null is valid
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = null };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(queryParams);
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(queryParams, validationContext, validationResults, true);

        // Assert - null is valid for optional SourcePropertyId
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CombinePropertyHistoryQueryParameters_WithZeroSourcePropertyId_IsValid()
    {
        // Arrange - SourcePropertyId is now optional (int?), so 0 is a valid value
        // The service will treat it as "no filter" since it's not a meaningful property ID
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = 0 };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(queryParams);
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(queryParams, validationContext, validationResults, true);

        // Assert - 0 is valid for optional int? (validation passes, service handles logic)
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CombinePropertyHistoryQueryParameters_WithNegativeSourcePropertyId_IsValid()
    {
        // Arrange - SourcePropertyId is now optional (int?), so -1 is a valid value
        // The service will treat it as "no filter" since it's not a meaningful property ID
        var queryParams = new CombinePropertyHistoryQueryParameters { SourcePropertyId = -1 };

        // Act
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(queryParams);
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(queryParams, validationContext, validationResults, true);

        // Assert - -1 is valid for optional int? (validation passes, service handles logic)
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    #endregion
}
