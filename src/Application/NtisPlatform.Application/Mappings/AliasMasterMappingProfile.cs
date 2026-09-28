using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for AliasMaster entity and DTOs
/// </summary>
public class AliasMasterMappingProfile : Profile
{
    public AliasMasterMappingProfile()
    {
        CreateMap<AliasMasterEntity, AliasMasterDto>();
        CreateMap<AliasMasterDto, AliasMasterEntity>();

        CreateMap<CreateAliasMasterDto, AliasMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateAliasMasterDto, AliasMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.KeyName, opt => opt.Ignore());
    }
}
