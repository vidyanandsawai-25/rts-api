using AutoMapper;
using NtisPlatform.Application.DTOs.Master.BlockMaster;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Mappings;

public class BlockMasterMappingProfile : Profile
{
    public BlockMasterMappingProfile()
    {
        CreateMap<BlockMasterEntity, BlockMasterDtos>();

        CreateMap<CreateBlockMasterDto, BlockMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<UpdateBlockMasterDto, BlockMasterEntity>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));
    }
}