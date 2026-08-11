using AutoMapper;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RTSApplicationMappingProfile:Profile
{
    public RTSApplicationMappingProfile()
    {

        CreateMap<CreateRTSApplicationDetailsDto, RTSApplicationDetailsEntity>()  //insert  ApplicationDetails When Application is submit
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.MapFrom(s => DateTime.Now))
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.ApplicationNo, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.MapFrom(s => false))
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.FieldValueData, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));

        CreateMap<CreateRTSFieldValueDto, RTSFieldValueEntity>()   //insert FieldValueData When Application is submit
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.CreatedBy));


        CreateMap<RTSApplicationDetailsEntity, RTSApplicationDetailsDto>()    //SELECT APPLICATION DETAILS FOR APPROVAL DASHBAORD
                   .ForMember(dest => dest.FieldValues, opt => opt.MapFrom(src => src.FieldValueData));

        CreateMap<RTSFieldValueEntity, RTSFieldValueDto>();
        // ──────────────────────────Other MappingProfile Are Not configured Due To No Requirements Provided──────────────────────────────────────

        CreateMap<UpdateRTSFieldValueDto, RTSFieldValueEntity>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedDate, o => o.Ignore())
            .ForMember(d => d.CreatedBy, o => o.Ignore())
            .ForMember(d => d.UpdatedDate, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletion, o => o.Ignore())
            .ForMember(d => d.MarkedForDeletionDate, o => o.Ignore())
            .ForMember(d => d.UpdatedBy, o => o.MapFrom(s => s.UpdatedBy));

    }

}
