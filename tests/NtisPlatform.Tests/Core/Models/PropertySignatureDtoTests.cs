using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Core.Models;

public class PropertySignatureDtoTests
{
    [Fact]
    public void PropertySignaturePagedResultDto_ComputesPaginationMetadata()
    {
        var result = new PropertySignaturePagedResultDto<int>
        {
            Items = new[] { 4, 5 },
            TotalCount = 11,
            PageNumber = 2,
            PageSize = 5
        };

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPrevious);
        Assert.True(result.HasNext);
    }

    [Fact]
    public void PropertySignaturePendingExportDto_DefaultsToEmptyStrings()
    {
        var dto = new PropertySignaturePendingExportDto();

        Assert.Equal(string.Empty, dto.Zone);
        Assert.Equal(string.Empty, dto.BuildingNo);
        Assert.Equal(string.Empty, dto.SrNoticeNo);
        Assert.Equal(string.Empty, dto.PendingSignAt);
        Assert.Equal(string.Empty, dto.PendingOfficerName);
    }

    [Fact]
    public void PropertySignaturePendingExportSourceDto_DefaultsSignedAuthoritiesToEmptyList()
    {
        var dto = new PropertySignaturePendingExportSourceDto();

        Assert.NotNull(dto.SignedAuthorityIds);
        Assert.Empty(dto.SignedAuthorityIds);
    }

    [Fact]
    public void SignAuthorityZoneDataDto_DefaultsZoneNoToEmptyString()
    {
        var dto = new SignAuthorityZoneDataDto();

        Assert.Equal(string.Empty, dto.ZoneNo);
    }

    [Fact]
    public void SignAuthorityClassificationDto_DefaultsTypeIdToZero()
    {
        var dto = new SignAuthorityClassificationDto();

        Assert.Equal(0, dto.TypeId);
    }
}
