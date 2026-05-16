using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers.Master;

public class SocietyDetailsControllerTests
{
    private readonly Mock<ISocietyDetailsService> _serviceMock;
    private readonly Mock<IHardDeleteCleanupService> _cleanupServiceMock;
    private readonly Mock<ILogger<SocietyDetailsController>> _loggerMock;
    private readonly SocietyDetailsController _controller;

    public SocietyDetailsControllerTests()
    {
        _serviceMock = new Mock<ISocietyDetailsService>();
        _cleanupServiceMock = new Mock<IHardDeleteCleanupService>();
        _loggerMock = new Mock<ILogger<SocietyDetailsController>>();
        _controller = new SocietyDetailsController(_serviceMock.Object, _cleanupServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        var queryParams = new SocietyDetailsQueryParameters();
        var pagedResult = new PagedResult<SocietyDetailsDto>
        {
            Items = new List<SocietyDetailsDto> { new SocietyDetailsDto { Id = 1 } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<SocietyDetailsQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        var dto = new SocietyDetailsDto { Id = 1 };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocietyDetailsDto?)null);

        var result = await _controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsOkResult()
    {
        var createDto = new CreateSocietyDetailsDto();
        var createdDto = new SocietyDetailsDto { Id = 1 };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateSocietyDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithValidData_ReturnsOkResult()
    {
        var updateDto = new UpdateSocietyDetailsDto();
        var updatedDto = new SocietyDetailsDto { Id = 1 };
        _serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateSocietyDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsOk()
    {
        var updateDto = new UpdateSocietyDetailsDto();
        _serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateSocietyDetailsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocietyDetailsDto?)null);

        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithValidId_ReturnsOk()
    {
        _cleanupServiceMock.Setup(s => s.ForceHardDeleteAsync<SocietyDetailsEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Purge(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Purge_WithInvalidId_ReturnsOk()
    {
        _cleanupServiceMock.Setup(s => s.ForceHardDeleteAsync<SocietyDetailsEntity, int>(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Purge(999, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }
}
