using AutoMapper;
using NtisPlatform.Application.DTOs.Master.ULBMaster;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for ULB Master entity and DTOs
/// </summary>
public class ULBMasterMappingProfile : Profile
{
    public ULBMasterMappingProfile()
    {
        // Entity to DTO
        CreateMap<ULBMasterEntity, ULBMasterDto>();
        // Create DTO to Entity
        CreateMap<CreateULBMasterDto, ULBMasterEntity>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        // Update DTO to Entity
        CreateMap<UpdateULBMasterDto, ULBMasterEntity>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
