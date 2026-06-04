using AutoMapper;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Mappings;

public class BulkUpdateMasterMappingProfile : Profile
{
    public BulkUpdateMasterMappingProfile()
    {
        CreateMap<BulkUpdateMasterEntity, BulkUpdateMasterDto>();

        CreateMap<CreateBulkUpdateMasterDto, BulkUpdateMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.FieldConfigs, opt => opt.Ignore());

        CreateMap<UpdateBulkUpdateMasterDto, BulkUpdateMasterEntity>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.FieldConfigs, opt => opt.Ignore());
    }
}
