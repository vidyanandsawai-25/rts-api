using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

/// <summary>
/// Unit tests for UlbImageMasterController
/// </summary>
public class UlbImageMasterControllerTests
{
    private readonly Mock<IUlbImageMasterService> _serviceMock;
    private readonly Mock<ILogger<UlbImageMasterController>> _loggerMock;
    private readonly UlbImageMasterController _controller;

    public UlbImageMasterControllerTests()
    {
        _serviceMock = new Mock<IUlbImageMasterService>();
        _loggerMock = new Mock<ILogger<UlbImageMasterController>>();
        _controller = new UlbImageMasterController(_serviceMock.Object, _loggerMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithDefaultParameters_ReturnsOkResult()
    {
        // Arrange
        var queryParams = new UlbImageMasterQueryParameters();
        var pagedResult = new PagedResult<UlbImageMasterDto>
        {
            Items = new List<UlbImageMasterDto>
            {
                new UlbImageMasterDto { Id = 1, ImageType = "Logo", ImageId = 10, IsActive = true }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<UlbImageMasterQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(s => s.GetAllAsync(It.IsAny<UlbImageMasterQueryParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithImageTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var queryParams = new UlbImageMasterQueryParameters { ImageType = "Logo", IsActive = true };
        var pagedResult = new PagedResult<UlbImageMasterDto>
        {
            Items = new List<UlbImageMasterDto>
            {
                new UlbImageMasterDto { Id = 1, ImageType = "Logo", IsActive = true }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<UlbImageMasterQueryParameters>(), It.IsAny<CancellationToken>()))
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
        var queryParams = new UlbImageMasterQueryParameters();
        var pagedResult = new PagedResult<UlbImageMasterDto>
        {
            Items = new List<UlbImageMasterDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<UlbImageMasterQueryParameters>(), It.IsAny<CancellationToken>()))
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
        var queryParams = new UlbImageMasterQueryParameters { PageNumber = 2, PageSize = 5 };
        var pagedResult = new PagedResult<UlbImageMasterDto>
        {
            Items = new List<UlbImageMasterDto>(),
            TotalCount = 0,
            PageNumber = 2,
            PageSize = 5
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<UlbImageMasterQueryParameters>(), It.IsAny<CancellationToken>()))
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
        var dto = new UlbImageMasterDto
        {
            Id = 1,
            ImageType = "Logo",
            ImageId = 10,
            IsActive = true
        };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbImageMasterDto?)null);

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
            .ReturnsAsync((UlbImageMasterDto?)null);

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
            .ReturnsAsync((UlbImageMasterDto?)null);

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
        var createDto = new CreateUlbImageMasterDto
        {
            ImageType = "Logo",
            ImageId = 10,
            CreatedBy = 1
        };
        var createdDto = new UlbImageMasterDto
        {
            Id = 1,
            ImageType = "Logo",
            ImageId = 10,
            IsActive = true
        };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreateUlbImageMasterDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithNullImageId_ReturnsOkResult()
    {
        // Arrange
        var createDto = new CreateUlbImageMasterDto { ImageType = "Banner", ImageId = null, CreatedBy = 1 };
        var createdDto = new UlbImageMasterDto { Id = 1, ImageType = "Banner", ImageId = null, IsActive = true };

        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Create_WithDifferentImageTypes_ReturnsOkResult()
    {
        // Arrange
        var imageTypes = new[] { "Logo", "Banner", "Icon", "Favicon" };
        foreach (var imageType in imageTypes)
        {
            var createDto = new CreateUlbImageMasterDto { ImageType = imageType, CreatedBy = 1 };
            var createdDto = new UlbImageMasterDto { Id = 1, ImageType = imageType, IsActive = true };

            _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.Create(createDto, CancellationToken.None);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdateUlbImageMasterDto
        {
            ImageType = "Banner",
            ImageId = 20,
            UpdatedBy = 2
        };
        var updatedDto = new UlbImageMasterDto
        {
            Id = 1,
            ImageType = "Banner",
            ImageId = 20,
            IsActive = true
        };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _serviceMock.Verify(s => s.UpdateAsync(1, It.IsAny<UpdateUlbImageMasterDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsOk()
    {
        // Arrange
        var updateDto = new UpdateUlbImageMasterDto { ImageType = "Banner", UpdatedBy = 2 };
        _serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UlbImageMasterDto?)null);

        // Act
        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithNullImageId_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdateUlbImageMasterDto { ImageType = "Logo", ImageId = null, UpdatedBy = 2 };
        var updatedDto = new UlbImageMasterDto { Id = 1, ImageType = "Logo", ImageId = null, IsActive = true };

        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithImageTypeChange_ReturnsOkResult()
    {
        // Arrange
        var updateDto = new UpdateUlbImageMasterDto { ImageType = "NewType", ImageId = 10, UpdatedBy = 2 };
        var updatedDto = new UlbImageMasterDto { Id = 1, ImageType = "NewType", ImageId = 10, IsActive = true };

        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
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
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

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
        _serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

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
        _serviceMock.Setup(s => s.DeleteAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync(false);

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
            _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }

        // Act & Assert
        foreach (var id in ids)
        {
            var result = await _controller.Delete(id, CancellationToken.None);
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
    public async Task CompleteWorkflow_CreateUpdateDelete_AllReturnOk()
    {
        // Arrange - Create
        var createDto = new CreateUlbImageMasterDto { ImageType = "Logo", ImageId = 1, CreatedBy = 1 };
        var createdDto = new UlbImageMasterDto { Id = 1, ImageType = "Logo", ImageId = 1, IsActive = true };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var createResult = await _controller.Create(createDto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(createResult);

        // Arrange - Update
        var updateDto = new UpdateUlbImageMasterDto { ImageType = "Banner", ImageId = 2, UpdatedBy = 2 };
        var updatedDto = new UlbImageMasterDto { Id = 1, ImageType = "Banner", ImageId = 2, IsActive = true };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateUlbImageMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var updateResult = await _controller.Update(1, updateDto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(updateResult);

        // Arrange - Delete
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var deleteResult = await _controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(deleteResult);

        // Assert
        _serviceMock.Verify(s => s.CreateAsync(It.IsAny<CreateUlbImageMasterDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.UpdateAsync(1, It.IsAny<UpdateUlbImageMasterDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _serviceMock.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
