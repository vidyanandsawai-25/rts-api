using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleCategory;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Mappings
{
    public class RuleCategoryMappingProfile : Profile
    {
        public RuleCategoryMappingProfile()
        {
            CreateMap<RuleCategoryEntity, RuleCategoryDto>();

            CreateMap<CreateRuleCategoryDto, RuleCategoryEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateRuleCategoryDto, RuleCategoryEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
