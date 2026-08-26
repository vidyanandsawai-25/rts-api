using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for GisDepartmentFilterMapping entity and DTOs
/// </summary>
public class GisDepartmentFilterMappingProfile : Profile
{
    public GisDepartmentFilterMappingProfile()
    {
        CreateMap<GisDepartmentFilterMappingEntity, GisDepartmentFilterMappingDto>()
            .ForMember(d => d.DepartmentName, opt => opt.MapFrom(s => s.Department != null ? s.Department.DepartmentName : null))
            .ForMember(d => d.FilterKey, opt => opt.MapFrom(s => s.FilterMaster != null ? s.FilterMaster.FilterKey : null))
            .ForMember(d => d.DefaultFilterLabel, opt => opt.MapFrom(s => s.FilterMaster != null ? s.FilterMaster.FilterLabel : null));
        CreateMap<CreateGisDepartmentFilterMappingDto, GisDepartmentFilterMappingEntity>();
        CreateMap<UpdateGisDepartmentFilterMappingDto, GisDepartmentFilterMappingEntity>();
    }
}
