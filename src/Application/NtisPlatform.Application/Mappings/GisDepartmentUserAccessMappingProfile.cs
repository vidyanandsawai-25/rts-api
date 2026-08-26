using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for GisDepartmentUserAccess entity and DTOs
/// </summary>
public class GisDepartmentUserAccessMappingProfile : Profile
{
    public GisDepartmentUserAccessMappingProfile()
    {
        CreateMap<GisDepartmentUserAccessEntity, GisDepartmentUserAccessDto>()
            .ForMember(d => d.DepartmentName, opt => opt.MapFrom(s => s.Department != null ? s.Department.DepartmentName : null));
        CreateMap<CreateGisDepartmentUserAccessDto, GisDepartmentUserAccessEntity>();
        CreateMap<UpdateGisDepartmentUserAccessDto, GisDepartmentUserAccessEntity>();
    }
}
