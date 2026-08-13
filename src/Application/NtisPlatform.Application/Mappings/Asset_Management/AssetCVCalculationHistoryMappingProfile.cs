using AutoMapper;
using NtisPlatform.Application.DTOs.AssetCapitalValue;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class AssetCVCalculationHistoryMappingProfile : Profile
{
    public AssetCVCalculationHistoryMappingProfile()
    {
        CreateMap<AssetCVCalculationHistoryEntity, AssetCVCalculationHistoryDto>()
            .ForMember(d => d.AssetNo, o => o.MapFrom(s => s.AssetMaster != null ? s.AssetMaster.AssetNo : string.Empty))
            .ForMember(d => d.AssetName, o => o.MapFrom(s => s.AssetMaster != null ? s.AssetMaster.AssetName : string.Empty))
            // Resolved separately (batched lookup) and assigned by the caller after mapping —
            // see AssetCapitalValueService.GetCalculationHistoryAsync.
            .ForMember(d => d.FloorDescription, o => o.Ignore())
            // No corresponding column on AssetCVCalculationHistoryEntity / [AMS].[AssetCVCalculationHistory].
            .ForMember(d => d.ConstructionTypeDescription, o => o.Ignore())
            .ForMember(d => d.TypeOfUseDescription, o => o.Ignore())
            .ForMember(d => d.SubTypeOfUseDescription, o => o.Ignore())
            .ForMember(d => d.HasLift, o => o.Ignore())
            .ForMember(d => d.CalculationFormula, o => o.Ignore());
    }
}
