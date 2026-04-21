using AutoMapper;
using NtisPlatform.Application.DTOs.Master.GenderMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class GenderMasterMappingProfile : Profile
{
    public GenderMasterMappingProfile()
    {
        CreateMap<GenderMasterEntity, GenderMasterDtos>();

        CreateMap<CreateGenderMasterDto, GenderMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateGenderMasterDto, GenderMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}