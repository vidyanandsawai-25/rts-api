using AutoMapper;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.DTOs.WingDetailsMast;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class WingDetailsMastMappingProfile : Profile
{
    public WingDetailsMastMappingProfile()
    {
        // ── WingDetailsMast Entity → Read DTO ────────────────────────────────
        CreateMap<WingDetailsMastEntity, WingDetailsMastDto>()
            .ForMember(dest => dest.SocietyWingDetails,
                opt => opt.MapFrom(src => src.SocietyWingDetails));

        // ── Create DTO → Entity ──────────────────────────────────────────────
        CreateMap<CreateWingDetailsMastDto, WingDetailsMastEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())

            // Navigation Properties
            .ForMember(dest => dest.SocietyDetailsMast, opt => opt.Ignore())
            .ForMember(dest => dest.WingMaster, opt => opt.Ignore())
            .ForMember(dest => dest.ManagerMobileNoRemarkMaster, opt => opt.Ignore())
            .ForMember(dest => dest.SecretaryMobileNoRemarkMaster, opt => opt.Ignore())

            // Child Entity
            .ForMember(dest => dest.SocietyWingDetails,
                opt => opt.MapFrom(src => src.SocietyWingDetails))

            .ForMember(dest => dest.Properties, opt => opt.Ignore());

        // ── Update DTO → Entity ──────────────────────────────────────────────
        CreateMap<UpdateWingDetailsMastDto, WingDetailsMastEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())

            // Immutable Fields
            .ForMember(dest => dest.SocietyDetailsMastId, opt => opt.Ignore())
            .ForMember(dest => dest.WingMasterId, opt => opt.Ignore())

            // Navigation Properties
            .ForMember(dest => dest.SocietyDetailsMast, opt => opt.Ignore())
            .ForMember(dest => dest.WingMaster, opt => opt.Ignore())
            .ForMember(dest => dest.ManagerMobileNoRemarkMaster, opt => opt.Ignore())
            .ForMember(dest => dest.SecretaryMobileNoRemarkMaster, opt => opt.Ignore())
            .ForMember(dest => dest.SocietyWingDetails, opt => opt.Ignore())
            .ForMember(dest => dest.Properties, opt => opt.Ignore());

        // ── PropertyPhoto Entity → Upload Response DTO ───────────────────────
        CreateMap<PropertyPhotoEntity, PropertyPhotoUploadResponseDto>()
            .ForMember(dest => dest.PropertyPhotoId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.PropertyId, opt => opt.MapFrom(src => src.PropertyId))
            .ForMember(dest => dest.PhotoTypeId, opt => opt.MapFrom(src => src.PhotoTypeId))
            .ForMember(dest => dest.PhotoTypeCode, opt => opt.MapFrom(src =>
                    src.PhotoType != null ? src.PhotoType.PhotoTypeCode : null))

            .ForMember(dest => dest.DocumentBindingId, opt => opt.MapFrom(src => src.DocumentBindingId))
            .ForMember(dest => dest.DocumentId, opt => opt.MapFrom(src => src.DocumentBinding!.Document!.Id))
            .ForMember(dest => dest.DocumentGuid, opt => opt.MapFrom(src => src.DocumentBinding!.Document!.DocumentGuid))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.DocumentBinding!.Document!.FileName))
            .ForMember(dest => dest.FileSizeBytes, opt => opt.MapFrom(src => src.DocumentBinding!.Document!.FileSizeBytes))
            .ForMember(dest => dest.StoragePath, opt => opt.MapFrom(src => src.DocumentBinding!.Document!.StoragePath))
            .ForMember(dest => dest.IsPrimary, opt => opt.MapFrom(src => src.DocumentBinding!.IsPrimaryDocument))
            .ForMember(dest => dest.IsLatest, opt => opt.MapFrom(src => src.IsLatest));
    }
}