using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveCalculationEvidence;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class RetrospectiveCalculationEvidenceMappingProfile : Profile
{
    public RetrospectiveCalculationEvidenceMappingProfile()
    {
        CreateMap<RetrospectiveCalculationEvidenceEntity, RetrospectiveCalculationEvidenceDto>();

        CreateMap<CreateRetrospectiveCalculationEvidenceDto, RetrospectiveCalculationEvidenceEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());

        CreateMap<UpdateRetrospectiveCalculationEvidenceDto, RetrospectiveCalculationEvidenceEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));
    }
}
