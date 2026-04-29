using AutoMapper;
using NtisPlatform.Application.DTOs.Master.UseFactorCVMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class UseFactorCVMasterMappingProfile : Profile
{
    public UseFactorCVMasterMappingProfile()
    {
        CreateMap<UseFactorCVMasterEntity, UseFactorCVMasterDto>();

        CreateMap<CreateUseFactorCVMasterDto, UseFactorCVMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateUseFactorCVMasterDto, UseFactorCVMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
