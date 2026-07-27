using AutoMapper;
using NtisPlatform.Application.DTOs.Master.CertificateTaxGuideline;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class CertificateTaxGuidelineMappingProfile : Profile
{
    public CertificateTaxGuidelineMappingProfile()
    {
        CreateMap<CertificateTaxGuidelineEntity, CertificateTaxGuidelineDto>();

        CreateMap<CertificateTaxGuidelineDto, CertificateTaxGuidelineEntity>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        CreateMap<CreateCertificateTaxGuidelineDto, CertificateTaxGuidelineEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.GuidelineGroup, opt => opt.MapFrom(src => src.GuidelineGroup ?? string.Empty));

        CreateMap<UpdateCertificateTaxGuidelineDto, CertificateTaxGuidelineEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.GuidelineGroup, opt => opt.MapFrom(src => src.GuidelineGroup ?? string.Empty));

    }
}
