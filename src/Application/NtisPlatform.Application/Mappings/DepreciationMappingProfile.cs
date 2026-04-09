using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;


namespace NtisPlatform.Application.Mappings;

public class DepreciationMappingProfile : Profile
{
    public DepreciationMappingProfile()
    {
        CreateMap<DepreciationMasterEntity, DepreciationDtos>()
            ;

        CreateMap<CreateDepreciationDto, DepreciationMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateDepreciationDto, DepreciationMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
