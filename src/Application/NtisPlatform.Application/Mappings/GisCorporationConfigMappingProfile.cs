using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for GisCorporationConfig entity and DTOs
/// </summary>
public class GisCorporationConfigMappingProfile : Profile
{
    public GisCorporationConfigMappingProfile()
    {
        CreateMap<GisCorporationConfigEntity, GisCorporationConfigDto>();
        CreateMap<CreateGisCorporationConfigDto, GisCorporationConfigEntity>();
        CreateMap<UpdateGisCorporationConfigDto, GisCorporationConfigEntity>();
    }
}
