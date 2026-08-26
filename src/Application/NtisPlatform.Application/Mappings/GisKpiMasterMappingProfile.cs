using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for GisKpiMaster entity and DTOs
/// </summary>
public class GisKpiMasterMappingProfile : Profile
{
    public GisKpiMasterMappingProfile()
    {
        CreateMap<GisKpiMasterEntity, GisKpiMasterDto>();
        CreateMap<CreateGisKpiMasterDto, GisKpiMasterEntity>();
        CreateMap<UpdateGisKpiMasterDto, GisKpiMasterEntity>();
    }
}
