using AutoMapper;
using NtisPlatform.Application.DTOs.DualMethod;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings
{
    /// <summary>
    /// AutoMapper profile for DualMethod service mappings
    /// Maps tax entities to TaxDataDto for simplified processing
    /// </summary>
    public class DualMethodMappingProfile : Profile
    {
        public DualMethodMappingProfile()
        {
            // TransMastEntity to TaxDataDto (unified approach for CV data)
            CreateMap<TransMastEntity, TaxDataDto>()
                .ForMember(dest => dest.TaxId, opt => opt.MapFrom(src => src.TaxId))
                .ForMember(dest => dest.TaxName, opt => opt.MapFrom(src => 
                    src.Tax != null && !string.IsNullOrWhiteSpace(src.Tax.TaxName) 
                        ? src.Tax.TaxName 
                        : $"Tax_{src.TaxId}"))
                .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount));

            // TransMastCVEntity to TaxDataDto
            CreateMap<TransMastCVEntity, TaxDataDto>()
                .ForMember(dest => dest.TaxId, opt => opt.MapFrom(src => src.TaxId))
                .ForMember(dest => dest.TaxName, opt => opt.MapFrom(src => 
                    src.TaxMaster != null && !string.IsNullOrWhiteSpace(src.TaxMaster.TaxName) 
                        ? src.TaxMaster.TaxName 
                        : $"Tax_{src.TaxId}"))
                .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount));

            // TransMastRVEntity to TaxDataDto
            CreateMap<TransMastRVEntity, TaxDataDto>()
                .ForMember(dest => dest.TaxId, opt => opt.MapFrom(src => src.TaxId))
                .ForMember(dest => dest.TaxName, opt => opt.MapFrom(src => 
                    src.TaxMaster != null && !string.IsNullOrWhiteSpace(src.TaxMaster.TaxName) 
                        ? src.TaxMaster.TaxName 
                        : $"Tax_{src.TaxId}"))
                .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount));

            // TransMastOldEntity to TaxDataDto
            CreateMap<TransMastOldEntity, TaxDataDto>()
                .ForMember(dest => dest.TaxId, opt => opt.MapFrom(src => src.TaxId))
                .ForMember(dest => dest.TaxName, opt => opt.MapFrom(src => 
                    src.TaxMaster != null && !string.IsNullOrWhiteSpace(src.TaxMaster.TaxName) 
                        ? src.TaxMaster.TaxName 
                        : $"Tax_{src.TaxId}"))
                .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.TaxAmount));
        }
    }
}
