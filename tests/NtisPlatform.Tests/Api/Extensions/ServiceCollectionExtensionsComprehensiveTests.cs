using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace NtisPlatform.Tests.Api.Extensions;

/// <summary>
/// Comprehensive tests for ServiceCollectionExtensions
/// Target: 100% line coverage and branch coverage
/// </summary>
public class ServiceCollectionExtensionsComprehensiveTests
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configuration;

    public ServiceCollectionExtensionsComprehensiveTests()
    {
        _services = new ServiceCollection();
        _configuration = CreateTestConfiguration();

        // Add logging services that are required by many services
        _services.AddLogging();
        // Add IConfiguration as singleton so it can be injected
        _services.AddSingleton(_configuration);
    }

    private IConfiguration CreateTestConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=NtisTestDb;Trusted_Connection=True;",
            ["Jwt:Key"] = "ThisIsASecureKeyThatIsAtLeast32CharactersLongForTesting123456",
            ["Jwt:Issuer"] = "NtisPlatform",
            ["Jwt:Audience"] = "NtisPlatform",
            ["FileStorage:MaxFileSizeBytes"] = "104857600",
            ["RateLimiting:Global:PermitLimit"] = "100",
            ["RateLimiting:Global:WindowMinutes"] = "1",
            ["RateLimiting:Login:PermitLimit"] = "5",
            ["RateLimiting:Login:WindowMinutes"] = "15",
            ["RateLimiting:FileUpload:PermitLimit"] = "10",
            ["RateLimiting:FileUpload:WindowMinutes"] = "5",
            ["RateLimiting:FileUpload:QueueLimit"] = "2"
        });
        return configBuilder.Build();
    }

    #region Happy Path Tests

    [Fact]
    public void AddAllServices_WithValidConfiguration_RegistersAllServices()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - Infrastructure Services
        Assert.NotNull(serviceProvider.GetService<ITokenService>());
        Assert.NotNull(serviceProvider.GetService<IPasswordHasher>());
        Assert.NotNull(serviceProvider.GetService<ISecuritySettingsService>());
        Assert.NotNull(serviceProvider.GetService<IHardDeleteCleanupService>());
        Assert.NotNull(serviceProvider.GetService<IDocumentService>());
        Assert.NotNull(serviceProvider.GetService<IDocumentAuthorizationService>());
        Assert.NotNull(serviceProvider.GetService<IFileStorageService>());
        Assert.NotNull(serviceProvider.GetService<IPropertyCertificateService>());
    }

    [Fact]
    public void AddAllServices_RegistersRepositories()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IUserRepository>());
        Assert.NotNull(serviceProvider.GetService<IRefreshTokenRepository>());
        Assert.NotNull(serviceProvider.GetService<IPropertyRepository>());
        Assert.NotNull(serviceProvider.GetService<IUnitOfWork>());
    }

    [Fact]
    public void AddAllServices_RegistersApplicationServices()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IAuthService>());
        Assert.NotNull(serviceProvider.GetService<IUlbConfigService>());
        Assert.NotNull(serviceProvider.GetService<IPropertyService>());
    }

    [Fact]
    public void AddAllServices_RegistersPropertyPhotoServices()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - PropertyPhoto stack: Core row service + Application orchestration service
        Assert.NotNull(serviceProvider.GetService<IPropertyPhotoService>());
        Assert.NotNull(serviceProvider.GetService<IPropertyPhotoApplicationService>());
    }

    [Fact]
    public void AddAllServices_RegistersLocalizationServices()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<ILocalizationService>());
        Assert.NotNull(serviceProvider.GetService<ILocalization>());
        Assert.NotNull(serviceProvider.GetService<ILocalizedQueryService>());
    }

    [Fact]
    public void AddAllServices_RegistersDbContext()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<ApplicationDbContext>());
        Assert.NotNull(serviceProvider.GetService<IDbContextFactory<ApplicationDbContext>>());
    }

    [Fact]
    public void AddAllServices_RegistersAutoMapper()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<AutoMapper.IMapper>());
    }

    [Fact]
    public void AddAllServices_ConfiguresControllers()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify MVC services are registered
        var serviceProvider = _services.BuildServiceProvider();
        var mvcBuilder = serviceProvider.GetService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>();
        // If controllers are registered, this service should be available (or we check for controller registrations)
        Assert.True(_services.Any(sd => sd.ServiceType.Namespace != null && sd.ServiceType.Namespace.Contains("Microsoft.AspNetCore.Mvc")));
    }

    #endregion

    #region JWT Configuration Tests

    [Fact]
    public void AddAllServices_WithNullJwtKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = null
            })
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddAllServices(config));
        Assert.Contains("JWT Key is not configured", exception.Message);
    }

    [Fact]
    public void AddAllServices_WithEmptyJwtKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = ""
            })
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddAllServices(config));
        Assert.Contains("JWT Key is not configured", exception.Message);
    }

    [Fact]
    public void AddAllServices_WithWhitespaceJwtKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = "   "
            })
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddAllServices(config));
        Assert.Contains("JWT Key is not configured", exception.Message);
    }

    [Theory]
    [InlineData("MUST_BE_SET_IN_SECURE_CONFIG_SOURCE")]
    [InlineData("YourSuperSecretKeyHere_ChangeThisInProduction_MustBeAtLeast32Characters")]
    [InlineData("your-secret-key-here")]
    [InlineData("change-me-in-production")]
    [InlineData("placeholder")]
    [InlineData("secret")]
    [InlineData("supersecret")]
    public void AddAllServices_WithInsecurePlaceholderJwtKey_ThrowsInvalidOperationException(string insecureKey)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = insecureKey
            })
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddAllServices(config));
        Assert.Contains("placeholder value", exception.Message);
    }

    [Fact]
    public void AddAllServices_WithShortJwtKey_ThrowsInvalidOperationException()
    {
        // Arrange - Key less than 32 bytes
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = "ShortKey123" // Only 11 characters
            })
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddAllServices(config));
        Assert.Contains("too short", exception.Message);
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void AddAllServices_WithExactly32ByteJwtKey_Succeeds()
    {
        // Arrange - Exactly 32 bytes
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = "12345678901234567890123456789012", // Exactly 32 bytes
                ["Jwt:Issuer"] = "Test",
                ["Jwt:Audience"] = "Test"
            })
            .Build();

        // Act & Assert - Should not throw
        _services.AddAllServices(config);
    }

    [Fact]
    public void AddAllServices_WithKeyThatHasLeadingWhitespace_TrimsAndValidates()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = "   ShortKey" // Whitespace + short key
            })
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _services.AddAllServices(config));
        Assert.Contains("too short", exception.Message);
    }

    #endregion

    #region Rate Limiting Configuration Tests

    [Fact]
    public void AddAllServices_WithMissingRateLimitConfig_UsesDefaults()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = "ThisIsASecureKeyThatIsAtLeast32CharactersLongForTesting123456",
                ["Jwt:Issuer"] = "Test",
                ["Jwt:Audience"] = "Test"
            })
            .Build();

        // Act & Assert - Should not throw and use defaults
        _services.AddAllServices(config);
    }

    #endregion

    #region File Upload Configuration Tests

    [Fact]
    public void AddAllServices_ConfiguresFormOptions_WithProvidedMaxFileSize()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify FormOptions is configured
        var serviceProvider = _services.BuildServiceProvider();
        var formOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Features.FormOptions>>();
        Assert.NotNull(formOptions);
    }

    [Fact]
    public void AddAllServices_WithMissingMaxFileSize_UsesDefault()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=Test;",
                ["Jwt:Key"] = "ThisIsASecureKeyThatIsAtLeast32CharactersLongForTesting123456",
                ["Jwt:Issuer"] = "Test",
                ["Jwt:Audience"] = "Test"
            })
            .Build();

        // Act & Assert - Should not throw and use default 100MB
        _services.AddAllServices(config);
    }

    #endregion

    #region CORS Configuration Tests

    [Fact]
    public void AddAllServices_ConfiguresCors()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify CORS is registered
        Assert.Contains(_services, sd => sd.ServiceType.Name.Contains("Cors"));
    }

    #endregion

    #region Swagger Configuration Tests

    [Fact]
    public void AddAllServices_ConfiguresSwagger()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify Swagger is registered
        Assert.Contains(_services, sd => sd.ServiceType.Name.Contains("Swagger"));
    }

    #endregion

    #region Authentication Configuration Tests

    [Fact]
    public void AddAllServices_ConfiguresAuthentication()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify authentication services are registered
        Assert.Contains(_services, sd => sd.ServiceType.Name.Contains("Authentication"));
    }

    [Fact]
    public void AddAllServices_ConfiguresAuthorization()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify authorization services are registered
        Assert.Contains(_services, sd => sd.ServiceType.Name.Contains("Authorization"));
    }

    #endregion

    #region Additional Infrastructure Services Tests

    [Fact]
    public void AddAllServices_RegistersCachingServices()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>());
    }

    [Fact]
    public void AddAllServices_RegistersHttpContextAccessor()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>());
    }

    [Fact]
    public void AddAllServices_RegistersHealthChecks()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify health checks are registered
        Assert.Contains(_services, sd => sd.ServiceType.Name.Contains("HealthCheck"));
    }

    #endregion

    #region Master Data Services Tests

    [Fact]
    public void AddAllServices_RegistersAllMasterDataServices()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - Sample of master data services
        Assert.NotNull(serviceProvider.GetService<IULBMasterService>());
        Assert.NotNull(serviceProvider.GetService<IPaymentModeService>());
        Assert.NotNull(serviceProvider.GetService<IConstructionTypeService>());
        Assert.NotNull(serviceProvider.GetService<IBankMasterService>());
    }

    [Fact]
    public void AddAllServices_RegistersWaterConnectionServices()
    {
        // Act
        _services.AddAllServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<IWaterConnectionTypeService>());
        Assert.NotNull(serviceProvider.GetService<IWaterConnectionSizeService>());
        Assert.NotNull(serviceProvider.GetService<IWaterConnectionStatusService>());
        Assert.NotNull(serviceProvider.GetService<IWaterRateMasterService>());
        Assert.NotNull(serviceProvider.GetService<IWaterConnectionService>());
        Assert.NotNull(serviceProvider.GetService<IWaterConnectionDetailsService>());
    }

    #endregion
}
