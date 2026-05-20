using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Tests.Helpers;

/// <summary>
/// Provides shared AutoMapper configuration for tests
/// </summary>
public static class AutoMapperTestHelper
{
    /// <summary>
    /// Creates a configured IMapper instance for RateMasterForCV tests
    /// </summary>
    public static IMapper CreateRateMasterForCVMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            // Map RateMasterForCVEntity to RateMasterForCVDto
            cfg.CreateMap<RateMasterForCVEntity, RateMasterForCVDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // Map CreateRateMasterForCVDto to RateMasterForCVEntity
            cfg.CreateMap<CreateRateMasterForCVDto, RateMasterForCVEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());
                // Ignore navigation properties - managed by EF Core
                //.ForMember(dest => dest.AssessmentYearRange, opt => opt.Ignore())
                //.ForMember(dest => dest.FloorGroup, opt => opt.Ignore())
                //.ForMember(dest => dest.TypeOfUseGroup, opt => opt.Ignore());

            // Map UpdateRateMasterForCVDto to RateMasterForCVEntity
            cfg.CreateMap<UpdateRateMasterForCVDto, RateMasterForCVEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());
                // Ignore navigation properties - managed by EF Core
                //.ForMember(dest => dest.AssessmentYearRange, opt => opt.Ignore())
                //.ForMember(dest => dest.FloorGroup, opt => opt.Ignore())
                //.ForMember(dest => dest.TypeOfUseGroup, opt => opt.Ignore());
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    /// <summary>
    /// Creates a configured IMapper instance for SubTypeOfUse tests
    /// </summary>
    public static IMapper CreateSubTypeOfUseMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SubTypeOfUseEntity, SubTypeOfUseDto>();
            cfg.CreateMap<CreateSubTypeOfUseDto, SubTypeOfUseEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
            cfg.CreateMap<UpdateSubTypeOfUseDto, SubTypeOfUseEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    /// <summary>
    /// Creates a configured IMapper instance that includes ALL application profiles.
    /// Use this for comprehensive tests that need all mappings.
    /// 
    /// Note: Validation is disabled to allow intentionally unmapped properties:
    /// - Auto-generated IDs (Id, PropertySeqNo, etc.)
    /// - Audit fields (CreatedBy, UpdatedBy, CreatedDate, UpdatedDate)
    /// - Navigation properties (managed by EF Core)
    /// - Computed/derived fields
    /// 
    /// Run AutoMapperValidationTest to verify only expected properties are unmapped.
    /// </summary>
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            // Add all mapping profiles from the Application assembly
            cfg.AddMaps(typeof(NtisPlatform.Application.Mappings.CapitalValueMappingProfile).Assembly);
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        // Validation is intentionally disabled to allow documented unmapped properties.
        // See AutoMapperValidationTest.cs for comprehensive validation with documented exceptions.
        // config.AssertConfigurationIsValid();

        return config.CreateMapper();
    }
}

