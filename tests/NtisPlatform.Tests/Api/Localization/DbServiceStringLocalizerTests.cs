using NtisPlatform.Application.Interfaces;
using Microsoft.Extensions.Localization;
using Moq;
using NtisPlatform.Api.Localization;
using NtisPlatform.Core.Constants;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace NtisPlatform.Tests.Api.Localization;

public class DbServiceStringLocalizerTests
{
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly DefaultHttpContext _httpContext;

    public DbServiceStringLocalizerTests()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        // Use DefaultHttpContext so Items dictionary works properly
        _httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);
    }

    [Fact]
    public void Indexer_WithKey_ReturnsTranslation()
    {
        // Arrange
        var resource = "TestResource";
        var key = "TestKey";
        var expectedValue = "Translated Value";

        _httpContext.Items[HttpContextKeys.CurrentLanguage] = "en";
        _mockLocalizationService.Setup(x => x.GetTranslation(resource, "en", key))
            .Returns(expectedValue);

        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer[key];

        // Assert
        Assert.Equal(key, result.Name);
        Assert.Equal(expectedValue, result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WithKey_WhenTranslationNotFound_ReturnsKeyAsValue()
    {
        // Arrange
        var resource = "TestResource";
        var key = "TestKey";

        _httpContext.Items[HttpContextKeys.CurrentLanguage] = "en";
        _mockLocalizationService.Setup(x => x.GetTranslation(resource, "en", key))
            .Returns(key); // Service returns key when not found

        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer[key];

        // Assert
        Assert.Equal(key, result.Name);
        Assert.Equal(key, result.Value);
        Assert.True(result.ResourceNotFound);
    }

    [Fact]
    public void Indexer_WithoutHttpContext_UsesDefaultLanguage()
    {
        // Arrange
        var resource = "TestResource";
        var key = "TestKey";
        var expectedValue = "Default Translation";

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _mockLocalizationService.Setup(x => x.GetTranslation(resource, "en", key))
            .Returns(expectedValue);

        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer[key];

        // Assert
        Assert.Equal(expectedValue, result.Value);
        _mockLocalizationService.Verify(x => x.GetTranslation(resource, "en", key), Times.Once);
    }

    [Fact]
    public void Indexer_WithDifferentLanguage_UsesLanguageFromContext()
    {
        // Arrange
        var resource = "TestResource";
        var key = "TestKey";
        var expectedValue = "Hindi Translation";

        _httpContext.Items[HttpContextKeys.CurrentLanguage] = "hi-IN";
        _mockLocalizationService.Setup(x => x.GetTranslation(resource, "hi-IN", key))
            .Returns(expectedValue);

        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer[key];

        // Assert
        Assert.Equal(expectedValue, result.Value);
        _mockLocalizationService.Verify(x => x.GetTranslation(resource, "hi-IN", key), Times.Once);
    }

    [Fact]
    public void Indexer_WithMarathiLanguage_UsesCorrectLanguage()
    {
        // Arrange
        var resource = "TestResource";
        var key = "TestKey";
        var expectedValue = "Marathi Translation";

        _httpContext.Items[HttpContextKeys.CurrentLanguage] = "mr-IN";
        _mockLocalizationService.Setup(x => x.GetTranslation(resource, "mr-IN", key))
            .Returns(expectedValue);

        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer[key];

        // Assert
        Assert.Equal(expectedValue, result.Value);
        _mockLocalizationService.Verify(x => x.GetTranslation(resource, "mr-IN", key), Times.Once);
    }

    [Fact]
    public void IndexerWithArguments_FormatsStringCorrectly()
    {
        // Arrange
        var resource = "TestResource";
        var key = "TestKey";
        var translationTemplate = "Hello {0}, you have {1} messages";

        _httpContext.Items[HttpContextKeys.CurrentLanguage] = "en";
        _mockLocalizationService.Setup(x => x.GetTranslation(resource, "en", key))
            .Returns(translationTemplate);

        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer[key, "John", 5];

        // Assert
        Assert.Equal(key, result.Name);
        Assert.Equal("Hello John, you have 5 messages", result.Value);
        Assert.False(result.ResourceNotFound);
    }

    [Fact]
    public void IndexerWithArguments_WhenNotFound_FormatsKeyWithArguments()
    {
        // Arrange
        var resource = "TestResource";
        var key = "TestKey";

        _httpContext.Items[HttpContextKeys.CurrentLanguage] = "en";
        _mockLocalizationService.Setup(x => x.GetTranslation(resource, "en", key))
            .Returns(key); // Not found

        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer[key, "arg1", "arg2"];

        // Assert
        Assert.Equal(key, result.Name);
        Assert.True(result.ResourceNotFound);
    }

    [Fact]
    public void GetAllStrings_ReturnsEmptyEnumerable()
    {
        // Arrange
        var resource = "TestResource";
        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer.GetAllStrings(includeParentCultures: true);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllStrings_WithIncludeParentCulturesFalse_ReturnsEmptyEnumerable()
    {
        // Arrange
        var resource = "TestResource";
        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);

        // Act
        var result = localizer.GetAllStrings(includeParentCultures: false);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void WithCulture_ReturnsSameInstance()
    {
        // Arrange
        var resource = "TestResource";
        var localizer = new DbServiceStringLocalizer(_mockLocalizationService.Object, resource, _mockHttpContextAccessor.Object);
        var culture = new System.Globalization.CultureInfo("hi-IN");

        // Act
        var result = localizer.WithCulture(culture);

        // Assert
        Assert.Same(localizer, result);
    }
}

public class DbServiceStringLocalizerFactoryTests
{
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    public DbServiceStringLocalizerFactoryTests()
    {
        _mockLocalizationService = new Mock<ILocalizationService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
    }

    [Fact]
    public void Create_WithType_CreatesLocalizerWithTypeName()
    {
        // Arrange
        var factory = new DbServiceStringLocalizerFactory(_mockLocalizationService.Object, _mockHttpContextAccessor.Object);
        var resourceType = typeof(DbServiceStringLocalizerFactoryTests);

        // Act
        var localizer = factory.Create(resourceType);

        // Assert
        Assert.NotNull(localizer);
        Assert.IsType<DbServiceStringLocalizer>(localizer);
    }

    [Fact]
    public void Create_WithBaseName_CreatesLocalizerWithBaseName()
    {
        // Arrange
        var factory = new DbServiceStringLocalizerFactory(_mockLocalizationService.Object, _mockHttpContextAccessor.Object);
        var baseName = "TestResource";
        var location = "TestLocation";

        // Act
        var localizer = factory.Create(baseName, location);

        // Assert
        Assert.NotNull(localizer);
        Assert.IsType<DbServiceStringLocalizer>(localizer);
    }

    [Fact]
    public void Create_WithNullLocation_CreatesLocalizer()
    {
        // Arrange
        var factory = new DbServiceStringLocalizerFactory(_mockLocalizationService.Object, _mockHttpContextAccessor.Object);
        var baseName = "TestResource";

        // Act
        var localizer = factory.Create(baseName, location: null);

        // Assert
        Assert.NotNull(localizer);
        Assert.IsType<DbServiceStringLocalizer>(localizer);
    }

    [Fact]
    public void Create_WithDifferentTypes_CreatesDifferentLocalizers()
    {
        // Arrange
        var factory = new DbServiceStringLocalizerFactory(_mockLocalizationService.Object, _mockHttpContextAccessor.Object);
        var type1 = typeof(DbServiceStringLocalizerFactoryTests);
        var type2 = typeof(DbServiceStringLocalizerTests);

        // Act
        var localizer1 = factory.Create(type1);
        var localizer2 = factory.Create(type2);

        // Assert
        Assert.NotNull(localizer1);
        Assert.NotNull(localizer2);
        Assert.NotSame(localizer1, localizer2);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesFactory()
    {
        // Arrange & Act
        var factory = new DbServiceStringLocalizerFactory(_mockLocalizationService.Object, _mockHttpContextAccessor.Object);

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void Create_CreatesWorkingLocalizer()
    {
        // Arrange
        var mockHttpContext = new Mock<HttpContext>();
        mockHttpContext.Setup(x => x.Items[HttpContextKeys.CurrentLanguage]).Returns("en");
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        _mockLocalizationService.Setup(x => x.GetTranslation(It.IsAny<string>(), "en", "TestKey"))
            .Returns("Test Value");

        var factory = new DbServiceStringLocalizerFactory(_mockLocalizationService.Object, _mockHttpContextAccessor.Object);

        // Act
        var localizer = factory.Create("TestResource", null);
        var result = localizer["TestKey"];

        // Assert
        Assert.Equal("Test Value", result.Value);
    }
}
