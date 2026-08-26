using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for GisDepartmentKpiMapping entity and DTOs
/// </summary>
public class GisDepartmentKpiMappingProfile : Profile
{
    public GisDepartmentKpiMappingProfile()
    {
        CreateMap<GisDepartmentKpiMappingEntity, GisDepartmentKpiMappingDto>()
            .ForMember(d => d.DepartmentName, opt => opt.MapFrom(s => s.Department != null ? s.Department.DepartmentName : null))
            .ForMember(d => d.KpiCode, opt => opt.MapFrom(s => s.KpiMaster != null ? s.KpiMaster.KpiCode : null))
            .ForMember(d => d.DefaultTitle, opt => opt.MapFrom(s => s.KpiMaster != null ? s.KpiMaster.DefaultTitle : null));
        CreateMap<CreateGisDepartmentKpiMappingDto, GisDepartmentKpiMappingEntity>();
        CreateMap<UpdateGisDepartmentKpiMappingDto, GisDepartmentKpiMappingEntity>();
    }
}
