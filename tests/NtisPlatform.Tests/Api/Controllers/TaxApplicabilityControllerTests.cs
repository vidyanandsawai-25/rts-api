using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.TaxApplicability;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Unit tests for <see cref="TaxApplicabilityController"/>
/// </summary>
public class TaxApplicabilityControllerTests
{
    private readonly Mock<ITaxApplicabilityService> _mockService;
    private readonly Mock<ILogger<TaxApplicabilityController>> _mockLogger;
    private readonly TaxApplicabilityController _controller;

    public TaxApplicabilityControllerTests()
    {
        _mockService = new Mock<ITaxApplicabilityService>();
        _mockLogger = new Mock<ILogger<TaxApplicabilityController>>();
        _controller = new TaxApplicabilityController(_mockService.Object, _mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidRequest_ReturnsPagedResult()
    {
        // Arrange
        var request = new TaxApplicabilityRequestDto
        {
            PropertyId = 1,
            AssessmentYearRangeId = 2,
            TypeOfUseId = 3,
            CalculationType = "RV",
            PageNumber = 1,
            PageSize = 10
        };

        var serviceResponse = new TaxApplicabilityResponseDto
        {
            PropertyId = 1,
            AssessmentYearRangeId = 2,
            TypeOfUseId = 3,
            ApplicableCount = 1,
            ExemptedCount = 0,
            ApplicableTaxes = new List<TaxApplicabilityDetailDto>
            {
                new() { TaxId = 101, TaxHead = "Water Tax", TaxPercentage = 5.0m, TaxAmount = 500.0m, IsApplicable = true }
            }
        };

        var pagedResult = new PagedResult<TaxApplicabilityResponseDto>(
            new List<TaxApplicabilityResponseDto> { serviceResponse },
            1,
            1,
            10);

        _mockService.Setup(s => s.GetAllAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPagedResult = Assert.IsType<PagedResult<TaxApplicabilityResponseDto>>(okResult.Value);
        Assert.Equal(1, returnedPagedResult.TotalCount);
        Assert.Single(returnedPagedResult.Items);
        var firstItem = returnedPagedResult.Items.First();
        Assert.Equal(1, firstItem.ApplicableCount);
        Assert.Equal("Water Tax", firstItem.ApplicableTaxes.First().TaxHead);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new TaxApplicabilityRequestDto
        {
            PropertyId = 1,
            AssessmentYearRangeId = 2,
            TypeOfUseId = 3,
            CalculationType = "RV"
        };

        _mockService.Setup(s => s.GetAllAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated database failure"));

        // Act
        var result = await _controller.GetAll(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("An error occurred while processing your request.", apiResponse.Message);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new CreateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<CreateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = true }
            }
        };

        var resultDto = new TaxApplicabilityResponseDto
        {
            PropertyId = 1,
            AssessmentYearRangeId = 0,
            TypeOfUseId = 0,
            ApplicableCount = 1,
            ExemptedCount = 0
        };

        _mockService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal("Record inserted successfully", apiResponse.Message);
        Assert.Equal(1, apiResponse.Items.PropertyId);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsDuplicateStatusException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<CreateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = true }
            }
        };

        _mockService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot create tax applicability. The following tax(es) already have the same status: Water Tax (already applicable). No changes are needed for these taxes."));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("An error occurred while creating the record", apiResponse.Message);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsArgumentException_ReturnsConflict()
    {
        // Arrange
        var request = new CreateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<CreateTaxStatusDto>
            {
                new() { TaxId = 999, IsApplicable = true }
            }
        };

        _mockService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Cannot create tax applicability. The following Tax ID(s) do not exist: 999."));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsDuplicateException_ReturnsConflict()
    {
        // Arrange
        var request = new CreateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<CreateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = true }
            }
        };

        _mockService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("duplicate key violation"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsUniqueConstraintException_ReturnsConflict()
    {
        // Arrange
        var request = new CreateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<CreateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = true }
            }
        };

        _mockService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("unique constraint failed"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsGeneralException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<CreateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = true }
            }
        };

        _mockService.Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated database failure"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("An error occurred while creating the record", apiResponse.Message);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidRequest_ReturnsOk()
    {
        // Arrange
        int id = 1;
        var request = new UpdateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<UpdateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = false }
            }
        };

        var resultDto = new TaxApplicabilityResponseDto
        {
            PropertyId = 1,
            AssessmentYearRangeId = 0,
            TypeOfUseId = 0,
            ApplicableCount = 0,
            ExemptedCount = 1
        };

        _mockService.Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal("Record updated successfully", apiResponse.Message);
        Assert.Equal(1, apiResponse.Items.PropertyId);
    }

    [Fact]
    public async Task Update_WhenRecordNotFound_ReturnsOkWithFailure()
    {
        // Arrange
        int id = 999;
        var request = new UpdateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<UpdateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = false }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxApplicabilityResponseDto?)null);

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsDuplicateStatusException_ReturnsInternalServerError()
    {
        // Arrange
        int id = 1;
        var request = new UpdateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<UpdateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = true }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot update tax applicability. The following tax(es) already have the same status: Water Tax (already applicable). No changes are needed for these taxes."));

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("An error occurred while updating the record", apiResponse.Message);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsArgumentException_ReturnsConflict()
    {
        // Arrange
        int id = 1;
        var request = new UpdateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<UpdateTaxStatusDto>
            {
                new() { TaxId = 999, IsApplicable = true }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Cannot update tax applicability. The following Tax ID(s) do not exist: 999."));

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsDuplicateKeyException_ReturnsConflict()
    {
        // Arrange
        int id = 1;
        var request = new UpdateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<UpdateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = false }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("duplicate key violation"));

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsGeneralException_ReturnsInternalServerError()
    {
        // Arrange
        int id = 1;
        var request = new UpdateTaxApplicabilityRequestDto
        {
            PropertyId = 1,
            UserId = 10,
            Taxes = new List<UpdateTaxStatusDto>
            {
                new() { TaxId = 101, IsApplicable = false }
            }
        };

        _mockService.Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated database failure"));

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<TaxApplicabilityResponseDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Equal("An error occurred while updating the record", apiResponse.Message);
    }

    #endregion
}
