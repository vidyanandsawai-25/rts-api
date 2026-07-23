using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class GSTMappingProfile : Profile
{
    public GSTMappingProfile()
    {
        CreateMap<GSTMasterEntity, GSTDto>();

        CreateMap<CreateGSTDto, GSTMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdateGSTDto, GSTMasterEntity>()
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}
