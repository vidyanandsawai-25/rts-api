using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class BulkUpdateFieldConfigControllerTests
{
    private static BulkUpdateFieldConfigController Create(out Mock<IBulkUpdateFieldConfigService> service)
    {
        service = new Mock<IBulkUpdateFieldConfigService>();
        var logger = new Mock<ILogger<BulkUpdateFieldConfigController>>();
        return new BulkUpdateFieldConfigController(service.Object, logger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var query = new BulkUpdateFieldConfigQueryParameters();
        var pagedResult = new PagedResult<BulkUpdateFieldConfigDto>(
            new List<BulkUpdateFieldConfigDto>
            {
                new BulkUpdateFieldConfigDto
                {
                    Id = 1,
                    BulkUpdateMasterId = 1,
                    FieldName = "PropertyType",
                    DisplayName = "Property Type",
                    ControlType = "Dropdown",
                    DataType = "String",
                    SequenceNo = 1,
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
        var dto = new BulkUpdateFieldConfigDto
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
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
            .ReturnsAsync((BulkUpdateFieldConfigDto?)null);

        // Act
        var result = await controller.GetById(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result); // Changed from NotFoundObjectResult
        service.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var createDto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "Ward",
            DisplayName = "Ward",
            DisplayNameMarathi = "प्रभाग",
            ControlType = "Dropdown",
            DataType = "Integer",
            SequenceNo = 2,
            CreatedBy = 1
        };
        var resultDto = new BulkUpdateFieldConfigDto
        {
            Id = 2,
            BulkUpdateMasterId = 1,
            FieldName = "Ward",
            DisplayName = "Ward",
            DisplayNameMarathi = "प्रभाग",
            ControlType = "Dropdown",
            DataType = "Integer",
            SequenceNo = 2,
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
        var updateDto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type (Updated)",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            UpdatedBy = 1
        };
        var resultDto = new BulkUpdateFieldConfigDto
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type (Updated)",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
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
        var updateDto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "NonExistent",
            DisplayName = "Non Existent",
            ControlType = "TextBox",
            DataType = "String",
            SequenceNo = 1,
            UpdatedBy = 1
        };
        service.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateFieldConfigDto?)null);

        // Act
        var result = await controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result); // Returns OkObjectResult with Success=false
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
        Assert.IsType<OkObjectResult>(result); // Returns OkObjectResult with Success=true
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
        Assert.IsType<OkObjectResult>(result); // Returns OkObjectResult with Success=false
        service.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithFilterByBulkUpdateMasterId_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var query = new BulkUpdateFieldConfigQueryParameters
        {
            BulkUpdateMasterId = 1
        };
        var pagedResult = new PagedResult<BulkUpdateFieldConfigDto>(
            new List<BulkUpdateFieldConfigDto>
            {
                new BulkUpdateFieldConfigDto { Id = 1, BulkUpdateMasterId = 1, FieldName = "Field1" },
                new BulkUpdateFieldConfigDto { Id = 2, BulkUpdateMasterId = 1, FieldName = "Field2" }
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
    public async Task GetAll_WithSearchByFieldName_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var query = new BulkUpdateFieldConfigQueryParameters
        {
            FieldName = "Property"
        };
        var pagedResult = new PagedResult<BulkUpdateFieldConfigDto>(
            new List<BulkUpdateFieldConfigDto>
            {
                new BulkUpdateFieldConfigDto { Id = 1, FieldName = "PropertyType" },
                new BulkUpdateFieldConfigDto { Id = 2, FieldName = "PropertyCategory" }
            }, 2, 1, 10);
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAll_WithSortBySequenceNo_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var query = new BulkUpdateFieldConfigQueryParameters
        {
            SequenceNo = 1
        };
        var pagedResult = new PagedResult<BulkUpdateFieldConfigDto>(
            new List<BulkUpdateFieldConfigDto>
            {
                new BulkUpdateFieldConfigDto { Id = 1, SequenceNo = 1 },
                new BulkUpdateFieldConfigDto { Id = 5, SequenceNo = 1 }
            }, 2, 1, 10);
        service.Setup(s => s.GetAllAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetAll(query, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
