using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Api.Filters;
using NtisPlatform.Api.Localization;
using NtisPlatform.Api.Middleware;
using NtisPlatform.Application.Configuration;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Interfaces.ICapitalValueService;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Calculation;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Persistence;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Interfaces.FieldConfiguration;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Services.Master;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Application.Services.Rules.Effects;
using NtisPlatform.Application.Services.FieldConfiguration;
using NtisPlatform.Application.Services.TaxEngine;
using NtisPlatform.Application.Services.PropertyTaxOperations;
using NtisPlatform.Application.Services.CapitalValue;
using NtisPlatform.Application.Services.CapitalValue.CVCalculator;
using NtisPlatform.Application.Services.CapitalValue.CVPersistenceService;
using NtisPlatform.Application.Services.CapitalValue.DataLoader;
using NtisPlatform.Application.Services.CapitalValue.MasterDataProviders;
using NtisPlatform.Application.Services.CapitalValueService;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Interfaces.Rules;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using NtisPlatform.Infrastructure.Repositories.Property;
using NtisPlatform.Infrastructure.Repositories.Rules;
using NtisPlatform.Infrastructure.Services;
using NtisPlatform.Infrastructure.Services.Handlers;
using NtisPlatform.Infrastructure.Services.Localization;
using System.Text;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Services.ReportDataProviders;

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

        // Report queue DB (separate database; schema owned by the ntis DB project mapping only)
        services.AddPooledDbContextFactory<ReportingDbContext>(options =>
        {
            var reportingConnection = configuration.GetConnectionString("ReportingConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:ReportingConnection is not configured.");
            options.UseSqlServer(reportingConnection);
        });
        services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ReportingDbContext>>();
            return factory.CreateDbContext();
        });

        // Read-only report DATA context (replica / read-only login) for heavy provider reads.
        // A generous CommandTimeout (default 300s) lets a large report's per-page query finish instead
        // of dying at SQL Server's 30s default — the common cause of large reports failing.
        var reportDataCommandTimeout = configuration.GetValue<int>("ReportData:CommandTimeoutSeconds", 300);
        services.AddPooledDbContextFactory<ReportDataDbContext>(options =>
        {
            var readOnlyConnection = configuration.GetConnectionString("ReportDataReadOnlyConnection")
                ?? configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(readOnlyConnection,
                sql => sql.CommandTimeout(reportDataCommandTimeout));
        });
        services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<ReportDataDbContext>>();
            return factory.CreateDbContext();
        });

        // Localization service (singleton dictionary cache for both validation messages and field localization)
        services.AddSingleton<ILocalizationService, LocalizationService>();
        // Preload on startup
        services.AddHostedService<LocalizationWarmupHostedService>();

        // Department ID cache - eliminates sync DB queries in DI setup
        services.AddSingleton<IDepartmentIdCache, DepartmentIdCache>();
        services.AddHostedService<DepartmentIdCacheInitializer>();

        // Document orphan cleanup service (disabled by default until enabled/configured)
        // services.AddHostedService<DocumentOrphanCleanupService>();
        // Replace RESX localizer with service-backed factory
        services.AddSingleton<IStringLocalizerFactory, DbServiceStringLocalizerFactory>();
        // Configure localization options from appsettings
        services.Configure<NtisPlatform.Application.Options.LocalizationOptions>(configuration.GetSection("Localization"));
        // Configure feature flags from appsettings
        services.Configure<NtisPlatform.Application.Options.FeatureFlagsOptions>(configuration.GetSection("FeatureFlags"));
        // model data fill culture wise.
        services.AddScoped<LocalizationProcessor>();

        // Field-level localization (uses ILocalizationService for caching)
        services.AddScoped<ILocalization, LocalizationRepoService>();
        services.AddScoped<ILocalizedQueryService, LocalizedQueryService>();

        // Configure file upload limits from the strongly-typed FileStorage options (single source of truth)
        var fileStorageOptions = configuration.GetSection(NtisPlatform.Application.Options.FileStorageOptions.Section)
            .Get<NtisPlatform.Application.Options.FileStorageOptions>() ?? new NtisPlatform.Application.Options.FileStorageOptions();
        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = fileStorageOptions.MaxFileSizeBytes;
        });

        // API Layer - Controllers with DataAnnotations localization
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            })
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

        // Report catalogue (definitions + parameters) lives in the report database. Bind their
        // IRepository<T,int> to ReportingDbContext these closed-generic registrations override the
        // open-generic ApplicationDbContext binding above for these two entity types, so every
        // consumer (CRUD services, cache warmup, ReportService, ReportWorkerService) reads from the
        // report DB. The matching write-side IUnitOfWork is injected per CRUD service below.
        services.AddScoped<IRepository<NtisPlatform.Core.Entities.Master.ReportDefinitionEntity, int>>(
            sp => new ReportDbRepository<NtisPlatform.Core.Entities.Master.ReportDefinitionEntity, int>(
                sp.GetRequiredService<ReportingDbContext>()));
        services.AddScoped<IRepository<NtisPlatform.Core.Entities.Master.ReportParameterDefinitionEntity, int>>(
            sp => new ReportDbRepository<NtisPlatform.Core.Entities.Master.ReportParameterDefinitionEntity, int>(
                sp.GetRequiredService<ReportingDbContext>()));
        // Read-only from this app's perspective — modules are managed by the report-admin tool.
        services.AddScoped<IRepository<NtisPlatform.Core.Entities.Master.ReportModuleEntity, int>>(
            sp => new ReportDbRepository<NtisPlatform.Core.Entities.Master.ReportModuleEntity, int>(
                sp.GetRequiredService<ReportingDbContext>()));

        // Reporting repositories/UoW bound to ReportingDbContext (report queue DB)
        services.AddScoped(typeof(IReportingRepository<,>), typeof(ReportingRepository<,>));
        services.AddScoped<IReportingUnitOfWork, ReportingUnitOfWork>();
        // Read-only data repository bound to ReportDataDbContext (report data replica)
        services.AddScoped(typeof(IReportDataRepository<>), typeof(ReportDataRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<ITypeOfUseByPropertyTypeRepository, TypeOfUseByPropertyTypeRepository>();
       

        // Per-tab Clean Architecture split (Basic Details): shared master checks + feature repository
        services.AddScoped<IMasterRepository, MasterRepository>();
        services.AddScoped<IPropertyBasicDetailsRepository, PropertyBasicDetailsRepository>();
        services.AddScoped<IPropertyKycRepository, PropertyKycRepository>();
        services.AddScoped<IPropertySocietyRepository, PropertySocietyRepository>();
        services.AddScoped<IPropertyDiscountRepository, PropertyDiscountRepository>();
        services.AddScoped<IPropertyOldDetailsRepository, PropertyOldDetailsRepository>();
        services.AddScoped<IPropertySocialDetailsRepository, PropertySocialDetailsRepository>();
        services.AddScoped<IPropertySearchRepository, PropertySearchRepository>();
        services.AddScoped<IPropertyWorkflowDetailsRepository, PropertyWorkflowDetailsRepository>();
        services.AddScoped<IRuleFieldsRepository, RuleFieldsRepository>();
        services.AddScoped<IApartmentQCRepository, ApartmentQCRepository>();

        // Infrastructure Layer - Services
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ISecuritySettingsService, SecuritySettingsService>();
        services.AddScoped<IHardDeleteCleanupService, HardDeleteCleanupService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentAuthorizationService, DocumentAuthorizationService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IModuleLookupService, ModuleLookupService>();

        // Document Authorization Handlers (per-department entity-level authorization)
        // Register handlers for document access based on parent entity (Property, WaterConnection, etc.)
        services.AddScoped<IDocumentAuthorizationHandler>(sp =>
        {
            // PTIS department ID - resolved from startup cache (no sync DB queries)
            var deptCache = sp.GetRequiredService<IDepartmentIdCache>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<NtisPlatform.Infrastructure.Services.Handlers.PtisDocumentAuthorizationHandler>>();

            var ptisDepartmentId = deptCache.GetPtisdepartmentId();

            return new NtisPlatform.Infrastructure.Services.Handlers.PtisDocumentAuthorizationHandler(
                ptisDepartmentId,
                logger);
        });
        services.AddScoped<IPropertyCertificateService, PropertyCertificateService>();
        services.AddScoped<IPropertyPhotoService, PropertyPhotoService>();
        services.AddScoped<IDynamicEntityLoader, DynamicEntityLoader>();
        services.AddScoped<IDynamicBindingService, DynamicBindingService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailSettingsProvider, EmailSettingsProvider>();
        services.AddScoped<IFieldRegistryService, FieldRegistryService>();

        // Translation Management
        services.AddScoped<IMultilingualTranslation, MultilingualTranslationService>();

        // Google Translate Service
        services.AddHttpClient<ITranslationService, TranslationService>();
        services.Configure<TranslationServiceOptions>(configuration.GetSection("GoogleTranslate"));
        services.Configure<NtisPlatform.Application.Options.ApartmentQCOptions>(configuration.GetSection(NtisPlatform.Application.Options.ApartmentQCOptions.Section));
        services.Configure<NtisPlatform.Application.Options.FileStorageOptions>(configuration.GetSection(NtisPlatform.Application.Options.FileStorageOptions.Section));

        // Application Layer - Helpers
        services.AddSingleton<NtisPlatform.Application.Helpers.FileValidationHelper>();

        // Application Layer - Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUlbConfigService, UlbConfigService>();
        services.AddScoped<IDocumentApplicationService, DocumentApplicationService>();
        services.AddScoped<IPropertyCertificateApplicationService, PropertyCertificateApplicationService>();
        services.AddScoped<IPropertyPhotoApplicationService, PropertyPhotoApplicationService>();
        services.AddScoped<ICommonDetailsService, CommonDetailsService>();

        // Global Document Binding Handlers (OCP extension points).
        // Each module registers its own handler — DocumentApplicationService dispatches to them
        // by reference table name, with zero if/else branching on entity names.
        // Pattern: For each new module, create a handler implementing IDocumentBindingHandler
        // and register it here. DocumentApplicationService will auto-discover via DI.
        services.AddScoped<IDocumentBindingHandler, PropertyPhotoDocumentBindingHandler>();
        services.AddScoped<IDocumentBindingHandler, PropertyCertificateDocumentBindingHandler>();
        services.AddScoped<IDocumentBindingHandler, RenterMastDocumentBindingHandler>();
        // Future modules: Add handlers following this pattern:
        // services.AddScoped<IDocumentBindingHandler, WaterConnectionDocumentBindingHandler>();
        // services.AddScoped<IDocumentBindingHandler, AssetDocumentBindingHandler>();

        // TODO: Add other providers when implemented
        // services.AddScoped<IAuthenticationProvider, AzureAdAuthProvider>();
        // services.AddScoped<IAuthenticationProvider, GoogleAuthProvider>();


        // CRUD Services
        services.AddScoped<IULBMasterService, ULBMasterService>();
        services.AddScoped<IPaymentModeService, PaymentModeService>();
        services.AddScoped<ICapitalValueService, CapitalValueService>();
        services.AddScoped<IPropertyTaxCalculationCVResultsService, PropertyTaxCalculationCVResultsService>();
        services.AddScoped<IPolicyTaxDetailsCVService, PolicyTaxDetailsCVService>();
        services.AddScoped<ITransMastService, TransMastService>();
        // Capital Value Supporting Services - Data Loaders
        services.AddScoped<IPropertyDataLoader, PropertyDataLoader>();
        services.AddScoped<ICapitalValueMasterDataProvider, CapitalValueMasterDataProvider>();
        // Capital Value Supporting Services - Calculators
        services.AddScoped<ICapitalValueCalculator, CapitalValueCalculatorService>();
        // Capital Value Supporting Services - Persistence
        services.AddScoped<ICapitalValuePersistenceService, CapitalValuePersistenceService>();
        // Capital Value Configuration Options 
        services.Configure<CapitalValueOptions>(configuration.GetSection(CapitalValueOptions.SectionName));
        services.AddScoped<IDualMethodService, DualMethodService>();
        services.AddScoped<IRateableValueService, NtisPlatform.Application.Services.TaxEngine.RateableValueService>();
        services.AddScoped<IPropertyTaxOperationsService, PropertyTaxOperationsService>();
        services.AddScoped<NtisPlatform.Application.Interfaces.TaxEngine.ITaxMasterDataService,
                       NtisPlatform.Application.Services.TaxEngine.TaxMasterDataService>();

        services.AddScoped<NtisPlatform.Application.Interfaces.TaxEngine.IRVPersistenceService,
                           NtisPlatform.Application.Services.TaxEngine.RVPersistenceService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<NtisPlatform.Application.Interfaces.IFinanceYearProvider, NtisPlatform.Application.Services.SystemFinanceYearProvider>();
        services.AddScoped<ITaxZoningService, TaxZoningService>();
        services.AddScoped<IPolicyConfigurationService, PolicyConfigurationService>();
        services.AddScoped<ICombinePropertyService, CombinePropertyService>();
        services.AddScoped<ICombinePropertyValidator, CombinePropertyValidator>();
        services.AddScoped<IPropertyDataCopier, PropertyDataCopier>();
        services.AddScoped<IPropertyDeactivator, PropertyDeactivator>();
        services.AddScoped<ICombinePropertyTaxService, CombinePropertyTaxService>();
        services.AddScoped<ILockUnlockService, LockUnlockService>();
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
        services.AddScoped<ITypeOfUseCategoryService, TypeOfUseCategoryService>();
        services.AddScoped<ITypeOfUseByPropertyTypeService, TypeOfUseByPropertyTypeService>();
        services.AddScoped<ITypeOfUseGroupService, TypeOfUseGroupService>();
        services.AddScoped<IDepreciationService, DepreciationService>();
        services.AddScoped<IWardService, WardService>();
        services.AddScoped<IBankMasterService, BankMasterService>();
        services.AddScoped<IPropertyWorkflowStageMasterService, PropertyWorkflowStageMasterService>();
        services.AddScoped<IPropertyRuleEvaluationMasterService, PropertyRuleEvaluationMasterService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IRateSectionService, RateSectionService>();
        services.AddScoped<IRateSectionDetailsService, RateSectionDetailsService>();
        services.AddScoped<IAssessmentYearRangeService, AssessmentYearRangeService>();
        services.AddScoped<IScreenMasterService, ScreenMasterService>();
        services.AddScoped<IAssessmentYearRangeCVService, AssessmentYearRangeCVService>();
        services.AddScoped<IActiveTaxesService, ActiveTaxesService>();
        services.AddScoped<IUlbImageMasterService, UlbImageMasterService>();
        services.AddScoped<IFloorFactorCVMasterService, FloorFactorCVMasterService>();
        services.AddScoped<INatureFactorCVMasterService, NatureFactorCVMasterService>();
        services.AddScoped<IUseFactorCVMasterService, UseFactorCVMasterService>();
        services.AddScoped<IAgeFactorCVMasterService, AgeFactorCVMasterService>();
        services.AddScoped<IDepartmentLicenceDetailsService, DepartmentLicenceDetailsService>();
        services.AddScoped<IScreenGroupMasterService, ScreenGroupMasterService>();
        services.AddScoped<IRoleWiseScreenAccessMasterService, RoleWiseScreenAccessMasterService>();
        services.AddScoped<IDesignationMasterService, DesignationMasterService>();
        services.AddScoped<IPropertyAggregateRepository, PropertyAggregateRepository>();
        services.AddScoped<IPropertyMutationInvariantPolicy, PropertyMutationInvariantPolicy>();
        services.AddScoped<PropertyApiExceptionFilter>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IPropertyBasicDetailsService, PropertyBasicDetailsService>();
        services.AddScoped<IPropertyKycService, PropertyKycService>();
        services.AddScoped<IPropertySocietyService, PropertySocietyService>();
        services.AddScoped<IPropertyDiscountService, PropertyDiscountService>();
        services.AddScoped<IPropertyOldDetailsService, PropertyOldDetailsService>();
        services.AddScoped<IPropertySearchService, PropertySearchService>();
        services.AddScoped<IPropertyWorkflowDetailsService, PropertyWorkflowDetailsService>();
        services.AddScoped<IApartmentQCService, ApartmentQCService>();
        services.AddScoped<IOwnerTypeService, OwnerTypeService>();
        services.AddScoped<ISocialAttributeService, SocialAttributeService>();
        services.AddScoped<IPropertySocialDetailsService, PropertySocialDetailsService>();

        // Localization (DB-backed)
        services.AddScoped<IModuleMasterService, ModuleMasterService>();
        services.AddScoped<IDepartmentMasterService, DepartmentMasterService>();
        services.AddScoped<IGrievanceCategoryService, GrievanceCategoryService>();
        services.AddScoped<IPropertyCategoryService, PropertyCategoryService>();
        services.AddScoped<IConfigCategoryMasterService, ConfigCategoryMasterService>();
        services.AddScoped<IPropertyTypeCategoryService, PropertyTypeCategoryService>();
        services.AddScoped<IPropertyTypeMasterService, PropertyTypeMasterService>();
        services.AddScoped<IPropertyPhotoTypeService, PropertyPhotoTypeService>();
        services.AddScoped<IPropertyCertificateTypeService, PropertyCertificateTypeService>();
        services.AddScoped<IConfigKeyMasterService, ConfigKeyMasterService>();
        services.AddScoped<IConfigValueMasterService, ConfigValueMasterService>();
        services.AddScoped<IWingService, WingService>();
        services.AddScoped<IFloorGroupService, FloorGroupService>();
        services.AddScoped<ITypeOfUseGroupCVService, TypeOfUseGroupCVService>();
        services.AddScoped<ISubZoneDetailsForCVService, SubZoneDetailsForCVService>();
        services.AddScoped<IBulkUpdateFieldConfigService, BulkUpdateFieldConfigService>();
        services.AddScoped<IBulkUpdateMasterService, BulkUpdateMasterService>();
        services.AddScoped<IPropertyWorkflowStageMasterService, PropertyWorkflowStageMasterService>();

        services.AddScoped<IDataEntryService, DataEntryService>();
        services.AddScoped<IDataEntrySameAsService, DataEntrySameAsService>();
        services.AddScoped<IPropertyReassessmentService, PropertyReassessmentService>();
        services.AddScoped<IRetrospectiveTaxService, RetrospectiveTaxService>();
        services.AddScoped<IRenterDetailService, RenterDetailService>();
        services.AddScoped<IRenterMastService, RenterMastService>();
        services.AddScoped<IRoomWiseMinusService, RoomWiseMinusService>();

        services.AddScoped<IRoomWiseSubmissionDetailsService, RoomWiseSubmissionDetailsService>();



        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserScreenAccessService, UserScreenAccessService>();
        services.AddScoped<IEmployeeType, EmployeeTypeService>();
        services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();

        services.AddScoped<IPropertyDescriptionAndTypeOfUseValidationService, PropertyDescriptionAndTypeOfUseValidationService>();
        services.AddScoped<IBlockMasterService, BlockMasterService>();
        services.AddScoped<IReferenceValidationService, ReferenceValidationService>();
        services.AddScoped<IRuleScopeService, RuleScopeService>();
        services.AddScoped<IRuleCategoryService, RuleCategoryService>();
        services.AddScoped<IRuleEffectTypeService, RuleEffectTypeService>();
        services.AddScoped<IRuleOperatorService, RuleOperatorService>();

        services.AddScoped<IGenderMasterService, GenderMasterService>();
        services.AddScoped<ISocietyDetailsService, SocietyDetailsService>();
        services.AddScoped<ISocietyWingDetailsService, SocietyWingDetailsService>();
        services.AddScoped<ICommonRemarkTypeMasterService, CommonRemarkTypeMasterService>();
        services.AddScoped<ICommonRemarkDetailsService, CommonRemarkDetailsService>();
        services.AddScoped<IPropertyMapMasterService, PropertyMapMasterService>();

        // Water Connection Services
        services.AddScoped<IWaterConnectionTypeService, WaterConnectionTypeService>();
        services.AddScoped<IWaterConnectionSizeService, WaterConnectionSizeService>();
        services.AddScoped<IWaterConnectionStatusService, WaterConnectionStatusService>();
        services.AddScoped<IWaterRateMasterService, WaterRateMasterService>();
        services.AddScoped<IWaterConnectionService, WaterConnectionService>();
        services.AddScoped<IWaterConnectionDetailsService, WaterConnectionDetailsService>();

        services.AddScoped<IPropertyAssessmentStatusService, PropertyAssessmentStatusService>();
        services.AddScoped<ICertificateTaxGuidelineService, CertificateTaxGuidelineService>();
        services.AddScoped<IRoomTypeMasterService, RoomTypeMasterService>();
        services.AddScoped<IAssetCategoryService, AssetCategoryService>();
        services.AddScoped<IAssetTypeService, AssetTypeService>();
        services.AddScoped<IOwnershipTypeService, OwnershipTypeService>();
        services.AddScoped<IOwningDepartmentService, OwningDepartmentService>();
        //Asset Start
        services.AddScoped<IInventoryItemCategoryService, InventoryItemCategoryService>();
        services.AddScoped<IInventoryItemNameService, InventoryItemNameService>();
        services.AddScoped<IInventoryItemConditionService, InventoryItemConditionService>();
        services.AddScoped<IInventoryItemModelService, InventoryItemModelService>();
        services.AddScoped<IScreenService, ScreenService>();
        services.AddScoped<IScreenFormSectionMasterService, ScreenFormSectionMasterService>();
        services.AddScoped<IScreenFormFieldMasterService, ScreenFormFieldMasterService>();
        services.AddScoped<IAssetAgeFactorCVService, AssetAgeFactorCVService>();
        services.AddScoped<IAssetNatureFactorCVService, AssetNatureFactorCVService>();
        services.AddScoped<IAssetAssessmentYearRangeCVService, AssetAssessmentYearRangeCVService>();
        services.AddScoped<IAssetDesignationService, AssetDesignationService>();
        services.AddScoped<IAssetConditionMasterService, AssetConditionMasterService>();
        services.AddScoped<IAssetRoomTypeMasterService, AssetRoomTypeService>();
        // Rules namespace registrations
        services.AddScoped<IRuleFieldsService, RuleFieldsService>();
        services.AddScoped<IRuleEngineService, RuleEngineService>();
        services.AddScoped<IPropertyRuleApplicationLogService, PropertyRuleApplicationLogService>();
        services.AddScoped<IFieldConfigurationService, FieldConfigurationService>();
        services.AddScoped<IRuleApplierService, RuleApplierService>();
        services.AddScoped<IPropertyContextLoaderService, PropertyContextLoaderService>();
        services.AddScoped<IAssetDocumentDefinitionService, AssetDocumentDefinitionService>();
        services.AddScoped<IAssetFieldDefinitionService, AssetFieldDefinitionService>();
        services.AddScoped<IAssetAuthorityMasterService, AssetAuthorityMasterService>();
        services.AddScoped<IAssetOrganizationMasterService, AssetOrganizationMasterService>();
        services.AddScoped<IGSTService, GSTService>();
        services.AddScoped<IPenaltyRuleService, PenaltyRuleService>();
        services.AddScoped<IAssetMoujaService, AssetMoujaService>();
        services.AddScoped<IAssetSubZoneDetailsForCVService, AssetSubZoneDetailsForCVService>();
        services.AddScoped<IAssetTypeOfUseGroupService, AssetTypeOfUseGroupService>();
        services.AddScoped<IAssetTypeOfUseService, AssetTypeOfUseService>();
        services.AddScoped<IAssetSubTypeOfUseService, AssetSubTypeOfUseService>();


        // Rule Execution Service - Scoped to match IRepository lifetime (DbContext safety)
        // IMemoryCache is singleton and thread-safe, so cache is still shared across all requests
        // Effect applicators are stateless, safe as singleton for better performance
        services.AddScoped<IRuleExecutionService, RuleExecutionService>();
        services.AddSingleton<IRuleEffectApplicator, DecreasePercentApplicator>();
        services.AddSingleton<IRuleEffectApplicator, IncreasePercentApplicator>();
        services.AddSingleton<IRuleEffectApplicator, MultiplyApplicator>();
        services.AddSingleton<IRuleEffectApplicator, OverrideApplicator>();
        services.AddSingleton<IRuleEffectApplicator, ExemptionApplicator>();
        // RateLookupApplicator is Scoped because it depends on IRepository (DbContext-bound)
        services.AddScoped<IRuleEffectApplicator, RateLookupApplicator>();
        services.AddScoped<ITaxMasterService, TaxMasterService>();
        services.AddScoped<IRateableValueCalculatorService, RateableValueCalculatorService>();
        services.AddScoped<IRVCalculationCleanupService, RVCalculationCleanupService>();
        services.AddScoped<ITaxApplicabilityService, TaxApplicabilityService>();

        // Report Services
        // Singleton cache + hosted warmup (same instance used for both DI injection and startup warmup)
        services.AddSingleton<ReportDefinitionCacheService>();
        services.AddSingleton<ReportDefinitionCacheWarmupService>();
        services.AddHostedService(sp => sp.GetRequiredService<ReportDefinitionCacheWarmupService>());
        // Report catalogue CRUD services write to the report database, so they receive a
        // ReportingDbContext-backed IUnitOfWork (the same scoped context their repository resolves,
        // so tracked changes are persisted). The repository comes from the closed-generic
        // registrations above.
        services.AddScoped<IReportDefinitionService>(sp => new ReportDefinitionService(
            sp.GetRequiredService<IRepository<NtisPlatform.Core.Entities.Master.ReportDefinitionEntity, int>>(),
            new ReportDbUnitOfWork(sp.GetRequiredService<ReportingDbContext>()),
            sp.GetRequiredService<IMapper>()));
        services.AddScoped<IReportParameterDefinitionService>(sp => new ReportParameterDefinitionService(
            sp.GetRequiredService<IRepository<NtisPlatform.Core.Entities.Master.ReportParameterDefinitionEntity, int>>(),
            new ReportDbUnitOfWork(sp.GetRequiredService<ReportingDbContext>()),
            sp.GetRequiredService<IMapper>()));
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IReportWorkerService, ReportWorkerService>();
        // Read-only lookup for report module name/logo (GetAll/GetById only — modules are
        // managed exclusively through the report-admin tool). Same ReportingDbContext-backed
        // IUnitOfWork wiring as ReportDefinitionService/ReportParameterDefinitionService above.
        services.AddScoped<IReportModuleService>(sp => new ReportModuleService(
            sp.GetRequiredService<IRepository<NtisPlatform.Core.Entities.Master.ReportModuleEntity, int>>(),
            new ReportDbUnitOfWork(sp.GetRequiredService<ReportingDbContext>()),
            sp.GetRequiredService<IMapper>()));
        // No job-enqueuer here: the platform only inserts Pending rows into dbo.ReportRequest. The
        // ntis-report worker polls for them and enqueues/renders itself, so the two repos share only
        // the database (no cross-repo Hangfire job-contract assembly).
        // Async reporting options (token lifetimes, page sizes, lease, retries)
        services.Configure<NtisPlatform.Application.Options.ReportingOptions>(
            configuration.GetSection(NtisPlatform.Application.Options.ReportingOptions.Section));
        // Data providers — one per report type; add new reports here
        services.AddScoped<IReportDataProvider, NoticeNewDataProvider>();
        services.AddScoped<IReportDataProvider, NoDueCertificateDataProvider>();
        services.AddScoped<IReportDataProvider, KarakarniDataProvider>();
        services.AddScoped<IReportDataProvider, JaptiNoticeDataProvider>();
        services.AddScoped<IReportDataProvider, WarrentNoticeDataProvider>();
        services.AddScoped<IReportDataProvider, SpotSurveyFormDataProvider>();
        services.AddScoped<IReportDataProvider, SpecialNoticeDataProvider>();
        services.AddScoped<IReportDataProvider, RentedNoticeDataProvider>();
        services.AddScoped<IReportDataProvider, TransferCertificateDataProvider>();
        services.AddScoped<IReportDataProvider, Notice120DataProvider>();
        services.AddScoped<IReportDataProvider, SocietyOutstandingReportDataProvider>();
        services.AddScoped<IReportDataProvider, PrarupYadiDataProvider>();
        services.AddScoped<IReportDataProvider, BlankHearingFormatDataProvider>();
        services.AddScoped<IReportDataProvider, DocumentNoticeDataProvider>();
        services.AddScoped<IReportDataProvider, PermissionNoticeDataProvider>();
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

            // Enable Swagger schema support for C# non-nullable reference types
            options.SupportNonNullableReferenceTypes();

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

            // SignalR hub CORS: needs AllowCredentials for the HTTP negotiation handshake.
            options.AddPolicy("HubCors", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        services.AddSignalR();

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

            // Allow SignalR WebSocket connections to pass the JWT via query string.
            // WebSocket protocol doesn't support custom headers, so SignalR clients send
            // the token as ?access_token=... on the /hubs/* path only.
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(token) &&
                        ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    {
                        ctx.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Authorization with fallback policy - all endpoints require authentication by default
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Worker data/upload/notify endpoints: valid JWT + scope=report-worker.
            options.AddPolicy("ReportWorker", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "report-worker");
            });

            // SignalR hub: valid JWT + scope=report-hub (hub tokens carry this claim).
            options.AddPolicy("ReportHub", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("scope", "report-hub");
            });
        });

        // Rate Limiting (ASP.NET Core 7+)
        // Only enabled if RateLimiting:Enabled is true (for production)
        var rateLimitingEnabled = configuration.GetValue<bool>("RateLimiting:Enabled", false);
        if (rateLimitingEnabled)
        {
            services.AddRateLimiter(options =>
            {
                // Read rate limiting configuration
                var globalPermitLimit = configuration.GetValue<int>("RateLimiting:Global:PermitLimit", 100);
                var globalWindowMinutes = configuration.GetValue<int>("RateLimiting:Global:WindowMinutes", 1);
                var loginPermitLimit = configuration.GetValue<int>("RateLimiting:Login:PermitLimit", 5);
                var loginWindowMinutes = configuration.GetValue<int>("RateLimiting:Login:WindowMinutes", 15);
                var uploadPermitLimit = configuration.GetValue<int>("RateLimiting:FileUpload:PermitLimit", 10);
                var uploadWindowMinutes = configuration.GetValue<int>("RateLimiting:FileUpload:WindowMinutes", 5);
                var uploadQueueLimit = configuration.GetValue<int>("RateLimiting:FileUpload:QueueLimit", 2);

                // Global default policy for all endpoints (unless overridden)
                options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = globalPermitLimit,
                            Window = TimeSpan.FromMinutes(globalWindowMinutes),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                // Stricter fixed window rate limiter for login endpoint
                options.AddPolicy("login", context =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = loginPermitLimit,
                            Window = TimeSpan.FromMinutes(loginWindowMinutes),
                            QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                // Dedicated rate limiter for file upload endpoints
                // Prevents abuse and resource exhaustion from excessive file uploads
                options.AddPolicy("fileupload", context =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = uploadPermitLimit,
                            Window = TimeSpan.FromMinutes(uploadWindowMinutes),
                            QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                            QueueLimit = uploadQueueLimit
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
        }

        // P0: Caching with size limits to prevent memory leaks
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100; // Max 100 cache entries (each rule category = 1 entry)
            options.CompactionPercentage = 0.25; // Remove 25% of entries when size limit reached
        });

        services.AddResponseCaching();

        // Health checks
        services.AddHealthChecks();

        return services;
    }
}
