using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleScopeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class RuleScopeMappingProfile : Profile
    {
        public RuleScopeMappingProfile()
        {
            CreateMap<RuleScopeEntity, RuleScopeDto>();

            CreateMap<CreateRuleScopeDto, RuleScopeEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateRuleScopeDto, RuleScopeEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
