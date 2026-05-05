using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Exceptions;
using Xunit;
using NtisPlatform.Application.DTOs.Master.RuleScopeMaster;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Comprehensive unit tests for RuleScopeController
/// Tests all CRUD API endpoints with >90% branch coverage
/// </summary>
public class RuleScopeControllerTests
{
    private readonly Mock<IRuleScopeService> _serviceMock;
    private readonly Mock<ILogger<RuleScopeController>> _loggerMock;
    private readonly RuleScopeController _controller;

    public RuleScopeControllerTests()
    {
        _serviceMock = new Mock<IRuleScopeService>();
        _loggerMock = new Mock<ILogger<RuleScopeController>>();
        _controller = new RuleScopeController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Endpoint Tests

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParameters = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            RuleScope = "Tax"
        };

        var pagedResult = new PagedResult<RuleScopeDto>
        {
            Items = new List<RuleScopeDto>
            {
                new() { Id = 1, RuleScope = "Tax Rules", IsActive = true },
                new() { Id = 2, RuleScope = "Tax Calculation", IsActive = true }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<RuleScopeDto>>(okResult.Value);
        Assert.Equal(2, returnedResult.Items.Count());
        Assert.Equal(2, returnedResult.TotalCount);
        _serviceMock.Verify(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var queryParameters = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<RuleScopeDto>
        {
            Items = new List<RuleScopeDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<RuleScopeDto>>(okResult.Value);
        Assert.Empty(returnedResult.Items);
        Assert.Equal(0, returnedResult.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithIsActiveFilter_ReturnsFilteredResults()
    {
        // Arrange
        var queryParameters = new RuleScopeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        var pagedResult = new PagedResult<RuleScopeDto>
        {
            Items = new List<RuleScopeDto>
            {
                new() { Id = 1, RuleScope = "Active Rule", IsActive = true },
                new() { Id = 2, RuleScope = "Another Active", IsActive = true }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<RuleScopeDto>>(okResult.Value);
        Assert.All(returnedResult.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAll_WithFilterValidationException_ReturnsBadRequest()
    {
        // Arrange
        var queryParameters = new RuleScopeQueryParameters
        {
            SortBy = "InvalidField"
        };

        var errors = new Dictionary<string, string>
        {
            { "SortBy", "Field 'InvalidField' is not sortable" }
        };

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("Filter validation failed", errors));

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);

        var responseType = badRequestResult.Value.GetType();
        var messageProperty = responseType.GetProperty("message");
        var errorsProperty = responseType.GetProperty("errors");

        Assert.NotNull(messageProperty);
        Assert.NotNull(errorsProperty);

        var messageValue = messageProperty.GetValue(badRequestResult.Value) as string;
        Assert.Equal("Filter validation failed", messageValue);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var queryParameters = new RuleScopeQueryParameters();

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetById Endpoint Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOkWithRuleScope()
    {
        // Arrange
        var ruleScopeId = 1;
        var ruleScopeDto = new RuleScopeDto
        {
            Id = ruleScopeId,
            RuleScope = "Tax Rules",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.GetByIdAsync(ruleScopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleScopeDto);

        // Act
        var result = await _controller.GetById(ruleScopeId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<RuleScopeDto>(okResult.Value);
        Assert.Equal(ruleScopeId, returnedDto.Id);
        Assert.Equal("Tax Rules", returnedDto.RuleScope);
        Assert.True(returnedDto.IsActive);
        _serviceMock.Verify(x => x.GetByIdAsync(ruleScopeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var ruleScopeId = 999;

        _serviceMock.Setup(x => x.GetByIdAsync(ruleScopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeDto?)null);

        // Act
        var result = await _controller.GetById(ruleScopeId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _serviceMock.Verify(x => x.GetByIdAsync(ruleScopeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var ruleScopeId = 1;

        _serviceMock.Setup(x => x.GetByIdAsync(ruleScopeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetById(ruleScopeId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Create Endpoint Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsOkWithApiResponse()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "New Tax Scope",
            IsActive = true,
            CreatedBy = 1
        };

        var createdDto = new RuleScopeDto
        {
            Id = 1,
            RuleScope = "New Tax Scope",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("inserted successfully", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(1, apiResponse.Items.Id);
        Assert.Equal("New Tax Scope", apiResponse.Items.RuleScope);
        _serviceMock.Verify(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithDuplicateRuleScope_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "Existing Scope",
            IsActive = true,
            CreatedBy = 1
        };

        var exception = new InvalidOperationException("Duplicate key violation: RuleScope 'Existing Scope' already exists");
        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithUniqueConstraintViolation_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "Test Scope",
            IsActive = true,
            CreatedBy = 1
        };

        var exception = new Exception("unique constraint violation");
        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsGeneralException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "Test Scope",
            IsActive = true,
            CreatedBy = 1
        };

        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithInactiveRuleScope_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "Inactive Scope",
            IsActive = false,
            CreatedBy = 1
        };

        var createdDto = new RuleScopeDto
        {
            Id = 1,
            RuleScope = "Inactive Scope",
            IsActive = false,
            CreatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.False(apiResponse.Items!.IsActive);
    }

    #endregion

    #region Update Endpoint Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithApiResponse()
    {
        // Arrange
        var ruleScopeId = 1;
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Updated Scope",
            IsActive = true,
            UpdatedBy = 1
        };

        var updatedDto = new RuleScopeDto
        {
            Id = ruleScopeId,
            RuleScope = "Updated Scope",
            IsActive = true,
            UpdatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.UpdateAsync(ruleScopeId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(ruleScopeId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("updated successfully", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(ruleScopeId, apiResponse.Items.Id);
        Assert.Equal("Updated Scope", apiResponse.Items.RuleScope);
        _serviceMock.Verify(x => x.UpdateAsync(ruleScopeId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsOkWithFailureResponse()
    {
        // Arrange
        var ruleScopeId = 999;
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Non-existent",
            IsActive = true,
            UpdatedBy = 1
        };

        _serviceMock.Setup(x => x.UpdateAsync(ruleScopeId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeDto?)null);

        // Act
        var result = await _controller.Update(ruleScopeId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(apiResponse.Items);
    }

    [Fact]
    public async Task Update_WithDuplicateRuleScope_ReturnsConflict()
    {
        // Arrange
        var ruleScopeId = 1;
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Duplicate Scope",
            IsActive = true,
            UpdatedBy = 1
        };

        var exception = new InvalidOperationException("Duplicate constraint violation");
        _serviceMock.Setup(x => x.UpdateAsync(ruleScopeId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Update(ruleScopeId, updateDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var ruleScopeId = 1;
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Test",
            UpdatedBy = 1
        };

        _serviceMock.Setup(x => x.UpdateAsync(ruleScopeId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Update(ruleScopeId, updateDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Update_DeactivateRuleScope_UpdatesSuccessfully()
    {
        // Arrange
        var ruleScopeId = 1;
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Test Scope",
            IsActive = false,
            UpdatedBy = 1
        };

        var updatedDto = new RuleScopeDto
        {
            Id = ruleScopeId,
            RuleScope = "Test Scope",
            IsActive = false,
            UpdatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.UpdateAsync(ruleScopeId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(ruleScopeId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.False(apiResponse.Items!.IsActive);
    }

    #endregion

    #region Delete Endpoint Tests

    [Fact]
    public async Task Delete_WithExistingId_ReturnsOkWithSuccessResponse()
    {
        // Arrange
        var ruleScopeId = 1;

        _serviceMock.Setup(x => x.DeleteAsync(ruleScopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(ruleScopeId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("marked for deletion", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        _serviceMock.Verify(x => x.DeleteAsync(ruleScopeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsOkWithFailureResponse()
    {
        // Arrange
        var ruleScopeId = 999;

        _serviceMock.Setup(x => x.DeleteAsync(ruleScopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(ruleScopeId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        _serviceMock.Verify(x => x.DeleteAsync(ruleScopeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var ruleScopeId = 1;

        _serviceMock.Setup(x => x.DeleteAsync(ruleScopeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Delete(ruleScopeId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Edge Cases and Additional Coverage Tests

    [Fact]
    public async Task GetAll_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var queryParameters = new RuleScopeQueryParameters
        {
            PageNumber = 2,
            PageSize = 5
        };

        var pagedResult = new PagedResult<RuleScopeDto>
        {
            Items = new List<RuleScopeDto>
            {
                new() { Id = 6, RuleScope = "Scope 6" },
                new() { Id = 7, RuleScope = "Scope 7" }
            },
            TotalCount = 12,
            PageNumber = 2,
            PageSize = 5
        };

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<RuleScopeDto>>(okResult.Value);
        Assert.Equal(2, returnedResult.PageNumber);
        Assert.Equal(5, returnedResult.PageSize);
        Assert.Equal(12, returnedResult.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        var queryParameters = new RuleScopeQueryParameters
        {
            SearchTerm = "Tax",
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<RuleScopeDto>
        {
            Items = new List<RuleScopeDto>
            {
                new() { Id = 1, RuleScope = "Tax Rules" },
                new() { Id = 2, RuleScope = "Tax Calculation" }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<RuleScopeDto>>(okResult.Value);
        Assert.All(returnedResult.Items, item =>
            Assert.Contains("Tax", item.RuleScope, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetById_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        var ruleScopeId = 0;

        _serviceMock.Setup(x => x.GetByIdAsync(ruleScopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeDto?)null);

        // Act
        var result = await _controller.GetById(ruleScopeId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        var ruleScopeId = -1;

        _serviceMock.Setup(x => x.GetByIdAsync(ruleScopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleScopeDto?)null);

        // Act
        var result = await _controller.GetById(ruleScopeId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WithExceptionContainingInnerException_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto
        {
            RuleScope = "Test",
            CreatedBy = 1
        };

        var innerException = new Exception("duplicate key value violates unique constraint");
        var exception = new Exception("Outer exception", innerException);

        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Update_WithExceptionContainingInnerException_ReturnsConflict()
    {
        // Arrange
        var ruleScopeId = 1;
        var updateDto = new UpdateRuleScopeDto
        {
            RuleScope = "Test",
            UpdatedBy = 1
        };

        var innerException = new Exception("UNIQUE constraint failed");
        var exception = new Exception("Outer exception", innerException);

        _serviceMock.Setup(x => x.UpdateAsync(ruleScopeId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Update(ruleScopeId, updateDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleScopeDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task GetAll_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var queryParameters = new RuleScopeQueryParameters();
        var cancellationToken = new CancellationToken();
        var pagedResult = new PagedResult<RuleScopeDto>(new List<RuleScopeDto>(), 0, 1, 10);

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, cancellationToken))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(queryParameters, cancellationToken);

        // Assert
        _serviceMock.Verify(x => x.GetAllAsync(queryParameters, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetById_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var ruleScopeId = 1;
        var cancellationToken = new CancellationToken();
        var dto = new RuleScopeDto { Id = ruleScopeId };

        _serviceMock.Setup(x => x.GetByIdAsync(ruleScopeId, cancellationToken))
            .ReturnsAsync(dto);

        // Act
        await _controller.GetById(ruleScopeId, cancellationToken);

        // Assert
        _serviceMock.Verify(x => x.GetByIdAsync(ruleScopeId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Create_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var createDto = new CreateRuleScopeDto { RuleScope = "Test", CreatedBy = 1 };
        var cancellationToken = new CancellationToken();
        var resultDto = new RuleScopeDto { Id = 1 };

        _serviceMock.Setup(x => x.CreateAsync(createDto, cancellationToken))
            .ReturnsAsync(resultDto);

        // Act
        await _controller.Create(createDto, cancellationToken);

        // Assert
        _serviceMock.Verify(x => x.CreateAsync(createDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Update_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var ruleScopeId = 1;
        var updateDto = new UpdateRuleScopeDto { RuleScope = "Test", UpdatedBy = 1 };
        var cancellationToken = new CancellationToken();
        var resultDto = new RuleScopeDto { Id = ruleScopeId };

        _serviceMock.Setup(x => x.UpdateAsync(ruleScopeId, updateDto, cancellationToken))
            .ReturnsAsync(resultDto);

        // Act
        await _controller.Update(ruleScopeId, updateDto, cancellationToken);

        // Assert
        _serviceMock.Verify(x => x.UpdateAsync(ruleScopeId, updateDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Delete_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var ruleScopeId = 1;
        var cancellationToken = new CancellationToken();

        _serviceMock.Setup(x => x.DeleteAsync(ruleScopeId, cancellationToken))
            .ReturnsAsync(true);

        // Act
        await _controller.Delete(ruleScopeId, cancellationToken);

        // Assert
        _serviceMock.Verify(x => x.DeleteAsync(ruleScopeId, cancellationToken), Times.Once);
    }

    #endregion
}
