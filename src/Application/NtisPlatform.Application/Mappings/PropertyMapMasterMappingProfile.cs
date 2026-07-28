using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyMapMaster;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Mappings;

public class PropertyMapMasterMappingProfile : Profile
{
    public PropertyMapMasterMappingProfile()
    {
        CreateMap<PropertyMapMasterEntity, PropertyMapMasterDtos>();

        CreateMap<CreatePropertyMapMasterDto, PropertyMapMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdatePropertyMapMasterDto, PropertyMapMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

        CreateMap<PropertyMastOldEntity, OldPropertyInfoDto>();

        CreateMap<PropertyDetailsOldEntity, PropertyDetailsOldDto>()
            .ForMember(dest => dest.PropertyId, opt => opt.MapFrom(src => src.PropertyMastOldId))
            .ForMember(dest => dest.FloorDescription, opt => opt.Ignore())
            .ForMember(dest => dest.SubFloorDescription, opt => opt.Ignore())
            .ForMember(dest => dest.ConstructionTypeDescription, opt => opt.Ignore())
            .ForMember(dest => dest.TypeOfUseDescription, opt => opt.Ignore())
            .ForMember(dest => dest.SubTypeOfUseDescription, opt => opt.Ignore())
            .ForMember(dest => dest.ConstructionYearValue, opt => opt.Ignore())
            .ForMember(dest => dest.AssessmentYearValue, opt => opt.Ignore())
            .ForMember(dest => dest.OldCarpetAreaSqMeter, opt => opt.MapFrom(src => src.OldCarpetAreaSqMeter.HasValue ? Math.Round(src.OldCarpetAreaSqMeter.Value, 2) : (double?)null))
            .ForMember(dest => dest.OldCarpetAreaSqFeet, opt => opt.MapFrom(src => src.OldCarpetAreaSqFeet.HasValue ? Math.Round(src.OldCarpetAreaSqFeet.Value, 2) : (double?)null))
            .ForMember(dest => dest.OldBuiltupAreaSqMeter, opt => opt.MapFrom(src => src.OldBuiltupAreaSqMeter.HasValue ? Math.Round(src.OldBuiltupAreaSqMeter.Value, 2) : (double?)null))
            .ForMember(dest => dest.OldBuiltupAreaSqFeet, opt => opt.MapFrom(src => src.OldBuiltupAreaSqFeet.HasValue ? Math.Round(src.OldBuiltupAreaSqFeet.Value, 2) : (double?)null))
            .AfterMap((src, dest) =>
            {
                if (!string.IsNullOrEmpty(src.OldConstructionYear) && int.TryParse(src.OldConstructionYear, out int cyear))
                {
                    dest.ConstructionYearValue = cyear;
                }
                if (!string.IsNullOrEmpty(src.OldAssessmentYear) && int.TryParse(src.OldAssessmentYear, out int ayear))
                {
                    dest.AssessmentYearValue = ayear;
                }
            });
    }
}