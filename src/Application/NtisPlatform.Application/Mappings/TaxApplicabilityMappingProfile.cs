using AutoMapper;
using NtisPlatform.Application.DTOs.TaxApplicability;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings
{
    public class TaxApplicabilityMappingProfile : Profile
    {
        public TaxApplicabilityMappingProfile()
        {
            // ============================================
            // CREATE MAPPINGS
            // ============================================
            
            CreateMap<ApplyTaxesMasterEntity, CreateTaxStatusDto>()
                .ForMember(dest => dest.IsApplicable, opt => opt.MapFrom(src => !src.IsActive));

            CreateMap<CreateTaxStatusDto, ApplyTaxesMasterEntity>()
                .ForMember(dest => dest.PropertyId, opt => opt.Ignore())
                .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => !src.IsApplicable))
                .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(src => src.IsApplicable))
                .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.MapFrom(src => src.IsApplicable ? (DateTime?)DateTime.Now : null))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

            // ============================================
            // UPDATE MAPPINGS
            // ============================================
            
            CreateMap<ApplyTaxesMasterEntity, UpdateTaxStatusDto>()
                .ForMember(dest => dest.IsApplicable, opt => opt.MapFrom(src => !src.IsActive));

            CreateMap<UpdateTaxStatusDto, ApplyTaxesMasterEntity>()
                .ForMember(dest => dest.PropertyId, opt => opt.Ignore())
                .ForMember(dest => dest.PropertyMast, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => !src.IsApplicable))
                .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(src => src.IsApplicable))
                .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.MapFrom(src => src.IsApplicable ? (DateTime?)DateTime.Now : null))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

            // ============================================
            // RESPONSE MAPPINGS
            // ============================================
            
            CreateMap<ApplyTaxesMasterEntity, TaxApplicabilityResponseDto>()
                .ForMember(dest => dest.ApplicableCount, opt => opt.Ignore())
                .ForMember(dest => dest.ExemptedCount, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicableTaxes, opt => opt.Ignore())
                .ForMember(dest => dest.ExemptedTaxes, opt => opt.Ignore())
                .ForMember(dest => dest.FinancialYearId, opt => opt.Ignore())
                .ForMember(dest => dest.TypeOfUseGroupId, opt => opt.Ignore());
        }
    }
}
