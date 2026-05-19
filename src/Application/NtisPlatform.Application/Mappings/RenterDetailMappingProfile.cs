using AutoMapper;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RenterDetailMappingProfile : Profile
{
    public RenterDetailMappingProfile()
    {
        // ── Entity → Read DTO ────────────────────────────────────────
        // BaseDtos: Id, IsActive, CreatedDate, UpdatedDate — all match
        // PropertyDetailsId added to RenterDetailDto — maps directly
        CreateMap<RenterDetailEntity, RenterDetailDto>();

        // ── Create DTO → Entity ──────────────────────────────────────
        CreateMap<CreateRenterDetailsDto, RenterDetailEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())  // set by Repository.AddAsync
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
             
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore()); // navigation property

        // ── Update DTO → Entity ──────────────────────────────────────
        CreateMap<UpdateRenterDetailsDto, RenterDetailEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())  // set by Repository.UpdateAsync
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyDetailsId, opt => opt.Ignore())  // FK never changes on update
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore()); // navigation property

        // ── Entity → Update DTO ──────────────────────────────────────
        // Used for loading existing data into edit form
        CreateMap<RenterDetailEntity, UpdateRenterDetailsDto>();
    }
}