using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class TransMastMappingProfile : Profile
{
    public TransMastMappingProfile()
    {
        // Entity to DTO mapping
        CreateMap<TransMastEntity, TransMastDto>()
            .ForMember(dest => dest.TaxName, opt => opt.MapFrom(src => src.Tax != null ? src.Tax.TaxName : null))
            .ForMember(dest => dest.FinanceYear, opt => opt.MapFrom(src => src.FinanceYear != null ? src.FinanceYear.Year : (int?)null));

        // CreateDto to Entity mapping
        CreateMap<CreateTransMastDto, TransMastEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.Property, opt => opt.Ignore())
            .ForMember(dest => dest.Tax, opt => opt.Ignore())
            .ForMember(dest => dest.FinanceYear, opt => opt.Ignore());

        // UpdateDto to Entity mapping
        CreateMap<UpdateTransMastDto, TransMastEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyId, opt => opt.Ignore())
            .ForMember(dest => dest.FinanceYearId, opt => opt.Ignore())
            .ForMember(dest => dest.TaxId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletion, opt => opt.Ignore())
            .ForMember(dest => dest.MarkedForDeletionDate, opt => opt.Ignore())
            .ForMember(dest => dest.Property, opt => opt.Ignore())
            .ForMember(dest => dest.Tax, opt => opt.Ignore())
            .ForMember(dest => dest.FinanceYear, opt => opt.Ignore());
    }
}
