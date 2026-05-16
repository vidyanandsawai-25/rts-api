using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using System.Security.Claims;
using Xunit;

namespace NtisPlatform.Tests.Api.Extensions;

/// <summary>
/// Comprehensive tests for CrudControllerExtensions to achieve 100% line coverage
/// </summary>
public class CrudControllerExtensionsComprehensiveTests
{
    private readonly Mock<ICommonCrudService<TestEntity, TestDto, TestCreateDto, TestUpdateDto, TestQueryParams, int>> _mockService;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly TestController _controller;

    public CrudControllerExtensionsComprehensiveTests()
    {
        _mockService = new Mock<ICommonCrudService<TestEntity, TestDto, TestCreateDto, TestUpdateDto, TestQueryParams, int>>();
        _mockLogger = new Mock<ILogger>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _controller = new TestController();
    }

    #region ExecuteForceDelete Tests

    [Fact]
    public async Task ExecuteForceDelete_WithExistingId_ReturnsOk()
    {
        // Arrange
        var id = 1;
        _mockCleanupService.Setup(x => x.ForceHardDeleteAsync<TestEntity, int>(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ExecuteForceDelete<TestEntity, int>(_mockCleanupService.Object, id, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("permanently deleted", response.Message);
    }

    [Fact]
    public async Task ExecuteForceDelete_WithNonExistingId_ReturnsNotFoundResponse()
    {
        // Arrange
        var id = 999;
        _mockCleanupService.Setup(x => x.ForceHardDeleteAsync<TestEntity, int>(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ExecuteForceDelete<TestEntity, int>(_mockCleanupService.Object, id, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task ExecuteForceDelete_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var id = 1;
        _mockCleanupService.Setup(x => x.ForceHardDeleteAsync<TestEntity, int>(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteForceDelete<TestEntity, int>(_mockCleanupService.Object, id, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task ExecuteForceDelete_WithAnonymousUser_LogsAnonymous()
    {
        // Arrange
        var id = 1;
        _mockCleanupService.Setup(x => x.ForceHardDeleteAsync<TestEntity, int>(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var anonymousController = new TestController(false);

        // Act
        var result = await anonymousController.ExecuteForceDelete<TestEntity, int>(_mockCleanupService.Object, id, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult);
    }

    #endregion

    #region ExecuteBulkCreate Tests

    [Fact]
    public async Task ExecuteBulkCreate_WithValidItems_ReturnsOk()
    {
        // Arrange
        var items = new[] { new TestCreateDto { Name = "Item1" }, new TestCreateDto { Name = "Item2" } };
        var bulkResult = new BulkResult<TestDto>(
            2, 
            0, 
            new List<TestDto> { new TestDto { Id = 1, Name = "Item1" }, new TestDto { Id = 2, Name = "Item2" } });

        _mockService.Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.ExecuteBulkCreate(_mockService.Object, items, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("created successfully", response.Message);
    }

    [Fact]
    public async Task ExecuteBulkCreate_WithNullItems_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkCreate(_mockService.Object, null!, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkCreate_WithEmptyItems_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkCreate(_mockService.Object, Array.Empty<TestCreateDto>(), _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkCreate_WithPartialFailures_ReturnsOkWithErrors()
    {
        // Arrange
        var items = new[] { new TestCreateDto { Name = "Item1" }, new TestCreateDto { Name = "Item2" } };
        var bulkResult = new BulkResult<TestDto>(
            1, 
            1, 
            new List<TestDto> { new TestDto { Id = 1, Name = "Item1" } },
            new List<string> { "Item2 failed" });

        _mockService.Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.ExecuteBulkCreate(_mockService.Object, items, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("failed", response.Message);
    }

    [Fact]
    public async Task ExecuteBulkCreate_WithDuplicateError_ReturnsConflict()
    {
        // Arrange
        var items = new[] { new TestCreateDto { Name = "Item1" } };
        _mockService.Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Duplicate key constraint"));

        // Act
        var result = await _controller.ExecuteBulkCreate(_mockService.Object, items, _mockLogger.Object);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkCreate_WithUniqueConstraintError_ReturnsConflict()
    {
        // Arrange
        var items = new[] { new TestCreateDto { Name = "Item1" } };
        _mockService.Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unique constraint violation"));

        // Act
        var result = await _controller.ExecuteBulkCreate(_mockService.Object, items, _mockLogger.Object);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkCreate_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var items = new[] { new TestCreateDto { Name = "Item1" } };
        _mockService.Setup(x => x.BulkCreateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteBulkCreate(_mockService.Object, items, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region ExecuteBulkUpdate Tests

    [Fact]
    public async Task ExecuteBulkUpdate_WithValidItems_ReturnsOk()
    {
        // Arrange
        var items = new[] 
        { 
            new BulkUpdateItem<int, TestUpdateDto>(1, new TestUpdateDto { Name = "Updated1" }),
            new BulkUpdateItem<int, TestUpdateDto>(2, new TestUpdateDto { Name = "Updated2" })
        };
        var bulkResult = new BulkResult<TestDto>(
            2, 
            0, 
            new List<TestDto> { new TestDto { Id = 1, Name = "Updated1" }, new TestDto { Id = 2, Name = "Updated2" } });

        _mockService.Setup(x => x.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.ExecuteBulkUpdate(_mockService.Object, items, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkUpdate_WithNullItems_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkUpdate(_mockService.Object, null!, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkUpdate_WithEmptyItems_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkUpdate(_mockService.Object, Array.Empty<BulkUpdateItem<int, TestUpdateDto>>(), _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkUpdate_WithDuplicateError_ReturnsConflict()
    {
        // Arrange
        var items = new[] { new BulkUpdateItem<int, TestUpdateDto>(1, new TestUpdateDto { Name = "Updated" }) };
        _mockService.Setup(x => x.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unique constraint violation"));

        // Act
        var result = await _controller.ExecuteBulkUpdate(_mockService.Object, items, _mockLogger.Object);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<TestDto>>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkUpdate_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var items = new[] { new BulkUpdateItem<int, TestUpdateDto>(1, new TestUpdateDto { Name = "Updated" }) };
        _mockService.Setup(x => x.BulkUpdateAsync(items, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteBulkUpdate(_mockService.Object, items, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region ExecuteBulkDelete Tests

    [Fact]
    public async Task ExecuteBulkDelete_WithValidIds_ReturnsOk()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        var bulkResult = new BulkResult<int>(3, 0, new List<int> { 1, 2, 3 });

        _mockService.Setup(x => x.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.ExecuteBulkDelete(_mockService.Object, ids, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkDelete_WithNullIds_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkDelete(_mockService.Object, null!, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkDelete_WithEmptyIds_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkDelete(_mockService.Object, Array.Empty<int>(), _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkDelete_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        _mockService.Setup(x => x.BulkDeleteAsync(ids, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteBulkDelete(_mockService.Object, ids, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region ExecuteBulkForceDelete Tests

    [Fact]
    public async Task ExecuteBulkForceDelete_WithValidIds_ReturnsOk()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        var bulkResult = new BulkResult<int>(3, 0, new List<int> { 1, 2, 3 });

        _mockCleanupService.Setup(x => x.BulkForceHardDeleteAsync<TestEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, ids, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("permanently deleted successfully", response.Message);
    }

    [Fact]
    public async Task ExecuteBulkForceDelete_WithNullIds_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, null!, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkForceDelete_WithEmptyIds_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, Array.Empty<int>(), _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkForceDelete_WithPartialFailures_ReturnsOkWithErrors()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        var bulkResult = new BulkResult<int>(
            2, 
            1, 
            new List<int> { 1, 2 },
            new List<string> { "Error deleting ID 3" });

        _mockCleanupService.Setup(x => x.BulkForceHardDeleteAsync<TestEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, ids, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("failed", response.Message);
    }

    [Fact]
    public async Task ExecuteBulkForceDelete_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        _mockCleanupService.Setup(x => x.BulkForceHardDeleteAsync<TestEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, ids, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region ExecuteCreateFromRange Tests (with transformer)

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };
        var rangeResult = new RangeResult<TestDto>(
            5,
            0,
            new List<TestDto> 
            { 
                new TestDto { Id = 1, Name = "Test-1" },
                new TestDto { Id = 2, Name = "Test-2" },
                new TestDto { Id = 3, Name = "Test-3" },
                new TestDto { Id = 4, Name = "Test-4" },
                new TestDto { Id = 5, Name = "Test-5" }
            });

        _mockService.Setup(x => x.CreateFromRangeAsync(request, It.IsAny<Func<TestCreateDto, string, int, TestCreateDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("created successfully from range", response.Message);
        Assert.Equal(5, response.Items!.SuccessCount);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, null!, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("No request provided for Range create.", response.Message);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_EmptyRangeFrom_ReturnsBadRequest()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("RangeFrom and RangeTo are required.", response.Message);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_EmptyRangeTo_ReturnsBadRequest()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "",
            Template = new TestCreateDto { Name = "Test" }
        };

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("RangeFrom and RangeTo are required.", response.Message);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_WhitespaceRangeFrom_ReturnsBadRequest()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "   ",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("RangeFrom and RangeTo are required.", response.Message);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_WhitespaceRangeTo_ReturnsBadRequest()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "   ",
            Template = new TestCreateDto { Name = "Test" }
        };

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal("RangeFrom and RangeTo are required.", response.Message);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "10",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };

        _mockService.Setup(x => x.CreateFromRangeAsync(request, It.IsAny<Func<TestCreateDto, string, int, TestCreateDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid range: RangeFrom must be less than RangeTo"));

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Invalid range", response.Message);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_DuplicateError_ReturnsConflict()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };

        _mockService.Setup(x => x.CreateFromRangeAsync(request, It.IsAny<Func<TestCreateDto, string, int, TestCreateDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("duplicate key value violates unique constraint"));

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Contains("already exists", response.Message);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_UniqueConstraintError_ReturnsConflict()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };

        _mockService.Setup(x => x.CreateFromRangeAsync(request, It.IsAny<Func<TestCreateDto, string, int, TestCreateDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("UNIQUE constraint failed"));

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_ConstraintError_ReturnsConflict()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };

        _mockService.Setup(x => x.CreateFromRangeAsync(request, It.IsAny<Func<TestCreateDto, string, int, TestCreateDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("constraint violation occurred"));

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_GenericException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };

        _mockService.Setup(x => x.CreateFromRangeAsync(request, It.IsAny<Func<TestCreateDto, string, int, TestCreateDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_WithTransformer_PartialFailures_ReturnsOkWithErrors()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new TestCreateDto { Name = "Test" }
        };
        var rangeResult = new RangeResult<TestDto>(
            3,
            2,
            new List<TestDto> 
            { 
                new TestDto { Id = 1, Name = "Test-1" },
                new TestDto { Id = 2, Name = "Test-2" },
                new TestDto { Id = 3, Name = "Test-3" }
            },
            new List<string> { "Error creating Test-4", "Error creating Test-5" });

        _mockService.Setup(x => x.CreateFromRangeAsync(request, It.IsAny<Func<TestCreateDto, string, int, TestCreateDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, str, i) => dto, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("3 records created, 2 failed", response.Message);
        Assert.NotNull(response.Errors);
        Assert.Equal(2, response.Errors.Count);
    }

    #endregion

    #region ExecuteCreateFromRange Tests (without transformer - dynamic)

    [Fact]
    public async Task ExecuteCreateFromRangeNoTransformer_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "3",
            Template = new TestCreateDto { Name = "Test" }
        };
        var rangeResult = new RangeResult<TestDto>(
            3,
            0,
            new List<TestDto> 
            { 
                new TestDto { Id = 1, Name = "Test-1" },
                new TestDto { Id = 2, Name = "Test-2" },
                new TestDto { Id = 3, Name = "Test-3" }
            });

        // Mock the service
        _mockService.Setup(x => x.CreateFromRangeAsync(request, It.IsAny<Func<TestCreateDto, string, int, TestCreateDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rangeResult);

        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, request, (dto, s, i) => dto, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ExecuteCreateFromRange_NullRequest_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteCreateFromRange(_mockService.Object, null!, (dto, s, i) => dto, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<RangeResult<TestDto>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Test Helper Classes

    public class TestEntity { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
    public class TestDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
    public class TestCreateDto { public string Name { get; set; } = string.Empty; }
    public class TestUpdateDto { public string Name { get; set; } = string.Empty; }
    public class TestQueryParams : BaseQueryParameters { }

    public class TestController : ControllerBase
    {
        public TestController(bool authenticated = true)
        {
            if (authenticated)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.Name, "TestUser"),
                            new Claim("sub", "user123"),
                            new Claim("userId", "user123")
                        }, "TestAuth"))
                    }
                };
            }
            else
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity())
                    }
                };
            }
        }
    }

    #endregion
}
