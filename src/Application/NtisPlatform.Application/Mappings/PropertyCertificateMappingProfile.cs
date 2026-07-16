using AutoMapper;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class PropertyCertificateMappingProfile : Profile
{
    public PropertyCertificateMappingProfile()
    {
        // ── Entity → DTO ─────────────────────────────────────────────
        CreateMap<PropertyCertificateEntity, PropertyCertificateDto>()
            .ForMember(dest => dest.CertificateTypeCode,
                opt => opt.MapFrom(src => src.CertificateType != null ? src.CertificateType.CertificateTypeCode : null));
    }
}
