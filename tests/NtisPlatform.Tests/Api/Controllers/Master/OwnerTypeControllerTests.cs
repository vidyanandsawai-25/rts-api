using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class OwnerTypeControllerTests
{
    private readonly Mock<IOwnerTypeService> _serviceMock;
    private readonly Mock<ILogger<OwnerTypeController>> _loggerMock;
    private readonly OwnerTypeController _controller;

    public OwnerTypeControllerTests()
    {
        _serviceMock = new Mock<IOwnerTypeService>();
        _loggerMock = new Mock<ILogger<OwnerTypeController>>();
        _controller = new OwnerTypeController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        var queryParams = new OwnerTypeQueryParameters();
        var pagedResult = new PagedResult<OwnerTypeDto>
        {
            Items = new List<OwnerTypeDto> { new OwnerTypeDto { Id = 1 } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<OwnerTypeQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        var dto = new OwnerTypeDto { Id = 1 };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnerTypeDto?)null);

        var result = await _controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkResult()
    {
        var createDto = new CreateOwnerTypeDto();
        var createdDto = new OwnerTypeDto { Id = 1 };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateOwnerTypeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsOkResult()
    {
        var updateDto = new UpdateOwnerTypeDto();
        var updatedDto = new OwnerTypeDto { Id = 1 };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateOwnerTypeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsOk()
    {
        var updateDto = new UpdateOwnerTypeDto();
        _serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateOwnerTypeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OwnerTypeDto?)null);

        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsOk()
    {
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsOk()
    {
        _serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Delete(999, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
