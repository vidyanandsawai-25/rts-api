using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management;

public class SubUnitsDetailsMappingProfile : Profile
{
    public SubUnitsDetailsMappingProfile()
    {
        // Entity to DTO
        CreateMap<SubUnitsDetailsEntity, SubUnitsDetailsDto>()
            .ForMember(dest => dest.Names, opt => opt.Ignore())
            .ForMember(dest => dest.SubAssetCount, opt => opt.Ignore());

        // CreateDto to Entity
        CreateMap<CreateSubUnitsDetailsDto, SubUnitsDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Asset, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsRented, opt => opt.Ignore());

        // UpdateDto to Entity
        CreateMap<UpdateSubUnitsDetailsDto, SubUnitsDetailsEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.Asset, opt => opt.Ignore())
            .ForMember(dest => dest.IsRented, opt => opt.Ignore());
    }
}
