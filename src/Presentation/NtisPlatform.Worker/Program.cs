using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Application.Configuration;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Worker;
using NtisPlatform.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add Windows Service support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "NTIS Platform Worker Service";
});

// Infrastructure Layer - Database
builder.Services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

// Register scoped DbContext for DI (created from factory)
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    return factory.CreateDbContext();
});

// Add Memory Cache & AutoMapper Configuration
builder.Services.AddMemoryCache();
var mapperConfig = new AutoMapper.MapperConfiguration(cfg =>
{
    cfg.AddMaps(typeof(NtisPlatform.Application.Mappings.FloorMappingProfile).Assembly);
}, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
builder.Services.AddSingleton<AutoMapper.IMapper>(mapperConfig.CreateMapper());
// Report queue DB (separate database) used by the retention sweep worker
builder.Services.AddPooledDbContextFactory<ReportingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ReportingConnection")));
builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IDbContextFactory<ReportingDbContext>>();
    return factory.CreateDbContext();
});

// Infrastructure Layer - Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Async reporting options (SLT/LLT lifetimes, retention sweep)
builder.Services.Configure<ReportingOptions>(builder.Configuration.GetSection(ReportingOptions.Section));

// Infrastructure Layer - Localization
builder.Services.Configure<LocalizationOptions>(builder.Configuration.GetSection("Localization"));
builder.Services.AddSingleton<ILocalizationService, NtisPlatform.Infrastructure.Services.Localization.LocalizationService>();

// Application Layer - Services
builder.Services.Configure<NtisPlatform.Application.Options.FileStorageOptions>(
    builder.Configuration.GetSection(NtisPlatform.Application.Options.FileStorageOptions.Section));
builder.Services.AddScoped<IHardDeleteCleanupService, HardDeleteCleanupService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Tax Engine & Property Tax Operations Services
builder.Services.AddScoped<IRateableValueService, RateableValueService>();
builder.Services.AddScoped<ITaxMasterDataService, TaxMasterDataService>();
builder.Services.AddScoped<IRVPersistenceService, RVPersistenceService>();
builder.Services.AddScoped<IPolicyConfigurationService, PolicyConfigurationService>();
builder.Services.AddScoped<IRateableValueCalculatorService, RateableValueCalculatorService>();
builder.Services.AddSingleton<IFinanceYearProvider, SystemFinanceYearProvider>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPropertyContextLoaderService, PropertyContextLoaderService>();
builder.Services.AddScoped<IRuleApplierService, RuleApplierService>();
builder.Services.AddScoped<IRVCalculationCleanupService, RVCalculationCleanupService>();
builder.Services.AddScoped<IRuleExecutionService, RuleExecutionService>();
builder.Services.AddSingleton<IRuleEffectApplicator, DecreasePercentApplicator>();
builder.Services.AddSingleton<IRuleEffectApplicator, IncreasePercentApplicator>();
builder.Services.AddSingleton<IRuleEffectApplicator, MultiplyApplicator>();
builder.Services.AddSingleton<IRuleEffectApplicator, OverrideApplicator>();
builder.Services.AddSingleton<IRuleEffectApplicator, ExemptionApplicator>();
builder.Services.AddScoped<IRuleEffectApplicator, RateLookupApplicator>();
builder.Services.AddScoped<ITaxMasterService, TaxMasterService>();
builder.Services.AddScoped<ITaxApplicabilityService, TaxApplicabilityService>();
builder.Services.AddScoped<IPropertyMutationInvariantPolicy, PropertyMutationInvariantPolicy>();
builder.Services.AddScoped<IUserScreenAccessService, UserScreenAccessService>();
builder.Services.AddScoped<IReferenceValidationService, ReferenceValidationService>();

// Capital Value (CV) calculation services required for PropertyTaxOperationsService
builder.Services.AddScoped<NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.ICapitalValueService, NtisPlatform.Application.Services.CapitalValue.CapitalValueService>();
builder.Services.AddScoped<NtisPlatform.Application.Interfaces.ICapitalValueService.IPropertyTaxCalculationCVResultsService, NtisPlatform.Application.Services.CapitalValueService.PropertyTaxCalculationCVResultsService>();
builder.Services.AddScoped<NtisPlatform.Application.Interfaces.ICapitalValueService.IPolicyTaxDetailsCVService, NtisPlatform.Application.Services.CapitalValueService.PolicyTaxDetailsCVService>();
builder.Services.AddScoped<NtisPlatform.Application.Interfaces.ICapitalValueService.ITransMastService, NtisPlatform.Application.Services.CapitalValueService.TransMastService>();

// Capital Value supporting services (Data Loaders, Master Data Providers, Calculators, and Persistence)
builder.Services.AddScoped<NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data.IPropertyDataLoader, NtisPlatform.Application.Services.CapitalValue.DataLoader.PropertyDataLoader>();
builder.Services.AddScoped<NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data.ICapitalValueMasterDataProvider, NtisPlatform.Application.Services.CapitalValue.MasterDataProviders.CapitalValueMasterDataProvider>();
builder.Services.AddScoped<NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Calculation.ICapitalValueCalculator, NtisPlatform.Application.Services.CapitalValue.CVCalculator.CapitalValueCalculatorService>();
builder.Services.AddScoped<NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Persistence.ICapitalValuePersistenceService, NtisPlatform.Application.Services.CapitalValue.CVPersistenceService.CapitalValuePersistenceService>();

// Dual Method tax calculation service
builder.Services.AddScoped<IDualMethodService, DualMethodService>();

builder.Services.AddScoped<IPropertyTaxOperationsService, NtisPlatform.Application.Services.PropertyTaxOperations.PropertyTaxOperationsService>();
        builder.Services.Configure<CapitalValueOptions>(
            builder.Configuration.GetSection(CapitalValueOptions.SectionName));

// Add background workers
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<HardDeleteCleanupWorker>();
builder.Services.AddHostedService<PropertyTaxJobProcessorWorker>();
builder.Services.AddHostedService<PropertyTaxJobRecoveryWorker>();
// StaleReportRequestReclaimWorker retired Hangfire's invisibility timeout + server heartbeat now
// reclaim jobs whose worker died, so a separate lease-sweep is no longer needed.
builder.Services.AddHostedService<ReportRetentionWorker>();

var host = builder.Build();
host.Run();
