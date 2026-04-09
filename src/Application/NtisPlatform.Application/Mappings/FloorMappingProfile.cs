using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class FloorMappingProfile : Profile
{
    public FloorMappingProfile()
    {
        CreateMap<FloorEntity, FloorDto>()
            
            .ForMember(dest => dest.MaxFloorNo, opt => opt.MapFrom(src => src.MaxFloorNo));

        CreateMap<CreateFloorDto, FloorEntity>()
          .ForMember(dest => dest.MaxFloorNo, opt => opt.MapFrom(src => src.MaxFloorNo))
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
          .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateFloorDto, FloorEntity>()
      .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
      .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
      .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

    }
}
