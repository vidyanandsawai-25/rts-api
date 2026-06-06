using NtisPlatform.Application.DTOs;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class TypeOfUseGroupCVQueryParametersTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        // Arrange & Act
        var query = new TypeOfUseGroupCVQueryParameters
        {
            TypeOfUseGroupCVCode = "RES",
            GroupName = "Residential",
            IsFloorWiseRateApplicable = true
        };

        // Assert
        Assert.Equal("RES", query.TypeOfUseGroupCVCode);
        Assert.Equal("Residential", query.GroupName);
        Assert.True(query.IsFloorWiseRateApplicable);
    }

    [Fact]
    public void TypeOfUseGroupCVCode_CanBeNull()
    {
        // Arrange & Act
        var query = new TypeOfUseGroupCVQueryParameters { TypeOfUseGroupCVCode = null };

        // Assert
        Assert.Null(query.TypeOfUseGroupCVCode);
    }

    [Fact]
    public void GroupName_CanBeNull()
    {
        // Arrange & Act
        var query = new TypeOfUseGroupCVQueryParameters { GroupName = null };

        // Assert
        Assert.Null(query.GroupName);
    }

    [Fact]
    public void IsFloorWiseRateApplicable_CanBeNull()
    {
        // Arrange & Act
        var query = new TypeOfUseGroupCVQueryParameters { IsFloorWiseRateApplicable = null };

        // Assert
        Assert.Null(query.IsFloorWiseRateApplicable);
    }

    [Fact]
    public void IsFloorWiseRateApplicable_CanBeFalse()
    {
        // Arrange & Act
        var query = new TypeOfUseGroupCVQueryParameters { IsFloorWiseRateApplicable = false };

        // Assert
        Assert.False(query.IsFloorWiseRateApplicable);
    }

    [Fact]
    public void DefaultConstructor_InitializesWithNull()
    {
        // Arrange & Act
        var query = new TypeOfUseGroupCVQueryParameters();

        // Assert
        Assert.Null(query.TypeOfUseGroupCVCode);
        Assert.Null(query.GroupName);
        Assert.Null(query.IsFloorWiseRateApplicable);
    }

    [Fact]
    public void InheritsFromBaseQueryParameters()
    {
        // Arrange & Act
        var query = new TypeOfUseGroupCVQueryParameters();

        // Assert
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(query);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange & Act
        var query = new TypeOfUseGroupCVQueryParameters
        {
            TypeOfUseGroupCVCode = "COM",
            GroupName = "Commercial",
            IsFloorWiseRateApplicable = false
        };

        // Assert
        Assert.Equal("COM", query.TypeOfUseGroupCVCode);
        Assert.Equal("Commercial", query.GroupName);
        Assert.False(query.IsFloorWiseRateApplicable);
    }
}
