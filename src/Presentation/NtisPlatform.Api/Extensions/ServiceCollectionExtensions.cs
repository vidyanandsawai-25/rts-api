using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Api.Localization;
using NtisPlatform.Api.Middleware;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Infrastructure.Services.Localization;
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

        // AutoMapper - scan Application assembly for all mapping profiles in NtisPlatform.Application.Mappings
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(FloorMappingProfile).Assembly);
        }, NullLoggerFactory.Instance);


        // Shared configuration for DbContext
        Action<DbContextOptionsBuilder> configureDbContext = options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString);
            // Add interceptors, logging, etc. here if needed
        };

        // Register pooled DbContextFactory (singleton)
        services.AddPooledDbContextFactory<ApplicationDbContext>(configureDbContext);

        // Register scoped DbContext for DI (created from factory)
        services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            return factory.CreateDbContext();
        });

        // Localization service (singleton dictionary cache for both validation messages and field localization)
        services.AddSingleton<ILocalizationService, LocalizationService>();
        // Preload on startup
        services.AddHostedService<LocalizationWarmupHostedService>();
        // Replace RESX localizer with service-backed factory
        services.AddSingleton<IStringLocalizerFactory, DbServiceStringLocalizerFactory>();
        // model data fill culture wise.
        services.AddScoped<LocalizationProcessor>();

        // Field-level localization (uses ILocalizationService for caching)
        services.AddScoped<ILocalization, LocalizationRepoService>();
        services.AddScoped<ILocalizedQueryService, LocalizedQueryService>();

        // API Layer - Controllers with DataAnnotations localization
        services.AddControllers()
            .AddDataAnnotationsLocalization(options =>
            {
                // Route all validation messages through a single "ValidationMessages" resource
                // Keys in DB: Resource = "ValidationMessages", Key = "RequiredField", etc.
                options.DataAnnotationLocalizerProvider = (type, factory) =>
                    factory.Create("ValidationMessages", string.Empty);
            });


        // Infrastructure Layer - Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();

        // Infrastructure Layer - Services
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ISecuritySettingsService, SecuritySettingsService>();
        services.AddScoped<IHardDeleteCleanupService, HardDeleteCleanupService>();

        // Application Layer - Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUlbConfigService, UlbConfigService>();


        // TODO: Add other providers when implemented
        // services.AddScoped<IAuthenticationProvider, AzureAdAuthProvider>();
        // services.AddScoped<IAuthenticationProvider, GoogleAuthProvider>();


        // CRUD Services
        services.AddScoped<IULBMasterService, ULBMasterService>();
        services.AddScoped<IPaymentModeService, PaymentModeService>();
        services.AddScoped<ITaxZoningService, TaxZoningService>();
        services.AddScoped<IRuleService, RuleService>();

        services.AddScoped<IFloorService, FloorService>();
        services.AddScoped<IConstructionTypeService, ConstructionTypeService>();
        services.AddScoped<ISubFloorService, SubFloorService>();
        services.AddScoped<IRateService, RateService>();
        services.AddScoped<IRateMasterForCVService, RateMasterForCVService>();
        services.AddScoped<IRetentionFactWiseService, RetentionFactWiseService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        services.AddScoped<IRetentionYearWiseService, RetentionYearWiseService>();
        services.AddScoped<IYearMasterService, YearMasterService>();
        services.AddScoped<ITaxZoneService, TaxZoneService>();
        services.AddScoped<IMoujaService, MoujaService>();
        services.AddScoped<IOfficeService, OfficeService>();
        services.AddScoped<ISubTypeOfUseService, SubTypeOfUseService>();
        services.AddScoped<ITypeOfUseService, TypeOfUseService>();
        services.AddScoped<ITypeOfUseGroupService, TypeOfUseGroupService>();
        services.AddScoped<IDepreciationService, DepreciationService>();
        services.AddScoped<IWardService, WardService>();
        services.AddScoped<IBankMasterService, BankMasterService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IRateSectionService, RateSectionService>();
        services.AddScoped<IRateSectionDetailsService, RateSectionDetailsService>();
        services.AddScoped<IAssessmentYearRangeService, AssessmentYearRangeService>();
        services.AddScoped<IScreenMasterService, ScreenMasterService>();
        services.AddScoped<IAssessmentYearRangeCVService, AssessmentYearRangeCVService>();
        services.AddScoped<IActiveTaxesService, ActiveTaxesService>();
        services.AddScoped<IFloorFactorCVMasterService, FloorFactorCVMasterService>();
        services.AddScoped<INatureFactorCVMasterService, NatureFactorCVMasterService>();
        services.AddScoped<IUseFactorCVMasterService, UseFactorCVMasterService>();
        services.AddScoped<IAgeFactorCVMasterService, AgeFactorCVMasterService>();
        services.AddScoped<IDepartmentLicenceDetailsService, DepartmentLicenceDetailsService>();
        services.AddScoped<IScreenGroupMasterService, ScreenGroupMasterService>();
        services.AddScoped<IRoleWiseScreenAccessMasterService, RoleWiseScreenAccessMasterService>();
        services.AddScoped<IDesignationMasterService, DesignationMasterService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IOwnerTypeService, OwnerTypeService>();
        // Localization (DB-backed)
        services.AddScoped<IModuleMasterService, ModuleMasterService>();
        services.AddScoped<IDepartmentMasterService, DepartmentMasterService>();
        services.AddScoped<IGrievanceCategoryService, GrievanceCategoryService>();
        services.AddScoped<IPropertyCategoryService, PropertyCategoryService>();
        services.AddScoped<IConfigCategoryMasterService, ConfigCategoryMasterService>();
        services.AddScoped<IPropertyTypeCategoryService, PropertyTypeCategoryService>();
        services.AddScoped<IPropertyTypeMasterService, PropertyTypeMasterService>();
        services.AddScoped<IPropertyCertificateTypeService, PropertyCertificateTypeService>();
        services.AddScoped<IConfigKeyMasterService, ConfigKeyMasterService>();
        services.AddScoped<IConfigValueMasterService, ConfigValueMasterService>();
        services.AddScoped<IWingService, WingService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmployeeType, EmployeeTypeService>();
        services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();
        services.AddScoped<IPropertyDescriptionAndTypeOfUseValidationService, PropertyDescriptionAndTypeOfUseValidationService>();
        services.AddScoped<IReferenceValidationService, ReferenceValidationService>();
        services.AddScoped<IRuleScopeService, RuleScopeService>();
        services.AddScoped<IRuleEffectTypeService, RuleEffectTypeService>();

        services.AddScoped<IGenderMasterService, GenderMasterService>();
        // AutoMapper
        services.AddSingleton<IMapper>(mapperConfig.CreateMapper());
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NTIS Platform API",
                Version = "v1",
                Description = "Enterprise-grade .NET API with clean architecture\n\n" +
                              "**To test authorized endpoints:**\n" +
                              "1. Use /api/auth/login endpoint to get JWT token\n" +
                              "2. Click the 'Authorize' button (??) at top right\n" +
                              "3. Enter: Bearer YOUR_JWT_TOKEN\n" +
                              "4. Click 'Authorize' then 'Close'\n" +
                              "5. Test your protected endpoints",
                Contact = new OpenApiContact { Name = "NTIS Platform Team" }
            });

            // Enable JWT Bearer authentication in Swagger UI - Adds "Authorize" button
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and your token. Example: \"Bearer eyJhbGc...\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
                // Note: AllowCredentials removed - tokens are sent in Authorization header only
            });
        });

        // JWT Authentication - Validate JWT Key
        var jwtKey = configuration.GetValue<string>("Jwt:Key");

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT Key is not configured or is empty. " +
                "Please configure Jwt:Key in user-secrets (development), " +
                "environment variables (production), or Azure Key Vault. " +
                "Example: dotnet user-secrets set 'Jwt:Key' 'your-secret-key-min-32-bytes'");
        }

        // Reject known insecure placeholder values
        var insecurePlaceholders = new[]
        {
            "MUST_BE_SET_IN_SECURE_CONFIG_SOURCE",
            "YourSuperSecretKeyHere_ChangeThisInProduction_MustBeAtLeast32Characters",
            "your-secret-key-here",
            "change-me-in-production",
            "placeholder",
            "secret",
            "supersecret"
        };

        var jwtKeyNormalized = jwtKey.Trim();
        if (insecurePlaceholders.Any(p => string.Equals(jwtKeyNormalized, p, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "JWT Key appears to be a placeholder value and is not secure. " +
                "Do not use placeholder or example keys in any environment. " +
                "Generate a cryptographically secure random key using: " +
                "dotnet user-secrets set 'Jwt:Key' '$(openssl rand -base64 32)' or similar.");
        }

        // Enforce minimum key length for security (32 bytes = 256 bits) using the normalized key
        var keyBytes = Encoding.UTF8.GetBytes(jwtKeyNormalized);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                $"JWT Key is too short ({keyBytes.Length} bytes). " +
                "Minimum 32 bytes (256 bits) required for secure HMAC-SHA256 signing. " +
                "Current key length is insufficient to prevent brute-force attacks.");
        }

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
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = true,
                ValidIssuer = configuration.GetValue<string>("Jwt:Issuer"),
                ValidateAudience = true,
                ValidAudience = configuration.GetValue<string>("Jwt:Audience"),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Token must be provided in Authorization header only
            // Cookie-based authentication removed - incomplete security model
        });

        // Authorization with fallback policy - all endpoints require authentication by default
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

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
