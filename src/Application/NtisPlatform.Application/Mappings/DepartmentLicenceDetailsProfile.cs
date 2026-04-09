using AutoMapper;
using NtisPlatform.Application.DTOs.Master.DepartmentLicenceDetails;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for Department Licence Details mappings
/// </summary>
public class DepartmentLicenceDetailsProfile : Profile
{
    public DepartmentLicenceDetailsProfile()
    {
        // Entity to DTO
       CreateMap<DepartmentLicenceDetailsEntity, DepartmentLicenceDetailsDto>()
            ;

        // Create DTO to Entity
        CreateMap<CreateDepartmentLicenceDetailsDto, DepartmentLicenceDetailsEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        // Update DTO to Entity
        CreateMap<UpdateDepartmentLicenceDetailsDto, DepartmentLicenceDetailsEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
