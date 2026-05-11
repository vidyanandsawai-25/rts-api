using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RoomTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class RoomTypeMasterMappingProfile : Profile
{
    public RoomTypeMasterMappingProfile()
    {
        CreateMap<RoomTypeMasterEntity, RoomTypeMasterDto>();

        CreateMap<CreateRoomTypeMasterDto, RoomTypeMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());

        CreateMap<UpdateRoomTypeMasterDto, RoomTypeMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
