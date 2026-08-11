using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetRoomWiseMinusData;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

/// <summary>
/// AutoMapper profile for AssetRoomWiseMinusData mapping.
/// </summary>
public class AssetRoomWiseMinusDataMappingProfile : Profile
{
    public AssetRoomWiseMinusDataMappingProfile()
    {
        // Entity to DTO
        CreateMap<AssetRoomWiseMinusDataEntity, AssetRoomWiseMinusDataDto>()
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => src.UpdatedDate));

        // CreateDto to Entity
        CreateMap<CreateAssetRoomWiseMinusDataDto, AssetRoomWiseMinusDataEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.RoomWiseSubmissionDetails, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            // Feet-unit dimensions are real columns but not yet exposed on this create contract.
            .ForMember(dest => dest.LengthFt, opt => opt.Ignore())
            .ForMember(dest => dest.WidthFt, opt => opt.Ignore())
            .ForMember(dest => dest.HeightFt, opt => opt.Ignore())
            .ForMember(dest => dest.AreaSqFeet, opt => opt.Ignore());

        // UpdateDto to Entity
        CreateMap<UpdateAssetRoomWiseMinusDataDto, AssetRoomWiseMinusDataEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.RoomWiseSubmissionDetails, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.LengthFt, opt => opt.Ignore())
            .ForMember(dest => dest.WidthFt, opt => opt.Ignore())
            .ForMember(dest => dest.HeightFt, opt => opt.Ignore())
            .ForMember(dest => dest.AreaSqFeet, opt => opt.Ignore());
    }
}
