using AutoMapper;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetLeaseRentDetailsMappingProfile : Profile
{
    public AssetLeaseRentDetailsMappingProfile()
    {
        CreateMap<AssetLeaseRentDetailsEntity, AssetLeaseRentDetailsDto>()
            .ForMember(dest => dest.Names, opt => opt.MapFrom(src => new AssetLeaseRentDetailsNamesDto
            {
                AssetNo = src.Asset != null ? src.Asset.AssetNo : null,
                AssetName = src.Asset != null ? src.Asset.AssetName : null,
                AssetCategoryName = src.Asset != null && src.Asset.AssetCategory != null ? src.Asset.AssetCategory.CategoryName : null,
                ApplicationTypeName = src.ApplicationType != null ? src.ApplicationType.ApplicationTypeName : null,
                FloorDescription = null
            }))
            // Flat mirror of the above — see AssetLeaseRentDetailsDto.AssetName/AssetCategoryName.
            .ForMember(dest => dest.AssetNo, opt => opt.MapFrom(src => src.Asset != null ? src.Asset.AssetNo : null))
            .ForMember(dest => dest.AssetName, opt => opt.MapFrom(src => src.Asset != null ? src.Asset.AssetName : null))
            .ForMember(dest => dest.AssetCategoryName, opt => opt.MapFrom(src => src.Asset != null && src.Asset.AssetCategory != null ? src.Asset.AssetCategory.CategoryName : null))
            // The rent column was renamed MonthlyRent -> RentAmount. The UI still reads the legacy
            // MonthlyRent field for display, so mirror RentAmount into it on the read side.
            .ForMember(dest => dest.MonthlyRent, opt => opt.MapFrom(src => src.RentAmount ?? 0m));

        CreateMap<CreateAssetLeaseRentDetailsDto, AssetLeaseRentDetailsEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            // The DB column was renamed MonthlyRent -> RentAmount. The UI still posts the rent value
            // in MonthlyRent, so persist RentAmount from whichever field carries a positive amount.
            .ForMember(dest => dest.RentAmount, opt => opt.MapFrom(src =>
                src.RentAmount.HasValue && src.RentAmount.Value > 0m ? src.RentAmount : (decimal?)src.MonthlyRent))
            // Real column but not yet exposed on this create contract.
            .ForMember(dest => dest.IsIncrement, opt => opt.Ignore());

        CreateMap<UpdateAssetLeaseRentDetailsDto, AssetLeaseRentDetailsEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.RentAmount, opt => opt.MapFrom(src =>
                src.RentAmount.HasValue && src.RentAmount.Value > 0m ? src.RentAmount : (decimal?)src.MonthlyRent))
            .ForMember(dest => dest.IsIncrement, opt => opt.Ignore());
    }
}
