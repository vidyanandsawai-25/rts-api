using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Application.Mappings;

public class TypeOfUseMappingProfile : Profile
{

    public TypeOfUseMappingProfile()
    {
        CreateMap<TypeOfUseEntity, TypeOfUseDto>()
            ;

        CreateMap<CreateTypeOfUseDto, TypeOfUseEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        CreateMap<UpdateTypeOfUseDto, TypeOfUseEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());
    }

}

