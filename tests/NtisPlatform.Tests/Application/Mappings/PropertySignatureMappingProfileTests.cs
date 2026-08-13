using AutoMapper;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Application.Mappings;

public class PropertySignatureMappingProfileTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<PropertySignatureMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
        return config.CreateMapper();
    }

    [Fact]
    public void PropertySignatureMappingProfile_MapsRejectedPropertyReasonFromContext()
    {
        var mapper = CreateMapper();

        var result = mapper.Map<RejectedPropertyDto>(25, opt => opt.Items["Reason"] = "Rejected reason");

        Assert.Equal(25, result.PropertyId);
        Assert.Equal("Rejected reason", result.Reason);
    }

    [Fact]
    public void PropertySignatureMappingProfile_MapsPendingExportAuthorityContext()
    {
        var mapper = CreateMapper();
        var source = new PropertySignaturePendingExportSourceDto
        {
            Zone = "MM",
            BuildingNo = "MM8-216",
            SrNoticeNo = "NOTICE-1"
        };

        var result = mapper.Map<PropertySignaturePendingExportDto>(source, opt =>
        {
            opt.Items["PendingSignAt"] = "Tax Inspector";
            opt.Items["PendingOfficerName"] = "Officer";
        });

        Assert.Equal("MM", result.Zone);
        Assert.Equal("MM8-216", result.BuildingNo);
        Assert.Equal("NOTICE-1", result.SrNoticeNo);
        Assert.Equal("Tax Inspector", result.PendingSignAt);
        Assert.Equal("Officer", result.PendingOfficerName);
    }
}
