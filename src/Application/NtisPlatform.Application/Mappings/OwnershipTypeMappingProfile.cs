using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class OwnershipTypeMappingProfile : Profile
    {
        public OwnershipTypeMappingProfile()
        {
            CreateMap<OwnershipTypeEntity, OwnershipTypeDto>();

            CreateMap<CreateOwnershipTypeDto, OwnershipTypeEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
                .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateOwnershipTypeDto, OwnershipTypeEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
                .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
