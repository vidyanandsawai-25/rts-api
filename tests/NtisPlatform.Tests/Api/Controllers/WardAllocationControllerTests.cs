using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.wardallocation;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

public class WardAllocationControllerTests
{
    private readonly Mock<IWardAllocationService> _serviceMock;
    private readonly Mock<ILogger<WardAllocationController>> _loggerMock;
    private readonly WardAllocationController _controller;

    public WardAllocationControllerTests()
    {
        _serviceMock = new Mock<IWardAllocationService>();
        _loggerMock = new Mock<ILogger<WardAllocationController>>();
        _controller = new WardAllocationController(_serviceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult()
    {
        var queryParams = new WardAllocationQueryParameters();
        var pagedResult = new PagedResult<WardAllocationDto>
        {
            Items = new List<WardAllocationDto> { new WardAllocationDto { Id = 1 } },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<WardAllocationQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        var dto = new WardAllocationDto { Id = 1 };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardAllocationDto?)null);

        var result = await _controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateFlexible_WithValidDto_ReturnsOkResult()
    {
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 1,
            DepartmentId = 1,
            ModuleId = 1,
            Allocations = { new ZoneWardAllocationDto { ZoneId = 1, WardIds = { 1 } } }
        };

        var created = new List<WardAllocationDto> { new WardAllocationDto { Id = 1 } };

        _serviceMock.Setup(s => s.CreateFlexibleAsync(It.IsAny<CreateFlexibleWardAllocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await _controller.CreateFlexible(createDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateFlexible_WhenDuplicate_ThrowsConflict()
    {
        var createDto = new CreateFlexibleWardAllocationDto
        {
            UserId = 1,
            DepartmentId = 1,
            ModuleId = 1,
            Allocations = { new ZoneWardAllocationDto { ZoneId = 1, WardIds = { 1 } } }
        };

        _serviceMock.Setup(s => s.CreateFlexibleAsync(It.IsAny<CreateFlexibleWardAllocationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("duplicate"));

        var result = await _controller.CreateFlexible(createDto, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithValidId_ReturnsOkResult()
    {
        var existing = new WardAllocationDto { Id = 1 };
        _serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var updateDto = new UpdateFlexibleWardAllocationDto
        {
            UserId = 1,
            DepartmentId = 1,
            ModuleId = 1,
            Allocations = { new ZoneWardAllocationDto { ZoneId = 1, WardIds = { 1 } } }
        };

        var replaced = new List<WardAllocationDto> { new WardAllocationDto { Id = 2 } };

        _serviceMock.Setup(s => s.ReplaceAllocationsAsync(1, 1, It.IsAny<UpdateFlexibleWardAllocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(replaced);

        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WardAllocationDto?)null);

        var updateDto = new UpdateFlexibleWardAllocationDto
        {
            UserId = 1,
            DepartmentId = 1,
            ModuleId = 1,
            Allocations = { new ZoneWardAllocationDto { ZoneId = 1, WardIds = { 1 } } }
        };

        var result = await _controller.Update(999, updateDto, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithValidId_ReturnsOkResult()
    {
        _serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }


    [Fact]
    public async Task GetModulesByUserId_ReturnsOkResult()
    {
        var list = new List<WardAllocationModuleDto> { new WardAllocationModuleDto { ModuleId = 1 } };
        _serviceMock.Setup(s => s.GetModulesByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _controller.GetModulesByUserId(1, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetZones_Wards_Departments_ReturnOk()
    {
       
        _serviceMock.Setup(s => s.GetWardsByZoneIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WardAllocationWardDto> { new WardAllocationWardDto { WardId = 1 } });
        var wards = await _controller.GetWardsByZoneId(1, CancellationToken.None);
       
        Assert.IsType<OkObjectResult>(wards);
     
    }

    [Fact]
    public async Task GetAllocatedEndpoints_HandleDeallocatedAndAllocated()
    {
        // Deallocated case
        _serviceMock.Setup(s => s.IsUserDeallocatedAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var deallocated = await _controller.GetAllocatedZonesAndWardsByUserId(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(deallocated);

        // Allocated case
        _serviceMock.Setup(s => s.IsUserDeallocatedAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _serviceMock.Setup(s => s.GetAllocatedZonesAndWardsByUserIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAllocatedZoneWardDto> { new UserAllocatedZoneWardDto { ZoneId = 1 } });

        var allocated = await _controller.GetAllocatedZonesAndWardsByUserId(2, CancellationToken.None);
        Assert.IsType<OkObjectResult>(allocated);

        // Zones and Wards endpoints use same deallocation check
        _serviceMock.Setup(s => s.IsUserDeallocatedAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _serviceMock.Setup(s => s.GetAllocatedZonesAndWardsByUserIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAllocatedZoneWardDto> { new UserAllocatedZoneWardDto { ZoneId = 1 } });

        var zones = await _controller.GetAllocatedZonesByUserId(3, CancellationToken.None);
        var wards = await _controller.GetAllocatedWardsByUserId(3, CancellationToken.None);

        Assert.IsType<OkObjectResult>(zones);
        Assert.IsType<OkObjectResult>(wards);
    }
}
