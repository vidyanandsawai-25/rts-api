using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;
public class InventoryItemConditionMappingFields : Profile
{
    public InventoryItemConditionMappingFields()
    {
        CreateMap<InventoryItemConditionEntity, InventoryItemConditionDto>();

        CreateMap<CreateInventoryItemConditionMasterDto, InventoryItemConditionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateInventoryItemConditionMasterDto, InventoryItemConditionEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
