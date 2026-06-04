using AutoMapper;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class BulkUpdateFieldConfigMappingProfile : Profile
{
    public BulkUpdateFieldConfigMappingProfile()
    {
        CreateMap<BulkUpdateFieldConfigEntity, BulkUpdateFieldConfigDto>();

        CreateMap<CreateBulkUpdateFieldConfigDto, BulkUpdateFieldConfigEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Master, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateBulkUpdateFieldConfigDto, BulkUpdateFieldConfigEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Master, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
