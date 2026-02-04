using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Api.Middleware;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Auth;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Resources;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Infrastructure.Services.Auth;
using System.Text;

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
        // Infrastructure Layer - Database (Single deployment per organization)
        services.AddHttpContextAccessor();

        // Register organization context (reads from config at deployment)
        // Register DbContext with single connection string
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString);
        });

        // Infrastructure Layer - Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Authentication Services
        services.AddScoped<IPasswordHasher, Infrastructure.Services.Auth.PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Authentication Providers
        services.AddScoped<IAuthenticationProvider, BasicAuthProvider>();
        // TODO: Add other providers when implemented
        // services.AddScoped<IAuthenticationProvider, AzureAdAuthProvider>();
        // services.AddScoped<IAuthenticationProvider, GoogleAuthProvider>();

        // Application Layer - Services
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationSettingsService, OrganizationSettingsService>();

        // CRUD Services
        services.AddScoped<IFloorService, FloorService>();
        services.AddScoped<IConstructionTypeService, ConstructionTypeService>();
        services.AddScoped<ISubFloorService, SubFloorService>();
        services.AddScoped<IRateService, RateService>();
        services.AddScoped<IRateMasterForCVService, RateMasterForCVService>();
        services.AddScoped<IRetentionFactWiseService, RetentionFactWiseService>();
        services.AddScoped<IYearMasterService, YearMasterService>();
        services.AddScoped<IMultilingualDetailsService, MultilingualDetailsService>();
        services.AddScoped<ITaxZoneService, TaxZoneService>();
        services.AddScoped<ITypeOfUseService, TypeOfUseService>();
        services.AddScoped<ITypeOfUseGroupService, TypeOfUseGroupService>();
        services.AddScoped<IDepreciationService, DepreciationService>();
        services.AddScoped<IBankMasterService, BankMasterService>();

        // Localization (DB-backed)
        services.AddScoped<IMultilingualResourceProvider, MultilingualResourceProvider>();
        services.AddSingleton<IStringLocalizerFactory, DbStringLocalizerFactory>();

        // AutoMapper
        services.AddAutoMapper(typeof(NtisPlatform.Application.Mappings.FloorMappingProfile).Assembly);

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
                policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials(); // Required for cookies
            });
        });

        // JWT Authentication
        var jwtKey = configuration.GetValue<string>("Jwt:Key")
            ?? throw new InvalidOperationException("JWT key not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = configuration.GetValue<string>("Jwt:Issuer"),
                ValidateAudience = true,
                ValidAudience = configuration.GetValue<string>("Jwt:Audience"),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Support reading token from cookie for web clients
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Cookies["session_token"];
                    if (!string.IsNullOrEmpty(token))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        // Rate Limiting (ASP.NET Core 7+)
        services.AddRateLimiter(options =>
        {
            // Global default policy for all endpoints (unless overridden)
            options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
                System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100, // 100 requests
                        Window = TimeSpan.FromMinutes(1), // per minute
                        SegmentsPerWindow = 6, // 6 segments (10 seconds each)
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Stricter fixed window rate limiter for login endpoint
            options.AddPolicy("login", context =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5, // 5 attempts
                        Window = TimeSpan.FromMinutes(15), // per 15 minutes
                        QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing
                    }));

            // On rejection, return 429 Too Many Requests
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                var retryAfterSeconds = context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfter)
                    ? (double?)retryAfter.TotalSeconds
                    : null;

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too Many Requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = retryAfterSeconds
                }, cancellationToken);
            };
        });

        // Caching
        services.AddMemoryCache();
        services.AddResponseCaching();

        // Health checks
        services.AddHealthChecks();

        return services;
    }
}
