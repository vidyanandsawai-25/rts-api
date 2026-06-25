using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Property.PropertyWorkflowDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Unit tests for PropertyController workflow-details endpoints (PropertyController.WorkflowDetails.cs)
/// </summary>
public class PropertyControllerWorkflowDetailsTests
{
    private readonly Mock<IPropertyWorkflowDetailsService> _serviceMock;
    private readonly PropertyController _controller;

    public PropertyControllerWorkflowDetailsTests()
    {
        _serviceMock = new Mock<IPropertyWorkflowDetailsService>();
        var propertyService = new Mock<IPropertyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        _controller = PropertyControllerTestHelper.CreateController(propertyService, logger, workflowDetailsService: _serviceMock);
    }

    #region GetWorkflowDetailsByPropertyId Tests

    [Fact]
    public async Task GetWorkflowDetailsByPropertyId_WithExistingRecords_ReturnsOkWithItems()
    {
        // Arrange
        var dtos = new List<PropertyWorkflowDetailsDto>
        {
            new PropertyWorkflowDetailsDto { Id = 2, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = true },
            new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 1, CurrentStatus = false }
        };
        _serviceMock.Setup(s => s.GetByPropertyIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtos);

        // Act
        var result = await _controller.GetWorkflowDetailsByPropertyId(10, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertyWorkflowDetailsDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(2, response.Items.Count);
        _serviceMock.Verify(s => s.GetByPropertyIdAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkflowDetailsByPropertyId_WithNoRecords_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByPropertyIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertyWorkflowDetailsDto>());

        // Act
        var result = await _controller.GetWorkflowDetailsByPropertyId(99, CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertyWorkflowDetailsDto>>>(notFound.Value);
        Assert.False(response.Success);
        _serviceMock.Verify(s => s.GetByPropertyIdAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkflowDetailsByPropertyId_WithNullResult_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByPropertyIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<PropertyWorkflowDetailsDto>?)null!);

        // Act
        var result = await _controller.GetWorkflowDetailsByPropertyId(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetWorkflowDetailsByPropertyId_WithSingleRecord_ReturnsOk()
    {
        // Arrange
        var dtos = new List<PropertyWorkflowDetailsDto>
        {
            new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 5, WorkflowStageId = 1, CurrentStatus = true }
        };
        _serviceMock.Setup(s => s.GetByPropertyIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(dtos);

        // Act
        var result = await _controller.GetWorkflowDetailsByPropertyId(5, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region GetWorkflowDetailsById Tests

    [Fact]
    public async Task GetWorkflowDetailsById_WithValidId_ReturnsOkWithDto()
    {
        // Arrange
        var dto = new PropertyWorkflowDetailsDto
        {
            Id = 1,
            PropertyId = 10,
            WorkflowStageId = 2,
            CurrentStatus = true
        };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetWorkflowDetailsById(1, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyWorkflowDetailsDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(1, response.Items.Id);
        _serviceMock.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkflowDetailsById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsDto?)null);

        // Act
        var result = await _controller.GetWorkflowDetailsById(999, CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyWorkflowDetailsDto>>(notFound.Value);
        Assert.False(response.Success);
        _serviceMock.Verify(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkflowDetailsById_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsDto?)null);

        // Act
        var result = await _controller.GetWorkflowDetailsById(0, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetWorkflowDetailsById_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(-1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsDto?)null);

        // Act
        var result = await _controller.GetWorkflowDetailsById(-1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region CreateWorkflowDetails Tests

    [Fact]
    public async Task CreateWorkflowDetails_WithValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowDetailsDto
        {
            PropertyId = 10,
            WorkflowStageId = 2,
            ModuleId = 3,
            CreatedBy = 1
        };
        var createdDto = new PropertyWorkflowDetailsDto
        {
            Id = 1,
            PropertyId = 10,
            WorkflowStageId = 2,
            ModuleId = 3,
            CurrentStatus = true
        };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.CreateWorkflowDetails(10, createDto, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(_controller.GetWorkflowDetailsById), created.ActionName);
        Assert.Equal(1, created.RouteValues!["id"]);
        var response = Assert.IsType<ApiResponse<PropertyWorkflowDetailsDto>>(created.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.True(response.Items.CurrentStatus);
        _serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateWorkflowDetails_SetsPropertyIdFromRoute()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowDetailsDto { WorkflowStageId = 2, CreatedBy = 1 };
        var createdDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 15, WorkflowStageId = 2, CurrentStatus = true };

        CreatePropertyWorkflowDetailsDto? capturedDto = null;
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .Callback<CreatePropertyWorkflowDetailsDto, CancellationToken>((dto, _) => capturedDto = dto)
            .ReturnsAsync(createdDto);

        // Act
        await _controller.CreateWorkflowDetails(15, createDto, CancellationToken.None);

        // Assert — PropertyId must be set from the route parameter, not the body
        Assert.NotNull(capturedDto);
        Assert.Equal(15, capturedDto!.PropertyId);
    }

    [Fact]
    public async Task CreateWorkflowDetails_WithNullModuleId_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreatePropertyWorkflowDetailsDto { WorkflowStageId = 2, ModuleId = null, CreatedBy = 1 };
        var createdDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, ModuleId = null, CurrentStatus = true };

        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.CreateWorkflowDetails(10, createDto, CancellationToken.None);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
    }

    #endregion

    #region UpdateWorkflowDetails Tests

    [Fact]
    public async Task UpdateWorkflowDetails_WithValidData_ReturnsOkWithUpdatedDto()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowDetailsDto
        {
            WorkflowStageId = 3,
            ModuleId = 5,
            CurrentStatus = false,
            UpdatedBy = 2
        };
        var updatedDto = new PropertyWorkflowDetailsDto
        {
            Id = 1,
            PropertyId = 10,
            WorkflowStageId = 3,
            ModuleId = 5,
            CurrentStatus = false
        };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.UpdateWorkflowDetails(1, updateDto, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyWorkflowDetailsDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Items);
        Assert.Equal(3, response.Items.WorkflowStageId);
        Assert.False(response.Items.CurrentStatus);
        _serviceMock.Verify(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateWorkflowDetails_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowDetailsDto { WorkflowStageId = 3, UpdatedBy = 2 };
        _serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyWorkflowDetailsDto?)null);

        // Act
        var result = await _controller.UpdateWorkflowDetails(999, updateDto, CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyWorkflowDetailsDto>>(notFound.Value);
        Assert.False(response.Success);
        _serviceMock.Verify(s => s.UpdateAsync(999, It.IsAny<UpdatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateWorkflowDetails_WithCurrentStatusChange_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowDetailsDto { WorkflowStageId = 2, CurrentStatus = false, UpdatedBy = 2 };
        var updatedDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = false };

        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.UpdateWorkflowDetails(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateWorkflowDetails_WithNullModuleId_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdatePropertyWorkflowDetailsDto { WorkflowStageId = 2, ModuleId = null, UpdatedBy = 2 };
        var updatedDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, ModuleId = null };

        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.UpdateWorkflowDetails(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    #endregion

    #region DeleteWorkflowDetails Tests

    [Fact]
    public async Task DeleteWorkflowDetails_WithValidId_ReturnsOk()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteWorkflowDetails(1, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.True(response.Success);
        _serviceMock.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWorkflowDetails_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteWorkflowDetails(999, CancellationToken.None);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(notFound.Value);
        Assert.False(response.Success);
        _serviceMock.Verify(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWorkflowDetails_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.DeleteAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteWorkflowDetails(0, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteWorkflowDetails_MultipleIds_VerifiesEachCall()
    {
        // Arrange
        var ids = new[] { 1, 2, 3 };
        foreach (var id in ids)
        {
            _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        // Act & Assert
        foreach (var id in ids)
        {
            var result = await _controller.DeleteWorkflowDetails(id, CancellationToken.None);
            Assert.IsType<OkObjectResult>(result);
        }

        foreach (var id in ids)
        {
            _serviceMock.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CompleteWorkflow_CreateGetUpdateDelete_AllReturnExpectedStatus()
    {
        // Arrange - Create
        var createDto = new CreatePropertyWorkflowDetailsDto { WorkflowStageId = 1, CreatedBy = 1 };
        var createdDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 1, CurrentStatus = true };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act - Create
        var createResult = await _controller.CreateWorkflowDetails(10, createDto, CancellationToken.None);
        Assert.IsType<CreatedAtActionResult>(createResult);

        // Arrange - GetById
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(createdDto);

        // Act - GetById
        var getResult = await _controller.GetWorkflowDetailsById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(getResult);

        // Arrange - Update
        var updateDto = new UpdatePropertyWorkflowDetailsDto { WorkflowStageId = 2, CurrentStatus = false, UpdatedBy = 2 };
        var updatedDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 2, CurrentStatus = false };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act - Update
        var updateResult = await _controller.UpdateWorkflowDetails(1, updateDto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(updateResult);

        // Arrange - Delete
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act - Delete
        var deleteResult = await _controller.DeleteWorkflowDetails(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(deleteResult);

        // Assert final verify
        _serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByPropertyId_AfterCreate_ReflectsNewRecord()
    {
        // Arrange - Create
        var createDto = new CreatePropertyWorkflowDetailsDto { WorkflowStageId = 1, CreatedBy = 1 };
        var createdDto = new PropertyWorkflowDetailsDto { Id = 1, PropertyId = 10, WorkflowStageId = 1, CurrentStatus = true };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyWorkflowDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        await _controller.CreateWorkflowDetails(10, createDto, CancellationToken.None);

        // Arrange - GetByPropertyId
        var allDtos = new List<PropertyWorkflowDetailsDto> { createdDto };
        _serviceMock.Setup(s => s.GetByPropertyIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(allDtos);

        // Act
        var result = await _controller.GetWorkflowDetailsByPropertyId(10, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<PropertyWorkflowDetailsDto>>>(ok.Value);
        Assert.Single(response.Items!);
        Assert.True(response.Items![0].CurrentStatus);
    }

    #endregion
}
