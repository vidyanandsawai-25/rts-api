using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings
{
    public class TypeOfUseGroupMappingProfile : Profile
    {
        public TypeOfUseGroupMappingProfile()
        {
            CreateMap<TypeOfUseGroupEntity, TypeOfUseGroupDto>()

                .ForMember(dest => dest.CountOfTypes, opt => opt.MapFrom(src => src.TypeOfUse.Count));

            CreateMap<CreateTypeOfUseGroupDto, TypeOfUseGroupEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));


            CreateMap<UpdateTypeOfUseGroupDto, TypeOfUseGroupEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
