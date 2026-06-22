using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyDiscount;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Comprehensive tests for PropertySocialDetailsController
/// </summary>
public class PropertySocialDetailsControllerTests
{
    private readonly Mock<IPropertySocialDetailsService> _mockService;
    private readonly Mock<ILogger<PropertySocialDetailsController>> _mockLogger;
    private readonly Mock<IPropertySocialDetailsDocumentService> _mockSocialDetailsDocumentService;
    private readonly PropertySocialDetailsController _controller;

    public PropertySocialDetailsControllerTests()
    {
        _mockService = new Mock<IPropertySocialDetailsService>();
        _mockLogger = new Mock<ILogger<PropertySocialDetailsController>>();
        _mockSocialDetailsDocumentService = new Mock<IPropertySocialDetailsDocumentService>();

        var mockEnvironment = new Mock<IWebHostEnvironment>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var fileValidationHelper = new FileValidationHelper(configuration);

        _controller = new PropertySocialDetailsController(
            _mockLogger.Object,
            _mockService.Object,
            _mockSocialDetailsDocumentService.Object,
            mockEnvironment.Object,
            fileValidationHelper);

        // Set up HttpContext with authenticated user
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "TestAuth"));
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WithValidQueryParameters_ReturnsOkResult()
    {
        // Arrange
        var queryParameters = new PropertySocialDetailsQueryParameters();
        var pagedResult = new PagedResult<PropertySocialDetailsDto>
        {
            Items = new List<PropertySocialDetailsDto>
            {
                new() { Id = 1, PropertyId = 100, SocialAttributeId = 5, BitValue = true }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<PropertySocialDetailsDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
        _mockService.Verify(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkResultWithEmptyList()
    {
        // Arrange
        var queryParameters = new PropertySocialDetailsQueryParameters();
        var pagedResult = new PagedResult<PropertySocialDetailsDto>
        {
            Items = new List<PropertySocialDetailsDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAll_WithFilterParameters_ReturnsFilteredResults()
    {
        // Arrange
        var queryParameters = new PropertySocialDetailsQueryParameters
        {
            PropertyId = 100,
            SocialAttributeId = 5,
            IsActive = true
        };
        var pagedResult = new PagedResult<PropertySocialDetailsDto>
        {
            Items = new List<PropertySocialDetailsDto>
            {
                new() { Id = 1, PropertyId = 100, SocialAttributeId = 5, BitValue = true }
            },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<PropertySocialDetailsDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
    }

    [Fact]
    public async Task GetAll_WithFilterValidationException_ReturnsBadRequest()
    {
        // Arrange
        var queryParameters = new PropertySocialDetailsQueryParameters();
        var errors = new Dictionary<string, string> { { "PropertyId", "Invalid filter parameter" } };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FilterValidationException("Filter validation failed", errors));

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);

        var responseType = badRequestResult.Value.GetType();
        var messageProperty = responseType.GetProperty("message");
        Assert.NotNull(messageProperty);
        var messageValue = messageProperty.GetValue(badRequestResult.Value) as string;
        Assert.Equal("Filter validation failed", messageValue);
    }

    [Fact]
    public async Task GetAll_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var queryParameters = new PropertySocialDetailsQueryParameters();

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAll_WithPropertyIdFilter_ReturnsFilteredByPropertyId()
    {
        // Arrange
        var queryParameters = new PropertySocialDetailsQueryParameters
        {
            PropertyId = 100
        };

        var pagedResult = new PagedResult<PropertySocialDetailsDto>
        {
            Items = new List<PropertySocialDetailsDto>
            {
                new() { Id = 1, PropertyId = 100, SocialAttributeId = 5 },
                new() { Id = 2, PropertyId = 100, SocialAttributeId = 6 }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockService.Setup(s => s.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<PropertySocialDetailsDto>>(okResult.Value);
        Assert.Equal(2, returnedResult.Items.Count());
        Assert.All(returnedResult.Items, item => Assert.Equal(100, item.PropertyId));
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var id = 1;
        var dto = new PropertySocialDetailsDto
        {
            Id = id,
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = true,
            IntValue = 10,
            TextValue = "Test Value"
        };

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<PropertySocialDetailsDto>(okResult.Value);
        Assert.Equal(id, returnedDto.Id);
        Assert.Equal(100, returnedDto.PropertyId);
        _mockService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySocialDetailsDto?)null);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var id = 1;

        _mockService.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreatedResult()
    {
        // Arrange
        var createDto = new CreatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = true,
            IntValue = 10,
            TextValue = "New Value",
            IsActive = true
        };

        var createdDto = new PropertySocialDetailsDto
        {
            Id = 1,
            PropertyId = createDto.PropertyId,
            SocialAttributeId = createDto.SocialAttributeId,
            BitValue = createDto.BitValue,
            IntValue = createDto.IntValue,
            TextValue = createDto.TextValue,
            IsActive = createDto.IsActive
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(createdDto.Id, apiResponse.Items.Id);
        _mockService.Verify(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithDuplicateConstraint_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = true,
            IsActive = true
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Duplicate key constraint violation"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Create_WithAllValueTypes_StoresCorrectly()
    {
        // Arrange
        var createDto = new CreatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = true,
            IntValue = 25,
            DecimalValue = 99.99m,
            TextValue = "Complex Value",
            DateValue = new DateTime(2024, 1, 15),
            Remark = "Test remark",
            IsActive = true
        };

        var createdDto = new PropertySocialDetailsDto
        {
            Id = 1,
            PropertyId = createDto.PropertyId,
            SocialAttributeId = createDto.SocialAttributeId,
            BitValue = createDto.BitValue,
            IntValue = createDto.IntValue,
            DecimalValue = createDto.DecimalValue,
            TextValue = createDto.TextValue,
            DateValue = createDto.DateValue,
            Remark = createDto.Remark,
            IsActive = createDto.IsActive
        };

        _mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(createDto.BitValue, apiResponse.Items.BitValue);
        Assert.Equal(createDto.IntValue, apiResponse.Items.IntValue);
        Assert.Equal(createDto.DecimalValue, apiResponse.Items.DecimalValue);
        Assert.Equal(createDto.TextValue, apiResponse.Items.TextValue);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidDto_ReturnsOkResult()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = false,
            IntValue = 20,
            TextValue = "Updated Value",
            IsActive = true
        };

        var updatedDto = new PropertySocialDetailsDto
        {
            Id = id,
            PropertyId = updateDto.PropertyId,
            SocialAttributeId = updateDto.SocialAttributeId,
            BitValue = updateDto.BitValue,
            IntValue = updateDto.IntValue,
            TextValue = updateDto.TextValue,
            IsActive = updateDto.IsActive
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedDto);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(updatedDto.TextValue, apiResponse.Items.TextValue);
        _mockService.Verify(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySocialDetailsDto?)null);

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5
        };

        _mockService.Setup(s => s.UpdateAsync(id, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Update(id, updateDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var id = 1;

        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        _mockService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var id = 999;

        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var id = 1;

        _mockService.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region Document Upload Tests

    [Fact]
    public async Task UploadSocialDocument_ReturnsOk_WhenUploadSuccessful()
    {
        // Arrange
        var formDto = new SocialDetailsDocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf"),
            PropertyId = 1,
            SocialAttributeId = 1,
            Remark = "Test remark",
            IsPhoto = false
        };

        var expectedResponse = new PropertySocialDetailsDocumentResponseDto
        {
            PropertySocialDetailId = 10,
            PropertyId = 1,
            SocialAttributeId = 1,
            DocumentBindingId = 100,
            DocumentGuid = Guid.NewGuid(),
            FileName = "test.pdf",
            Remark = "Test remark"
        };

        _mockSocialDetailsDocumentService.Setup(s => s.UploadSocialDetailsDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            formDto.PropertyId,
            formDto.SocialAttributeId,
            formDto.Remark,
            It.IsAny<int>(),
            formDto.IsPhoto,
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UploadSocialDocument(formDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDocumentResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Document uploaded successfully", apiResponse.Message);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(10, apiResponse.Items.PropertySocialDetailId);
    }

    [Fact]
    public async Task UploadSocialDocument_ReturnsBadRequest_WhenFileIsNull()
    {
        // Arrange
        var formDto = new SocialDetailsDocumentUploadFormDto
        {
            File = null!,
            PropertyId = 1,
            SocialAttributeId = 1
        };

        // Act
        var result = await _controller.UploadSocialDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("File is required", apiResponse.Message);
    }

    [Fact]
    public async Task UploadSocialDocument_ReturnsBadRequest_WhenPropertyIdInvalid()
    {
        // Arrange
        var formDto = new SocialDetailsDocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf"),
            PropertyId = 0,
            SocialAttributeId = 1
        };

        // Act
        var result = await _controller.UploadSocialDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("PropertyId is required", apiResponse.Message);
    }

    [Fact]
    public async Task UploadSocialDocument_ReturnsBadRequest_WhenSocialAttributeIdInvalid()
    {
        // Arrange
        var formDto = new SocialDetailsDocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf"),
            PropertyId = 1,
            SocialAttributeId = 0
        };

        // Act
        var result = await _controller.UploadSocialDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("SocialAttributeId is required", apiResponse.Message);
    }

    [Fact]
    public async Task UploadSocialDocument_ReturnsBadRequest_WhenFileTypeInvalid()
    {
        // Arrange
        var formDto = new SocialDetailsDocumentUploadFormDto
        {
            File = CreateMockFormFile("malicious.exe", "application/x-msdownload"),
            PropertyId = 1,
            SocialAttributeId = 1
        };

        // Act
        var result = await _controller.UploadSocialDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid file type", apiResponse.Message);
    }

    #endregion

    #region Replace Document Tests

    [Fact]
    public async Task ReplaceSocialDocument_ReturnsOk_WhenReplaceSuccessful()
    {
        // Arrange
        var propertySocialDetailId = 1;
        var formDto = new ReplaceSocialDetailsDocumentFormDto
        {
            File = CreateMockFormFile("new.pdf", "application/pdf"),
            Remark = "Updated document"
        };

        var expectedResponse = new PropertySocialDetailsDocumentResponseDto
        {
            PropertySocialDetailId = propertySocialDetailId,
            DocumentGuid = Guid.NewGuid(),
            FileName = "new.pdf"
        };

        _mockSocialDetailsDocumentService.Setup(s => s.ReplaceSocialDetailsDocumentAsync(
            propertySocialDetailId,
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            formDto.Remark,
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ReplaceSocialDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDocumentResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Document replaced successfully", apiResponse.Message);
    }

    [Fact]
    public async Task ReplaceSocialDocument_ReturnsNotFound_WhenRecordNotFound()
    {
        // Arrange
        var propertySocialDetailId = 999;
        var formDto = new ReplaceSocialDetailsDocumentFormDto
        {
            File = CreateMockFormFile("new.pdf", "application/pdf")
        };

        _mockSocialDetailsDocumentService.Setup(s => s.ReplaceSocialDetailsDocumentAsync(
            propertySocialDetailId,
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException($"PropertySocialDetails with ID {propertySocialDetailId} not found"));

        // Act
        var result = await _controller.ReplaceSocialDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task ReplaceSocialDocument_ReturnsBadRequest_WhenFileIsNull()
    {
        // Arrange
        var propertySocialDetailId = 1;
        var formDto = new ReplaceSocialDetailsDocumentFormDto
        {
            File = null!,
            Remark = "Test"
        };

        // Act
        var result = await _controller.ReplaceSocialDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("File is required", apiResponse.Message);
    }

    [Fact]
    public async Task ReplaceSocialDocument_ReturnsBadRequest_WhenPropertySocialDetailIdInvalid()
    {
        // Arrange
        var propertySocialDetailId = 0;
        var formDto = new ReplaceSocialDetailsDocumentFormDto
        {
            File = CreateMockFormFile("new.pdf", "application/pdf")
        };

        // Act
        var result = await _controller.ReplaceSocialDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid PropertySocialDetailId", apiResponse.Message);
    }

    [Fact]
    public async Task ReplaceSocialDocument_ReturnsBadRequest_WhenFileTypeInvalid()
    {
        // Arrange
        var propertySocialDetailId = 1;
        var formDto = new ReplaceSocialDetailsDocumentFormDto
        {
            File = CreateMockFormFile("malicious.exe", "application/x-msdownload")
        };

        // Act
        var result = await _controller.ReplaceSocialDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid file type", apiResponse.Message);
    }

    #endregion

    #region Delete Social Document Tests

    [Fact]
    public async Task DeleteSocialDocument_ReturnsBadRequest_WhenIdInvalid()
    {
        // Act
        var result = await _controller.DeleteSocialDocument(0, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid PropertySocialDetailId", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteSocialDocument_ReturnsUnauthorized_OnUnauthorizedAccessException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext(); // No user identity
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.DeleteSocialDocument(123, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteSocialDocument_ReturnsBadRequest_OnInvalidOperationException()
    {
        // Arrange
        _mockSocialDetailsDocumentService.Setup(s => s.DeleteSocialDetailsDocumentAsync(
            123, 1, It.IsAny<CancellationToken>(), false, true, false))
            .ThrowsAsync(new InvalidOperationException("Not found"));

        // Act
        var result = await _controller.DeleteSocialDocument(123, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Not found", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteSocialDocument_ReturnsBadRequest_OnArgumentException()
    {
        // Arrange
        _mockSocialDetailsDocumentService.Setup(s => s.DeleteSocialDetailsDocumentAsync(
            123, 1, It.IsAny<CancellationToken>(), false, true, false))
            .ThrowsAsync(new ArgumentException("Invalid attribute"));

        // Act
        var result = await _controller.DeleteSocialDocument(123, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid attribute", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteSocialDocument_ReturnsOk_OnSuccessfulDelete()
    {
        // Arrange
        _mockSocialDetailsDocumentService.Setup(s => s.DeleteSocialDetailsDocumentAsync(
            123, 1, It.IsAny<CancellationToken>(), false, true, false))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteSocialDocument(123, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("Document deleted successfully", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteSocialDocument_Returns500_OnGenericException()
    {
        // Arrange
        _mockSocialDetailsDocumentService.Setup(s => s.DeleteSocialDetailsDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("unexpected"));

        // Act
        var result = await _controller.DeleteSocialDocument(123, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region Helper Methods

    private static IFormFile CreateMockFormFile(string fileName, string contentType)
    {
        var content = "Test file content"u8.ToArray();
        var stream = new MemoryStream(content);
        var mockFile = new Mock<IFormFile>();

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        return mockFile.Object;
    }

    #endregion
}
