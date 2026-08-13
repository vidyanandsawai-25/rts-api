using AutoMapper;
using NtisPlatform.Application.DTOs.wardallocation;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mapping;

public class WardAllocationProfile : Profile
{
    public WardAllocationProfile()
    {
        CreateMap<GlobalSurveyWardAllocationEntity, WardAllocationDto>()
            .ForMember(dest => dest.EmployeeName,
                opt => opt.MapFrom(src => src.User != null ? src.User.FirstName : null))
            .ForMember(dest => dest.EmpCode,
                opt => opt.MapFrom(src => src.User != null ? src.User.UserCode : null))
            .ForMember(dest => dest.DepartmentName,
                opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : null))
            .ForMember(dest => dest.ModuleName,
                opt => opt.MapFrom(src => src.Module != null ? src.Module.ModuleName : null))
            .ForMember(dest => dest.ZoneNo,
                opt => opt.MapFrom(src => src.Zone != null ? src.Zone.ZoneNo : null))
            .ForMember(dest => dest.WardNo,
                opt => opt.MapFrom(src => src.Ward != null ? src.Ward.WardNo : null));

        CreateMap<CreateWardAllocationDto, GlobalSurveyWardAllocationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.DepartmentId, opt => opt.Ignore()) // set from service
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Department, opt => opt.Ignore())
            .ForMember(dest => dest.Module, opt => opt.Ignore())
            .ForMember(dest => dest.Zone, opt => opt.Ignore())
            .ForMember(dest => dest.Ward, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<UpdateWardAllocationDto, GlobalSurveyWardAllocationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.DepartmentId, opt => opt.Ignore())
            .ForMember(dest => dest.ModuleId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Department, opt => opt.Ignore())
            .ForMember(dest => dest.Module, opt => opt.Ignore())
            .ForMember(dest => dest.Zone, opt => opt.Ignore())
            .ForMember(dest => dest.Ward, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());
    }
}