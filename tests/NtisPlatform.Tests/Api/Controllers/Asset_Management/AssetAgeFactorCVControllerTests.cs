using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAgeFactorCVMaster;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Asset_Management;

/// <summary>
/// Comprehensive test suite for AssetAgeFactorCVController covering all CRUD operations,
/// bulk operations, validation scenarios, and error handling.
/// </summary>
public class AssetAgeFactorCVControllerTests
{
    private readonly Mock<IAssetAgeFactorCVService> _mockService;
    private readonly Mock<ILogger<AssetAgeFactorCVController>> _mockLogger;
    private readonly AssetAgeFactorCVController _controller;

    public AssetAgeFactorCVControllerTests()
    {
        _mockService = new Mock<IAssetAgeFactorCVService>();
        _mockLogger = new Mock<ILogger<AssetAgeFactorCVController>>();
        _controller = new AssetAgeFactorCVController(_mockService.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var controller = new AssetAgeFactorCVController(_mockService.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(controller);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidQuery_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParams = new AssetAgeFactorCVMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var expectedData = new List<AssetAgeFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, ConstructionTypeDescription = "Type A", AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true },
            new() { Id = 2, ConstructionTypeId = 2, ConstructionTypeDescription = "Type B", AgeFrom = 6, AgeTo = 10, Factor = 0.9m, YearRangeCVId = 1, IsActive = true }
        };

        var pagedResult = new PagedResult<AssetAgeFactorCVMasterDto>(expectedData, 2, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.Equal(2, returnedData.TotalCount);
        Assert.Equal(2, returnedData.Items.Count());
        Assert.Contains(returnedData.Items, x => x.Id == 1 && x.ConstructionTypeDescription == "Type A");
        Assert.Contains(returnedData.Items, x => x.Id == 2 && x.ConstructionTypeDescription == "Type B");
        _mockService.Verify(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var queryParams = new AssetAgeFactorCVMasterQueryParameters();
        var pagedResult = new PagedResult<AssetAgeFactorCVMasterDto>(new List<AssetAgeFactorCVMasterDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.Equal(0, returnedData.TotalCount);
        Assert.Empty(returnedData.Items);
    }

    [Fact]
    public async Task GetAll_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var queryParams = new AssetAgeFactorCVMasterQueryParameters
        {
            ConstructionTypeId = 1,
            YearRangeCVId = 1,
            IsActive = true
        };

        var filteredData = new List<AssetAgeFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true }
        };

        var pagedResult = new PagedResult<AssetAgeFactorCVMasterDto>(filteredData, 1, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.Single(returnedData.Items);
        Assert.All(returnedData.Items, item => Assert.Equal(1, item.ConstructionTypeId));
    }

    [Fact]
    public async Task GetAll_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var queryParams = new AssetAgeFactorCVMasterQueryParameters();
        var cts = new CancellationTokenSource();
        var pagedResult = new PagedResult<AssetAgeFactorCVMasterDto>(new List<AssetAgeFactorCVMasterDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, cts.Token))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(queryParams, cts.Token);

        // Assert
        _mockService.Verify(s => s.GetAllAsync(queryParams, cts.Token), Times.Once);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOkWithData()
    {
        // Arrange
        const int id = 1;
        var expectedDto = new AssetAgeFactorCVMasterDto
        {
            Id = id,
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true
        };

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<AssetAgeFactorCVMasterDto>(okResult.Value);
        Assert.Equal(id, returnedDto.Id);
        Assert.Equal(expectedDto.ConstructionTypeId, returnedDto.ConstructionTypeId);
        Assert.Equal(expectedDto.AgeFrom, returnedDto.AgeFrom);
        Assert.Equal(expectedDto.AgeTo, returnedDto.AgeTo);
    }

