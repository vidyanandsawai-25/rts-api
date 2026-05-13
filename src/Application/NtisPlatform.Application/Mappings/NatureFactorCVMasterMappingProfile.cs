using AutoMapper;
using NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Mappings
{
    public class NatureFactorCVMasterMappingProfile : Profile
    {
        public NatureFactorCVMasterMappingProfile()
        {
            CreateMap<NatureFactorCVMasterEntity, NatureFactorCVMasterDto>()
                .ForMember(dest => dest.NatureFactorId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ConstructionCode, opt => opt.MapFrom(src => src.ConstructionType != null ? src.ConstructionType.ConstructionCode : null))
                .ForMember(dest => dest.ConstructionDescription, opt => opt.MapFrom(src => src.ConstructionType != null ? src.ConstructionType.Description : null))
                .ForMember(dest => dest.FromYear, opt => opt.MapFrom(src => src.YearRangeCV != null ? src.YearRangeCV.FromYear : (int?)null))
                .ForMember(dest => dest.ToYear, opt => opt.MapFrom(src => src.YearRangeCV != null ? src.YearRangeCV.ToYear : (int?)null));

            CreateMap<CreateNatureFactorCVMasterDto, NatureFactorCVMasterEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.ConstructionType, opt => opt.Ignore())
                .ForMember(dest => dest.YearRangeCV, opt => opt.Ignore());

            CreateMap<UpdateNatureFactorCVMasterDto, NatureFactorCVMasterEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
                .ForMember(dest => dest.ConstructionType, opt => opt.Ignore())
                .ForMember(dest => dest.YearRangeCV, opt => opt.Ignore());
        }
    }

}
