using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.PropertyWorkflowStageMaster;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Unit tests for PropertyWorkflowStageMasterController
/// </summary>
public class PropertyWorkflowStageMasterControllerTests
{
    private readonly Mock<IPropertyWorkflowStageMasterService> _serviceMock;
    private readonly Mock<ILogger<PropertyWorkflowStageMasterController>> _loggerMock;
    private readonly PropertyWorkflowStageMasterController _controller;

    public PropertyWorkflowStageMasterControllerTests()
    {
        _serviceMock = new Mock<IPropertyWorkflowStageMasterService>();
        _loggerMock = new Mock<ILogger<PropertyWorkflowStageMasterController>>();
        _controller = new PropertyWorkflowStageMasterController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithDefaultParameters_ReturnsOkResult()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters();
        var pagedResult = new PagedResult<PropertyWorkflowStageMasterDto>
        {
            Items = new List<PropertyWorkflowStageMasterDto>
            {
                new PropertyWorkflowStageMasterDto { Id = 1, StageName = "GeoSequencing", DisplayOrder = 1, IsActive = true }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<PropertyWorkflowStageMasterQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        _serviceMock.Verify(s => s.GetAllAsync(It.IsAny<PropertyWorkflowStageMasterQueryParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters { StageName = "Geo", IsActive = true };
        var pagedResult = new PagedResult<PropertyWorkflowStageMasterDto>
        {
            Items = new List<PropertyWorkflowStageMasterDto>
            {
                new PropertyWorkflowStageMasterDto { Id = 1, StageName = "GeoSequencing", DisplayOrder = 1, IsActive = true }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<PropertyWorkflowStageMasterQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters { PageNumber = 2, PageSize = 5 };
        var pagedResult = new PagedResult<PropertyWorkflowStageMasterDto>
        {
            Items = new List<PropertyWorkflowStageMasterDto>(),
            TotalCount = 0,
            PageNumber = 2,
            PageSize = 5
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<PropertyWorkflowStageMasterQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithEmptyResults_ReturnsOkWithEmptyList()
    {
        // Arrange
        var queryParams = new PropertyWorkflowStageMasterQueryParameters();
        var pagedResult = new PagedResult<PropertyWorkflowStageMasterDto>
        {
            Items = new List<PropertyWorkflowStageMasterDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<PropertyWorkflowStageMasterQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var dto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            IsActive = true,
            Description = "First stage"
        };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        _serviceMock.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowStageMasterDto?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _serviceMock.Verify(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowStageMasterDto?)null);

        // Act
        var result = await _controller.GetById(0, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowStageMasterDto?)null);

        // Act
        var result = await _controller.GetById(-1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "Initial stage",
            CreatedBy = 1
        };
        var createdDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            IsActive = true,
            Description = "Initial stage"
        };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        _serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithAllRequiredFields_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "InternalSurvey",
            DisplayOrder = 2,
            CreatedBy = 1
        };
        var createdDto = new PropertyWorkflowStageMasterDto
        {
            Id = 2,
            StageName = "InternalSurvey",
            DisplayOrder = 2,
            IsActive = true
        };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithOptionalDescription_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "Assessment",
            DisplayOrder = 3,
            Description = "Assessment stage for property",
            CreatedBy = 1
        };
        var createdDto = new PropertyWorkflowStageMasterDto
        {
            Id = 3,
            StageName = "Assessment",
            DisplayOrder = 3,
            Description = "Assessment stage for property",
            IsActive = true
        };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing Updated",
            DisplayOrder = 1,
            Description = "Updated description",
            UpdatedBy = 2
        };
        var updatedDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing Updated",
            DisplayOrder = 1,
            Description = "Updated description",
            IsActive = true
        };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        _serviceMock.Verify(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "Updated",
            DisplayOrder = 1,
            UpdatedBy = 2
        };
        _serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowStageMasterDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithChangedDisplayOrder_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing",
            DisplayOrder = 5,
            UpdatedBy = 2
        };
        var updatedDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 5,
            IsActive = true
        };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithOnlyDescriptionChange_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "New description",
            UpdatedBy = 2
        };
        var updatedDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            Description = "New description",
            IsActive = true
        };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithZeroId_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(0, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_MultipleIds_VerifiesEachCall()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        foreach (var id in ids)
        {
            _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        // Act
        foreach (var id in ids)
        {
            var result = await _controller.Delete(id, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        // Assert
        foreach (var id in ids)
        {
            _serviceMock.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteWorkflow_CreateUpdateDelete_AllReturnOk()
    {
        // Arrange - Create
        var createDto = new CreatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            CreatedBy = 1
        };
        var createdDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing",
            DisplayOrder = 1,
            IsActive = true
        };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act - Create
        var createResult = await _controller.Create(createDto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(createResult);

        // Arrange - Update
        var updateDto = new UpdatePropertyWorkflowStageMasterDto
        {
            StageName = "GeoSequencing Updated",
            DisplayOrder = 1,
            UpdatedBy = 2
        };
        var updatedDto = new PropertyWorkflowStageMasterDto
        {
            Id = 1,
            StageName = "GeoSequencing Updated",
            DisplayOrder = 1,
            IsActive = true
        };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act - Update
        var updateResult = await _controller.Update(1, updateDto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(updateResult);

        // Arrange - Delete
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - Delete
        var deleteResult = await _controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(deleteResult);

        // Assert
        _serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowStageMasterDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
