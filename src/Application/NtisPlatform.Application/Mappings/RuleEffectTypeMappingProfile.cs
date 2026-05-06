using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class RuleEffectTypeMappingProfile : Profile
    {
        public RuleEffectTypeMappingProfile()
        {
            CreateMap<RuleEffectTypeEntity, RuleEffectTypeDto>();

            CreateMap<CreateRuleEffectTypeDto, RuleEffectTypeEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateRuleEffectTypeDto, RuleEffectTypeEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
