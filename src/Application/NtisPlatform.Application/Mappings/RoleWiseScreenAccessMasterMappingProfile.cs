using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RoleWiseScreenAccessMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class RoleWiseScreenAccessMasterMappingProfile : Profile
    {
        public RoleWiseScreenAccessMasterMappingProfile()
        {
            CreateMap<RoleWiseScreenAccessMasterEntity, RoleWiseScreenAccessMasterDTO>();

            CreateMap<CreateRoleWiseScreenAccessMasterDto, RoleWiseScreenAccessMasterEntity>()
                .ForMember(dest => dest.RoleWiseScreenAccessId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UserRole, opt => opt.Ignore())
                .ForMember(dest => dest.Screen, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateRoleWiseScreenAccessMasterDto, RoleWiseScreenAccessMasterEntity>()
                .ForMember(dest => dest.RoleWiseScreenAccessId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UserRole, opt => opt.Ignore())
                .ForMember(dest => dest.Screen, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
