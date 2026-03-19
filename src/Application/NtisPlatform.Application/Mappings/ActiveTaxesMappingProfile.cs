using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class ActiveTaxesMappingProfile : Profile
{
    public ActiveTaxesMappingProfile()
    {
        CreateMap<ActiveTaxesEntity, ActiveTaxesDto>();

        CreateMap<CreateActiveTaxesDto, ActiveTaxesEntity>()
            .ForMember(dest => dest.ActiveTaxesId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateActiveTaxesDto, ActiveTaxesEntity>()
            .ForMember(dest => dest.ActiveTaxesId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
