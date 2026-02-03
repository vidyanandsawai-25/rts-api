using AutoMapper;
using NtisPlatform.Application.DTOs.Master.YearMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings
{
    public class YearMasterMappingProfile : Profile
    {
        public YearMasterMappingProfile()
        {
            CreateMap<YearMasterEntity, YearMasterDto>();

            CreateMap<CreateYearMasterDto, YearMasterEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

            CreateMap<UpdateYearMasterDto, YearMasterEntity>()
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
        }
    }
}
