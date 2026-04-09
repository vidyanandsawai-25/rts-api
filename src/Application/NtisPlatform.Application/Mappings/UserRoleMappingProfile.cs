using AutoMapper;
using NtisPlatform.Application.DTOs.Master.UserRoleMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class UserRoleMappingProfile : Profile
{
    public UserRoleMappingProfile()
    {
        CreateMap<UserRoleMasterEntity, UserRoleMasterDto>()
            ;

        CreateMap<CreateUserRoleMasterDto, UserRoleMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateUserRoleMasterDto, UserRoleMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
