using AutoMapper;
using NtisPlatform.Application.DTOs.Master.UserRoleMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class UserRoleMappingProfile : Profile
{
    public UserRoleMappingProfile()
    {
        CreateMap<UserRoleMasterEntity, UserRoleMasterDto>()
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : string.Empty))
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
