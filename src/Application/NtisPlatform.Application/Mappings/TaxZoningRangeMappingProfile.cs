using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class TaxZoningRangeMappingProfile : Profile
{
    public TaxZoningRangeMappingProfile()
    {
        CreateMap<TaxZoningRangeEntity, TaxZoningRangeDto>()
            .ForMember(dest => dest.WardNo, opt => opt.MapFrom(src => src.Ward != null ? src.Ward.WardNo : string.Empty))
            .ForMember(dest => dest.TaxZoneNo, opt => opt.MapFrom(src => src.TaxZone != null ? src.TaxZone.TaxZoneNo : string.Empty))
            .ForMember(dest => dest.MinPropertyNo, opt => opt.Ignore())
            .ForMember(dest => dest.MaxPropertyNo, opt => opt.Ignore());

        CreateMap<UpdateTaxZoningRangeDto, TaxZoningRangeEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.Ward, opt => opt.Ignore())
            .ForMember(dest => dest.TaxZone, opt => opt.Ignore());
    }
}
