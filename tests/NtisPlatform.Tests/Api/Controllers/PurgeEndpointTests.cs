using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Unit tests for purge (permanent delete) endpoints.
/// Tests authorization expectations, FK conflicts, and semantic differences.
/// </summary>
public class PurgeEndpointTests
{
    #region Authorization and Route Tests

    [Fact]
    public void PurgeEndpoint_HasAuthorizeAttribute()
    {
        // Arrange
        var method = typeof(RateSectionDetailsController).GetMethod(nameof(RateSectionDetailsController.Purge));

        // Act
        var attributes = method?.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false);

        // Assert
        Assert.NotNull(attributes);
        Assert.NotEmpty(attributes);
    }

    [Fact]
    public void PurgeEndpoint_HasHttpDeleteAttribute()
    {
        // Arrange
        var method = typeof(RateSectionDetailsController).GetMethod(nameof(RateSectionDetailsController.Purge));

        // Act
        var attributes = method?.GetCustomAttributes(typeof(HttpDeleteAttribute), false);

        // Assert
        Assert.NotNull(attributes);
        Assert.NotEmpty(attributes);
    }

    [Fact]
    public void PurgeEndpoint_HasCorrectRouteTemplate()
    {
        // Arrange
        var method = typeof(RateSectionDetailsController).GetMethod(nameof(RateSectionDetailsController.Purge));

        // Act
        var attribute = method?.GetCustomAttributes(typeof(HttpDeleteAttribute), false)
            .FirstOrDefault() as HttpDeleteAttribute;

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("{id}/purge", attribute.Template);
    }

    #endregion

    #region FK Conflict Behavioral Tests

    [Fact]
    public async Task PurgeEndpoint_WhenForeignKeyConflict_Returns409Conflict()
    {
        // Arrange
        var mockService = new Mock<IRateSectionDetailsService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        var mockLogger = new Mock<ILogger<RateSectionDetailsController>>();

        var controller = new RateSectionDetailsController(
            mockService.Object,
            mockCleanupService.Object,
            mockReferenceValidationService.Object,
            mockLogger.Object);

        // Simulate FK violation with fallback message check
        var innerException = new Exception("The DELETE statement conflicted with the FOREIGN KEY constraint");
        var dbException = new Microsoft.EntityFrameworkCore.DbUpdateException(
            "An error occurred while updating the entries.", innerException);

        mockCleanupService
            .Setup(s => s.ForceHardDeleteAsync<RateSectionDetailsEntity, int>(
                1,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbException);

        // The mock for GetReferencingTablesWithDataAsync won't be called in this scenario
        // because the code will use the fallback path (not SqlException with number 547)

        // Act
        var result = await controller.Purge(1, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);

        var response = Assert.IsType<ApiResponse<object>>(
            conflictResult.Value);

        Assert.False(response.Success);
        Assert.Contains("Cannot delete this record", response.Message);
        // When using fallback path, the message will contain "unknown reference"
        Assert.Contains("unknown reference", response.Message);
    }

    #endregion

    #region Semantic Difference Tests (Soft Delete vs Permanent Delete)

    [Fact]
    public async Task SoftDelete_ReturnsSuccess_WithMarkedForDeletionMessage()
    {
        // Arrange
        var mockService = new Mock<IRateSectionDetailsService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        var mockLogger = new Mock<ILogger<RateSectionDetailsController>>();

        var controller = new RateSectionDetailsController(
            mockService.Object,
            mockCleanupService.Object,
            mockReferenceValidationService.Object,
            mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - soft delete
        var result = await controller.Delete(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.True(response?.Success);
        Assert.Contains("marked for deletion", response?.Message?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PermanentDelete_ReturnsSuccess_WithPermanentlyDeletedMessage()
    {
        // Arrange
        var mockService = new Mock<IRateSectionDetailsService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        var mockLogger = new Mock<ILogger<RateSectionDetailsController>>();

        var controller = new RateSectionDetailsController(
            mockService.Object,
            mockCleanupService.Object,
            mockReferenceValidationService.Object,
            mockLogger.Object);

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RateSectionDetailsEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - permanent delete
        var result = await controller.Purge(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Contains("permanently deleted", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SoftDelete_And_PermanentDelete_HaveDifferentMessages()
    {
        // Arrange
        var mockService = new Mock<IRateSectionDetailsService>();
        var mockCleanupService = new Mock<IHardDeleteCleanupService>();
        var mockReferenceValidationService = new Mock<IReferenceValidationService>();
        var mockLogger = new Mock<ILogger<RateSectionDetailsController>>();

        var controller = new RateSectionDetailsController(
            mockService.Object,
            mockCleanupService.Object,
            mockReferenceValidationService.Object,
            mockLogger.Object);

        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockCleanupService.Setup(s => s.ForceHardDeleteAsync<RateSectionDetailsEntity, int>(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var softDeleteResult = await controller.Delete(1, CancellationToken.None);
        var permanentDeleteResult = await controller.Purge(2, CancellationToken.None);

        // Assert
        var softOk = Assert.IsType<OkObjectResult>(softDeleteResult);
        var permOk = Assert.IsType<OkObjectResult>(permanentDeleteResult);

        var softResponse = softOk.Value as dynamic;
        var permResponse = Assert.IsType<ApiResponse<object>>(permOk.Value);

        // Messages should be different
        Assert.NotEqual(softResponse?.Message?.ToString(), permResponse.Message);
        Assert.Contains("marked", softResponse?.Message?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permanently", permResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
