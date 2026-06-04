using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using System.Collections.Generic;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Helper class for creating PropertyController instances in tests with all required dependencies
/// </summary>
public static class PropertyControllerTestHelper
{
    /// <summary>
    /// Creates a PropertyController instance with mocked dependencies for testing
    /// </summary>
    /// <param name="propertyService">Mock of IPropertyService</param>
    /// <param name="logger">Mock of ILogger</param>
    /// <returns>PropertyController instance ready for testing</returns>
    public static PropertyController CreateController(
        Mock<IPropertyService> propertyService,
        Mock<ILogger<PropertyController>> logger)
    {
        var mockDiscountDocumentService = new Mock<IPropertyDiscountDocumentService>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();

        // Create a simple in-memory configuration with default file validation settings
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var fileValidationHelper = new FileValidationHelper(configuration);

        return new PropertyController(
            propertyService.Object,
            logger.Object,
            mockDiscountDocumentService.Object,
            mockEnvironment.Object,
            fileValidationHelper);
    }

    /// <summary>
    /// Creates a PropertyController instance with all mocked dependencies
    /// </summary>
    /// <returns>Tuple containing the controller and all mocked dependencies</returns>
    public static (
        PropertyController Controller,
        Mock<IPropertyService> PropertyService,
        Mock<ILogger<PropertyController>> Logger,
        Mock<IPropertyDiscountDocumentService> DiscountDocumentService,
        Mock<IWebHostEnvironment> Environment,
        FileValidationHelper FileValidationHelper
    ) CreateControllerWithMocks()
    {
        var mockPropertyService = new Mock<IPropertyService>();
        var mockLogger = new Mock<ILogger<PropertyController>>();
        var mockDiscountDocumentService = new Mock<IPropertyDiscountDocumentService>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();

        // Create a simple in-memory configuration with default file validation settings
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var fileValidationHelper = new FileValidationHelper(configuration);

        var controller = new PropertyController(
            mockPropertyService.Object,
            mockLogger.Object,
            mockDiscountDocumentService.Object,
            mockEnvironment.Object,
            fileValidationHelper);

        return (
            controller,
            mockPropertyService,
            mockLogger,
            mockDiscountDocumentService,
            mockEnvironment,
            fileValidationHelper
        );
    }
}
