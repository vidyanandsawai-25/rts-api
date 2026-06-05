using AutoMapper;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class LockUnlockMappingProfile : Profile
{
    public LockUnlockMappingProfile()
    {
        CreateMap<ScreenMasterEntity, LockableScreenDto>()
            .ForMember(dest => dest.ScreenCode, opt => opt.MapFrom(src => src.ScreenCode ?? string.Empty))
            .ForMember(dest => dest.ScreenName, opt => opt.MapFrom(src => src.ScreenName ?? string.Empty));
    }
}
