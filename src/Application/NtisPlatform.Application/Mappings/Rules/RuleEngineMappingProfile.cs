using AutoMapper;
using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Mappings.Rules
{
    /// <summary>
    /// AutoMapper profile for RuleEngine entity and DTOs
    /// </summary>
    public class RuleEngineMappingProfile : Profile
    {
        public RuleEngineMappingProfile()
        {
            // Entity to DTO mapping
            CreateMap<RuleEngineEntity, RuleEngineDto>()
                .ForMember(dest => dest.RuleScopeName,
                    opt => opt.MapFrom(src => src.RuleScope != null ? src.RuleScope.RuleScope : null))
                .ForMember(dest => dest.PropertyRuleEvaluationMasterName,
                    opt => opt.MapFrom(src => src.PropertyRuleEvaluationMaster != null ? src.PropertyRuleEvaluationMaster.ParameterName : null))
                .ForMember(dest => dest.ParameterCode,
                    opt => opt.MapFrom(src => src.PropertyRuleEvaluationMaster != null ? src.PropertyRuleEvaluationMaster.ParameterCode : null))
                .ForMember(dest => dest.SubRules, opt => opt.Ignore());

            CreateMap<RuleEngineEntity, RuleEngineSummaryDto>()
                .ForMember(dest => dest.RuleScopeName,
                    opt => opt.MapFrom(src => src.RuleScope != null ? src.RuleScope.RuleScope : null))
                .ForMember(dest => dest.PropertyRuleEvaluationMasterName,
                    opt => opt.MapFrom(src => src.PropertyRuleEvaluationMaster != null ? src.PropertyRuleEvaluationMaster.ParameterName : null))
                .ForMember(dest => dest.ParameterCode,
                    opt => opt.MapFrom(src => src.PropertyRuleEvaluationMaster != null ? src.PropertyRuleEvaluationMaster.ParameterCode : null))
                .ForMember(dest => dest.SubRules, opt => opt.Ignore());

            // Create DTO to Entity mapping
            CreateMap<CreateRuleEngineDto, RuleEngineEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.RuleCode, opt => opt.MapFrom(src => src.RuleCode))
                .ForMember(dest => dest.RuleName, opt => opt.MapFrom(src => src.RuleName))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.RuleCategory, opt => opt.MapFrom(src => src.RuleCategory))
                .ForMember(dest => dest.RuleJson, opt => opt.MapFrom(src => src.RuleJson))
                .ForMember(dest => dest.ConditionsJson, opt => opt.MapFrom(src => src.ConditionsJson))
                .ForMember(dest => dest.EffectJson, opt => opt.MapFrom(src => src.EffectJson))
                .ForMember(dest => dest.TargetFiltersJson, opt => opt.MapFrom(src => src.TargetFiltersJson))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => src.IsEnabled))
                .ForMember(dest => dest.StopProcessing, opt => opt.MapFrom(src => src.StopProcessing))
                .ForMember(dest => dest.RuleScopeId, opt => opt.MapFrom(src => src.RuleScopeId))
                .ForMember(dest => dest.PropertyRuleEvaluationMasterId, opt => opt.MapFrom(src => src.PropertyRuleEvaluationMasterId))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // Update DTO to Entity mapping
            CreateMap<UpdateRuleEngineDto, RuleEngineEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
                .ForMember(dest => dest.RuleCode, opt => opt.Ignore())
                .ForMember(dest => dest.RuleName, opt => opt.MapFrom(src => src.RuleName))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.RuleCategory, opt => opt.MapFrom(src => src.RuleCategory))
                .ForMember(dest => dest.RuleJson, opt => opt.MapFrom(src => src.RuleJson))
                .ForMember(dest => dest.ConditionsJson, opt => opt.MapFrom(src => src.ConditionsJson))
                .ForMember(dest => dest.EffectJson, opt => opt.MapFrom(src => src.EffectJson))
                .ForMember(dest => dest.TargetFiltersJson, opt => opt.MapFrom(src => src.TargetFiltersJson))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => src.IsEnabled))
                .ForMember(dest => dest.RuleScopeId, opt => opt.MapFrom(src => src.RuleScopeId))
                .ForMember(dest => dest.PropertyRuleEvaluationMasterId, opt => opt.MapFrom(src => src.PropertyRuleEvaluationMasterId))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
