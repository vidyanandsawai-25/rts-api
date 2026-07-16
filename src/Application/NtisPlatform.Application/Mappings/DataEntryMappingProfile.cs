using AutoMapper;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class DataEntryMappingProfile : Profile
{
    public DataEntryMappingProfile()
    {
        // ── Entity → Read DTO ────────────────────────────────────────
        CreateMap<PropertyDetailsEntity, PropertyDetailsDto>()
          
            .ForMember(dest => dest.FloorDescription,
                opt => opt.MapFrom(src => src.Floor != null ? src.Floor.Description : null))
            .ForMember(dest => dest.SubFloorDescription,
                opt => opt.MapFrom(src => src.SubFloor != null ? src.SubFloor.Description : null))
            .ForMember(dest => dest.ConstructionTypeDescription,
                opt => opt.MapFrom(src => src.ConstructionType != null ? src.ConstructionType.Description : null))
            .ForMember(dest => dest.TypeOfUseDescription,
                opt => opt.MapFrom(src => src.TypeOfUse != null ? src.TypeOfUse.Description : null))
            .ForMember(dest => dest.SubTypeOfUseDescription,
                opt => opt.MapFrom(src => src.SubTypeOfUse != null ? src.SubTypeOfUse.Description : null))
            .ForMember(dest => dest.Length, opt => opt.Ignore())
            .ForMember(dest => dest.Width, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyCertificates, opt => opt.Ignore())
            .ForMember(dest => dest.Property, opt => opt.Ignore());

        // ── Create DTO → Entity ──────────────────────────────────────
        // Description fields (FloorDescription etc.) only exist on the DTO
        // not on the entity — AutoMapper ignores unknown destination members
        // automatically, so NO ForMember needed for them here
        CreateMap<CreatePropertyDetailsDto, PropertyDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())

            .ForMember(dest => dest.Property, opt => opt.Ignore())
            .ForMember(dest => dest.Floor, opt => opt.Ignore())
            .ForMember(dest => dest.SubFloor, opt => opt.Ignore())
            .ForMember(dest => dest.ConstructionType, opt => opt.Ignore())
            .ForMember(dest => dest.TypeOfUse, opt => opt.Ignore())
            .ForMember(dest => dest.SubTypeOfUse, opt => opt.Ignore())
            .ForMember(dest => dest.RenterDetails, opt => opt.Ignore())
            .ForMember(dest => dest.Renters, opt => opt.Ignore())
            .ForMember(dest => dest.RoomWiseSubmissionDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTaxCalculationCVResults, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTaxCalculationRVResults, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTaxCalculationSection129Results, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyOccupancyDetails, opt => opt.Ignore());

        // ── Update DTO → Entity ──────────────────────────────────────
        CreateMap<UpdatePropertyDetailsDto, PropertyDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())

            .ForMember(dest => dest.Property, opt => opt.Ignore())
            .ForMember(dest => dest.Floor, opt => opt.Ignore())
            .ForMember(dest => dest.SubFloor, opt => opt.Ignore())
            .ForMember(dest => dest.ConstructionType, opt => opt.Ignore())
            .ForMember(dest => dest.TypeOfUse, opt => opt.Ignore())
            .ForMember(dest => dest.SubTypeOfUse, opt => opt.Ignore())
            .ForMember(dest => dest.RenterDetails, opt => opt.Ignore())
            .ForMember(dest => dest.Renters, opt => opt.Ignore())
            .ForMember(dest => dest.RoomWiseSubmissionDetails, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTaxCalculationCVResults, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTaxCalculationRVResults, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTaxCalculationSection129Results, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyOccupancyDetails, opt => opt.Ignore());

        // ── Entity → Update DTO ──────────────────────────────────────
        CreateMap<PropertyDetailsEntity, UpdatePropertyDetailsDto>()
     
            .ForMember(dest => dest.FloorDescription,
                opt => opt.MapFrom(src => src.Floor != null ? src.Floor.Description : null))
            .ForMember(dest => dest.SubFloorDescription,
                opt => opt.MapFrom(src => src.SubFloor != null ? src.SubFloor.Description : null))
            .ForMember(dest => dest.ConstructionTypeDescription,
                opt => opt.MapFrom(src => src.ConstructionType != null ? src.ConstructionType.Description : null))
            .ForMember(dest => dest.TypeOfUseDescription,
                opt => opt.MapFrom(src => src.TypeOfUse != null ? src.TypeOfUse.Description : null))
            .ForMember(dest => dest.SubTypeOfUseDescription,
                opt => opt.MapFrom(src => src.SubTypeOfUse != null ? src.SubTypeOfUse.Description : null))
            .ForMember(dest => dest.RenterDetails, opt => opt.Ignore())
            .ForMember(dest => dest.Renters, opt => opt.Ignore())
            .ForMember(dest => dest.RoomWiseSubmissionDetails, opt => opt.Ignore());
    }
}
