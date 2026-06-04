using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyControllerFloorDetailsOldTests
{
    private static PropertyController Create(out Mock<IPropertyService> service)
    {
        service = new Mock<IPropertyService>();
        var logger = new Mock<ILogger<PropertyController>>();
        return PropertyControllerTestHelper.CreateController(service, logger);
    }

    #region Get

    [Fact]
    public async Task GetFloorDetailsOld_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetFloorDetailsOldAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDetailsOldListDto());

        var result = await controller.GetFloorDetailsOld(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetFloorDetailsOld_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetFloorDetailsOldAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDetailsOldListDto?)null);

        var result = await controller.GetFloorDetailsOld(99, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetFloorDetailsOld_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetFloorDetailsOld(1, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetFloorDetailsOldById_ReturnsOk_WhenFound()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetFloorDetailsOldByIdAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDetailsOldDto());

        var result = await controller.GetFloorDetailsOldById(1, 5, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldById_ReturnsNotFound_WhenMissing()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetFloorDetailsOldByIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDetailsOldDto?)null);

        var result = await controller.GetFloorDetailsOldById(99, 5, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldById_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.GetFloorDetailsOldByIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await controller.GetFloorDetailsOldById(1, 5, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    #endregion

    #region GetPaged

    [Fact]
    public async Task GetFloorDetailsOldPaged_ReturnsOk_WhenFound()
    {
        // Arrange
        var controller = Create(out var service);
        var queryParameters = new FloorDetailsOldQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };
        var pagedResult = new PagedResult<PropertyDetailsOldDto>(
            new List<PropertyDetailsOldDto> { new PropertyDetailsOldDto { Id = 1 } },
            1,
            1,
            10
        );
        service.Setup(s => s.GetFloorDetailsOldPagedAsync(1, It.IsAny<FloorDetailsOldQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetFloorDetailsOldPaged(1, queryParameters, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldPaged_ReturnsNotFound_WhenMissing()
    {
        // Arrange
        var controller = Create(out var service);
        var queryParameters = new FloorDetailsOldQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };
        service.Setup(s => s.GetFloorDetailsOldPagedAsync(99, It.IsAny<FloorDetailsOldQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<PropertyDetailsOldDto>?)null);

        // Act
        var result = await controller.GetFloorDetailsOldPaged(99, queryParameters, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetFloorDetailsOldPaged_Returns500_OnException()
    {
        // Arrange
        var controller = Create(out var service);
        var queryParameters = new FloorDetailsOldQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };
        service.Setup(s => s.GetFloorDetailsOldPagedAsync(It.IsAny<int>(), It.IsAny<FloorDetailsOldQueryParameters>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        // Act
        var result = await controller.GetFloorDetailsOldPaged(1, queryParameters, CancellationToken.None);

        // Assert
        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    [Fact]
    public async Task GetFloorDetailsOldPaged_ForwardsQueryParametersToService()
    {
        // Arrange
        var controller = Create(out var service);
        var queryParameters = new FloorDetailsOldQueryParameters
        {
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "test",
            SortBy = "OldFloorId",
            SortOrder = "desc",
            OldFloorId = 1,
            OldSubFloorId = 2,
            OldConstructionTypeId = 3,
            OldTypeOfUseId = 4,
            OldSubTypeOfUseId = 5,
            OldConstructionYear = "2020",
            OldAssessmentYear = "2021"
        };
        var pagedResult = new PagedResult<PropertyDetailsOldDto>(
            new List<PropertyDetailsOldDto>(),
            0,
            2,
            20
        );
        service.Setup(s => s.GetFloorDetailsOldPagedAsync(
                1,
                It.Is<FloorDetailsOldQueryParameters>(q =>
                    q.PageNumber == 2 &&
                    q.PageSize == 20 &&
                    q.SearchTerm == "test" &&
                    q.SortBy == "OldFloorId" &&
                    q.SortOrder == "desc" &&
                    q.OldFloorId == 1 &&
                    q.OldSubFloorId == 2 &&
                    q.OldConstructionTypeId == 3 &&
                    q.OldTypeOfUseId == 4 &&
                    q.OldSubTypeOfUseId == 5 &&
                    q.OldConstructionYear == "2020" &&
                    q.OldAssessmentYear == "2021"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetFloorDetailsOldPaged(1, queryParameters, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        service.Verify(s => s.GetFloorDetailsOldPagedAsync(
            1,
            It.IsAny<FloorDetailsOldQueryParameters>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFloorDetailsOldPaged_WithUnpagedMode_ReturnsOk()
    {
        // Arrange
        var controller = Create(out var service);
        var queryParameters = new FloorDetailsOldQueryParameters
        {
            PageNumber = 1,
            PageSize = -1 // Unpaged mode
        };
        var pagedResult = new PagedResult<PropertyDetailsOldDto>(
            new List<PropertyDetailsOldDto> { new PropertyDetailsOldDto { Id = 1 }, new PropertyDetailsOldDto { Id = 2 } },
            2,
            1,
            2 // Normalized
        );
        service.Setup(s => s.GetFloorDetailsOldPagedAsync(1, It.IsAny<FloorDetailsOldQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await controller.GetFloorDetailsOldPaged(1, queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region Add

    [Fact]
    public async Task AddFloorDetailsOld_ReturnsCreated_WhenSuccess()
    {
        var controller = Create(out var service);
        service.Setup(s => s.AddFloorDetailsOldAsync(1, It.IsAny<AddPropertyDetailsOldDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDetailsOldDto { Id = 7 });

        var result = await controller.AddFloorDetailsOld(1, new AddPropertyDetailsOldDto(), CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task AddFloorDetailsOld_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.AddFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<AddPropertyDetailsOldDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDetailsOldDto?)null);

        var result = await controller.AddFloorDetailsOld(99, new AddPropertyDetailsOldDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AddFloorDetailsOld_ReturnsBadRequest_OnInvalidOperation()
    {
        var controller = Create(out var service);
        service.Setup(s => s.AddFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<AddPropertyDetailsOldDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var result = await controller.AddFloorDetailsOld(1, new AddPropertyDetailsOldDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddFloorDetailsOld_Returns500_OnGenericException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.AddFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<AddPropertyDetailsOldDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await controller.AddFloorDetailsOld(1, new AddPropertyDetailsOldDto(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    #endregion

    #region Update

    [Fact]
    public async Task UpdateFloorDetailsOld_ReturnsOk_WhenSuccess()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateFloorDetailsOldAsync(1, 5, It.IsAny<UpdatePropertyDetailsOldDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDetailsOldDto());

        var result = await controller.UpdateFloorDetailsOld(1, 5, new UpdatePropertyDetailsOldDto(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateFloorDetailsOld_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UpdatePropertyDetailsOldDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDetailsOldDto?)null);

        var result = await controller.UpdateFloorDetailsOld(99, 5, new UpdatePropertyDetailsOldDto(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateFloorDetailsOld_ReturnsBadRequest_OnInvalidOperation()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UpdatePropertyDetailsOldDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("invalid"));

        var result = await controller.UpdateFloorDetailsOld(1, 5, new UpdatePropertyDetailsOldDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateFloorDetailsOld_Returns500_OnGenericException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.UpdateFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<UpdatePropertyDetailsOldDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await controller.UpdateFloorDetailsOld(1, 5, new UpdatePropertyDetailsOldDto(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteFloorDetailsOld_ReturnsOk_WhenSuccess()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteFloorDetailsOldAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.DeleteFloorDetailsOld(1, 5, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task DeleteFloorDetailsOld_ReturnsNotFound_WhenServiceReturnsFalse()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.DeleteFloorDetailsOld(99, 5, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteFloorDetailsOld_Returns500_OnException()
    {
        var controller = Create(out var service);
        service.Setup(s => s.DeleteFloorDetailsOldAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var result = await controller.DeleteFloorDetailsOld(1, 5, CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    #endregion
}
