using AutoMapper;
using NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class RTSFieldDefinitionMappingProfile:Profile
{
    public RTSFieldDefinitionMappingProfile()
    {
        CreateMap<RTSFieldDefinitionEntity, RTSFieldDefinitionDto>();

        CreateMap<CreateRTSFieldDefinitionDto, RTSFieldDefinitionEntity>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<UpdateRTSFieldDefinitionDto, RTSFieldDefinitionEntity>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));
    }
}


