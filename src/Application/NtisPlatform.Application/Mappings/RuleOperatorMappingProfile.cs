using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RuleOperatorMaster;
using NtisPlatform.Core.Entities.Rules;

namespace NtisPlatform.Application.Mappings
{
    public class RuleOperatorMappingProfile : Profile
    {
        public RuleOperatorMappingProfile()
        {
            // Entity to DTO - for GET operations
            CreateMap<RuleOperatorEntity, RuleOperatorDto>()
                .ForMember(dest => dest.Operator, opt => opt.MapFrom(src => src.Operator))
                .ForMember(dest => dest.OperatorDescription, opt => opt.MapFrom(src => src.OperatorDescription));

            // CreateDTO to Entity - for POST operations
            CreateMap<CreateRuleOperatorDto, RuleOperatorEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Operator, opt => opt.MapFrom(src => src.Operator))
                .ForMember(dest => dest.OperatorDescription, opt => opt.MapFrom(src => src.OperatorDescription))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            // UpdateDTO to Entity - for PUT operations
            CreateMap<UpdateRuleOperatorDto, RuleOperatorEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Operator, opt => opt.MapFrom(src => src.Operator))
                .ForMember(dest => dest.OperatorDescription, opt => opt.MapFrom(src => src.OperatorDescription))
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
