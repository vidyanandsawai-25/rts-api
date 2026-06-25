using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.PropertyDiscount;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Tests for PropertyController Discount Details endpoints
/// </summary>
public class PropertyControllerDiscountDetailsTests
{
    private readonly Mock<IPropertyService> _mockPropertyService;
    private readonly Mock<IPropertyDiscountService> _mockDiscountService;
    private readonly Mock<ILogger<PropertyController>> _mockLogger;
    private readonly Mock<IPropertySocialDetailsDocumentService> _mockSocialDetailsDocumentService;
    private readonly PropertyController _controller;

    public PropertyControllerDiscountDetailsTests()
    {
        _mockPropertyService = new Mock<IPropertyService>();
        _mockDiscountService = new Mock<IPropertyDiscountService>();
        _mockLogger = new Mock<ILogger<PropertyController>>();
        _mockSocialDetailsDocumentService = new Mock<IPropertySocialDetailsDocumentService>();

        // Create controller with all dependencies
        var mockEnvironment = new Mock<IWebHostEnvironment>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var fileValidationHelper = new FileValidationHelper(configuration);

        _controller = new PropertyController(
            _mockPropertyService.Object,
            new Mock<IPropertyBasicDetailsService>().Object,
            new Mock<IPropertyKycService>().Object,
            new Mock<IPropertySocietyService>().Object,
            _mockDiscountService.Object,
            new Mock<IPropertyOldDetailsService>().Object,
            new Mock<IPropertySearchService>().Object,
            _mockLogger.Object,
            _mockSocialDetailsDocumentService.Object,
            mockEnvironment.Object,
            fileValidationHelper,
            new Mock<IPropertyWorkflowDetailsService>().Object);

        // Set up HttpContext with authenticated user
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "TestAuth"));
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    #region GetDiscountDetails Tests

    [Fact]
    public async Task GetDiscountDetails_ReturnsOk_WhenPropertyFound()
    {
        // Arrange
        var propertyId = 1;
        var expectedResponse = new PropertyDiscountInfoResponseDto
        {
            PropertyId = propertyId,
            DiscountAttributes = new List<DiscountAttributeDto>
            {
                new() { Id = 1, SocialAttributeCode = "SOLAR", SocialAttributeName = "Solar Panel" }
            }
        };

        _mockDiscountService.Setup(s => s.GetDiscountDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetDiscountDetails(propertyId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Discount information retrieved successfully", apiResponse.Message);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(propertyId, apiResponse.Items.PropertyId);
    }

    [Fact]
    public async Task GetDiscountDetails_ReturnsNotFound_WhenPropertyDoesNotExist()
    {
        // Arrange
        var propertyId = 999;
        _mockDiscountService.Setup(s => s.GetDiscountDetailsAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDiscountInfoResponseDto?)null);

        // Act
        var result = await _controller.GetDiscountDetails(propertyId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message);
    }

    #endregion

    #region UpdateDiscountDetails Tests

    [Fact]
    public async Task UpdateDiscountDetails_ReturnsOk_WhenUpdateSuccessful()
    {
        // Arrange
        var propertyId = 1;
        var updateDto = new UpsertPropertyDiscountInfoDto
        {
            PropertyId = propertyId,
            UpdatedBy = 1,
            DiscountAttributes = new List<DiscountAttributeItemDto>
            {
                new() { SocialAttributeId = 1, BitValue = true }
            }
        };

        var expectedResponse = new PropertyDiscountInfoResponseDto
        {
            PropertyId = propertyId,
            DiscountAttributes = new List<DiscountAttributeDto>()
        };

        _mockDiscountService.Setup(s => s.UpdateDiscountDetailsAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateDiscountDetails(propertyId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Discount information updated successfully", apiResponse.Message);
    }

    [Fact]
    public async Task UpdateDiscountDetails_ReturnsBadRequest_WhenPropertyIdMismatch()
    {
        // Arrange
        var propertyId = 1;
        var updateDto = new UpsertPropertyDiscountInfoDto
        {
            PropertyId = 2, // Different ID
            UpdatedBy = 1
        };

        // Act
        var result = await _controller.UpdateDiscountDetails(propertyId, updateDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("does not match", apiResponse.Message);
    }

    [Fact]
    public async Task UpdateDiscountDetails_ReturnsNotFound_WhenPropertyDoesNotExist()
    {
        // Arrange
        var propertyId = 999;
        var updateDto = new UpsertPropertyDiscountInfoDto
        {
            PropertyId = propertyId,
            UpdatedBy = 1
        };

        _mockDiscountService.Setup(s => s.UpdateDiscountDetailsAsync(propertyId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyDiscountInfoResponseDto?)null);

        // Act
        var result = await _controller.UpdateDiscountDetails(propertyId, updateDto, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertyDiscountInfoResponseDto>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Document Upload Tests

    [Fact]
    public async Task UploadDiscountDocument_ReturnsBadRequest_WhenFileIsNull()
    {
        // Arrange
        var formDto = new DiscountDocumentUploadFormDto
        {
            File = null!,
            PropertyId = 1,
            SocialAttributeId = 1
        };

        // Act
        var result = await _controller.UploadDiscountDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("File is required", apiResponse.Message);
    }

    [Fact]
    public async Task UploadDiscountDocument_ReturnsBadRequest_WhenPropertyIdInvalid()
    {
        // Arrange
        var formDto = new DiscountDocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf"),
            PropertyId = 0,
            SocialAttributeId = 1
        };

        // Act
        var result = await _controller.UploadDiscountDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("PropertyId is required", apiResponse.Message);
    }

    [Fact]
    public async Task UploadDiscountDocument_ReturnsBadRequest_WhenSocialAttributeIdInvalid()
    {
        // Arrange
        var formDto = new DiscountDocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf"),
            PropertyId = 1,
            SocialAttributeId = 0
        };

        // Act
        var result = await _controller.UploadDiscountDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("SocialAttributeId is required", apiResponse.Message);
    }

    [Fact]
    public async Task UploadDiscountDocument_ValidatesPropertyExistsAndAttributeIsDiscountApplicable()
    {
        // Arrange - This test verifies the service validates property and attribute
        var formDto = new DiscountDocumentUploadFormDto
        {
            File = CreateMockFormFile("test.pdf", "application/pdf"),
            PropertyId = 999,
            SocialAttributeId = 1
        };

        _mockSocialDetailsDocumentService.Setup(s => s.UploadSocialDetailsDocumentAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            formDto.PropertyId,
            formDto.SocialAttributeId,
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>(),
            It.IsAny<bool>()))
            .ThrowsAsync(new ArgumentException("Property with ID 999 not found", "propertyId"));

        // Act
        var result = await _controller.UploadDiscountDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task UploadDiscountDocument_ReturnsBadRequest_WhenFileTypeInvalid()
    {
        // Arrange
        var formDto = new DiscountDocumentUploadFormDto
        {
            File = CreateMockFormFile("malicious.exe", "application/x-msdownload"),
            PropertyId = 1,
            SocialAttributeId = 1
        };

        // Act
        var result = await _controller.UploadDiscountDocument(formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid file type", apiResponse.Message);
    }

    #endregion

    #region Replace Document Tests

    [Fact]
    public async Task ReplaceDiscountDocument_ReturnsOk_WhenReplaceSuccessful()
    {
        // Arrange
        var propertySocialDetailId = 1;
        var formDto = new ReplaceDiscountDocumentFormDto
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
        var result = await _controller.ReplaceDiscountDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<PropertySocialDetailsDocumentResponseDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Equal("Discount document replaced successfully", apiResponse.Message);
    }

    [Fact]
    public async Task ReplaceDiscountDocument_ReturnsNotFound_WhenRecordNotFound()
    {
        // Arrange
        var propertySocialDetailId = 999;
        var formDto = new ReplaceDiscountDocumentFormDto
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
        var result = await _controller.ReplaceDiscountDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task ReplaceDiscountDocument_ReturnsBadRequest_WhenFileIsNull()
    {
        // Arrange
        var propertySocialDetailId = 1;
        var formDto = new ReplaceDiscountDocumentFormDto
        {
            File = null!,
            Remark = "Test"
        };

        // Act
        var result = await _controller.ReplaceDiscountDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("File is required", apiResponse.Message);
    }

    [Fact]
    public async Task ReplaceDiscountDocument_ReturnsBadRequest_WhenPropertySocialDetailIdInvalid()
    {
        // Arrange
        var propertySocialDetailId = 0;
        var formDto = new ReplaceDiscountDocumentFormDto
        {
            File = CreateMockFormFile("new.pdf", "application/pdf")
        };

        // Act
        var result = await _controller.ReplaceDiscountDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid PropertySocialDetailId", apiResponse.Message);
    }

    [Fact]
    public async Task ReplaceDiscountDocument_RejectsNonDiscountApplicableAttribute()
    {
        // Arrange - This test ensures replace endpoint only works with discount-applicable attributes
        var propertySocialDetailId = 1;
        var formDto = new ReplaceDiscountDocumentFormDto
        {
            File = CreateMockFormFile("new.pdf", "application/pdf"),
            Remark = "Updated document"
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
            .ThrowsAsync(new ArgumentException(
                $"PropertySocialDetails with ID {propertySocialDetailId} is not linked to a discount-applicable SocialAttribute.",
                nameof(propertySocialDetailId)));

        // Act
        var result = await _controller.ReplaceDiscountDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not linked to a discount-applicable SocialAttribute", apiResponse.Message);
    }

    [Fact]
    public async Task ReplaceDiscountDocument_ReturnsBadRequest_WhenFileTypeInvalid()
    {
        // Arrange
        var propertySocialDetailId = 1;
        var formDto = new ReplaceDiscountDocumentFormDto
        {
            File = CreateMockFormFile("malicious.exe", "application/x-msdownload")
        };

        // Act
        var result = await _controller.ReplaceDiscountDocument(propertySocialDetailId, formDto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid file type", apiResponse.Message);
    }

    #endregion

    #region Delete Discount Document Tests

    [Fact]
    public async Task DeleteDiscountDocument_ReturnsBadRequest_WhenIdInvalid()
    {
        // Act
        var result = await _controller.DeleteDiscountDocument(0, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid PropertySocialDetailId", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteDiscountDocument_ReturnsUnauthorized_OnUnauthorizedAccessException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext(); // No user identity
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.DeleteDiscountDocument(123, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteDiscountDocument_ReturnsBadRequest_OnInvalidOperationException()
    {
        // Arrange
        _mockSocialDetailsDocumentService.Setup(s => s.DeleteSocialDetailsDocumentAsync(
            123, 1, It.IsAny<CancellationToken>(), true, false, false))
            .ThrowsAsync(new InvalidOperationException("Not found"));

        // Act
        var result = await _controller.DeleteDiscountDocument(123, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Not found", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteDiscountDocument_ReturnsBadRequest_OnArgumentException()
    {
        // Arrange
        _mockSocialDetailsDocumentService.Setup(s => s.DeleteSocialDetailsDocumentAsync(
            123, 1, It.IsAny<CancellationToken>(), true, false, false))
            .ThrowsAsync(new ArgumentException("Invalid attribute"));

        // Act
        var result = await _controller.DeleteDiscountDocument(123, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("Invalid attribute", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteDiscountDocument_ReturnsOk_OnSuccessfulDelete()
    {
        // Arrange
        _mockSocialDetailsDocumentService.Setup(s => s.DeleteSocialDetailsDocumentAsync(
            123, 1, It.IsAny<CancellationToken>(), true, false, false))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteDiscountDocument(123, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("Discount document deleted successfully", apiResponse.Message);
    }

    [Fact]
    public async Task DeleteDiscountDocument_Returns500_OnGenericException()
    {
        // Arrange
        _mockSocialDetailsDocumentService.Setup(s => s.DeleteSocialDetailsDocumentAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("unexpected"));

        // Act
        var result = await _controller.DeleteDiscountDocument(123, CancellationToken.None);

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
