using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;
public class InventoryItemCategoryMappingProfile : Profile
{
    public InventoryItemCategoryMappingProfile()
    {
        CreateMap<InventoryItemCategoryEntity, InventoryItemCategoryDto>();

        CreateMap<CreateInventoryItemCategoryDto, InventoryItemCategoryEntity>()
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateInventoryItemCategoryDto, InventoryItemCategoryEntity>()
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
