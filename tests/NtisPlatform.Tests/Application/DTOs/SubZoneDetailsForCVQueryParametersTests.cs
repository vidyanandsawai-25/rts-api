using NtisPlatform.Application.DTOs;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class SubZoneDetailsForCVQueryParametersTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        // Arrange & Act
        var query = new SubZoneDetailsForCVQueryParameters
        {
            MoujaId = 10,
            SubZoneNo = "SZ001",
            SubZoneName = "Zone A"
        };

        // Assert
        Assert.Equal(10, query.MoujaId);
        Assert.Equal("SZ001", query.SubZoneNo);
        Assert.Equal("Zone A", query.SubZoneName);
    }

    [Fact]
    public void MoujaId_CanBeNull()
    {
        // Arrange & Act
        var query = new SubZoneDetailsForCVQueryParameters { MoujaId = null };

        // Assert
        Assert.Null(query.MoujaId);
    }

    [Fact]
    public void SubZoneNo_CanBeNull()
    {
        // Arrange & Act
        var query = new SubZoneDetailsForCVQueryParameters { SubZoneNo = null };

        // Assert
        Assert.Null(query.SubZoneNo);
    }

    [Fact]
    public void SubZoneName_CanBeNull()
    {
        // Arrange & Act
        var query = new SubZoneDetailsForCVQueryParameters { SubZoneName = null };

        // Assert
        Assert.Null(query.SubZoneName);
    }

    [Fact]
    public void DefaultConstructor_InitializesWithNull()
    {
        // Arrange & Act
        var query = new SubZoneDetailsForCVQueryParameters();

        // Assert
        Assert.Null(query.MoujaId);
        Assert.Null(query.SubZoneNo);
        Assert.Null(query.SubZoneName);
    }

    [Fact]
    public void InheritsFromBaseQueryParameters()
    {
        // Arrange & Act
        var query = new SubZoneDetailsForCVQueryParameters();

        // Assert
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(query);
    }
}
