using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Core.Entities.GIS;

namespace NtisPlatform.Application.Mappings;

/// <summary>
/// AutoMapper profile for GisUploadHistory entity and DTOs
/// </summary>
public class GisUploadHistoryMappingProfile : Profile
{
    public GisUploadHistoryMappingProfile()
    {
        CreateMap<GisUploadHistoryEntity, GisUploadHistoryDto>();
        CreateMap<CreateGisUploadHistoryDto, GisUploadHistoryEntity>();
        CreateMap<UpdateGisUploadHistoryDto, GisUploadHistoryEntity>();
    }
}
