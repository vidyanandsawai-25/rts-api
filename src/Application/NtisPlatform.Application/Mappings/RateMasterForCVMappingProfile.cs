using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Master.CSNDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RateMasterForCVMappingProfile : Profile
{
    public RateMasterForCVMappingProfile()
    {
        // RateMasterForCV mappings
        CreateMap<RateMasterForCVEntity, RateMasterForCVDto>();

        CreateMap<CreateRateMasterForCVDto, RateMasterForCVEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CSNDetails, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));


        CreateMap<UpdateRateMasterForCVDto, RateMasterForCVEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now))
            .ForMember(dest => dest.CSNDetails, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

        // CSNDetails mappings
        CreateMap<CSNDetailsEntity, CSNDetailsDto>();

        CreateMap<CreateCSNDetailsDto, CSNDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RateCVMasterId, opt => opt.Ignore())
            .ForMember(dest => dest.MoujaId, opt => opt.MapFrom(src => src.MoujaId))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

        CreateMap<UpdateCSNDetailsDto, CSNDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RateCVMasterId, opt => opt.Ignore())
            .ForMember(dest => dest.MoujaId, opt => opt.MapFrom(src => src.MoujaId))
            .ForMember(dest => dest.CSN, opt => opt.MapFrom(src => src.CSN))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));
    }
}
