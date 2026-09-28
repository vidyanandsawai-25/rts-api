using AutoMapper;
using NtisPlatform.Application.DTOs.RTSAppeal;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for RTS Appeal entities and data transfer objects.
/// </summary>
public class RTSAppealMappingProfile : Profile
{
    public RTSAppealMappingProfile()
    {
        CreateMap<RTSAppealTypeMasterEntity, RTSAppealTypeDto>();

        CreateMap<CreateRTSAppealApplicationDto, RTSAppealApplicationEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.AppealStatus, opt => opt.Ignore())
            .ForMember(dest => dest.ActionRemarks, opt => opt.Ignore())
            .ForMember(dest => dest.ActionByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.ActionDate, opt => opt.Ignore())
            .ForMember(dest => dest.ReasonForComplaint, opt => opt.MapFrom(src => src.ReasonForAppeal))
            .ForMember(dest => dest.MobileNumber, opt => opt.MapFrom(src => src.MobileNo))
            .ForMember(dest => dest.EmailAddress, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.Application, opt => opt.Ignore())
            .ForMember(dest => dest.AppealType, opt => opt.Ignore())
            .ForMember(dest => dest.TrackAppealHistories, opt => opt.Ignore());
    }
}
