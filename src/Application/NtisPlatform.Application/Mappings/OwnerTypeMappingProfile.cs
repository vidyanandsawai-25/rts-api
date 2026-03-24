using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class OwnerTypeMappingProfile : Profile
{
    public OwnerTypeMappingProfile()
    {
        CreateMap<OwnerTypeEntity, OwnerTypeDto>();

        CreateMap<CreateOwnerTypeDto, OwnerTypeEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        CreateMap<UpdateOwnerTypeDto, OwnerTypeEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());
    }
}