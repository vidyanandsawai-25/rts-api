using AutoMapper;
using NtisPlatform.Core.Models.AutomationDashboard;

namespace NtisPlatform.Application.Mappings.AutomationDashboard;

/// <summary>
/// AutoMapper profile for Geo-Sequencing dashboard DTOs
/// </summary>
public class GeoSequencingMappingProfile : Profile
{
    public GeoSequencingMappingProfile()
    {
        // Projection mappings
        CreateMap<GeoSequencingStagePropertyProjection, GeoSequencingZoneDataDto>()
            .ForMember(dest => dest.ZoneName, opt => opt.Ignore())
            .ForMember(dest => dest.ZoneNo, opt => opt.Ignore())
            .ForMember(dest => dest.RegisteredProperties, opt => opt.Ignore())
            .ForMember(dest => dest.GeoSequencedProperties, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTypeBreakdown, opt => opt.Ignore())
            .ForMember(dest => dest.AssessmentStatusBreakdown, opt => opt.Ignore());

        CreateMap<GeoSequencingStagePropertyProjection, GeoSequencingWardDataDto>()
            .ForMember(dest => dest.WardNo, opt => opt.Ignore())
            .ForMember(dest => dest.RegisteredProperties, opt => opt.Ignore())
            .ForMember(dest => dest.GeoSequencedProperties, opt => opt.Ignore())
            .ForMember(dest => dest.PropertyTypeBreakdown, opt => opt.Ignore())
            .ForMember(dest => dest.AssessmentStatusBreakdown, opt => opt.Ignore());
    }
}
