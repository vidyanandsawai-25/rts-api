using AutoMapper;
using NtisPlatform.Application.DTOs.CitizenLoginDetails;
using NtisPlatform.Core.Entities.PropertyTax;

namespace NtisPlatform.Application.Mappings;

public class RTSCitizenLoginMappingProfile : Profile
{
    public RTSCitizenLoginMappingProfile()
    {
        CreateMap<RTSPropertyMastEntity, PropertyDto>();

        CreateMap<CreatePropertyDto, RTSPropertyMastEntity>(); //UNUSED
            
        CreateMap<UpdatePropertyDto, RTSPropertyMastEntity>(); //UNUSED

        CreateMap<RTSPropertyMastEntity, UpdatePropertyDto>(); //UNUSED
    }
}
