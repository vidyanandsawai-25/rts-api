using AutoMapper;
using NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;
using NtisPlatform.Core.Entities.RetrospectiveTax;

namespace NtisPlatform.Application.Mappings.RetrospectiveTax;

public class EvidenceTypeMasterMappingProfile : Profile
{
    public EvidenceTypeMasterMappingProfile()
    {
        CreateMap<EvidenceTypeMasterEntity, EvidenceTypeMasterDto>();

        CreateMap<CreateEvidenceTypeMasterDto, EvidenceTypeMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateEvidenceTypeMasterDto, EvidenceTypeMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
