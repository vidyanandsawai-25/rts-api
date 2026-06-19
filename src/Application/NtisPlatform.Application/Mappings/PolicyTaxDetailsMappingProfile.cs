using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class PolicyTaxDetailsMappingProfile : Profile
{
    public PolicyTaxDetailsMappingProfile()
    {
        // Entity to DTO mapping
        CreateMap<PolicyTaxDetailsCVEntity, PolicyTaxDetailsCVDto>()
            .ForMember(dest => dest.TaxName, opt => opt.MapFrom(src => src.TaxMaster != null ? src.TaxMaster.TaxName : null));

        // CreateDto to Entity mapping
        CreateMap<CreatePolicyTaxDetailsCVDto, PolicyTaxDetailsCVEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
            .ForMember(dest => dest.TaxMaster, opt => opt.Ignore());

        // UpdateDto to Entity mapping
        CreateMap<UpdatePolicyTaxDetailsCVDto, PolicyTaxDetailsCVEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyId, opt => opt.Ignore())
            .ForMember(dest => dest.TaxId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
            .ForMember(dest => dest.TaxMaster, opt => opt.Ignore());
    }
}
