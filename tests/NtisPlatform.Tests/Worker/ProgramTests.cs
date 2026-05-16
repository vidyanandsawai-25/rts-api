using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using Xunit;

namespace NtisPlatform.Tests.Worker;

public class ProgramTests
{
    [Fact]
    public void Program_CanBeReferenced()
    {
        // This test ensures the Worker project types are accessible for testing purposes
        var workerType = typeof(NtisPlatform.Worker.Worker);
        Assert.NotNull(workerType);
        Assert.Equal("Worker", workerType.Name);
        Assert.True(typeof(BackgroundService).IsAssignableFrom(workerType));
    }

    [Fact]
    public void Program_HostBuilder_CanBeCreated()
    {
        // Arrange & Act
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        // Assert
        Assert.NotNull(builder);
        Assert.NotNull(builder.Services);
        Assert.NotNull(builder.Configuration);
    }

    [Fact]
    public void Program_WindowsService_CanBeConfigured()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        // Act
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "NTIS Platform Worker Service";
        });

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public void Program_DbContextFactory_CanBeConfigured()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        var inMemorySettings = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=NtisWorkerTestDb;Trusted_Connection=True;"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Act
        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IDbContextFactory<ApplicationDbContext>>();
        Assert.NotNull(factory);
    }

    [Fact]
    public void Program_ScopedDbContext_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void Program_GenericRepository_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        // Act
        builder.Services.AddScoped(typeof(IRepository<>), typeof(NtisPlatform.Infrastructure.Repositories.Repository<>));

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public void Program_UnitOfWork_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        builder.Services.AddScoped(typeof(IRepository<>), typeof(NtisPlatform.Infrastructure.Repositories.Repository<>));

        // Act
        builder.Services.AddScoped<IUnitOfWork, NtisPlatform.Infrastructure.Repositories.UnitOfWork>();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();
        Assert.NotNull(unitOfWork);
    }

    [Fact]
    public void Program_LocalizationService_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        // Act
        builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var localizationService = serviceProvider.GetService<ILocalizationService>();
        Assert.NotNull(localizationService);
    }

    [Fact]
    public void Program_HardDeleteCleanupService_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        builder.Services.AddScoped(typeof(IRepository<>), typeof(NtisPlatform.Infrastructure.Repositories.Repository<>));
        builder.Services.AddScoped<IUnitOfWork, NtisPlatform.Infrastructure.Repositories.UnitOfWork>();
        builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();

        // Act
        builder.Services.AddScoped<IHardDeleteCleanupService, NtisPlatform.Infrastructure.Services.HardDeleteCleanupService>();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var hardDeleteService = scope.ServiceProvider.GetService<IHardDeleteCleanupService>();
        Assert.NotNull(hardDeleteService);
    }

    [Fact]
    public void Program_FileStorageService_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        // Act
        builder.Services.AddScoped<IFileStorageService, NtisPlatform.Infrastructure.Services.FileStorageService>();

        // Assert - Check registration without resolving concrete service to avoid filesystem side effects
        Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(IFileStorageService));
    }

    [Fact]
    public void Program_Worker_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        // Act
        builder.Services.AddHostedService<NtisPlatform.Worker.Worker>();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        Assert.Contains(hostedServices, service => service is NtisPlatform.Worker.Worker);
    }

    [Fact]
    public void Program_HardDeleteCleanupWorker_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        builder.Services.AddScoped(typeof(IRepository<>), typeof(NtisPlatform.Infrastructure.Repositories.Repository<>));
        builder.Services.AddScoped<IUnitOfWork, NtisPlatform.Infrastructure.Repositories.UnitOfWork>();
        builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();
        builder.Services.AddScoped<IHardDeleteCleanupService, NtisPlatform.Infrastructure.Services.HardDeleteCleanupService>();

        // Act
        builder.Services.AddHostedService<NtisPlatform.Worker.Services.HardDeleteCleanupWorker>();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
        Assert.Contains(hostedServices, service => service is NtisPlatform.Worker.Services.HardDeleteCleanupWorker);
    }

    [Fact]
    public void Program_AllServices_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        // Add Windows Service support
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "NTIS Platform Worker Service";
        });

        // Infrastructure Layer - Database
        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Register scoped DbContext for DI (created from factory)
        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        // Infrastructure Layer - Repositories
        builder.Services.AddScoped(typeof(IRepository<>), typeof(NtisPlatform.Infrastructure.Repositories.Repository<>));
        builder.Services.AddScoped<IUnitOfWork, NtisPlatform.Infrastructure.Repositories.UnitOfWork>();

        // Infrastructure Layer - Localization
        builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();

        // Application Layer - Services
        builder.Services.AddScoped<IHardDeleteCleanupService, NtisPlatform.Infrastructure.Services.HardDeleteCleanupService>();
        builder.Services.AddScoped<IFileStorageService, NtisPlatform.Infrastructure.Services.FileStorageService>();

        // Add background workers
        builder.Services.AddHostedService<NtisPlatform.Worker.Worker>();
        builder.Services.AddHostedService<NtisPlatform.Worker.Services.HardDeleteCleanupWorker>();

        // Act - Just verify services collection is configured
        var serviceCount = builder.Services.Count;

        // Assert
        Assert.NotNull(builder.Services);
        Assert.True(serviceCount > 0);

        // Verify specific service registrations
        Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));
        Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(IUnitOfWork));
        Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(ILocalizationService));
        Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(IHardDeleteCleanupService));
        Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(IFileStorageService));
        Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));
    }

    [Fact]
    public void Program_LocalizationService_CanBeRegisteredAsSingleton()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        // Act
        builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var localizationService = serviceProvider.GetService<ILocalizationService>();
        Assert.NotNull(localizationService);
    }

    [Fact]
    public void Program_HostedServices_CanBeRegistered()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        builder.Services.AddScoped(typeof(IRepository<>), typeof(NtisPlatform.Infrastructure.Repositories.Repository<>));
        builder.Services.AddScoped<IUnitOfWork, NtisPlatform.Infrastructure.Repositories.UnitOfWork>();
        builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();
        builder.Services.AddScoped<IHardDeleteCleanupService, NtisPlatform.Infrastructure.Services.HardDeleteCleanupService>();

        // Act
        builder.Services.AddHostedService<NtisPlatform.Worker.Services.HardDeleteCleanupWorker>();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        Assert.NotEmpty(hostedServices);
    }

    [Fact]
    public void Program_AllServices_CanBeBuilt()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));

        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        builder.Services.AddScoped(typeof(IRepository<>), typeof(NtisPlatform.Infrastructure.Repositories.Repository<>));
        builder.Services.AddScoped<IUnitOfWork, NtisPlatform.Infrastructure.Repositories.UnitOfWork>();
        builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();
        builder.Services.AddScoped<IHardDeleteCleanupService, NtisPlatform.Infrastructure.Services.HardDeleteCleanupService>();
        builder.Services.AddScoped<IFileStorageService, NtisPlatform.Infrastructure.Services.FileStorageService>();

        // Act
        var host = builder.Build();

        // Assert
        Assert.NotNull(host);
    }

    [Fact]
    public void Program_ConnectionString_CanBeRead()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string>
        {
            {"ConnectionStrings:DefaultConnection", "Server=localhost;Database=TestDb;"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Act
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Assert
        Assert.Equal("Server=localhost;Database=TestDb;", connectionString);
    }

    [Fact]
    public void Program_MigrationsAssembly_CanBeSpecified()
    {
        // Arrange
        var builder = Host.CreateApplicationBuilder(Array.Empty<string>());

        // Act
        builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
            options.UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=Test;",
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetService<IDbContextFactory<ApplicationDbContext>>();
        Assert.NotNull(factory);
    }
}
