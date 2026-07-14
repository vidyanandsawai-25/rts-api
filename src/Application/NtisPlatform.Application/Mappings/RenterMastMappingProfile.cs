using AutoMapper;
using NtisPlatform.Application.DTOs.RenterMast;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class RenterMastMappingProfile : Profile
{
    public RenterMastMappingProfile()
    {
        // ── Entity → Read DTO ────────────────────────────────────────
        // EF maps RenterId column → Id via HasColumnName in DbContext
        // AutoMapper maps Id property → Id property — no ForMember needed
        CreateMap<RenterMastEntity, RenterMastDto>()
            .ForMember(dest => dest.DocumentGuid, opt => opt.MapFrom(src => 
                src.DocumentBinding != null && src.DocumentBinding.Document != null 
                    ? src.DocumentBinding.Document.DocumentGuid 
                    : (Guid?)null));

        // ── Create DTO → Entity ──────────────────────────────────────
        CreateMap<CreateRenterMastDto, RenterMastEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())  // set by Repository.AddAsync
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore()) // navigation property
            .ForMember(dest => dest.DocumentBinding, opt => opt.Ignore()); // navigation property


        // ── Update DTO → Entity ──────────────────────────────────────
        CreateMap<UpdateRenterMastDto, RenterMastEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())  // set by Repository.UpdateAsync
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyDetailsId, opt => opt.Ignore()) // not included in UpdateDto
            .ForMember(dest => dest.PropertyDetails, opt => opt.Ignore()) // navigation property
            .ForMember(dest => dest.DocumentBinding, opt => opt.Ignore()); // navigation property

        // ── Entity → Update DTO ──────────────────────────────────────
        CreateMap<RenterMastEntity, UpdateRenterMastDto>();
    }
}