using AutoMapper;
using NtisPlatform.Application.DTOs.Master.CommonRemarkTypeMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class CommonRemarkTypeMasterMappingProfile : Profile
{
    public CommonRemarkTypeMasterMappingProfile()
    {
        CreateMap<CommonRemarkTypeMasterEntity, CommonRemarkTypeMasterDtos>();

        CreateMap<CreateCommonRemarkTypeMasterDto, CommonRemarkTypeMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateCommonRemarkTypeMasterDto, CommonRemarkTypeMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}
