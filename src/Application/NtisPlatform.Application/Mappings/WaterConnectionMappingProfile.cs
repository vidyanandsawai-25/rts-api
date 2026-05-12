using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.WaterConnection;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class WaterConnectionMappingProfile : Profile
{
    public WaterConnectionMappingProfile()
    {
        CreateMap<WaterConnectionMasterEntity, WaterConnectionDto>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.WaterConnectionType.ConnectionTypeName))
            .ForMember(dest => dest.TapSize, opt => opt.MapFrom(src =>
                src.WaterConnectionSize.ConnectionSize.ToString("G29") + " " + src.WaterConnectionSize.ConnectionSizeUnit))
            .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src =>
                src.WaterConnectionStatus != null ? src.WaterConnectionStatus.StatusName : null))
            .ForMember(dest => dest.InstallDate, opt => opt.MapFrom(src =>
                src.ConnectionStartDate.ToString("yyyy-MM-dd")))
            .ForMember(dest => dest.ActivatedDate, opt => opt.MapFrom(src =>
                src.IsActive ? src.ConnectionStartDate.ToString("dd/MM/yyyy") : null))
            .ForMember(dest => dest.StoppedDate, opt => opt.MapFrom(src =>
                !src.IsActive && src.ConnectionStopDate.HasValue
                    ? src.ConnectionStopDate.Value.ToString("dd/MM/yyyy")
                    : null))
            .ForMember(dest => dest.ApplicableRate, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicableCharges, opt => opt.Ignore())
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => "Yearly"));

        CreateMap<CreateWaterConnectionDto, WaterConnectionMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionType, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionSize, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionStatus, opt => opt.Ignore())
            .ForMember(dest => dest.Details, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateWaterConnectionDto, WaterConnectionMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionType, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionSize, opt => opt.Ignore())
            .ForMember(dest => dest.WaterConnectionStatus, opt => opt.Ignore())
            .ForMember(dest => dest.Details, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
