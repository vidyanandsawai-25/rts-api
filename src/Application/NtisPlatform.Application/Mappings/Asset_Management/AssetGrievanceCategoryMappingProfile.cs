using AutoMapper;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;

namespace NtisPlatform.Application.Mappings.Asset_Management
{
    public class AssetGrievanceCategoryMappingProfile : Profile
    {
        public AssetGrievanceCategoryMappingProfile()
        {
            CreateMap<AssetGrievanceCategoryEntity, AssetGrievanceCategoryDto>();

            CreateMap<CreateAssetGrievanceCategoryDto, AssetGrievanceCategoryEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(_ => false))
                .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

            CreateMap<UpdateAssetGrievanceCategoryDto, AssetGrievanceCategoryEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
                .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());
        }
    }
}
