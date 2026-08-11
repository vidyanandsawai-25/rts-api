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
    private readonly Mock<IReferenceValidationService> _mockReferenceValidationService;
    private readonly TestController _controller;

    public CrudControllerExtensionsComprehensiveTests()
    {
        _mockService = new Mock<ICommonCrudService<TestEntity, TestDto, TestCreateDto, TestUpdateDto, TestQueryParams, int>>();
        _mockLogger = new Mock<ILogger>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockReferenceValidationService = new Mock<IReferenceValidationService>();
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
        var result = await _controller.ExecuteForceDelete<TestEntity, int>(_mockCleanupService.Object, _mockReferenceValidationService.Object, id, _mockLogger.Object);

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
        var result = await _controller.ExecuteForceDelete<TestEntity, int>(_mockCleanupService.Object, _mockReferenceValidationService.Object, id, _mockLogger.Object);

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
        var result = await _controller.ExecuteForceDelete<TestEntity, int>(_mockCleanupService.Object, _mockReferenceValidationService.Object, id, _mockLogger.Object);

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
        var result = await anonymousController.ExecuteForceDelete<TestEntity, int>(_mockCleanupService.Object, _mockReferenceValidationService.Object, id, _mockLogger.Object);

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

        // Mock validation to return no references for all IDs
        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<TestEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _mockCleanupService.Setup(x => x.BulkForceHardDeleteAsync<TestEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        // Act
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, _mockReferenceValidationService.Object, ids, _mockLogger.Object);

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
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, _mockReferenceValidationService.Object, null!, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkForceDelete_WithEmptyIds_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, _mockReferenceValidationService.Object, Array.Empty<int>(), _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(badRequestResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteBulkForceDelete_WithPartialFailures_ReturnsConflict()
    {
        var ids = new[] { 1, 2, 3 };

        // Mock validation to return references for ID 3
        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<TestEntity, int>(3, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "TableA", "TableB" });

        // Mock validation to return no references for IDs 1 and 2
        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<TestEntity, int>(It.Is<int>(id => id == 1 || id == 2), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Mock cleanup service to return partial success
        var bulkResult = new BulkResult<int>(
            SuccessCount: 2,
            FailedCount: 1,
            Results: new List<int> { 1, 2 },
            Errors: new List<string> { "Failed to delete ID: 3" });

        _mockCleanupService.Setup(x => x.BulkForceHardDeleteAsync<TestEntity, int>(new[] { 1, 2 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(bulkResult);

        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(
            _mockCleanupService.Object,
            _mockReferenceValidationService.Object,
            ids,
            _mockLogger.Object);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(conflictResult.Value);
        Assert.False(response.Success);
        Assert.Contains("Some records cannot be deleted because they are still referenced by other entities.", response.Message);
        Assert.NotNull(response.Errors);
        Assert.Contains("ID: 3, References: TableA, TableB", response.Errors);
    }

    [Fact]
    public async Task ExecuteBulkForceDelete_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };

        // Mock validation to return no references
        _mockReferenceValidationService
            .Setup(s => s.GetReferencingTablesWithDataAsync<TestEntity, int>(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Mock cleanup service to throw a generic exception
        _mockCleanupService
            .Setup(x => x.BulkForceHardDeleteAsync<TestEntity, int>(ids, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteBulkForceDelete<TestEntity, int>(_mockCleanupService.Object, _mockReferenceValidationService.Object, ids, _mockLogger.Object);

        // Assert - Generic exceptions during cleanup return 500
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);

        var response = Assert.IsType<ApiResponse<BulkResult<int>>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Contains("An error occurred while processing the bulk delete operation", response.Message);
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
        Assert.Contains("already exists", response.Message);
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
        Assert.Contains("already exists", response.Message);
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

    #region ExecuteGetAllPaged Tests

    [Fact]
    public async Task ExecuteGetAllPaged_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParams = new TestQueryParams();
        var pagedResult = new PagedResult<TestDto>(
            new List<TestDto> { new TestDto { Id = 1, Name = "Item1" } },
            1,
            1,
            10);

        _mockService.Setup(x => x.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.ExecuteGetAllPaged(_mockService.Object, queryParams, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResult<TestDto>>(okResult.Value);
        Assert.Equal(1, response.TotalCount);
        Assert.Single(response.Items);
    }

    [Fact]
    public async Task ExecuteGetAllPaged_OnFilterValidationException_Returns400()
    {
        // Arrange
        var queryParams = new TestQueryParams();
        _mockService.Setup(x => x.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("SortBy", "Field is not sortable"));

        // Act
        var result = await _controller.ExecuteGetAllPaged(_mockService.Object, queryParams, _mockLogger.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task ExecuteGetAllPaged_OnGenericException_Returns500()
    {
        // Arrange
        var queryParams = new TestQueryParams();
        _mockService.Setup(x => x.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteGetAllPaged(_mockService.Object, queryParams, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<TestDto>>(statusCodeResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region ExecuteGetById Tests

    [Fact]
    public async Task ExecuteGetById_ReturnsOkWhenFound()
    {
        // Arrange
        var id = 1;
        var dto = new TestDto { Id = id, Name = "Item1" };
        _mockService.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.ExecuteGetById(_mockService.Object, id, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TestDto>(okResult.Value);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task ExecuteGetById_Returns404WhenNull()
    {
        // Arrange
        var id = 999;
        _mockService.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDto?)null);

        // Act
        var result = await _controller.ExecuteGetById(_mockService.Object, id, _mockLogger.Object);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ExecuteGetById_OnGenericException_Returns500()
    {
        // Arrange
        var id = 1;
        _mockService.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteGetById(_mockService.Object, id, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<TestDto>>(statusCodeResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region ExecuteCreate Tests

    [Fact]
    public async Task ExecuteCreate_ReturnsOkWithApiResponse()
    {
        // Arrange
        var createDto = new TestCreateDto { Name = "NewItem" };
        var dto = new TestDto { Id = 1, Name = "NewItem" };
        _mockService.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.ExecuteCreate(_mockService.Object, createDto, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TestDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(dto, response.Items);
    }

    [Fact]
    public async Task ExecuteCreate_OnDuplicateMessage_Returns409()
    {
        // Arrange
        var createDto = new TestCreateDto { Name = "NewItem" };
        _mockService.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("duplicate key value violates unique constraint"));

        // Act
        var result = await _controller.ExecuteCreate(_mockService.Object, createDto, _mockLogger.Object);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TestDto>>(conflictResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task ExecuteCreate_OnGenericException_Returns500()
    {
        // Arrange
        var createDto = new TestCreateDto { Name = "NewItem" };
        _mockService.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ExecuteCreate(_mockService.Object, createDto, _mockLogger.Object);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = Assert.IsType<ApiResponse<TestDto>>(statusCodeResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region ExecuteUpdate Tests

    [Fact]
    public async Task ExecuteUpdate_ReturnsOkTrue_WhenUpdated()
    {
        // Arrange
        var id = 1;
        var updateDto = new TestUpdateDto { Name = "Updated" };
        var dto = new TestDto { Id = id, Name = "Updated" };
        _mockService.Setup(x => x.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.ExecuteUpdate(_mockService.Object, id, updateDto, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<ApiResponse<TestDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(dto, response.Items);
    }

    [Fact]
    public async Task ExecuteUpdate_ReturnsOkFalse_WhenNotFound_NOT404()
    {
        // Arrange
        var id = 999;
        var updateDto = new TestUpdateDto { Name = "Updated" };
        _mockService.Setup(x => x.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDto?)null);

        // Act
        var result = await _controller.ExecuteUpdate(_mockService.Object, id, updateDto, _mockLogger.Object);

        // Assert - this must be 200 OK with Success = false, NOT a 404
        Assert.IsNotType<NotFoundResult>(result);
        Assert.IsNotType<NotFoundObjectResult>(result);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<ApiResponse<TestDto>>(okResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region ExecuteDelete Tests

    [Fact]
    public async Task ExecuteDelete_ReturnsOkTrue_WhenMarkedForDeletion()
    {
        // Arrange
        var id = 1;
        _mockService.Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ExecuteDelete(_mockService.Object, id, _mockLogger.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<ApiResponse<TestDto>>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task ExecuteDelete_ReturnsOkFalse_WhenNotFound()
    {
        // Arrange
        var id = 999;
        _mockService.Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ExecuteDelete(_mockService.Object, id, _mockLogger.Object);

        // Assert - still a 200, not a 404
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        var response = Assert.IsType<ApiResponse<TestDto>>(okResult.Value);
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
