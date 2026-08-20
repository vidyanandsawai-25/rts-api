using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAuditLog;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveRuleAuditLogMappingProfile : Profile
{
    public RetrospectiveRuleAuditLogMappingProfile()
    {
        CreateMap<RetrospectiveRuleAuditLogEntity, RetrospectiveRuleAuditLogDto>();

        CreateMap<CreateRetrospectiveRuleAuditLogDto, RetrospectiveRuleAuditLogEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateRetrospectiveRuleAuditLogDto, RetrospectiveRuleAuditLogEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));
    }
}
