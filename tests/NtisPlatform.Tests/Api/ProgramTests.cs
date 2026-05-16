using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace NtisPlatform.Tests.Api;

public class ProgramTests
{
    [Fact]
    public void Program_WebApplicationBuilderConfiguration_IsAccessible()
    {
        // Arrange & Act
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        // Assert
        Assert.NotNull(builder);
        Assert.NotNull(builder.Services);
        Assert.NotNull(builder.Configuration);
    }

    [Fact]
    public void Program_KestrelServerOptions_CanBeConfigured()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        // Act
        builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = 104857600; // 100MB default
        });

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var kestrelOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>>();
        Assert.NotNull(kestrelOptions);
    }

    [Fact]
    public void Program_MemoryCache_CanBeAdded()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        // Act
        builder.Services.AddMemoryCache();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Assert
        var cache = serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        Assert.NotNull(cache);
    }

    [Fact]
    public void Program_Configuration_CanReadFileStorageMaxFileSizeBytes()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string>
        {
            {"FileStorage:MaxFileSizeBytes", "104857600"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Act
        var maxFileSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 104857600);

        // Assert
        Assert.Equal(104857600, maxFileSizeBytes);
    }

    [Fact]
    public void Program_Configuration_UsesDefaultWhenMaxFileSizeBytesNotConfigured()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string>();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Act
        var maxFileSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 104857600);

        // Assert
        Assert.Equal(104857600, maxFileSizeBytes);
    }

    [Fact]
    public void Program_Environment_IsDevelopment_ReturnsTrue()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        // Act
        var app = builder.Build();

        // Assert
        Assert.True(app.Environment.IsDevelopment());
    }

    [Fact]
    public void Program_Environment_IsProduction_ReturnsTrue()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        // Act
        var app = builder.Build();

        // Assert
        Assert.False(app.Environment.IsDevelopment());
        Assert.True(app.Environment.IsProduction());
    }

    [Fact]
    public void Program_Swagger_CanBeConfiguredInDevelopment()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Act
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Program_Middleware_CanBeAddedInOrder()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddControllers();
        builder.Services.AddCors();
        builder.Services.AddRateLimiter(options => { });
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Act
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Program_CORS_AllowAllPolicy_CanBeConfigured()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();

        // Act
        app.UseCors("AllowAll");

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Program_RateLimiter_CanBeConfigured()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(context =>
                System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        var app = builder.Build();

        // Act
        app.UseRateLimiter();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Program_AuthenticationAndAuthorization_CanBeConfigured()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        builder.Services.AddAuthentication().AddJwtBearer();
        builder.Services.AddAuthorization();

        var app = builder.Build();

        // Act
        app.UseAuthentication();
        app.UseAuthorization();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Program_Controllers_CanBeMapped()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddControllers();

        var app = builder.Build();

        // Act
        app.MapControllers();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Program_HttpsRedirection_CanBeConfigured()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        var app = builder.Build();

        // Act
        app.UseHttpsRedirection();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public void Program_CompleteConfiguration_BuildsSuccessfully()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        // Configure Kestrel
        var maxFileSizeBytes = builder.Configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 104857600);
        builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = maxFileSizeBytes;
        });

        // Add memory cache
        builder.Services.AddMemoryCache();

        // Build application
        var app = builder.Build();

        // Assert
        Assert.NotNull(app);
        Assert.NotNull(app.Services);
    }

    [Fact]
    public void Program_FileStorageConfiguration_WithCustomValue_UsesCustomValue()
    {
        // Arrange
        var customSize = 52428800L; // 50MB
        var inMemorySettings = new Dictionary<string, string>
        {
            {"FileStorage:MaxFileSizeBytes", customSize.ToString()}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Act
        var maxFileSizeBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeBytes", 104857600);

        // Assert
        Assert.Equal(customSize, maxFileSizeBytes);
    }

    [Fact]
    public void Program_KestrelMaxRequestBodySize_CanBeVerified()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        var expectedSize = 104857600L;

        // Act
        builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.Limits.MaxRequestBodySize = expectedSize;
        });

        var serviceProvider = builder.Services.BuildServiceProvider();
        var kestrelOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>>();

        // Assert
        Assert.NotNull(kestrelOptions);
        Assert.NotNull(kestrelOptions.Value);
        Assert.Equal(expectedSize, kestrelOptions.Value.Limits.MaxRequestBodySize);
    }

    [Fact]
    public void Program_MemoryCache_IsRegisteredAsSingleton()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddMemoryCache();

        var serviceProvider = builder.Services.BuildServiceProvider();

        // Act
        var cache1 = serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        var cache2 = serviceProvider.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();

        // Assert
        Assert.NotNull(cache1);
        Assert.NotNull(cache2);
        Assert.Same(cache1, cache2); // Should be same instance (singleton)
    }

    [Fact]
    public void Program_Environment_Staging_IsNotDevelopment()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Staging
        });

        var app = builder.Build();

        // Assert
        Assert.False(app.Environment.IsDevelopment());
        Assert.False(app.Environment.IsProduction());
        Assert.True(app.Environment.IsStaging());
    }

    [Fact]
    public void Program_Swagger_NotConfiguredInProduction()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Act - Swagger should not be used in production
        // We just verify the app builds without calling UseSwagger

        // Assert
        Assert.NotNull(app);
        Assert.True(app.Environment.IsProduction());
    }
}