    [Fact]
    public async Task GetById_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        const int id = 999;
        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetAgeFactorCVMasterDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetAgeFactorCVMasterDto?)null);

        // Act
        var result = await _controller.GetById(0, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetByIdAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetAgeFactorCVMasterDto?)null);

        // Act
        var result = await _controller.GetById(-1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkWithCreatedData()
    {
        // Arrange
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = true,
            CreatedBy = 1
        };

        var createdDto = new AssetAgeFactorCVMasterDto
        {
            Id = 1,
            ConstructionTypeId = createDto.ConstructionTypeId!.Value,
            AgeFrom = createDto.AgeFrom!.Value,
            AgeTo = createDto.AgeTo!.Value,
            Factor = createDto.Factor,
            YearRangeCVId = createDto.YearRangeCVId!.Value,
            IsActive = createDto.IsActive
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(1, apiResponse.Items.Id);
    }

    [Fact]
    public async Task Create_WithDuplicateData_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Duplicate entry", OperationType.Create));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithInvalidConstructionTypeId_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 999,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("ConstructionTypeId", "Construction Type not found", OperationType.Create));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithInvalidYearRangeCVId_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 999
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("YearRangeCVId", "Year Range not found", OperationType.Create));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.Create(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task Create_WithInvalidAgeRange_ThrowsValidationException()
    {
        // Arrange - AgeTo is less than AgeFrom
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 10,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("AgeTo", "AgeTo must be greater than or equal to AgeFrom", OperationType.Create));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.Create(createDto, CancellationToken.None));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDto_ReturnsOkWithUpdatedData()
    {
        // Arrange
        const int id = 1;
        var updateDto = new UpdateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 0.95m,
            YearRangeCVId = 1,
            IsActive = true,
            UpdatedBy = 1
        };

        var updatedDto = new AssetAgeFactorCVMasterDto
        {
            Id = id,
            ConstructionTypeId = updateDto.ConstructionTypeId!.Value,
            AgeFrom = updateDto.AgeFrom!.Value,
            AgeTo = updateDto.AgeTo!.Value,
            Factor = updateDto.Factor,
            YearRangeCVId = updateDto.YearRangeCVId!.Value,
            IsActive = updateDto.IsActive
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(0.95m, apiResponse.Items.Factor);
    }

    [Fact]
    public async Task Update_WithNonExistingId_ReturnsOkWithFailureMessage()
    {
        // Arrange
        const int id = 999;
        var updateDto = new UpdateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetAgeFactorCVMasterDto?)null);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Null(apiResponse.Items);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_DeactivatingUsedRecord_ThrowsValidationException()
    {
        // Arrange
        const int id = 1;
        var updateDto = new UpdateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1,
            IsActive = false
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("IsActive", "Cannot deactivate - record is in use", OperationType.Update));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.Update(id, updateDto, CancellationToken.None));
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingId_ReturnsOkWithSuccessMessage()
    {
        // Arrange
        const int id = 1;
        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("marked for deletion", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_WithNonExistingId_ReturnsOkWithFailureMessage()
    {
        // Arrange
        const int id = 999;
        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_WithReferencedRecord_ThrowsValidationException()
    {
        // Arrange
        const int id = 1;
        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Id", "Cannot delete - record is referenced by other entities", OperationType.Delete));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.Delete(id, CancellationToken.None));
    }

    #endregion

    #region BulkCreate Tests

    [Fact]
    public async Task BulkCreate_WithValidItems_ReturnsOkWithSuccessCount()
    {
        // Arrange
        var items = new[]
        {
            new CreateAssetAgeFactorCVMasterDto
            {
                ConstructionTypeId = 1,
                AgeFrom = 0,
                AgeTo = 5,
                Factor = 1.0m,
                YearRangeCVId = 1
            },
            new CreateAssetAgeFactorCVMasterDto
            {
                ConstructionTypeId = 1,
                AgeFrom = 6,
                AgeTo = 10,
                Factor = 0.9m,
                YearRangeCVId = 1
            }
        };

        var createdItems = new List<AssetAgeFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 },
            new() { Id = 2, ConstructionTypeId = 1, AgeFrom = 6, AgeTo = 10, Factor = 0.9m, YearRangeCVId = 1 }
        };

        var bulkResult = new BulkResult<AssetAgeFactorCVMasterDto>(2, 0, createdItems);

        _mockService.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<AssetAgeFactorCVMasterDto>>>(okResult.Value);
        Assert.Equal(2, apiResponse.Items!.SuccessCount);
        Assert.Equal(0, apiResponse.Items!.FailedCount);
        Assert.Equal(2, apiResponse.Items!.Results.Count());
    }

    [Fact]
    public async Task BulkCreate_WithEmptyArray_ReturnsBadRequest()
    {
        // Arrange
        var items = Array.Empty<CreateAssetAgeFactorCVMasterDto>();

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<AssetAgeFactorCVMasterDto>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
        _mockService.Verify(
            s => s.BulkCreateAsync(It.IsAny<CreateAssetAgeFactorCVMasterDto[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkCreate_WithNullArray_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.BulkCreate(null!, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<AssetAgeFactorCVMasterDto>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
        _mockService.Verify(
            s => s.BulkCreateAsync(It.IsAny<CreateAssetAgeFactorCVMasterDto[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkCreate_WithPartialFailures_ReturnsOkWithMixedResults()
    {
        // Arrange
        var items = new[]
        {
            new CreateAssetAgeFactorCVMasterDto
            {
                ConstructionTypeId = 1,
                AgeFrom = 0,
                AgeTo = 5,
                Factor = 1.0m,
                YearRangeCVId = 1
            },
            new CreateAssetAgeFactorCVMasterDto
            {
                ConstructionTypeId = 999, // Invalid
                AgeFrom = 6,
                AgeTo = 10,
                Factor = 0.9m,
                YearRangeCVId = 1
            }
        };

        var createdItems = new List<AssetAgeFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1 }
        };

        var bulkResult = new BulkResult<AssetAgeFactorCVMasterDto>(1, 1, createdItems);

        _mockService.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<AssetAgeFactorCVMasterDto>>>(okResult.Value);
        Assert.Equal(1, apiResponse.Items!.SuccessCount);
        Assert.Equal(1, apiResponse.Items!.FailedCount);
    }

    #endregion

    #region BulkUpdate Tests

    [Fact]
    public async Task BulkUpdate_WithNullArray_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.BulkUpdate(null!, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<AssetAgeFactorCVMasterDto>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
        _mockService.Verify(
            s => s.BulkUpdateAsync(It.IsAny<BulkUpdateItem<int, UpdateAssetAgeFactorCVMasterDto>[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkUpdate_WithEmptyArray_ReturnsBadRequest()
    {
        // Arrange
        var items = Array.Empty<BulkUpdateItem<int, UpdateAssetAgeFactorCVMasterDto>>();

        // Act
        var result = await _controller.BulkUpdate(items, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<AssetAgeFactorCVMasterDto>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task BulkUpdate_WithValidItems_ReturnsOkWithSuccessCount()
    {
        // Arrange
        var items = new[]
        {
            new BulkUpdateItem<int, UpdateAssetAgeFactorCVMasterDto>(
                1,
                new UpdateAssetAgeFactorCVMasterDto
                {
                    ConstructionTypeId = 1,
                    AgeFrom = 0,
                    AgeTo = 5,
                    Factor = 0.95m,
                    YearRangeCVId = 1
                }),
            new BulkUpdateItem<int, UpdateAssetAgeFactorCVMasterDto>(
                2,
                new UpdateAssetAgeFactorCVMasterDto
                {
                    ConstructionTypeId = 1,
                    AgeFrom = 6,
                    AgeTo = 10,
                    Factor = 0.85m,
                    YearRangeCVId = 1
                })
        };

        var updatedItems = new List<AssetAgeFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 0.95m, YearRangeCVId = 1 },
            new() { Id = 2, ConstructionTypeId = 1, AgeFrom = 6, AgeTo = 10, Factor = 0.85m, YearRangeCVId = 1 }
        };

        var bulkResult = new BulkResult<AssetAgeFactorCVMasterDto>(2, 0, updatedItems);

        _mockService.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkUpdate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<AssetAgeFactorCVMasterDto>>>(okResult.Value);
        Assert.Equal(2, apiResponse.Items!.SuccessCount);
        Assert.Equal(0, apiResponse.Items!.FailedCount);
    }

    [Fact]
    public async Task BulkUpdate_WithNonExistingIds_ReturnsPartialFailure()
    {
        // Arrange
        var items = new[]
        {
            new BulkUpdateItem<int, UpdateAssetAgeFactorCVMasterDto>(
                1,
                new UpdateAssetAgeFactorCVMasterDto
                {
                    ConstructionTypeId = 1,
                    AgeFrom = 0,
                    AgeTo = 5,
                    Factor = 0.95m,
                    YearRangeCVId = 1
                }),
            new BulkUpdateItem<int, UpdateAssetAgeFactorCVMasterDto>(
                999, // Non-existing
                new UpdateAssetAgeFactorCVMasterDto
                {
                    ConstructionTypeId = 1,
                    AgeFrom = 6,
                    AgeTo = 10,
                    Factor = 0.85m,
                    YearRangeCVId = 1
                })
        };

        var updatedItems = new List<AssetAgeFactorCVMasterDto>
        {
            new() { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 0.95m, YearRangeCVId = 1 }
        };

        var bulkResult = new BulkResult<AssetAgeFactorCVMasterDto>(1, 1, updatedItems);

        _mockService.Setup(s => s.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkUpdate(items, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<AssetAgeFactorCVMasterDto>>>(okResult.Value);
        Assert.Equal(1, apiResponse.Items!.SuccessCount);
        Assert.Equal(1, apiResponse.Items!.FailedCount);
    }

    #endregion

    #region BulkDelete Tests

    [Fact]
    public async Task BulkDelete_WithValidIds_ReturnsOkWithSuccessCount()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        var bulkResult = new BulkResult<int>(3, 0, new List<int> { 1, 2, 3 });

        _mockService.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkDelete(ids, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.Equal(3, apiResponse.Items!.SuccessCount);
        Assert.Equal(0, apiResponse.Items!.FailedCount);
    }

    [Fact]
    public async Task BulkDelete_WithNonExistingIds_ReturnsPartialFailure()
    {
        // Arrange
        var ids = new[] { 1, 999, 3 };
        var bulkResult = new BulkResult<int>(2, 1, new List<int> { 1, 3 });

        _mockService.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkDelete(ids, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.Equal(2, apiResponse.Items!.SuccessCount);
        Assert.Equal(1, apiResponse.Items!.FailedCount);
    }

    [Fact]
    public async Task BulkDelete_WithEmptyArray_ReturnsBadRequest()
    {
        // Arrange
        var ids = Array.Empty<int>();

        // Act
        var result = await _controller.BulkDelete(ids, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
        _mockService.Verify(
            s => s.BulkDeleteAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkDelete_WithNullArray_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.BulkDelete(null!, CancellationToken.None);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequest.Value);
        Assert.False(apiResponse.Success);
        _mockService.Verify(
            s => s.BulkDeleteAsync(It.IsAny<int[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkDelete_WithReferencedRecords_ReturnsPartialFailure()
    {
        // Arrange
        var ids = new[] { 1, 2 };
        var bulkResult = new BulkResult<int>(1, 1, new List<int> { 2 });

        _mockService.Setup(s => s.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.BulkDelete(ids, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.Equal(1, apiResponse.Items!.SuccessCount);
        Assert.Equal(1, apiResponse.Items!.FailedCount);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task Create_WithMaxFactorValue_ReturnsOk()
    {
        // Arrange
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 999.99m, // Max value
            YearRangeCVId = 1
        };

        var createdDto = new AssetAgeFactorCVMasterDto
        {
            Id = 1,
            ConstructionTypeId = createDto.ConstructionTypeId!.Value,
            AgeFrom = createDto.AgeFrom!.Value,
            AgeTo = createDto.AgeTo!.Value,
            Factor = createDto.Factor,
            YearRangeCVId = createDto.YearRangeCVId!.Value
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.Equal(999.99m, apiResponse.Items!.Factor);
    }

    [Fact]
    public async Task Create_WithMinFactorValue_ReturnsOk()
    {
        // Arrange
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 0m, // Min value
            YearRangeCVId = 1
        };

        var createdDto = new AssetAgeFactorCVMasterDto
        {
            Id = 1,
            ConstructionTypeId = createDto.ConstructionTypeId!.Value,
            AgeFrom = createDto.AgeFrom!.Value,
            AgeTo = createDto.AgeTo!.Value,
            Factor = createDto.Factor,
            YearRangeCVId = createDto.YearRangeCVId!.Value
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.Equal(0m, apiResponse.Items!.Factor);
    }

    [Fact]
    public async Task GetAll_WithLargePage_HandlesCorrectly()
    {
        // Arrange
        var queryParams = new AssetAgeFactorCVMasterQueryParameters
        {
            PageNumber = 1,
            PageSize = 1000
        };

        var largeDataSet = Enumerable.Range(1, 1000)
            .Select(i => new AssetAgeFactorCVMasterDto
            {
                Id = i,
                ConstructionTypeId = 1,
                AgeFrom = i * 5,
                AgeTo = (i + 1) * 5,
                Factor = 1.0m,
                YearRangeCVId = 1
            })
            .ToList();

        var pagedResult = new PagedResult<AssetAgeFactorCVMasterDto>(largeDataSet, 1000, 1, 1000);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<AssetAgeFactorCVMasterDto>>(okResult.Value);
        Assert.Equal(1000, returnedData.TotalCount);
    }

    [Fact]
    public async Task Create_ServiceThrowsException_Returns500()
    {
        // Arrange
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    #endregion

    #region Service Call Verification Tests

    [Fact]
    public async Task GetAll_CallsServiceExactlyOnce()
    {
        // Arrange
        var queryParams = new AssetAgeFactorCVMasterQueryParameters();
        var pagedResult = new PagedResult<AssetAgeFactorCVMasterDto>(new List<AssetAgeFactorCVMasterDto>(), 0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()), Times.Once);
        _mockService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_CallsServiceExactlyOnce()
    {
        // Arrange
        var createDto = new CreateAssetAgeFactorCVMasterDto
        {
            ConstructionTypeId = 1,
            AgeFrom = 0,
            AgeTo = 5,
            Factor = 1.0m,
            YearRangeCVId = 1
        };

        var createdDto = new AssetAgeFactorCVMasterDto { Id = 1 };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        await _controller.Create(createDto, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
        _mockService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BulkCreate_CallsServiceExactlyOnce()
    {
        // Arrange
        var items = new[] { new CreateAssetAgeFactorCVMasterDto() };
        var bulkResult = new BulkResult<AssetAgeFactorCVMasterDto>(1, 0, new List<AssetAgeFactorCVMasterDto>());

        _mockService.Setup(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        await _controller.BulkCreate(items, CancellationToken.None);

        // Assert
        _mockService.Verify(s => s.BulkCreateAsync(items, It.IsAny<CancellationToken>()), Times.Once);
        _mockService.VerifyNoOtherCalls();
    }

    #endregion
}
