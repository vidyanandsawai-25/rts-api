using AutoMapper;
using NtisPlatform.Application.DTOs.Master.FloorFactorCVMaster;
using NtisPlatform.Core.Entities.Master;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.Mappings
{
    public class FloorFactorCVMasterMappingProfile : Profile
    {
        public FloorFactorCVMasterMappingProfile()
        {
            CreateMap<FloorFactorCVMasterEntity, FloorFactorCVMasterDto>()
                .ForMember(dest => dest.FloorCode, opt => opt.MapFrom(src => src.Floor != null ? src.Floor.FloorCode : null))
                .ForMember(dest => dest.FloorDescription, opt => opt.MapFrom(src => src.Floor != null ? src.Floor.Description : null))
                .ForMember(dest => dest.FromYear, opt => opt.MapFrom(src => src.YearRangeCV != null ? src.YearRangeCV.FromYear : (int?)null))
                .ForMember(dest => dest.ToYear, opt => opt.MapFrom(src => src.YearRangeCV != null ? src.YearRangeCV.ToYear : (int?)null));

            CreateMap<CreateFloorFactorCVMasterDto, FloorFactorCVMasterEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.Floor, opt => opt.Ignore())
                .ForMember(dest => dest.YearRangeCV, opt => opt.Ignore());

            CreateMap<UpdateFloorFactorCVMasterDto, FloorFactorCVMasterEntity>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
                .ForMember(dest => dest.Floor, opt => opt.Ignore())
                .ForMember(dest => dest.YearRangeCV, opt => opt.Ignore());
        }
    }

}
