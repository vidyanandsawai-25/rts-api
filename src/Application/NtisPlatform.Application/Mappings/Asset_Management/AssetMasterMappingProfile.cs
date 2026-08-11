using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetMasterMappingProfile : Profile
{
    public AssetMasterMappingProfile()
    {
        CreateMap<AssetMasterEntity, AssetMasterDto>()
            .ForMember(dest => dest.FieldValues, opt => opt.MapFrom(src => src.FieldValues))
            .ForMember(dest => dest.TotalUnits, opt => opt.Ignore())
            .ForMember(dest => dest.TotalSubUnits, opt => opt.Ignore())
            .ForMember(dest => dest.TotalFloors, opt => opt.Ignore())
            .ForMember(dest => dest.AssetDocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.Details, opt => opt.MapFrom(src => new AssetDetailsDto
            {
                Id = src.Details != null ? src.Details.Id : 0,
                PlotNo = src.Details != null ? src.Details.PlotNo : null,
                PropertyNo = src.Details != null ? src.Details.PropertyNo : null,
                PartitionNo = src.Details != null ? src.Details.PartitionNo : null,
                UpicId = src.Details != null ? src.Details.UpicId : null,
                InChargeName = src.Details != null ? src.Details.InChargeName : null,
                InChargeRegionalName = src.Details != null ? src.Details.InChargeRegionalName : null,
                InChargeDesignationId = src.Details != null ? src.Details.InChargeDesignationId : null,
                InChargeMobile = src.Details != null ? src.Details.InChargeMobile : null,
                InChargeEmail = src.Details != null ? src.Details.InChargeEmail : null,
                LandRate = src.Details != null ? src.Details.LandRate : null,
                OrganizationId = src.Details != null ? src.Details.OrganizationId : 0,
                ZoneId = src.Details != null ? src.Details.ZoneId : null,
                WardId = src.Details != null ? src.Details.WardId : null,
                SubZoneId = src.Details != null ? src.Details.SubZoneId : null,
                MoujaId = src.Details != null ? src.Details.MoujaId : null,
                AssetWardNo = src.Details != null ? src.Details.AssetWardNo : null,
                CSN = src.Details != null ? src.Details.CSN : null,
                Address = src.Details != null ? src.Details.Address : null,
                NearestLandmark = src.Details != null ? src.Details.NearestLandmark : null,
                PinCode = src.Details != null ? src.Details.PinCode : null,
                Latitude = src.Details != null ? src.Details.Latitude : null,
                Longitude = src.Details != null ? src.Details.Longitude : null,
                LengthMtr = src.Details != null ? src.Details.LengthMtr : null,
                WidthMtr = src.Details != null ? src.Details.WidthMtr : null,
                LandAreaSqMeter = src.Details != null ? src.Details.LandAreaSqMeter : null,
                LengthFt = src.Details != null ? src.Details.LengthFt : null,
                WidthFt = src.Details != null ? src.Details.WidthFt : null,
                LandAreaSqFeet = src.Details != null ? src.Details.LandAreaSqFeet : null
            }))
            .ForMember(dest => dest.Names, opt => opt.MapFrom(src => new AssetMasterNamesDto
            {
                AssetCategoryName = src.AssetCategory != null ? src.AssetCategory.CategoryName : null,
                AssetTypeName = src.AssetType != null ? src.AssetType.TypeName : null,
                ParentAssetName = src.ParentAsset != null ? src.ParentAsset.AssetName : null
            }));

        CreateMap<AssetFieldValueEntity, AssetFieldValueDto>();

        CreateMap<CreateAssetMasterDto, AssetMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.FieldValues, opt => opt.Ignore())
            .ForMember(dest => dest.AssetCategory, opt => opt.Ignore())
            .ForMember(dest => dest.AssetType, opt => opt.Ignore())
            .ForMember(dest => dest.ParentAsset, opt => opt.Ignore())
            .ForMember(dest => dest.Details, opt => opt.Ignore())
            .ForMember(dest => dest.InventoryBatch, opt => opt.Ignore())
            .ForMember(dest => dest.SubUnitsDetails, opt => opt.Ignore())
            // AssetNo is always backend-generated (GenerateAssetNoAsync).
            .ForMember(dest => dest.AssetNo, opt => opt.Ignore())
            .ForMember(dest => dest.AssetRegionalName, opt => opt.MapFrom(src => src.AssetRegionalName))
            // Dropped from AMS.AssetMaster (compatibility shims kept only for legacy code).
            .ForMember(dest => dest.PurchaseValue, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseDate, opt => opt.Ignore())
            .ForMember(dest => dest.DepreciationId, opt => opt.Ignore())
            .ForMember(dest => dest.AssetLocationDetailsId, opt => opt.Ignore());

        CreateMap<UpdateAssetMasterDto, AssetMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.FieldValues, opt => opt.Ignore())
            .ForMember(dest => dest.AssetCategory, opt => opt.Ignore())
            .ForMember(dest => dest.AssetType, opt => opt.Ignore())
            .ForMember(dest => dest.ParentAsset, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Details, opt => opt.Ignore())
            .ForMember(dest => dest.InventoryBatch, opt => opt.Ignore())
            .ForMember(dest => dest.SubUnitsDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseValue, opt => opt.Ignore())
            .ForMember(dest => dest.PurchaseDate, opt => opt.Ignore())
            .ForMember(dest => dest.DepreciationId, opt => opt.Ignore())
            .ForMember(dest => dest.AssetLocationDetailsId, opt => opt.Ignore());

        CreateMap<CreateAssetFieldValueDto, AssetFieldValueEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AssetId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Asset, opt => opt.Ignore())
            // FieldValue is set from TextValue/NumberValue/DateValue/BooleanValue only in
            // hand-written service code (AssetMasterService), never through this map.
            .ForMember(dest => dest.FieldValue, opt => opt.Ignore());
    }
}
