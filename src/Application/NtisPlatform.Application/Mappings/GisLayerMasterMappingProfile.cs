using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for GisLayerMaster entity and DTOs
/// </summary>
public class GisLayerMasterMappingProfile : Profile
{
    public GisLayerMasterMappingProfile()
    {
        CreateMap<GisLayerMasterEntity, GisLayerMasterDto>()
            .ForMember(d => d.DepartmentName, opt => opt.MapFrom(s => s.Department != null ? s.Department.DepartmentName : null));
        CreateMap<CreateGisLayerMasterDto, GisLayerMasterEntity>();
        CreateMap<UpdateGisLayerMasterDto, GisLayerMasterEntity>();
    }
}
