using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;

namespace NtisPlatform.Api.Extensions;

/// <summary>
/// Centralized dependency injection configuration for all layers
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services
    /// </summary>
    public static IServiceCollection AddAllServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Infrastructure Layer - Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Infrastructure Layer - Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application Layer - Services
        services.AddScoped<ISampleService, SampleService>();
        services.AddScoped<IServiceManagementService, ServiceManagementService>();
        services.AddScoped<IPTISConstructionTypeMasterService, PTISConstructionTypeMasterService>();
        services.AddScoped<IPTISFloorMasterService, PTISFloorMasterService>();


        // API Layer - Controllers, Swagger, CORS
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() 
            { 
                Title = "NTIS Platform API", 
                Version = "v1",
                Description = "Enterprise-grade .NET API with clean architecture",
                Contact = new() { Name = "NTIS Platform Team" }
            });

            // TODO: Add JWT authentication to Swagger
            // options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            // {
            //     Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
            //     Name = "Authorization",
            //     In = ParameterLocation.Header,
            //     Type = SecuritySchemeType.ApiKey,
            //     Scheme = "Bearer"
            // });
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            // TODO: Configure production CORS policy
            // options.AddPolicy("Production", policy =>
            // {
            //     policy.WithOrigins("https://yourdomain.com")
            //           .AllowAnyMethod()
            //           .AllowAnyHeader();
            // });
        });

        // TODO: Add authentication
        // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //     .AddJwtBearer(options => { ... });

        // TODO: Add caching
        // services.AddMemoryCache();
        // services.AddResponseCaching();

        // TODO: Add health checks
        // services.AddHealthChecks()
        //     .AddDbContextCheck<ApplicationDbContext>();

        return services;
    }
}
