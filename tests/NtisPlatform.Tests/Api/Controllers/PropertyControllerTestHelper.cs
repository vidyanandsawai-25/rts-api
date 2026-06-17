using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
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
        Mock<ILogger<PropertyController>> logger,
        Mock<IPropertyBasicDetailsService>? basicDetailsService = null,
        Mock<IPropertyKycService>? kycService = null,
        Mock<IPropertySocietyService>? societyService = null,
        Mock<IPropertyDiscountService>? discountService = null,
        Mock<IPropertyOldDetailsService>? oldDetailsService = null,
        Mock<IPropertySearchService>? searchService = null)
    {
        basicDetailsService ??= new Mock<IPropertyBasicDetailsService>();
        kycService ??= new Mock<IPropertyKycService>();
        societyService ??= new Mock<IPropertySocietyService>();
        discountService ??= new Mock<IPropertyDiscountService>();
        oldDetailsService ??= new Mock<IPropertyOldDetailsService>();
        searchService ??= new Mock<IPropertySearchService>();
        var mockSocialDetailsDocumentService = new Mock<IPropertySocialDetailsDocumentService>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();

        // Create a simple in-memory configuration with default file validation settings
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var fileValidationHelper = new FileValidationHelper(configuration);

        return new PropertyController(
            propertyService.Object,
            basicDetailsService.Object,
            kycService.Object,
            societyService.Object,
            discountService.Object,
            oldDetailsService.Object,
            searchService.Object,
            logger.Object,
            mockSocialDetailsDocumentService.Object,
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
        Mock<IPropertySocialDetailsDocumentService> SocialDetailsDocumentService,
        Mock<IWebHostEnvironment> Environment,
        FileValidationHelper FileValidationHelper
    ) CreateControllerWithMocks()
    {
        var mockPropertyService = new Mock<IPropertyService>();
        var mockBasicDetailsService = new Mock<IPropertyBasicDetailsService>();
        var mockKycService = new Mock<IPropertyKycService>();
        var mockSocietyService = new Mock<IPropertySocietyService>();
        var mockDiscountService = new Mock<IPropertyDiscountService>();
        var mockOldDetailsService = new Mock<IPropertyOldDetailsService>();
        var mockSearchService = new Mock<IPropertySearchService>();
        var mockLogger = new Mock<ILogger<PropertyController>>();
        var mockSocialDetailsDocumentService = new Mock<IPropertySocialDetailsDocumentService>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();

        // Create a simple in-memory configuration with default file validation settings
        var configData = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var fileValidationHelper = new FileValidationHelper(configuration);

        var controller = new PropertyController(
            mockPropertyService.Object,
            mockBasicDetailsService.Object,
            mockKycService.Object,
            mockSocietyService.Object,
            mockDiscountService.Object,
            mockOldDetailsService.Object,
            mockSearchService.Object,
            mockLogger.Object,
            mockSocialDetailsDocumentService.Object,
            mockEnvironment.Object,
            fileValidationHelper);

        return (
            controller,
            mockPropertyService,
            mockLogger,
            mockSocialDetailsDocumentService,
            mockEnvironment,
            fileValidationHelper
        );
    }
}
