using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Exceptions;
using Xunit;
using NtisPlatform.Application.DTOs.Master.RuleOperatorMaster;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Comprehensive unit tests for RuleOperatorController
/// Tests all CRUD API endpoints with >90% branch coverage
/// </summary>
public class RuleOperatorControllerTests
{
    private readonly Mock<IRuleOperatorService> _serviceMock;
    private readonly Mock<ILogger<RuleOperatorController>> _loggerMock;
    private readonly RuleOperatorController _controller;

    public RuleOperatorControllerTests()
    {
        _serviceMock = new Mock<IRuleOperatorService>();
        _loggerMock = new Mock<ILogger<RuleOperatorController>>();
        _controller = new RuleOperatorController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Endpoint Tests

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParameters = new RuleOperatorQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            OperatorDescription = "Equals"
        };

        var pagedResult = new PagedResult<RuleOperatorDto>
        {
            Items = new List<RuleOperatorDto>
            {
                new() { Id = 1, Operator = "=", OperatorDescription = "Equals", IsActive = true },
                new() { Id = 2, Operator = "==", OperatorDescription = "Equals Strict", IsActive = true }
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
        var returnedResult = Assert.IsType<PagedResult<RuleOperatorDto>>(okResult.Value);
        Assert.Equal(2, returnedResult.Items.Count());
        Assert.Equal(2, returnedResult.TotalCount);
        _serviceMock.Verify(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var queryParameters = new RuleOperatorQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<RuleOperatorDto>
        {
            Items = new List<RuleOperatorDto>(),
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
        var returnedResult = Assert.IsType<PagedResult<RuleOperatorDto>>(okResult.Value);
        Assert.Empty(returnedResult.Items);
        Assert.Equal(0, returnedResult.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithIsActiveFilter_ReturnsFilteredResults()
    {
        // Arrange
        var queryParameters = new RuleOperatorQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        var pagedResult = new PagedResult<RuleOperatorDto>
        {
            Items = new List<RuleOperatorDto>
            {
                new() { Id = 1, Operator = "=", OperatorDescription = "Equals", IsActive = true },
                new() { Id = 2, Operator = ">", OperatorDescription = "Greater Than", IsActive = true }
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
        var returnedResult = Assert.IsType<PagedResult<RuleOperatorDto>>(okResult.Value);
        Assert.All(returnedResult.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAll_WithFilterValidationException_ReturnsBadRequest()
    {
        // Arrange
        var queryParameters = new RuleOperatorQueryParameters
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
        var queryParameters = new RuleOperatorQueryParameters();

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetById Endpoint Tests

    [Fact]
    public async Task GetById_WithExistingId_ReturnsOkWithRuleOperator()
    {
        // Arrange
        var operatorId = 1;
        var operatorDto = new RuleOperatorDto
        {
            Id = operatorId,
            Operator = "=",
            OperatorDescription = "Equals",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.GetByIdAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operatorDto);

        // Act
        var result = await _controller.GetById(operatorId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<RuleOperatorDto>(okResult.Value);
        Assert.Equal(operatorId, returnedDto.Id);
        Assert.Equal("=", returnedDto.Operator);
        Assert.Equal("Equals", returnedDto.OperatorDescription);
        Assert.True(returnedDto.IsActive);
        _serviceMock.Verify(x => x.GetByIdAsync(operatorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var operatorId = 999;

        _serviceMock.Setup(x => x.GetByIdAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorDto?)null);

        // Act
        var result = await _controller.GetById(operatorId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _serviceMock.Verify(x => x.GetByIdAsync(operatorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var operatorId = 1;

        _serviceMock.Setup(x => x.GetByIdAsync(operatorId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetById(operatorId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task GetById_WithInactiveOperator_ReturnsOkWithInactiveFlag()
    {
        // Arrange
        var operatorId = 5;
        var operatorDto = new RuleOperatorDto
        {
            Id = operatorId,
            Operator = "DEPRECATED",
            OperatorDescription = "Old Operator",
            IsActive = false,
            CreatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.GetByIdAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operatorDto);

        // Act
        var result = await _controller.GetById(operatorId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<RuleOperatorDto>(okResult.Value);
        Assert.False(returnedDto.IsActive);
    }

    #endregion

    #region Create Endpoint Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsOkWithApiResponse()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = ">=",
            OperatorDescription = "Greater Than or Equal",
            IsActive = true,
            CreatedBy = 1
        };

        var createdDto = new RuleOperatorDto
        {
            Id = 1,
            Operator = ">=",
            OperatorDescription = "Greater Than or Equal",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("inserted successfully", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(1, apiResponse.Items.Id);
        Assert.Equal(">=", apiResponse.Items.Operator);
        _serviceMock.Verify(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithDuplicateOperator_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "=",

            OperatorDescription = "Equals",
            IsActive = true,
            CreatedBy = 1
        };

        var exception = new InvalidOperationException("Duplicate key violation: RuleOperator '=' already exists");
        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithUniqueConstraintViolation_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "LIKE",
            OperatorDescription = "Pattern Match",
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
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsGeneralException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "IN",
            OperatorDescription = "In List",
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
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithInactiveOperator_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "OLD_OP",
            OperatorDescription = "Deprecated Operator",
            IsActive = false,
            CreatedBy = 1
        };

        var createdDto = new RuleOperatorDto
        {
            Id = 1,
            Operator = "OLD_OP",
            OperatorDescription = "Deprecated Operator",
            IsActive = false,
            CreatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.False(apiResponse.Items!.IsActive);
    }

    #endregion

    #region Update Endpoint Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithApiResponse()
    {
        // Arrange
        var operatorId = 1;
        var updateDto = new UpdateRuleOperatorDto
        {
            Operator = "==",
            OperatorDescription = "Equals (Updated)",
            IsActive = true,
            UpdatedBy = 1
        };

        var updatedDto = new RuleOperatorDto
        {
            Id = operatorId,
            Operator = "==",
            OperatorDescription = "Equals (Updated)",
            IsActive = true,
            UpdatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.UpdateAsync(operatorId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(operatorId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("updated successfully", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal("==", apiResponse.Items.Operator);
        _serviceMock.Verify(x => x.UpdateAsync(operatorId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsOkWithFailureResponse()
    {
        // Arrange
        var operatorId = 999;
        var updateDto = new UpdateRuleOperatorDto
        {
            Operator = "=",
            OperatorDescription = "Equals",
            UpdatedBy = 1
        };

        _serviceMock.Setup(x => x.UpdateAsync(operatorId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorDto?)null);

        // Act
        var result = await _controller.Update(operatorId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WithDuplicateOperator_ReturnsConflict()
    {
        // Arrange
        var operatorId = 1;
        var updateDto = new UpdateRuleOperatorDto
        {
            Operator = "=",
            OperatorDescription = "Equals",
            UpdatedBy = 1
        };

        var exception = new InvalidOperationException("Duplicate key violation");
        _serviceMock.Setup(x => x.UpdateAsync(operatorId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Update(operatorId, updateDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var operatorId = 1;
        var updateDto = new UpdateRuleOperatorDto
        {
            Operator = "=",
            OperatorDescription = "Equals",
            UpdatedBy = 1
        };

        _serviceMock.Setup(x => x.UpdateAsync(operatorId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Update(operatorId, updateDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Update_DeactivatingOperator_UpdatesSuccessfully()
    {
        // Arrange
        var operatorId = 1;
        var updateDto = new UpdateRuleOperatorDto
        {
            Operator = "OLD",
            OperatorDescription = "Old Operator",
            IsActive = false,
            UpdatedBy = 1
        };

        var updatedDto = new RuleOperatorDto
        {
            Id = operatorId,
            Operator = "OLD",
            OperatorDescription = "Old Operator",
            IsActive = false,
            UpdatedDate = DateTime.Now
        };

        _serviceMock.Setup(x => x.UpdateAsync(operatorId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(operatorId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.False(apiResponse.Items!.IsActive);
    }

    #endregion

    #region Delete Endpoint Tests

    [Fact]
    public async Task Delete_WithExistingId_ReturnsOkWithSuccessResponse()
    {
        // Arrange
        var operatorId = 1;

        _serviceMock.Setup(x => x.DeleteAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(operatorId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("marked for deletion", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        _serviceMock.Verify(x => x.DeleteAsync(operatorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsOkWithFailureResponse()
    {
        // Arrange
        var operatorId = 999;

        _serviceMock.Setup(x => x.DeleteAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(operatorId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        _serviceMock.Verify(x => x.DeleteAsync(operatorId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithForeignKeyConstraint_ReturnsConflict()
    {
        // Arrange
        var operatorId = 1;

        var exception = new InvalidOperationException("Cannot delete: foreign key constraint violation");
        _serviceMock.Setup(x => x.DeleteAsync(operatorId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.Delete(operatorId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var operatorId = 1;

        _serviceMock.Setup(x => x.DeleteAsync(operatorId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Delete(operatorId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<RuleOperatorDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Pagination Tests

    [Fact]
    public async Task GetAll_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var queryParameters = new RuleOperatorQueryParameters
        {
            PageNumber = 2,
            PageSize = 5
        };

        var pagedResult = new PagedResult<RuleOperatorDto>
        {
            Items = new List<RuleOperatorDto>
            {
                new() { Id = 6, Operator = "IN", OperatorDescription = "In List", IsActive = true },
                new() { Id = 7, Operator = "NOT IN", OperatorDescription = "Not In List", IsActive = true }
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
        var returnedResult = Assert.IsType<PagedResult<RuleOperatorDto>>(okResult.Value);
        Assert.Equal(2, returnedResult.PageNumber);
        Assert.Equal(5, returnedResult.PageSize);
        Assert.Equal(12, returnedResult.TotalCount);
    }

    #endregion

    #region Search and Filter Tests

    [Fact]
    public async Task GetAll_WithOperatorFilter_ReturnsMatchingOperators()
    {
        // Arrange
        var queryParameters = new RuleOperatorQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            OperatorDescription = "Pattern"
        };

        var pagedResult = new PagedResult<RuleOperatorDto>
        {
            Items = new List<RuleOperatorDto>
            {
                new() { Id = 1, Operator = "LIKE", OperatorDescription = "Pattern Match", IsActive = true }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<RuleOperatorDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
        Assert.Equal("LIKE", returnedResult.Items.First().Operator);
    }

    #endregion
}
