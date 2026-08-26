using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for GisFilterMaster entity and DTOs
/// </summary>
public class GisFilterMasterMappingProfile : Profile
{
    public GisFilterMasterMappingProfile()
    {
        CreateMap<GisFilterMasterEntity, GisFilterMasterDto>();
        CreateMap<CreateGisFilterMasterDto, GisFilterMasterEntity>();
        CreateMap<UpdateGisFilterMasterDto, GisFilterMasterEntity>();
    }
}
