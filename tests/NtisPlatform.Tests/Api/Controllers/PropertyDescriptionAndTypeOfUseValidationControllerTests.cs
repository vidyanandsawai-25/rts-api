using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master.PropertyDescriptionAndTypeOfUseValidation;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Api.Controllers;

public class PropertyDescriptionAndTypeOfUseValidationControllerTests
{
    private readonly Mock<IPropertyDescriptionAndTypeOfUseValidationService> _mockService;
    private readonly Mock<ILogger<PropertyDescriptionAndTypeOfUseValidationController>> _mockLogger;
    private readonly PropertyDescriptionAndTypeOfUseValidationController _controller;

    public PropertyDescriptionAndTypeOfUseValidationControllerTests()
    {
        _mockService = new Mock<IPropertyDescriptionAndTypeOfUseValidationService>();
        _mockLogger = new Mock<ILogger<PropertyDescriptionAndTypeOfUseValidationController>>();
        _controller = new PropertyDescriptionAndTypeOfUseValidationController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAll_ReturnsOkResult_WithPagedData()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<PropertyDescriptionAndTypeOfUseValidationDto>(
            new List<PropertyDescriptionAndTypeOfUseValidationDto>
            {
                new() { Id = 1, PropertyTypeId = 5, TypeOfUseId = 10 },
                new() { Id = 2, PropertyTypeId = 6, TypeOfUseId = 11 }
            },
            totalCount: 2,
            pageNumber: 1,
            pageSize: 10
        );

        _mockService.Setup(s => s.GetAllAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PagedResult<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.Equal(2, returnValue.TotalCount);
        Assert.Equal(2, returnValue.Items.Count());
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkResult()
    {
        var dto = new PropertyDescriptionAndTypeOfUseValidationDto
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true
        };

        _mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PropertyDescriptionAndTypeOfUseValidationDto>(okResult.Value);
        Assert.Equal(1, returnValue.Id);
        Assert.Equal(5, returnValue.PropertyTypeId);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDescriptionAndTypeOfUseValidationDto?)null);

        var result = await _controller.GetById(9999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedResult()
    {
        var createDto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true
        };

        var createdDto = new PropertyDescriptionAndTypeOfUseValidationDto
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = true
        };

        _mockService.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyDescriptionAndTypeOfUseValidationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record inserted successfully", response.Message);
        Assert.Equal(1, response.Items!.Id);
        Assert.Equal(5, response.Items.PropertyTypeId);
        Assert.Equal(10, response.Items.TypeOfUseId);
    }

    [Fact]
    public async Task Update_ExistingId_ReturnsOkResult()
    {
        var updateDto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = true
        };

        var updatedDto = new PropertyDescriptionAndTypeOfUseValidationDto
        {
            Id = 1,
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = true
        };

        _mockService.Setup(s => s.UpdateAsync(1, It.IsAny<UpdatePropertyDescriptionAndTypeOfUseValidationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        var result = await _controller.Update(1, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record updated successfully", response.Message);
        Assert.Equal(1, response.Items!.Id);
        Assert.Equal(6, response.Items.PropertyTypeId);
        Assert.Equal(11, response.Items.TypeOfUseId);
    }

    [Fact]
    public async Task Update_NonExistingId_ReturnsNotFound()
    {
        var updateDto = new UpdatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 6,
            TypeOfUseId = 11,
            IsActive = true
        };

        _mockService.Setup(s => s.UpdateAsync(9999, It.IsAny<UpdatePropertyDescriptionAndTypeOfUseValidationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDescriptionAndTypeOfUseValidationDto?)null);

        var result = await _controller.Update(9999, updateDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        _mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Record marked for deletion", response.Message);
    }

    [Fact]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Delete(9999, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("not found", response.Message);
    }

    [Fact]
    public async Task GetAll_WithFiltering_ReturnsFilteredData()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            PropertyTypeId = 5,
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<PropertyDescriptionAndTypeOfUseValidationDto>(
            new List<PropertyDescriptionAndTypeOfUseValidationDto>
            {
                new() { Id = 1, PropertyTypeId = 5, TypeOfUseId = 10 }
            },
            totalCount: 1,
            pageNumber: 1,
            pageSize: 10
        );

        _mockService.Setup(s => s.GetAllAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PagedResult<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.Single(returnValue.Items);
        Assert.All(returnValue.Items, item => Assert.Equal(5, item.PropertyTypeId));
    }

    [Fact]
    public async Task GetAll_WithSorting_ReturnsSortedData()
    {
        var queryParams = new PropertyDescriptionAndTypeOfUseValidationQueryParameters
        {
            SortBy = "PropertyTypeId",
            SortOrder = "desc",
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<PropertyDescriptionAndTypeOfUseValidationDto>(
            new List<PropertyDescriptionAndTypeOfUseValidationDto>
            {
                new() { Id = 2, PropertyTypeId = 6, TypeOfUseId = 11 },
                new() { Id = 1, PropertyTypeId = 5, TypeOfUseId = 10 }
            },
            totalCount: 2,
            pageNumber: 1,
            pageSize: 10
        );

        _mockService.Setup(s => s.GetAllAsync(It.IsAny<PropertyDescriptionAndTypeOfUseValidationQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(queryParams, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<PagedResult<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.Equal(2, returnValue.Items.Count());
    }

    [Fact]
    public async Task Create_WithInactiveStatus_CreatesCorrectly()
    {
        var createDto = new CreatePropertyDescriptionAndTypeOfUseValidationDto
        {
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = false,
            CreatedBy = 1
        };

        var createdDto = new PropertyDescriptionAndTypeOfUseValidationDto
        {
            Id = 1,
            PropertyTypeId = 5,
            TypeOfUseId = 10,
            IsActive = false
        };

        _mockService.Setup(s => s.CreateAsync(It.IsAny<CreatePropertyDescriptionAndTypeOfUseValidationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        var result = await _controller.Create(createDto, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PropertyDescriptionAndTypeOfUseValidationDto>>(okResult.Value);
        Assert.True(response.Success);
        Assert.False(response.Items!.IsActive);
    }

    [Fact]
    public void Controller_HasApiControllerAttribute()
    {
        var type = typeof(PropertyDescriptionAndTypeOfUseValidationController);
        var attribute = type.GetCustomAttributes(typeof(ApiControllerAttribute), false).FirstOrDefault();
        
        Assert.NotNull(attribute);
    }

    [Fact]
    public void Controller_HasCorrectRouteAttribute()
    {
        var type = typeof(PropertyDescriptionAndTypeOfUseValidationController);
        var attribute = (RouteAttribute?)type.GetCustomAttributes(typeof(RouteAttribute), false).FirstOrDefault();
        
        Assert.NotNull(attribute);
        Assert.Equal("api/[controller]", attribute.Template);
    }
}
