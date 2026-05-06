using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive unit tests for RuleEffectTypeController
/// Tests all CRUD operations and HTTP status code handling
/// </summary>
public class RuleEffectTypeControllerTests
{
    private readonly Mock<IRuleEffectTypeService> _mockService;
    private readonly Mock<ILogger<RuleEffectTypeController>> _mockLogger;
    private readonly RuleEffectTypeController _controller;

    public RuleEffectTypeControllerTests()
    {
        _mockService = new Mock<IRuleEffectTypeService>();
        _mockLogger = new Mock<ILogger<RuleEffectTypeController>>();
        _controller = new RuleEffectTypeController(_mockService.Object, _mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkResult()
    {
        // Arrange
        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<RuleEffectTypeDto>(
            new List<RuleEffectTypeDto>
            {
                new() { Id = 1, EffectType = "Add", IsActive = true },
                new() { Id = 2, EffectType = "Multiply", IsActive = true }
            },
            2, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var returnedResult = okResult.Value as PagedResult<RuleEffectTypeDto>;
        Assert.NotNull(returnedResult);
        Assert.Equal(2, returnedResult.TotalCount);
        Assert.Equal(2, returnedResult.Items.Count());

        _mockService.Verify(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithFiltering_ReturnsFilteredResults()
    {
        // Arrange
        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true,
            EffectType = "Add"
        };

        var pagedResult = new PagedResult<RuleEffectTypeDto>(
            new List<RuleEffectTypeDto>
            {
                new() { Id = 1, EffectType = "Add", IsActive = true }
            },
            1, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = okResult.Value as PagedResult<RuleEffectTypeDto>;
        Assert.NotNull(returnedResult);
        Assert.Single(returnedResult.Items);
        Assert.All(returnedResult.Items, item => Assert.Equal("Add", item.EffectType));
    }

    [Fact]
    public async Task GetAll_EmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<RuleEffectTypeDto>(
            new List<RuleEffectTypeDto>(),
            0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = okResult.Value as PagedResult<RuleEffectTypeDto>;
        Assert.NotNull(returnedResult);
        Assert.Empty(returnedResult.Items);
        Assert.Equal(0, returnedResult.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var queryParams = new RuleEffectTypeQueryParameters
        {
            PageNumber = 2,
            PageSize = 5
        };

        var pagedResult = new PagedResult<RuleEffectTypeDto>(
            new List<RuleEffectTypeDto>
            {
                new() { Id = 6, EffectType = "Effect6", IsActive = true },
                new() { Id = 7, EffectType = "Effect7", IsActive = true }
            },
            12, 2, 5);

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = okResult.Value as PagedResult<RuleEffectTypeDto>;
        Assert.NotNull(returnedResult);
        Assert.Equal(2, returnedResult.PageNumber);
        Assert.Equal(5, returnedResult.PageSize);
        Assert.Equal(12, returnedResult.TotalCount);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkResult()
    {
        // Arrange
        var dto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true
        };

        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = okResult.Value as RuleEffectTypeDto;
        Assert.NotNull(returnedDto);
        Assert.Equal(1, returnedDto.Id);
        Assert.Equal("Add", returnedDto.EffectType);

        _mockService.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleEffectTypeDto?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _mockService.Verify(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task GetById_VariousValidIds_ReturnsCorrectDto(int id)
    {
        // Arrange
        var dto = new RuleEffectTypeDto
        {
            Id = id,
            EffectType = $"Effect{id}",
            IsActive = true
        };

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = okResult.Value as RuleEffectTypeDto;
        Assert.NotNull(returnedDto);
        Assert.Equal(id, returnedDto.Id);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = "Add",
            IsActive = true,
            CreatedBy = 1
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Add",
            IsActive = true
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Add")]
    [InlineData("Multiply")]
    [InlineData("Subtract")]
    [InlineData("Divide")]
    public async Task Create_DifferentEffectTypes_CreatesSuccessfully(string effectType)
    {
        // Arrange
        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = effectType,
            IsActive = true,
            CreatedBy = 1
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = effectType,
            IsActive = true
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Create_InactiveEntity_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRuleEffectTypeDto
        {
            EffectType = "Add",
            IsActive = false,
            CreatedBy = 1
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Add",
            IsActive = false
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingId_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Multiply",
            IsActive = true,
            UpdatedBy = 1
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Multiply",
            IsActive = true
        };

        _mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockService.Verify(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsOkWithFailureMessage()
    {
        // Arrange
        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Multiply",
            IsActive = true,
            UpdatedBy = 1
        };

        _mockService.Setup(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleEffectTypeDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockService.Verify(s => s.UpdateAsync(999, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_DeactivatingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Add",
            IsActive = false,
            UpdatedBy = 1
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Add",
            IsActive = false
        };

        _mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task Update_ChangingEffectType_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRuleEffectTypeDto
        {
            EffectType = "Multiply",
            IsActive = true,
            UpdatedBy = 1
        };

        var resultDto = new RuleEffectTypeDto
        {
            Id = 1,
            EffectType = "Multiply",
            IsActive = true
        };

        _mockService.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = okResult.Value as ApiResponse<RuleEffectTypeDto>;
        // Note: The actual response structure depends on the ExecuteUpdate extension method
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingId_ReturnsOkResult()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockService.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsOkWithFailureMessage()
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        _mockService.Verify(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Delete_VariousValidIds_DeletesSuccessfully(int id)
    {
        // Arrange
        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region Integration and Edge Case Tests

    [Fact]
    public async Task Controller_CancellationTokenPropagation_PassesToService()
    {
        // Arrange
        var queryParams = new RuleEffectTypeQueryParameters { PageNumber = 1, PageSize = 10 };
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var pagedResult = new PagedResult<RuleEffectTypeDto>(
            new List<RuleEffectTypeDto>(),
            0, 1, 10);

        _mockService.Setup(s => s.GetAllAsync(queryParams, cancellationToken))
            .ReturnsAsync(pagedResult);

        // Act
        await _controller.GetAll(queryParams, cancellationToken);

        // Assert
        _mockService.Verify(s => s.GetAllAsync(queryParams, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAll_ServiceThrowsException_Returns500StatusCode()
    {
        // Arrange
        var queryParams = new RuleEffectTypeQueryParameters { PageNumber = 1, PageSize = 10 };

        _mockService.Setup(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        // ExecuteGetAllPaged catches exceptions and returns 500 Internal Server Error
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);

        _mockService.Verify(s => s.GetAllAsync(queryParams, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Controller_HasCorrectRouteAttribute()
    {
        // Assert
        var controllerType = typeof(RuleEffectTypeController);
        var routeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RouteAttribute), false)
            .FirstOrDefault() as Microsoft.AspNetCore.Mvc.RouteAttribute;

        Assert.NotNull(routeAttribute);
        Assert.Equal("api/[controller]", routeAttribute.Template);
    }

    [Fact]
    public void Controller_HasAuthorizeAttribute()
    {
        // Assert
        var controllerType = typeof(RuleEffectTypeController);
        var authorizeAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
            .FirstOrDefault();

        Assert.NotNull(authorizeAttribute);
    }

    [Fact]
    public void Controller_HasApiControllerAttribute()
    {
        // Assert
        var controllerType = typeof(RuleEffectTypeController);
        var apiControllerAttribute = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.ApiControllerAttribute), false)
            .FirstOrDefault();

        Assert.NotNull(apiControllerAttribute);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        // Arrange & Act
        var controller = new RuleEffectTypeController(_mockService.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(controller);
    }

    // Note: Constructor does not perform null checks as dependencies are guaranteed by DI container

    #endregion
}
