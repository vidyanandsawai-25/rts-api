using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace NtisPlatform.Tests.Api.Extensions;

/// <summary>
/// Comprehensive tests for ServiceCollectionExtensions
/// Target: 100% line coverage and branch coverage
/// Unit tests verify service registration only (integration tests with real database for full validation)
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
    public void AddAllServices_RegistersInfrastructureServices()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify services are registered (not building provider to avoid hosted service startup)
        Assert.Contains(_services, sd => sd.ServiceType == typeof(ITokenService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IPasswordHasher));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(ISecuritySettingsService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IHardDeleteCleanupService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IDocumentService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IDocumentAuthorizationService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IFileStorageService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IPropertyCertificateService));
    }

    [Fact]
    public void AddAllServices_RegistersRateableValueApiClient_AsPlainScopedService_NotTypedHttpClient()
    {
        // RateableValueApiClient's constructor takes (IRateableValueService, ILogger<...>) --
        // it delegates directly to the local RV service and makes no HTTP calls. Registering it
        // via AddHttpClient<TClient, TImplementation>() requires TImplementation to have a
        // constructor accepting HttpClient, which this class does not, and fails at resolution
        // time with "A suitable constructor for type 'RateableValueApiClient' could not be
        // located. A Typed client must provide a constructor taking a 'System.Net.Http.HttpClient'
        // as a parameter." It must stay a plain AddScoped registration.
        _services.AddAllServices(_configuration);

        var descriptor = _services.SingleOrDefault(sd => sd.ServiceType == typeof(IRateableValueApiClient));

        Assert.NotNull(descriptor);
        Assert.Equal(typeof(RateableValueApiClient), descriptor!.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddAllServices_RegistersRepositories()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IUserRepository));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IRefreshTokenRepository));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IPropertyRepository));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IUnitOfWork));
    }

    [Fact]
    public void AddAllServices_RegistersApplicationServices()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IAuthService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IUlbConfigService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IPropertyService));
    }

    [Fact]
    public void AddAllServices_RegistersPropertyPhotoServices()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - PropertyPhoto stack: Core row service + Application orchestration service
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IPropertyPhotoService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IPropertyPhotoApplicationService));
    }

    [Fact]
    public void AddAllServices_RegistersLocalizationServices()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert
        Assert.Contains(_services, sd => sd.ServiceType == typeof(ILocalizationService));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(ILocalization));
        Assert.Contains(_services, sd => sd.ServiceType == typeof(ILocalizedQueryService));
    }

    [Fact]
    public void AddAllServices_RegistersDbContextFactory()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));
    }

    [Fact]
    public void AddAllServices_RegistersAutoMapper()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert
        Assert.Contains(_services, sd => sd.ServiceType == typeof(AutoMapper.IMapper));
    }

    [Fact]
    public void AddAllServices_RegistersDepartmentIdCache()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - DepartmentIdCache should be registered as singleton to avoid sync DB queries during DI setup
        Assert.Contains(_services, sd => sd.ServiceType == typeof(IDepartmentIdCache) && sd.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddAllServices_RegistersControllers()
    {
        // Act
        _services.AddAllServices(_configuration);

        // Assert - Verify MVC services are registered
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
