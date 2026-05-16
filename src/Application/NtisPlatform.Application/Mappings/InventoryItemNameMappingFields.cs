using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;
public class InventoryItemNameMappingFields : Profile
{
    public InventoryItemNameMappingFields()
    {
        CreateMap<InventoryItemNameEntity, InventoryItemNameDto>();

        CreateMap<CreateInventoryItemNameDto, InventoryItemNameEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));
        
        CreateMap<UpdateInventoryItemNameDto, InventoryItemNameEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) 
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy)); 
    }
}
