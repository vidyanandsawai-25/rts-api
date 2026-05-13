using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RateMasterForCVMappingProfile : Profile
{
    public RateMasterForCVMappingProfile()
    {
        CreateMap<RateMasterForCVEntity, RateMasterForCVDto>()
            .ForMember(dest => dest.RateMasterCVId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            // Map navigation properties for display purposes
            .ForMember(dest => dest.SubZoneNo, opt => opt.Ignore()) // TODO: Map from SubZone navigation if available
            .ForMember(dest => dest.SubZoneName, opt => opt.Ignore()) // TODO: Map from SubZone navigation if available
            .ForMember(dest => dest.TypeOfUseGroupName, opt => opt.MapFrom(src => src.TypeOfUseGroup != null ? src.TypeOfUseGroup.GroupName : null))
            .ForMember(dest => dest.FloorGroupName, opt => opt.MapFrom(src => src.FloorGroup != null ? src.FloorGroup.FloorGroup : null))
            .ForMember(dest => dest.FromYear, opt => opt.MapFrom(src => src.AssessmentYearRange != null ? src.AssessmentYearRange.FromYear : (int?)null))
            .ForMember(dest => dest.ToYear, opt => opt.MapFrom(src => src.AssessmentYearRange != null ? src.AssessmentYearRange.ToYear : (int?)null));

        CreateMap<CreateRateMasterForCVDto, RateMasterForCVEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            // Explicitly map required entity fields from DTO
            .ForMember(dest => dest.SubZoneId, opt => opt.MapFrom(src => src.SubZoneId))
            .ForMember(dest => dest.TypeOfUseGroupId, opt => opt.MapFrom(src => src.TypeOfUseGroupId))
            .ForMember(dest => dest.FloorGroupId, opt => opt.MapFrom(src => src.FloorGroupId))
            .ForMember(dest => dest.RateAmount, opt => opt.MapFrom(src => src.RateAmount))
            .ForMember(dest => dest.AssessmentYearRangeId, opt => opt.MapFrom(src => src.AssessmentYearRangeId))
            // Ignore navigation properties - they will be loaded by EF Core
            .ForMember(dest => dest.AssessmentYearRange, opt => opt.Ignore())
            .ForMember(dest => dest.FloorGroup, opt => opt.Ignore())
            .ForMember(dest => dest.TypeOfUseGroup, opt => opt.Ignore());

        CreateMap<UpdateRateMasterForCVDto, RateMasterForCVEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            // Explicitly map required entity fields from DTO
            .ForMember(dest => dest.SubZoneId, opt => opt.MapFrom(src => src.SubZoneId))
            .ForMember(dest => dest.TypeOfUseGroupId, opt => opt.MapFrom(src => src.TypeOfUseGroupId))
            .ForMember(dest => dest.FloorGroupId, opt => opt.MapFrom(src => src.FloorGroupId))
            .ForMember(dest => dest.RateAmount, opt => opt.MapFrom(src => src.RateAmount))
            .ForMember(dest => dest.AssessmentYearRangeId, opt => opt.MapFrom(src => src.AssessmentYearRangeId))
            // Ignore navigation properties - they will be loaded by EF Core
            .ForMember(dest => dest.AssessmentYearRange, opt => opt.Ignore())
            .ForMember(dest => dest.FloorGroup, opt => opt.Ignore())
            .ForMember(dest => dest.TypeOfUseGroup, opt => opt.Ignore());
    }
}
