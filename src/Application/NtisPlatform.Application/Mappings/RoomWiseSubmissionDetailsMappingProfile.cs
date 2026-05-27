using AutoMapper;
using NtisPlatform.Application.DTOs.RoomWiseMinusData;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RoomWiseSubmissionDetailsMappingProfile : Profile
{
    public RoomWiseSubmissionDetailsMappingProfile()
    {
        // ────────────────────────────────────────────────────────────────
        // CREATE MAPPINGS
        // ────────────────────────────────────────────────────────────────
        CreateMap<CreateRoomWiseSubmissionDetailsDto, RoomWiseSubmissionDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyRoomMinus,
                opt => opt.MapFrom(src => src.RoomWiseMinusData))  // ✅ Map nested collection
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
            .ForMember(dest => dest.RoomTypeMaster, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore());

        CreateMap<CreateRoomWiseMinusDataDto, RoomWiseMinusDataEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoomWiseSubmissionId, opt => opt.MapFrom(src => src.RoomWiseSubmissionId))
            .ForMember(dest => dest.RoomWiseSubmissionDetails, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore());

        // ────────────────────────────────────────────────────────────────
        // UPDATE MAPPINGS
        // ────────────────────────────────────────────────────────────────
        CreateMap<UpdateRoomWiseSubmissionDetailsDto, RoomWiseSubmissionDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyRoomMinus, opt => opt.Ignore())   
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
            .ForMember(dest => dest.RoomTypeMaster, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore());

        CreateMap<UpdateRoomWiseMinusDataDto, RoomWiseMinusDataEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RoomWiseSubmissionId, opt => opt.Ignore())
            .ForMember(dest => dest.RoomWiseSubmissionDetails, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore());

        // ────────────────────────────────────────────────────────────────
        // READ MAPPINGS (Entity → DTO)
        // ────────────────────────────────────────────────────────────────
        CreateMap<RoomWiseSubmissionDetailsEntity, RoomWiseSubmissionDetailsDto>()
             .ForMember(dest => dest.RoomWiseMinusData,
                 opt => opt.MapFrom(src => src.PropertyRoomMinus))  // ✅ Map to DTO collection
             .ForMember(dest => dest.RoomTypeDescription,
                 opt => opt.MapFrom(src => src.RoomTypeMaster != null ? src.RoomTypeMaster.RoomTypeName : null));  // ✅ Map RoomType description

        CreateMap<RoomWiseMinusDataEntity, RoomWiseMinusDataDto>();


    }
}