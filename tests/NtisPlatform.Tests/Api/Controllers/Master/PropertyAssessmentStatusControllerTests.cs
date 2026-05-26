using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.PropertyAssessmentStatus;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Comprehensive tests for PropertyAssessmentStatusController
/// Covers all controller actions for 100% line coverage
/// </summary>
public class PropertyAssessmentStatusControllerTests
{
    private static PropertyAssessmentStatusController Create(
        out Mock<IPropertyAssessmentStatusService> service,
        out Mock<IHardDeleteCleanupService> cleanup,
        out Mock<IReferenceValidationService> referenceValidation)
    {
        service = new Mock<IPropertyAssessmentStatusService>();
        cleanup = new Mock<IHardDeleteCleanupService>();
        referenceValidation = new Mock<IReferenceValidationService>();
        var logger = new Mock<ILogger<PropertyAssessmentStatusController>>();
        return new PropertyAssessmentStatusController(service.Object, cleanup.Object, referenceValidation.Object, logger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var query = new PropertyAssessmentStatusQueryParameters { PageNumber = 1, PageSize = 10 };
        var pagedResult = new PagedResult<PropertyAssessmentStatusDto>(
            new List<PropertyAssessmentStatusDto>
            {
                new PropertyAssessmentStatusDto { Id = 1, StatusName = "Pending" },
                new PropertyAssessmentStatusDto { Id = 2, StatusName = "Approved" }
            }, 
            2, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<PropertyAssessmentStatusDto>>(okResult.Value);
        Assert.Equal(2, returnedData.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var query = new PropertyAssessmentStatusQueryParameters();
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<PropertyAssessmentStatusDto>(new List<PropertyAssessmentStatusDto>(), 0, 1, 10));

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var query = new PropertyAssessmentStatusQueryParameters();
        var cancellationToken = new CancellationToken();

        service.Setup(s => s.GetAllAsync(query, cancellationToken))
            .ReturnsAsync(new PagedResult<PropertyAssessmentStatusDto>(new List<PropertyAssessmentStatusDto>(), 0, 1, 10));

        // Act
        await controller.GetAll(query, cancellationToken);

        // Assert
        service.Verify(s => s.GetAllAsync(query, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithFilterParameters_ReturnsFilteredResults()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var query = new PropertyAssessmentStatusQueryParameters 
        { 
            IsActive = true,
            StatusName = "Approved" 
        };
        var pagedResult = new PagedResult<PropertyAssessmentStatusDto>(
            new List<PropertyAssessmentStatusDto>
            {
                new PropertyAssessmentStatusDto { Id = 1, StatusName = "Approved", IsActive = true }
            }, 
            1, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAll_WithIdFilter_ReturnsFilteredResults()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var query = new PropertyAssessmentStatusQueryParameters 
        { 
            Id = 1
        };
        var pagedResult = new PagedResult<PropertyAssessmentStatusDto>(
            new List<PropertyAssessmentStatusDto>
            {
                new PropertyAssessmentStatusDto { Id = 1, StatusName = "Specific Status", IsActive = true }
            }, 
            1, 1, 10);

        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedData = Assert.IsType<PagedResult<PropertyAssessmentStatusDto>>(okResult.Value);
        Assert.Single(returnedData.Items);
        Assert.Equal(1, returnedData.Items.First().Id);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithDto()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var dto = new PropertyAssessmentStatusDto { Id = 1, StatusName = "Pending Assessment" };
        service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var result = await controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<PropertyAssessmentStatusDto>(okResult.Value);
        Assert.Equal(1, returnedDto.Id);
        Assert.Equal("Pending Assessment", returnedDto.StatusName);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((PropertyAssessmentStatusDto?)null);

        // Act
        var result = await controller.GetById(99, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.GetByIdAsync(1, cancellationToken)).ReturnsAsync((PropertyAssessmentStatusDto?)null);

        // Act
        await controller.GetById(1, cancellationToken);

        // Assert
        service.Verify(s => s.GetByIdAsync(1, cancellationToken), Times.Once);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsOkWithCreatedDto()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var createDto = new CreatePropertyAssessmentStatusDto { StatusName = "New Status" };
        var createdDto = new PropertyAssessmentStatusDto { Id = 1, StatusName = "New Status" };
        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(createdDto);

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<PropertyAssessmentStatusDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(1, response.Items!.Id);
    }

    [Fact]
    public async Task Create_WithIsActiveTrue_CreatesActiveStatus()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var createDto = new CreatePropertyAssessmentStatusDto 
        { 
            StatusName = "Active Status",
            IsActive = true 
        };
        var createdDto = new PropertyAssessmentStatusDto { Id = 1, StatusName = "Active Status", IsActive = true };
        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(createdDto);

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<PropertyAssessmentStatusDto>>(okResult.Value);
        Assert.True(response.Items!.IsActive);
    }

    [Fact]
    public async Task Create_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var createDto = new CreatePropertyAssessmentStatusDto { StatusName = "Test" };
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.CreateAsync(createDto, cancellationToken))
            .ReturnsAsync(new PropertyAssessmentStatusDto { Id = 1 });

        // Act
        await controller.Create(createDto, cancellationToken);

        // Assert
        service.Verify(s => s.CreateAsync(createDto, cancellationToken), Times.Once);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingId_ReturnsOkWithUpdatedDto()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Updated Status" };
        var updatedDto = new PropertyAssessmentStatusDto { Id = 1, StatusName = "Updated Status" };
        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(updatedDto);

        // Act
        var result = await controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<PropertyAssessmentStatusDto>>(okResult.Value);
        Assert.Equal("Updated Status", response.Items!.StatusName);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsOkWithFailure()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Does Not Exist" };
        service.Setup(s => s.UpdateAsync(99, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync((PropertyAssessmentStatusDto?)null);

        // Act
        var result = await controller.Update(99, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<PropertyAssessmentStatusDto>>(okResult.Value);
        Assert.False(response.Success);
    }

    [Fact]
    public async Task Update_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdatePropertyAssessmentStatusDto { StatusName = "Test" };
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.UpdateAsync(1, updateDto, cancellationToken))
            .ReturnsAsync(new PropertyAssessmentStatusDto { Id = 1 });

        // Act
        await controller.Update(1, updateDto, cancellationToken);

        // Assert
        service.Verify(s => s.UpdateAsync(1, updateDto, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Update_DeactivatingStatus_CallsServiceCorrectly()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var updateDto = new UpdatePropertyAssessmentStatusDto 
        { 
            StatusName = "Deactivated",
            IsActive = false 
        };
        var updatedDto = new PropertyAssessmentStatusDto { Id = 1, StatusName = "Deactivated", IsActive = false };
        service.Setup(s => s.UpdateAsync(1, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(updatedDto);

        // Act
        var result = await controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<PropertyAssessmentStatusDto>>(okResult.Value);
        Assert.False(response.Items!.IsActive);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingId_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await controller.Delete(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        var cancellationToken = new CancellationToken();
        service.Setup(s => s.DeleteAsync(1, cancellationToken)).ReturnsAsync(true);

        // Act
        await controller.Delete(1, cancellationToken);

        // Assert
        service.Verify(s => s.DeleteAsync(1, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Delete_MultipleIds_DeletesEachSuccessfully()
    {
        // Arrange
        var controller = Create(out var service, out _, out _);
        service.Setup(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result1 = await controller.Delete(1, CancellationToken.None);
        var result2 = await controller.Delete(2, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result1);
        Assert.IsType<OkObjectResult>(result2);
        service.Verify(s => s.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region Purge Tests

    [Fact]
    public async Task Purge_ExistingId_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service, out var cleanup, out var referenceValidation);
        cleanup.Setup(c => c.ForceHardDeleteAsync<PropertyAssessmentStatusEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await controller.Purge(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithCancellationToken_PassesTokenCorrectly()
    {
        // Arrange
        var controller = Create(out var service, out var cleanup, out var referenceValidation);
        var cancellationToken = new CancellationToken();
        cleanup.Setup(c => c.ForceHardDeleteAsync<PropertyAssessmentStatusEntity, int>(1, cancellationToken))
            .ReturnsAsync(true);

        // Act
        await controller.Purge(1, cancellationToken);

        // Assert
        cleanup.Verify(c => c.ForceHardDeleteAsync<PropertyAssessmentStatusEntity, int>(1, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Purge_EntityNotFound_ReturnsOkWithFailure()
    {
        // Arrange
        var controller = Create(out var service, out var cleanup, out var referenceValidation);
        cleanup.Setup(c => c.ForceHardDeleteAsync<PropertyAssessmentStatusEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await controller.Purge(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<NtisPlatform.Application.Models.ApiResponse<object>>(okResult.Value);
        Assert.False(response.Success);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithAllDependencies_CreatesController()
    {
        // Arrange & Act
        var controller = Create(out var service, out var cleanup, out var referenceValidation);

        // Assert
        Assert.NotNull(controller);
    }

    #endregion
}
