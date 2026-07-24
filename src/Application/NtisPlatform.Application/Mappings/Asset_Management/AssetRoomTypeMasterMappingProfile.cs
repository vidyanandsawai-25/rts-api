using AutoMapper;
using NtisPlatform.Application.DTOs.Master.AssetRoomType;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class AssetRoomTypeMasterMappingProfile : Profile
{
    public AssetRoomTypeMasterMappingProfile()
    {
        CreateMap<AssetRoomTypeMasterEntity, AssetRoomTypeMasterDto>();


        CreateMap<CreateAssetRoomTypeDto, AssetRoomTypeMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<UpdateAssetRoomTypeDto, AssetRoomTypeMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
