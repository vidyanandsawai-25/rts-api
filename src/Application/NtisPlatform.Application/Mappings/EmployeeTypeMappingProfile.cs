using AutoMapper;
using NtisPlatform.Application.DTOs.Master.EmployeeType;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class EmployeeTypeMappingProfile : Profile
    {
        public EmployeeTypeMappingProfile()
        {
            CreateMap<EmployeeTypeEntity, EmployeeTypeDto>();

            CreateMap<CreateEmployeeTypeDto, EmployeeTypeEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateEmployeeTypeDto, EmployeeTypeEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
