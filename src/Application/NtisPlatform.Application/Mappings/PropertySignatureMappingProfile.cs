using AutoMapper;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Mappings;

public class PropertySignatureMappingProfile : Profile
{
    private const string ReasonKey = "Reason";
    private const string PendingSignAtKey = "PendingSignAt";
    private const string PendingOfficerNameKey = "PendingOfficerName";

    public PropertySignatureMappingProfile()
    {
        CreateMap<int, RejectedPropertyDto>()
            .ForMember(dest => dest.PropertyId, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom((_, _, _, context) =>
                GetContextValue(context, ReasonKey)));

        CreateMap<PropertySignaturePendingExportSourceDto, PropertySignaturePendingExportDto>()
            .ForMember(dest => dest.Zone, opt => opt.MapFrom(src => src.Zone))
            .ForMember(dest => dest.BuildingNo, opt => opt.MapFrom(src => src.BuildingNo))
            .ForMember(dest => dest.SrNoticeNo, opt => opt.MapFrom(src => src.SrNoticeNo))
            .ForMember(dest => dest.PendingSignAt, opt => opt.MapFrom((_, _, _, context) =>
                GetContextValue(context, PendingSignAtKey)))
            .ForMember(dest => dest.PendingOfficerName, opt => opt.MapFrom((_, _, _, context) =>
                GetContextValue(context, PendingOfficerNameKey)));
    }

    private static string GetContextValue(ResolutionContext context, string key)
        => context.Items.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
}
