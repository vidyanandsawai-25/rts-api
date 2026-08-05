using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class BulkUpdateMasterControllerTests
{
    private static BulkUpdateMasterController Create(out Mock<IBulkUpdateMasterService> service)
    {
        service = new Mock<IBulkUpdateMasterService>();
        var logger = new Mock<ILogger<BulkUpdateMasterController>>();
        return new BulkUpdateMasterController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var query = new BulkUpdateMasterQueryParameters();
        var pagedResult = new PagedResult<BulkUpdateMasterDto>(
            new List<BulkUpdateMasterDto>
            {
                new BulkUpdateMasterDto
                {
                    Id = 1,
                    UpdateCode = "PROP_TYPE",
                    UpdateName = "Property Type Update",
                    ReferenceTableName = "PropertyTypeMaster",
                    IsActive = true
                }
            }, 1, 1, 10);
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var id = 1;
        var dto = new BulkUpdateMasterDto
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true
        };
        service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await controller.GetById(id, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var controller = Create(out var service);
        var id = 999;
        service.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateMasterDto?)null);

        // Act
        var result = await controller.GetById(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        service.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var createDto = new CreateBulkUpdateMasterDto
        {
            UpdateCode = "WARD_UPDATE",
            UpdateName = "Ward Bulk Update",
            ReferenceTableName = "WardMaster",
            CreatedBy = 1
        };
        var resultDto = new BulkUpdateMasterDto
        {
            Id = 2,
            UpdateCode = "WARD_UPDATE",
            UpdateName = "Ward Bulk Update",
            ReferenceTableName = "WardMaster",
            IsActive = true
        };
        service.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithValidIdAndDto_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var id = 1;
        var updateDto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update (Modified)",
            ReferenceTableName = "PropertyTypeMaster",
            UpdatedBy = 1
        };
        var resultDto = new BulkUpdateMasterDto
        {
            Id = id,
            UpdateCode = "PROP_TYPE",
            UpdateName = "Property Type Update (Modified)",
            ReferenceTableName = "PropertyTypeMaster",
            IsActive = true
        };
        service.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultDto);

        // Act
        var result = await controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsOkWithFailure()
    {
        // Arrange
        var controller = Create(out var service);
        var id = 999;
        var updateDto = new UpdateBulkUpdateMasterDto
        {
            UpdateCode = "NON_EXISTENT",
            UpdateName = "Non Existent",
            ReferenceTableName = "NonExistentTable",
            UpdatedBy = 1
        };
        service.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateMasterDto?)null);

        // Act
        var result = await controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsOkWithSuccess()
    {
        // Arrange
        var controller = Create(out var service);
        var id = 1;
        service.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await controller.Delete(id, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsOkWithFailure()
    {
        // Arrange
        var controller = Create(out var service);
        var id = 999;
        service.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await controller.Delete(id, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithFilterByUpdateCode_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var query = new BulkUpdateMasterQueryParameters
        {
            UpdateCode = "PROP"
        };
        var pagedResult = new PagedResult<BulkUpdateMasterDto>(
            new List<BulkUpdateMasterDto>
            {
                new BulkUpdateMasterDto { Id = 1, UpdateCode = "PROP_TYPE" },
                new BulkUpdateMasterDto { Id = 2, UpdateCode = "PROP_CATEGORY" }
            }, 2, 1, 10);
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
    }

    [Fact]
    public async Task GetAll_WithSearchByUpdateName_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var query = new BulkUpdateMasterQueryParameters
        {
            UpdateName = "Property"
        };
        var pagedResult = new PagedResult<BulkUpdateMasterDto>(
            new List<BulkUpdateMasterDto>
            {
                new BulkUpdateMasterDto { Id = 1, UpdateName = "Property Type Update" },
                new BulkUpdateMasterDto { Id = 2, UpdateName = "Property Category Update" }
            }, 2, 1, 10);
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithFilterByReferenceTableName_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var query = new BulkUpdateMasterQueryParameters
        {
            ReferenceTableName = "PropertyTypeMaster"
        };
        var pagedResult = new PagedResult<BulkUpdateMasterDto>(
            new List<BulkUpdateMasterDto>
            {
                new BulkUpdateMasterDto { Id = 1, ReferenceTableName = "PropertyTypeMaster" }
            }, 1, 1, 10);
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
