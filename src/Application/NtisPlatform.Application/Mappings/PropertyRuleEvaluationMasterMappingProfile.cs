using AutoMapper;
using NtisPlatform.Application.DTOs.Master.PropertyRuleEvaluationMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class PropertyRuleEvaluationMasterMappingProfile : Profile
    {
        public PropertyRuleEvaluationMasterMappingProfile()
        {
            CreateMap<PropertyRuleEvaluationMasterEntity, PropertyRuleEvaluationMasterDto>();

            CreateMap<CreatePropertyRuleEvaluationMasterDto, PropertyRuleEvaluationMasterEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdatePropertyRuleEvaluationMasterDto, PropertyRuleEvaluationMasterEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
